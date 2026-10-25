// Main-thread snapshots, serialized worker IO and event/timer-driven save controls; no idle frame callback.
using Godot;
using System;
using System.Threading.Tasks;
public partial class CampaignSaveController : Node
{
    private WorldScene _world;
    private CampaignStore _store;
    private string _fingerprint;
    private bool _activate, _ready, _leaving, _previousAutoQuit, _exiting;
    private bool Alive => !_exiting && GodotObject.IsInstanceValid(this) && IsInsideTree();
    private Task<bool> _writing;
    private Timer _autosave;
    private Label _status;
    private AcceptDialog _error;
    private ConfirmationDialog _pause;
    public string LastError { get; private set; } = "";
    public bool ReadyToSave => _ready;
    public bool Busy => _writing != null && !_writing.IsCompleted;

    // Load nearby chunks first; preserve the normal F6 disposable testing route by creating this only for campaigns.
    // =========================================================
    public async void Initialize(WorldScene world, bool isNew, string fingerprint)
    {
        _world = world; _store = CampaignSession.Store(); _fingerprint = fingerprint; _activate = isNew;
        _previousAutoQuit = GetTree().AutoAcceptQuit; GetTree().AutoAcceptQuit = false;
        ProcessMode = ProcessModeEnum.Always; SetProcess(false); SetPhysicsProcess(false);
        BuildControls();
        _autosave = new Timer { Name = "Autosave", WaitTime = float.IsFinite(CampaignSession.Settings().AutosaveSeconds) ? Math.Clamp(CampaignSession.Settings().AutosaveSeconds,1,1800) : 120, ProcessMode = ProcessModeEnum.Pausable };
        AddChild(_autosave); _autosave.Timeout += OnAutosave;
        var player = _world.Player; player.SetPhysicsProcess(false);
        // Physics receives freshly loaded colliders before input is enabled.
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        if (!Alive) return;
        var shape = player.GetNode<CollisionShape2D>("Collision");
        var query = new PhysicsShapeQueryParameters2D { Shape = shape.Shape, Transform = shape.GlobalTransform,
            CollisionMask = player.CollisionMask, Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        if (player.GetWorld2D().DirectSpaceState.IntersectShape(query,1).Count != 0)
        { ShowError("Saved arrival position is blocked. Campaign files were preserved; return to the main menu."); return; }
        _ready = true;
        if (isNew) await SaveAsync();
        if (!Alive) return;
        player.SetPhysicsProcess(player.HealthState.IsAlive);
        if (CampaignSession.Settings().AutosaveSeconds > 0) _autosave.Start();
    }
    // A fixed snapshot is captured synchronously; gameplay may continue while only detached data is written.
    // =========================================================
    public CampaignSnapshot Capture()
    {
        using var trace = RuntimeFrameTrace.Measure("Campaign snapshot");
        var player = _world.Player;
        var clock = _world.Environment.Clock;
        var b = _world.Blueprint;
        return new(CampaignSnapshot.CurrentSchema,b.ProfileId,b.RunId,b.WorldId,_fingerprint,DateTime.UtcNow.ToString("O"),b,
            new SavedPlayer(player.GlobalPosition.X,player.GlobalPosition.Y,Health:player.HealthState.Current,Inventory:_world.Harvesting.Inventory.Capture()),new SavedClock(clock.Hour,clock.Config.CycleSeconds,clock.Config.Paused),
            _world.EntityPlacement.Capture(),_world.Harvesting.MergeDrops(_world.Stream.CaptureChanges()));
    }
    // Only one write runs at a time; repeated requests coalesce, while Leave takes a fresh final snapshot.
    // =========================================================
    public Task<bool> SaveAsync()
    {
        if (!_ready || !Alive) return Task.FromResult(false);
        if (Busy) return _writing;
        return _writing = WriteSnapshot();
    }
    private async Task<bool> WriteSnapshot()
    {
        try
        {
            var snapshot = Capture(); bool activate = _activate;
            _status.Text = "Saving campaign…";
            await Task.Run(() => _store.Save(snapshot,activate));
            _activate = false; LastError = "";
            if (Alive) _status.Text = "Campaign saved  ·  F8 save  ·  Esc menu";
            return true;
        }
        catch (Exception error)
        { LastError = error.Message; if (Alive) ShowError("Save failed: " + error.Message); return false; }
    }
    // Autosave never queues another write while one is active; pausing also pauses this timer.
    // =========================================================
    private async void OnAutosave() { if (!_leaving && !Busy && _ready) await SaveAsync(); }
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo || _leaving) return;
        if (key.Keycode == Key.F8) { GetViewport().SetInputAsHandled(); ManualSave(); }
        if (key.Keycode == Key.Escape) { GetViewport().SetInputAsHandled(); OpenPause(); }
    }
    private async void ManualSave() { if (!_leaving) await SaveAsync(); }
    public override void _Notification(int what)
    { if (what == NotificationWMCloseRequest && !_leaving) Leave(true); }
    // Freeze gameplay for a consistent final snapshot and wait for disk completion before scene switch/quit.
    // =========================================================
    private async void Leave(bool quit)
    {
        if (_leaving) return;
        _leaving = true; _pause.Hide(); _autosave.Stop(); GetTree().Paused = true;
        if (Busy) await _writing;
        if (_ready && !await SaveAsync()) { _leaving = false; return; }
        if (!Alive) return;
        GetTree().Paused = false;
        if (quit) GetTree().Quit(); else MenuNavigation.Open(this,MenuNavigation.Main);
    }
    private void OpenPause()
    { GetTree().Paused = true; _pause.PopupCentered(); }
    // Small confirmation/error dialogs live under one removable UI layer.
    // =========================================================
    private void BuildControls()
    {
        var layer = new CanvasLayer { Name = "SaveControls", Layer = 90 }; AddChild(layer);
        _status = new Label { Text = "F8 save  ·  Esc menu", Position = new Vector2(18,1040), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_status);
        _error = new AcceptDialog { Title = "Campaign save", Exclusive = true }; layer.AddChild(_error);
        _error.Confirmed += () =>
        {
            if (_ready && !_leaving && !_pause.Visible)
            { GetTree().Paused = false; if (CampaignSession.Settings().AutosaveSeconds > 0) _autosave.Start(); }
        };
        _pause = new ConfirmationDialog { Title = "Campaign", DialogText = "Return to the main menu? Your campaign will be saved.", Exclusive = true };
        _pause.GetOkButton().Text = "SAVE & MAIN MENU"; _pause.GetCancelButton().Text = "RESUME";
        _pause.Confirmed += () => Leave(false); _pause.Canceled += () => GetTree().Paused = false;
        _pause.AddButton("SAVE & QUIT",true,"quit"); _pause.CustomAction += action => { if (action == "quit") Leave(true); };
        layer.AddChild(_pause);
    }
    private void ShowError(string message) { _status.Text = message; _error.DialogText = message; _error.PopupCentered(); }
    // External scene teardown cannot redirect a worker: every write already captured its own immutable IDs.
    // =========================================================
    public override void _ExitTree()
    { _exiting = true; GetTree().AutoAcceptQuit = _previousAutoQuit; }
}
