// Owns the removable snow comparison; all children/material adaptations are temporary runtime helpers.
using Godot;

public partial class DebugSnowTest : Node2D
{
    [Export] public SnowMasterConfig Config { get; set; }
    private readonly TreeSnowAdapter _trees = new();
    private readonly RockSnowAdapter _rocks = new();
    private ShaderMaterial _ground;
    private MeshInstance2D _cover;
    private HSlider _slider;
    private Label _label;
    private float _coverage;
    private bool _syncing;

    #region Setup
    // This last scene sibling starts after scenery and the day/night tree material cache are ready.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new SnowMasterConfig();
        var weather = GetNode<DebugWeather>("../Weather");
        Texture2D noise = GD.Load<Texture2D>("res://DEBUG/TopDownTest/Ground/Noise/GroundNoise.tres");
        _trees.Attach(GetNode<Node2D>("../Actors"), noise);
        Node2D grass = GetNodeOrNull<Node2D>("../GrassPatches");
        if (grass != null) _trees.Attach(grass, noise, true);
        _rocks.Attach(GetNode<Node2D>("../Actors"), noise);
        _ground = new ShaderMaterial { Shader = GD.Load<Shader>("res://DEBUG/TopDownTest/SnowTest/GroundSnow.gdshader") };
        _ground.SetShaderParameter("snow_noise", GD.Load<Texture2D>("res://DEBUG/TopDownTest/SnowTest/SnowCoverageNoise.tres"));
        _ground.SetShaderParameter("snow_detail", noise);
        Rect2 world = GetNode<DebugWorldScatter>("../Actors").Config.WorldBounds;
        _cover = new MeshInstance2D { Name = "GroundCover", Mesh = new QuadMesh { Size = world.Size }, Position = world.GetCenter(), ZIndex = -7, Material = _ground };
        AddChild(_cover);
        AddChild(new DebugSnowfall { Name = "Snowfall", Snow = this, Weather = weather });
        PlayerSurfaceVisual.Ensure(GetNode<TestPlayerMovement>("../Actors/Player")).Snow = this;
        BuildControls();
    }

    // Off is the initial state; the test can be enabled without changing existing weather settings.
    // =========================================================
    private void BuildControls()
    {
        var ui = new CanvasLayer { Name = "UI", Layer = 20 }; AddChild(ui);
        var panel = new PanelContainer { OffsetLeft = 16, OffsetTop = 745, OffsetRight = 336, OffsetBottom = 1010 };
        ui.AddChild(panel);
        var controls = new VBoxContainer(); panel.AddChild(controls);
        controls.AddChild(new Label { Text = "Shader Wind and Snow" });
        AddSwitch(controls, "Snow test (off restores scene)", Config.Enabled, value => Config.Enabled = value);
        _label = new Label(); controls.AddChild(_label);
        _slider = AddSlider(controls, Config.Coverage);
        _slider.ValueChanged += value => { if (!_syncing) { Config.Coverage = (float)value; _coverage = Config.Enabled ? (float)value : 0f; } };
        AddSwitch(controls, "Falling snow", Config.Snowfall, value => Config.Snowfall = value);
        controls.AddChild(new Label { Text = "Snowfall intensity" });
        HSlider rate = AddSlider(controls, Config.SnowfallRate);
        rate.ValueChanged += value => Config.SnowfallRate = (float)value;
        AddSwitch(controls, "Auto accumulate / melt", Config.AutoAccumulate, value => Config.AutoAccumulate = value);
    }

    // Controls leave keyboard focus free for player movement.
    // =========================================================
    private static HSlider AddSlider(VBoxContainer parent, double value)
    {
        var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = value, FocusMode = Control.FocusModeEnum.None };
        parent.AddChild(slider); return slider;
    }

    // Each snow feature remains independently comparable.
    // =========================================================
    private static void AddSwitch(VBoxContainer parent, string text, bool value, System.Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = value, FocusMode = Control.FocusModeEnum.None };
        toggle.Toggled += enabled => changed(enabled); parent.AddChild(toggle);
    }
    #endregion

    #region Simulation and cleanup
    // Manual sliders scrub immediately; automatic accumulation and Inspector changes blend gradually.
    // =========================================================
    public override void _Process(double delta)
    {
        if (Config.Enabled && Config.AutoAccumulate)
            Config.Coverage = Mathf.MoveToward(Config.Coverage, Config.Snowfall && Config.SnowfallRate > 0 ? 1f : 0f, (float)delta / Mathf.Max(5f, Config.AccumulationSeconds));
        _coverage = Config.Enabled ? Mathf.MoveToward(_coverage, Mathf.Clamp(Config.Coverage, 0f, 1f), (float)delta / Mathf.Max(0.1f, Config.TransitionSeconds)) : 0f;
        _cover.Visible = _coverage > 0f;
        _ground.SetShaderParameter("snow_coverage", _coverage);
        _ground.SetShaderParameter("snow_colour", Config.SnowColour);
        _ground.SetShaderParameter("drift_scale", Config.DriftScale);
        _ground.SetShaderParameter("drift_contrast", Config.DriftContrast);
        // Every snow-compatible surface uses the same settled coverage; no per-kind on/off gates.
        _trees.Update(_coverage, Config.SnowColour);
        _rocks.Update(_coverage, Config.SnowColour, Config.RockCapDepth);
        _syncing = true; _slider.Value = Config.Coverage; _syncing = false;
        _label.Text = $"Settled snow: {_coverage:P0}";
    }

    // The same smooth depth field as SnowDepth.gdshaderinc, evaluated only for the player.
    // Coverage gates burial so thin scattered deposits never swallow the player's feet.
    // =========================================================
    public float PlayerDepthAt(Vector2 world)
    {
        if (!Config.Enabled || !Config.PlayerSinks || _coverage <= 0f) return 0f;
        float x = world.X / Mathf.Max(60f, Config.DriftScale), y = world.Y / Mathf.Max(60f, Config.DriftScale);
        float depth = Mathf.Clamp(0.64f + 0.20f * Mathf.Sin(x + 0.42f * Mathf.Sin(y * 0.83f))
            + 0.16f * Mathf.Sin(y * 1.31f + x * 0.37f), 0f, 1f);
        return Mathf.Max(0f, Config.MaximumSinkPixels) * depth * Mathf.SmoothStep(0f, 1f, Mathf.Clamp((_coverage - 0.25f) / 0.75f, 0f, 1f));
    }

    // Removing SnowTest restores original rock and vegetation sprite materials.
    // =========================================================
    public override void _ExitTree()
    {
        _rocks.Restore();
        _trees.Restore();
    }
    #endregion
}
