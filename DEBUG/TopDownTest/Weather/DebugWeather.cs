// Coordinates one wind source, camera-centred GPU rain and optional overcast in the debug scene.
using Godot;

public partial class DebugWeather : Node2D
{
    [Export] public WeatherMasterConfig Config { get; set; }
    public float WindStrength => Mathf.Clamp(Config.WindStrength, -3f, 3f);
    public Vector2 Velocity => Config.DriftPerStrength * WindStrength;
    public Vector2 Displacement { get; private set; }
    public float Rain { get; private set; }
    public float CloudCover => Config.Overcast ? Rain : 0f;
    public DebugLightning Lightning { get; private set; }
    private GpuParticles2D _rain;
    private ParticleProcessMaterial _particles;
    private DebugRainSplashes _splashes;
    private Label _windLabel, _rainLabel;
    private HSlider _windSlider, _rainSlider;
    private bool _syncing;

    #region Setup
    // Weather runs before its consumers, independently of the accelerated day/night clock.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WeatherMasterConfig();
        ProcessPriority = -10;
        _particles = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Gravity = Vector3.Zero, Direction = Vector3.Down, Spread = 3f,
            InitialVelocityMin = Config.FallSpeed * 0.85f, InitialVelocityMax = Config.FallSpeed,
            ScaleMin = 0.7f, ScaleMax = 1.25f,
            Color = new Color(0.70f, 0.80f, 0.87f, 0.30f)
        };
        _particles.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        _particles.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.AlignYToVelocity, true);
        // A narrow vertical texture aligns to particle velocity; no external artwork is required.
        var image = Image.CreateEmpty(3, 20, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        for (int y = 0; y < 20; y++)
            image.SetPixel(1, y, new Color(1, 1, 1, Mathf.Sin((y + 0.5f) / 20f * Mathf.Pi)));
        _rain = new GpuParticles2D
        {
            Name = "Rain", Amount = Mathf.Clamp(Config.MaxDrops, 100, 4000), Lifetime = 0.65,
            Preprocess = 0.65, LocalCoords = false, Emitting = false, ZIndex = 6,
            ProcessMaterial = _particles, Texture = ImageTexture.CreateFromImage(image),
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        AddChild(_rain);
        _splashes = new DebugRainSplashes { Name = "GroundSplashes", ZIndex = -4, Weather = this };
        AddChild(_splashes);
        Lightning = new DebugLightning { Name = "Lightning", Weather = this, ZIndex = 7 };
        AddChild(Lightning);
        BuildControls();
    }

    // Keep comparison controls beside the existing atmosphere panel, outside colour grading.
    // =========================================================
    private void BuildControls()
    {
        var layer = new CanvasLayer { Name = "UI", Layer = 20 };
        AddChild(layer);
        var panel = new PanelContainer
        {
            AnchorLeft = 1f, AnchorRight = 1f, OffsetLeft = -296, OffsetRight = -16,
            OffsetTop = 264, OffsetBottom = 500
        };
        layer.AddChild(panel);
        var controls = new VBoxContainer();
        panel.AddChild(controls);
        controls.AddChild(new Label { Text = "Debug rain / shared wind" });
        _rainLabel = new Label(); controls.AddChild(_rainLabel);
        _rainSlider = AddSlider(controls, 0, 1, 0.01, Config.RainIntensity);
        _rainSlider.ValueChanged += value => { if (!_syncing) Config.RainIntensity = (float)value; };
        _windLabel = new Label(); controls.AddChild(_windLabel);
        _windSlider = AddSlider(controls, -3, 3, 0.05, Config.WindStrength);
        _windSlider.ValueChanged += value => { if (!_syncing) Config.WindStrength = (float)value; };
        var splashes = new CheckButton { Text = "Ground splashes", ButtonPressed = Config.Splashes };
        splashes.Toggled += value => Config.Splashes = value; controls.AddChild(splashes);
        var overcast = new CheckButton { Text = "Rain overcast lighting", ButtonPressed = Config.Overcast };
        overcast.Toggled += value => Config.Overcast = value; controls.AddChild(overcast);
        var lightning = new CheckButton { Text = "Storm lightning", ButtonPressed = Config.Lightning, FocusMode = Control.FocusModeEnum.None };
        lightning.Toggled += value => Config.Lightning = value; controls.AddChild(lightning);
        var nearby = new Button { Text = "Test nearby flash", FocusMode = Control.FocusModeEnum.None };
        nearby.Pressed += () => Lightning.Trigger(false); controls.AddChild(nearby);
        var strike = new Button { Text = "Test visible strike", FocusMode = Control.FocusModeEnum.None };
        strike.Pressed += () => Lightning.Trigger(true); controls.AddChild(strike);
    }

    // Prevent sliders from retaining keyboard focus and consuming movement arrow keys.
    // =========================================================
    private static HSlider AddSlider(VBoxContainer parent, double min, double max, double step, double value)
    {
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step, Value = value, FocusMode = Control.FocusModeEnum.None };
        parent.AddChild(slider);
        return slider;
    }
    #endregion

    #region Simulation
    // Integrating displacement avoids fog/cloud jumps when the wind slider changes.
    // =========================================================
    public override void _Process(double delta)
    {
        Rain = Mathf.MoveToward(Rain, Mathf.Clamp(Config.RainIntensity, 0f, 1f), (float)delta / Mathf.Max(0.1f, Config.TransitionSeconds));
        Displacement += Velocity * (float)delta;
        Rect2 view = VisibleWorldRect();
        // Cover the entire view plus particle travel and fast camera movement in every direction.
        float margin = 160f + new Vector2(Config.SlantPerStrength * 3f, Config.FallSpeed).Length() * (float)_rain.Lifetime;
        Vector2 half = view.Size / 2f + Vector2.One * margin;
        _rain.GlobalPosition = view.GetCenter();
        _particles.EmissionBoxExtents = new Vector3(half.X, half.Y, 1f);
        _rain.VisibilityRect = new Rect2(-half * 2f, half * 4f);
        Vector2 velocity = new(Config.SlantPerStrength * WindStrength, Mathf.Max(100f, Config.FallSpeed));
        _particles.Direction = new Vector3(velocity.X, velocity.Y, 0).Normalized();
        _particles.InitialVelocityMin = velocity.Length() * 0.85f;
        _particles.InitialVelocityMax = velocity.Length();
        _rain.AmountRatio = Mathf.Max(0.001f, Rain);
        _rain.Emitting = Rain > 0.001f;
        _syncing = true;
        _windSlider.Value = WindStrength; _rainSlider.Value = Config.RainIntensity;
        _syncing = false;
        _windLabel.Text = $"Wind: {WindStrength:0.00}  (left ← 0 → right)";
        _rainLabel.Text = $"Rain: {Rain:P0}";
    }

    // The canvas transform includes camera zoom and clamping, unlike the player's position.
    // =========================================================
    public Rect2 VisibleWorldRect()
    {
        Transform2D inverse = GetViewport().GetCanvasTransform().AffineInverse();
        Vector2 start = inverse * Vector2.Zero;
        Vector2 end = inverse * GetViewportRect().Size;
        return new Rect2(start, end - start).Abs();
    }
    #endregion
}
