// Production camera-centred GPU rain and overcast, consuming the one independent wind source.
using Godot;

public partial class WorldWeather : Node2D
{
    [Export] public WeatherProfile Config { get; set; }
    public WindController Wind { get; set; }
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    public float WindStrength => Wind.Strength;
    public float Rain { get; private set; }
    public float CloudCover => Config.Overcast ? Rain : 0f;
    public WorldLightning Lightning { get; private set; }
    private GpuParticles2D _rain;
    private ParticleProcessMaterial _particles;
    private GroundRainSplashes _splashes;
    #region Setup
    // Weather runs before its consumers, independently of the accelerated day/night clock.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WeatherProfile();
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
        _splashes = new GroundRainSplashes { Name = "GroundSplashes", ZIndex = -4, Weather = this, Actors = Actors, WorldBounds = WorldBounds };
        AddChild(_splashes);
        Lightning = new WorldLightning { Name = "Lightning", Weather = this, ZIndex = 7 };
        AddChild(Lightning);
    }

    #endregion

    #region Simulation
    // Integrating displacement avoids fog/cloud jumps when the wind slider changes.
    // =========================================================
    public override void _Process(double delta)
    {
        Rain = Mathf.MoveToward(Rain, Mathf.Clamp(Config.RainIntensity, 0f, 1f), (float)delta / Mathf.Max(0.1f, Config.TransitionSeconds));
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
