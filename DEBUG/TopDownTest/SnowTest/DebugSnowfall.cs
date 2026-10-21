// Camera-local coverage with world-space GPU flakes, driven by the existing shared wind.
using Godot;

public partial class DebugSnowfall : GpuParticles2D
{
    public DebugSnowTest Snow { get; set; }
    public DebugWeather Weather { get; set; }
    private ParticleProcessMaterial _motion;

    // Create a tiny soft flake texture once; random size and speed avoid uniform rain-like lines.
    // =========================================================
    public override void _Ready()
    {
        _motion = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Direction = new Vector3(0, 1, 0), Gravity = Vector3.Zero, Spread = 18,
            InitialVelocityMin = 24, InitialVelocityMax = 48, ScaleMin = 0.45f, ScaleMax = 1.2f,
            Color = new Color(0.90f, 0.95f, 1f, 0.8f)
        };
        _motion.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        var image = Image.CreateEmpty(7, 7, false, Image.Format.Rgba8);
        for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++)
            image.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp(1f - new Vector2(x - 3, y - 3).Length() / 3.2f, 0f, 1f)));
        Texture = ImageTexture.CreateFromImage(image);
        ProcessMaterial = _motion;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Amount = 1300; Lifetime = 4; Preprocess = 4; LocalCoords = false; Emitting = false;
        ZIndex = 6;
    }

    // Broad margins cover flake travel while the player moves; disabling hides existing flakes too.
    // =========================================================
    public override void _Process(double delta)
    {
        Visible = Snow.Config.Enabled && Snow.Config.Snowfall;
        Emitting = Visible && Snow.Config.SnowfallRate > 0f;
        AmountRatio = Mathf.Clamp(Snow.Config.SnowfallRate, 0.001f, 1f);
        Rect2 view = Weather.VisibleWorldRect();
        Vector2 half = view.Size / 2f + new Vector2(600, 400);
        GlobalPosition = view.GetCenter();
        _motion.EmissionBoxExtents = new Vector3(half.X, half.Y, 1);
        VisibilityRect = new Rect2(-half * 2f, half * 4f);
        Vector2 velocity = new(Weather.WindStrength * 32f, 42f);
        _motion.Direction = new Vector3(velocity.X, velocity.Y, 0).Normalized();
        _motion.InitialVelocityMin = velocity.Length() * 0.55f;
        _motion.InitialVelocityMax = velocity.Length();
    }
}
