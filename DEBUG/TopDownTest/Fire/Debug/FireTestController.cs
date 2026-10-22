// Manual FIRE1 controls and targeted shadow synchronization; does not spawn trees or drive lightning.
using Godot;
using System.Collections.Generic;

public partial class FireTestController : Node
{
    [Export] public NodePath ActorsPath { get; set; } = new("../Actors");
    [Export] public FireMasterConfig Config { get; set; }
    private readonly List<BurnableTree> _oaks = new();
    private readonly Dictionary<BurnableTree, BurnableTree.AppearanceChangedEventHandler> _appearanceHandlers = new();
    private Node2D _player;
    private DebugDayNight _day;
    private Label _status;
    private BurnableTree _selected;

    #region Setup and input
    // Run after generated Actors, DayNight and SnowTest; bind appearance events once per oak.
    // =========================================================
    public override void _Ready()
    {
        Node2D actors = GetNode<Node2D>(ActorsPath);
        _player = actors.GetNode<Node2D>("Player");
        _day = actors.GetParent().GetNodeOrNull<DebugDayNight>("DayNight");
        foreach (Node node in GetTree().GetNodesInGroup("debug_burnable_trees"))
        {
            if (node is not BurnableTree oak || !actors.IsAncestorOf(oak)) continue;
            _oaks.Add(oak);
            BurnableTree.AppearanceChangedEventHandler refresh = () => { if (GodotObject.IsInstanceValid(_day)) _day.RefreshShadowAppearance((Node2D)oak.GetParent()); };
            _appearanceHandlers.Add(oak, refresh); oak.AppearanceChanged += refresh;
            oak.BurnCompleted += RefreshStatus; oak.BurnReset += RefreshStatus;
        }
        var ui = new CanvasLayer { Name = "UI", Layer = 20 }; AddChild(ui);
        var panel = new PanelContainer { OffsetLeft = 720, OffsetTop = 16, OffsetRight = 1200, OffsetBottom = 165 }; ui.AddChild(panel);
        var controls = new VBoxContainer(); panel.AddChild(controls);
        controls.AddChild(new Label { Text = "FIRE1 — oak art transition (no flames yet)" });
        var ignite = new Button { Text = "Ignite nearest oak", FocusMode = Control.FocusModeEnum.None }; controls.AddChild(ignite);
        ignite.Pressed += IgniteNearest;
        var reset = new Button { Text = "Reset all debug oaks", FocusMode = Control.FocusModeEnum.None }; controls.AddChild(reset);
        reset.Pressed += ResetAll;
        _status = new Label { Text = "Walk near an oak, then ignite." }; controls.AddChild(_status);
        SetProcess(false);
    }

    // Closest healthy oak inside the radius, with stable actor-name tie breaking; never teleport/spawn.
    // =========================================================
    public void IgniteNearest()
    {
        BurnableTree nearest = null; float best = Mathf.Pow(Mathf.Max(0f, Config.NearbyRadius), 2f);
        foreach (BurnableTree oak in _oaks)
        {
            if (!GodotObject.IsInstanceValid(oak) || oak.State != FireTreeState.Healthy || !oak.Config.Enabled) continue;
            float distance = _player.GlobalPosition.DistanceSquaredTo(((Node2D)oak.GetParent()).GlobalPosition);
            if (distance > best) continue;
            if (distance == best && nearest != null && string.CompareOrdinal(oak.GetParent().Name, nearest.GetParent().Name) >= 0) continue;
            nearest = oak; best = distance;
        }
        if (nearest == null) { _status.Text = "No healthy oak in range."; return; }
        _selected = nearest;
        _selected.TryIgnite(((Node2D)nearest.GetParent()).GlobalPosition + new Vector2(0, -80), "ManualDebug");
        SetProcess(true); RefreshStatus();
    }

    // Explicit global debug reset, not a gameplay extinguish/repair action.
    // =========================================================
    public void ResetAll()
    {
        foreach (BurnableTree oak in _oaks) if (GodotObject.IsInstanceValid(oak)) oak.ResetBurn();
        _selected = null; _status.Text = "All debug oaks reset."; SetProcess(false);
    }
    #endregion

    // Only the selected burning tree needs a live progress label.
    // =========================================================
    public override void _Process(double delta) => RefreshStatus();
    // Terminal state events update the label once and stop the progress processor.
    // =========================================================
    private void RefreshStatus()
    {
        if (!GodotObject.IsInstanceValid(_selected) || !GodotObject.IsInstanceValid(_status)) return;
        _status.Text = $"{_selected.GetParent().Name}: {_selected.State} {_selected.BurnProgress:P0}";
        if (_selected.State != FireTreeState.Burning) SetProcess(false);
    }
    // Detach C# handlers when controls are removed, leaving no retained UI delegates.
    // =========================================================
    public override void _ExitTree()
    {
        foreach (var pair in _appearanceHandlers)
        {
            if (!GodotObject.IsInstanceValid(pair.Key)) continue;
            pair.Key.AppearanceChanged -= pair.Value;
            pair.Key.BurnCompleted -= RefreshStatus; pair.Key.BurnReset -= RefreshStatus;
        }
        _appearanceHandlers.Clear();
    }
}
