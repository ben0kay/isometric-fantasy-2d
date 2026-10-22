// Comparison UI and shared weather inputs for a reusable, removable debug liquid region.
using Godot;

public partial class LiquidTestController : Node2D
{
    [Export] public LiquidMasterConfig Config { get; set; }
    private LiquidRegion _region;
    private DebugWeather _weather;

    // Put the compact comparison panel below existing weather controls on the right.
    // =========================================================
    public override void _Ready()
    {
        ProcessPriority = 1;
        _region = GetNode<LiquidRegion>("Lake");
        _weather = GetNode<DebugWeather>("../Weather");
        AddChild(new LiquidWading { Name = "Wading", Region = _region });
        var ui = new CanvasLayer { Name = "UI", Layer = 20 }; AddChild(ui);
        var panel = new PanelContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -296, OffsetRight = -16, OffsetTop = 600, OffsetBottom = 875 };
        ui.AddChild(panel);
        var controls = new VBoxContainer(); panel.AddChild(controls);
        controls.AddChild(new Label { Text = "Liquid lake — southeast ↓→" });
        AddSwitch(controls, "Show lake / wading", Config.Enabled, value => Config.Enabled = value);
        AddSwitch(controls, "Surface animation", Config.Animate, value => Config.Animate = value);
        AddSwitch(controls, "Organic shoreline", Config.Shoreline, value => Config.Shoreline = value);
        controls.AddChild(new Label { Text = "Surface ripples" });
        var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = Config.RippleStrength, FocusMode = Control.FocusModeEnum.None };
        slider.ValueChanged += value => Config.RippleStrength = (float)value; controls.AddChild(slider);
        var bank = new Button { Text = "Go to lake bank", FocusMode = Control.FocusModeEnum.None };
        bank.Pressed += () =>
        {
            var player = GetNode<Node2D>("../Actors/Player");
            player.GlobalPosition = _region.ToGlobal(_region.Boundary[48]) + new Vector2(-45, 0);
            player.ResetPhysicsInterpolation();
        };
        controls.AddChild(bank);
    }

    // Configuration switches remain independent from snow/rain and retain arrow-key movement.
    // =========================================================
    private static void AddSwitch(VBoxContainer parent, string text, bool value, System.Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = value, FocusMode = Control.FocusModeEnum.None };
        toggle.Toggled += enabled => changed(enabled); parent.AddChild(toggle);
    }

    // Scene nodes already exist even though Weather.Ready runs later; processing starts afterwards.
    // =========================================================
    public override void _Process(double delta) => _region.UpdateSurface(delta, _weather.WindStrength);
}
