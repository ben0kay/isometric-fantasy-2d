// Existing fantasy menu extended with verified Continue and explicit new-run replacement confirmation.
using Godot;
using System;
using System.Threading.Tasks;
public partial class MainMenu : Control
{
    private Label _status;
    private Button _continue, _new, _profiles;
    private CampaignLoad _loaded;
    private bool _busy, _exiting;
    private bool Alive => !_exiting && GodotObject.IsInstanceValid(this) && IsInsideTree();
    // Read/verify saves on a worker; generation content compatibility is checked on the main thread.
    // =========================================================
    public override async void _Ready()
    {
        GetTree().Paused = false; Input.MouseMode = Input.MouseModeEnum.Visible; CampaignSession.Clear();
        if (ProfileStore.Selected == null) { CallDeferred(MethodName.ReturnToBoot); return; }
        MenuScreenStyle.AddBackground(this);
        VBoxContainer menu = MenuScreenStyle.CreatePanel(this,"THE UNTAMED LANDS","SURVIVE  ·  EXPLORE  ·  ENDURE");
        menu.AddChild(new Label { Text = "Traveller: " + ProfileStore.Selected.Name, HorizontalAlignment = HorizontalAlignment.Center });
        _continue = UIButtonFactory.Create("CONTINUE",ContinueCampaign,UIButtonFactory.Variant.Secondary); _continue.Disabled = true; menu.AddChild(_continue);
        _new = UIButtonFactory.Create("NEW CAMPAIGN",NewCampaign); menu.AddChild(_new);
        _profiles = UIButtonFactory.Create("CHANGE PROFILE",() => MenuNavigation.Open(this,MenuNavigation.Boot),UIButtonFactory.Variant.Secondary); menu.AddChild(_profiles);
        var options = UIButtonFactory.Create("OPTIONS  (COMING LATER)",null,UIButtonFactory.Variant.Secondary); options.Disabled = true; menu.AddChild(options);
        menu.AddChild(UIButtonFactory.Create("EXIT GAME",() => GetTree().Quit(),UIButtonFactory.Variant.Danger));
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; menu.AddChild(_status);
        await RefreshSave();
    }
    private void ReturnToBoot() => MenuNavigation.Open(this,MenuNavigation.Boot);
    // Selection remains locked during IO so a completed load cannot be applied to a different profile.
    // =========================================================
    private async Task RefreshSave()
    {
        _busy = true; _new.Disabled = true; _profiles.Disabled = true;
        string profile = ProfileStore.Selected.Id;
        try
        {
            var store = CampaignSession.Store();
            if (!store.HasCampaign(profile)) { _status.Text = "No saved campaign yet."; return; }
            _status.Text = "Checking saved campaign…";
            var loaded = await Task.Run(() => store.Load(profile));
            if (!Alive) return;
            CampaignSession.RequireContent(loaded.Snapshot); _loaded = loaded; _continue.Disabled = false;
            _status.Text = loaded.Recovered ? "Recovered the last valid backup. Continue will resume that checkpoint." : "Your campaign is ready to continue.";
        }
        catch (Exception error) { if (Alive) _status.Text = error.Message; }
        finally { if (Alive) { _busy = false; _new.Disabled = false; _profiles.Disabled = false; } }
    }
    private void ContinueCampaign()
    {
        if (_busy || _loaded == null) return;
        CampaignSession.Continue(_loaded.Snapshot); MenuNavigation.Open(this,MenuNavigation.World);
    }
    // Previous runs are archived on disk; only explicit confirmation changes the active-run selection.
    // =========================================================
    private void NewCampaign()
    {
        if (_busy) return;
        if (!CampaignSession.Store().HasCampaign(ProfileStore.Selected.Id)) { LaunchNew(); return; }
        var confirm = new ConfirmationDialog { Exclusive = true, Title = "Start a new campaign?",
            DialogText = "A campaign already exists. Start a new private world and make it the active campaign? The previous run's files will be retained." };
        AddChild(confirm); confirm.Confirmed += LaunchNew; confirm.Canceled += () => confirm.QueueFree(); confirm.PopupCentered();
    }
    public override void _ExitTree() => _exiting = true;
    private void LaunchNew()
    { CampaignSession.New(ProfileStore.Selected.Id); MenuNavigation.Open(this,MenuNavigation.World); }
}
