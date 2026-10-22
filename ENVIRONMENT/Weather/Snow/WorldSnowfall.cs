// Camera-local coverage with world-space GPU flakes, driven by the existing shared wind.
using Godot;

public partial class WorldSnowfall : GpuParticles2D
{
    public WorldSnow Snow { get; set; }
    public WorldWeather Weather { get; set; }
    private ParticleProcessMaterial _motion;

    // Generate distinct pixel flake shapes once; random atlas frame, size and speed add variation.
    // =========================================================
    public override void _Ready()
    {
        _motion = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Direction = new Vector3(0, 1, 0), Gravity = Vector3.Zero, Spread = 18,
            InitialVelocityMin = 24, InitialVelocityMax = 48, ScaleMin = 0.45f, ScaleMax = 1.0f,
            AnimOffsetMin = 0f, AnimOffsetMax = 0.999f,
            Color = new Color(0.97f, 0.985f, 1f, 0.88f)
        };
        _motion.SetParticleFlag(ParticleProcessMaterial.ParticleFlags.DisableZ, true);
        // Four crisp atlas frames: small cross, diamond, sparse speck and branched crystal.
        var image = Image.CreateEmpty(20, 5, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        string[][] shapes = {
            new[] { ".....", "..#..", ".###.", "..#..", "....." },
            new[] { "..#..", ".###.", "#####", ".###.", "..#.." },
            new[] { ".....", ".....", "..##.", "..#..", "....." },
            new[] { "#.#.#", ".###.", "#####", ".###.", "#.#.#" }
        };
        for (int frame = 0; frame < 4; frame++)
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                if (shapes[frame][y][x] == '#') image.SetPixel(frame * 5 + x, y, Colors.White);
        Texture = ImageTexture.CreateFromImage(image);
        ProcessMaterial = _motion;
        Material = new CanvasItemMaterial
        {
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
            ParticlesAnimation = true, ParticlesAnimHFrames = 4, ParticlesAnimVFrames = 1, ParticlesAnimLoop = false
        };
        TextureFilter = TextureFilterEnum.Nearest;
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
