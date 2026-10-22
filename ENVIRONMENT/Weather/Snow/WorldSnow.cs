// Owns production precipitation/coverage; all children/material adaptations are temporary runtime helpers.
using Godot;

public partial class WorldSnow : Node2D
{
    [Export] public SnowProfile Config { get; set; }
    private readonly VegetationSnowAdapter _trees = new();
    private readonly WorldRockSnowAdapter _rocks = new();
    private readonly System.Collections.Generic.Dictionary<Node2D,(VegetationSnowAdapter Trees, WorldRockSnowAdapter Rocks)> _chunks = new();
    private readonly System.Collections.Generic.Dictionary<Shader, Shader> _vegetationShaders = new();
    private readonly System.Collections.Generic.HashSet<Node2D> _inactiveChunks = new();
    private ShaderMaterial _ground;
    private MeshInstance2D _cover;
    public WorldWeather Weather { get; set; }
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    public float Coverage => _coverage;
    private float _coverage, _lastCoverage = float.NaN;
    private Color _lastColour;
    private float _lastCap, _lastScale, _lastContrast;
    private bool _applying;
    private WorldSnowfall _snowfall;
    public int AppliedUpdates { get; private set; }

    #region Setup
    // This last scene sibling starts after scenery and the day/night tree material cache are ready.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new SnowProfile();
        Texture2D noise = GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres");
        _trees.Attach(Actors, noise, _vegetationShaders, Weather);
        _rocks.Attach(Actors, noise);
        _ground = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Terrain/Ground/Surfaces/Snow/GroundSnow.gdshader") };
        _ground.SetShaderParameter("snow_noise", GD.Load<Texture2D>("res://ENVIRONMENT/Weather/Snow/SnowCoverageNoise.tres"));
        _ground.SetShaderParameter("snow_detail", noise);
        Rect2 world = WorldBounds;
        _cover = new MeshInstance2D { Name = "GroundCover", Mesh = new QuadMesh { Size = world.Size }, Position = world.GetCenter(), ZIndex = -7, Material = _ground };
        AddChild(_cover);
        Config.Changed += RefreshState;
        RefreshState();
        PlayerSnowVisual.Ensure(Actors.GetNode<PlayerMovement>("Player")).Snow = this;
    }

    // Each loaded chunk owns its material adapters; unload restores before actor nodes are released.
    // =========================================================
    public void RegisterChunk(Node2D chunk)
    {
        var trees = new VegetationSnowAdapter(); var rocks = new WorldRockSnowAdapter();
        var noise = GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres");
        trees.Attach(chunk,noise,_vegetationShaders,Weather); rocks.Attach(chunk,noise); _chunks.Add(chunk,(trees,rocks));
        trees.Update(_coverage,Config.SnowColour); rocks.Update(_coverage,Config.SnowColour,Config.RockCapDepth);
    }
    // Suspend cosmetics only; coverage is world-owned and refreshed before the chunk re-enters view.
    // =========================================================
    public void SetChunkVisualActive(Node2D chunk, bool active)
    {
        if (_chunks.TryGetValue(chunk, out var state)) state.Trees.SetVisualActive(active);
        if (!active) { _inactiveChunks.Add(chunk); return; }
        _inactiveChunks.Remove(chunk);
        if (_chunks.TryGetValue(chunk,out var adapters))
        { adapters.Trees.Update(_coverage,Config.SnowColour); adapters.Rocks.Update(_coverage,Config.SnowColour,Config.RockCapDepth); }
    }
    public void UnregisterChunk(Node2D chunk)
    {
        _inactiveChunks.Remove(chunk);
        if (!_chunks.Remove(chunk,out var adapters)) return;
        adapters.Trees.Restore(); adapters.Rocks.Restore();
    }

    // Scrub directly from tooling while preserving the target for later enable/disable comparisons.
    // =========================================================
    public void SetCoverage(float value)
    {
        _applying = true; Config.Coverage = Mathf.Clamp(value, 0f, 1f); _applying = false;
        _coverage = Config.Enabled ? Config.Coverage : 0f; ApplyCoverage(); RefreshState();
    }
    #endregion

    #region Simulation and cleanup
    // Manual sliders scrub immediately; automatic accumulation and Inspector changes blend gradually.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldSnow.Process");
        _applying = true;
        if (Config.Enabled && Config.AutoAccumulate)
            Config.Coverage = Mathf.MoveToward(Config.Coverage, AutoTarget, (float)delta / Mathf.Max(5f, Config.AccumulationSeconds));
        _applying = false;
        _coverage = Config.Enabled ? Mathf.MoveToward(_coverage, Mathf.Clamp(Config.Coverage, 0f, 1f), (float)delta / Mathf.Max(0.1f, Config.TransitionSeconds)) : 0f;
        ApplyCoverage(); RefreshProcessing();
    }

    private float AutoTarget => Config.Snowfall && Config.SnowfallRate > 0 ? 1f : 0f;
    // Profile setters wake sleeping coverage and independently manage the optional GPU emitter.
    // =========================================================
    private void RefreshState()
    {
        if (_applying) return;
        if (!Config.Enabled) _coverage = 0;
        bool falling = Config.Enabled && Config.Snowfall && Config.SnowfallRate > 0;
        if (falling && _snowfall == null)
        { _snowfall = new WorldSnowfall { Name = "Snowfall", Snow = this, Weather = Weather }; AddChild(_snowfall); }
        else if (!falling && _snowfall != null)
        { RemoveChild(_snowfall); _snowfall.Free(); _snowfall = null; }
        _snowfall?.ApplySettings();
        ApplyCoverage(); RefreshProcessing();
    }
    private void RefreshProcessing() => SetProcess(Config.Enabled &&
        (_coverage != Mathf.Clamp(Config.Coverage,0,1) || (Config.AutoAccumulate && Config.Coverage != AutoTarget)));

    // The world-bounded quad is clipped by the renderer; settled cover needs no camera callback.
    // Only changing coverage/settings visit material adapters, never the idle frame loop.
    // =========================================================
    private void ApplyCoverage()
    {
        if (_lastCoverage == _coverage && _lastColour == Config.SnowColour && _lastCap == Config.RockCapDepth
            && _lastScale == Config.DriftScale && _lastContrast == Config.DriftContrast) return;
        AppliedUpdates++;
        _lastCoverage = _coverage; _lastColour = Config.SnowColour; _lastCap = Config.RockCapDepth;
        _lastScale = Config.DriftScale; _lastContrast = Config.DriftContrast;
        _cover.Visible = _coverage > 0;
        _ground.SetShaderParameter("snow_coverage", _coverage);
        _ground.SetShaderParameter("snow_colour", Config.SnowColour);
        _ground.SetShaderParameter("drift_scale", Config.DriftScale);
        _ground.SetShaderParameter("drift_contrast", Config.DriftContrast);
        foreach (var pair in _chunks)
        {
            if (_coverage > 0 && _inactiveChunks.Contains(pair.Key)) continue;
            pair.Value.Trees.Update(_coverage,Config.SnowColour); pair.Value.Rocks.Update(_coverage,Config.SnowColour,Config.RockCapDepth);
        }
        _trees.Update(_coverage, Config.SnowColour); _rocks.Update(_coverage, Config.SnowColour, Config.RockCapDepth);
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
        Config.Changed -= RefreshState;
        foreach (var chunk in new System.Collections.Generic.List<Node2D>(_chunks.Keys)) UnregisterChunk(chunk);
        _rocks.Restore();
        _trees.Restore(); _vegetationShaders.Clear();
    }
    #endregion
}
