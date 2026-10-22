// Owns production precipitation/coverage; all children/material adaptations are temporary runtime helpers.
using Godot;

public partial class WorldSnow : Node2D
{
    [Export] public SnowProfile Config { get; set; }
    private readonly VegetationSnowAdapter _trees = new();
    private readonly WorldRockSnowAdapter _rocks = new();
    private ShaderMaterial _ground;
    private MeshInstance2D _cover;
    public WorldWeather Weather { get; set; }
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    public float Coverage => _coverage;
    private float _coverage;

    #region Setup
    // This last scene sibling starts after scenery and the day/night tree material cache are ready.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new SnowProfile();
        var weather = Weather;
        Texture2D noise = GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres");
        _trees.Attach(Actors, noise);
        _rocks.Attach(Actors, noise);
        _ground = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Terrain/Ground/Surfaces/Snow/GroundSnow.gdshader") };
        _ground.SetShaderParameter("snow_noise", GD.Load<Texture2D>("res://ENVIRONMENT/Weather/Snow/SnowCoverageNoise.tres"));
        _ground.SetShaderParameter("snow_detail", noise);
        Rect2 world = WorldBounds;
        _cover = new MeshInstance2D { Name = "GroundCover", Mesh = new QuadMesh { Size = world.Size }, Position = world.GetCenter(), ZIndex = -7, Material = _ground };
        AddChild(_cover);
        AddChild(new WorldSnowfall { Name = "Snowfall", Snow = this, Weather = weather });
        PlayerSnowVisual.Ensure(Actors.GetNode<PlayerMovement>("Player")).Snow = this;
    }

    // Scrub directly from tooling while preserving the target for later enable/disable comparisons.
    // =========================================================
    public void SetCoverage(float value)
    {
        Config.Coverage = Mathf.Clamp(value, 0f, 1f); _coverage = Config.Enabled ? Config.Coverage : 0f;
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

    // Removing this snow component restores original rock and vegetation sprite materials.
    // =========================================================
    public override void _ExitTree()
    {
        _rocks.Restore();
        _trees.Restore();
    }
    #endregion
}
