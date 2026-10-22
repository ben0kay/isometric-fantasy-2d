// Composition only: wires independently owned environment modules after world scenery generation.
using Godot;

public partial class EnvironmentRuntime : Node2D
{
    [Export] public EnvironmentMasterConfig Config { get; set; }
    public BiomeEnvironmentProfile Settings { get; private set; }
    public GameClock Clock { get; private set; }
    public WindController Wind { get; private set; }
    public WorldWeather Weather { get; private set; }
    public WorldDayNight DayNight { get; private set; }
    public WorldSnow Snow { get; private set; }
    public bool Initialized { get; private set; }

    private GroundRainSplashes _rainSources;
    private WorldAtmosphere _atmosphere;
    private readonly System.Collections.Generic.Dictionary<Shader, Shader> _wetnessShaders = new();
    private sealed class ChunkVisuals
    {
        public Rect2 Bounds;
        public VegetationWind Wind;
        public VegetationWetness Wetness;
        public bool Active = true;
    }
    private readonly System.Collections.Generic.Dictionary<Node2D,ChunkVisuals> _chunkVisuals = new();
    public int CosmeticChunkCount { get; private set; }
    public int RegisteredChunkCount => _chunkVisuals.Count;
    public double LastVisibilityMilliseconds { get; private set; }

    #region Composition
    // Called once by the production world after ground, spawn and scenery are configured.
    // =========================================================
    public void Initialize(WorldMasterConfig world, Node2D actors)
    {
        if (Initialized) return;
        ProcessPriority = -20;
        Config ??= GD.Load<EnvironmentMasterConfig>("res://ENVIRONMENT/DefaultEnvironmentConfig.tres");
        Config = (EnvironmentMasterConfig)Config.Duplicate();
        Settings = (world.Biome.Environment ?? new BiomeEnvironmentProfile()).Resolve(Config.Defaults);
        Clock = new GameClock { Name = "Clock", Config = (ClockMasterConfig)Config.Clock.Duplicate(true) }; AddChild(Clock);
        Wind = new WindController { Name = "Wind", Config = Settings.Wind }; AddChild(Wind);
        var vegetation = new VegetationWind { Name = "VegetationWind", Wind = Wind }; AddChild(vegetation); vegetation.Attach(actors);
        var torch = GD.Load<PackedScene>("res://WORLD/Content/Props/Torch/Torch.tscn").Instantiate<Node2D>();
        torch.Name = "Torch"; torch.Position = world.PlayerSpawn + Settings.Torch.OffsetFromSpawn; actors.AddChild(torch);
        AddChild(new TorchLight { Name = "TorchLight", Torch = torch, Config = Settings.Torch });
        // Temporary comparison lamps share the existing profile/controller; no new lighting manager.
        int previewIndex = 0;
        foreach (Vector2 offset in Settings.Torch.AdditionalOffsets)
        {
            if (++previewIndex > 2) break;
            Vector2 position = world.PlayerSpawn + offset;
            if (!world.Bounds.Grow(-20f).HasPoint(position)) continue;
            var extra = GD.Load<PackedScene>("res://WORLD/Content/Props/Torch/Torch.tscn").Instantiate<Node2D>();
            extra.Name = "PreviewTorch" + previewIndex; extra.Position = position; actors.AddChild(extra);
            AddChild(new TorchLight { Name = "PreviewTorchLight" + previewIndex, Torch = extra, Config = Settings.Torch });
        }
        Weather = new WorldWeather { Name = "Weather", Config = Settings.Weather, Wind = Wind, Actors = actors, WorldBounds = world.Bounds }; AddChild(Weather);
        _rainSources = Weather.GetNode<GroundRainSplashes>("GroundSplashes");
        // Wind is already attached; register leaf wetness before snow clones tree materials.
        var wetness = new VegetationWetness { Name = "VegetationWetness", Weather = Weather }; AddChild(wetness); wetness.Attach(actors, _wetnessShaders);
        var shadows = new SunShadowRenderer { Name = "SunShadows", ZIndex = -5, Wind = Wind }; AddChild(shadows); shadows.Attach(actors);
        DayNight = new WorldDayNight { Name = "DayNight", Config = Settings.Lighting, Clock = Clock, Weather = Weather, Shadows = shadows }; AddChild(DayNight);
        var clouds = new CloudShade { Name = "Clouds", Config = Settings.Clouds, Day = DayNight, Wind = Wind }; clouds.Configure(world.Bounds); AddChild(clouds);
        var grade = GD.Load<PackedScene>("res://ENVIRONMENT/Lighting/Colour/ColourGrade.tscn").Instantiate<WorldColourGrade>();
        grade.Name = "ColourGrade"; grade.Config = Settings.ColourGrade; AddChild(grade);
        var atmosphere = GD.Load<PackedScene>("res://ENVIRONMENT/Atmosphere/Atmosphere.tscn").Instantiate<WorldAtmosphere>();
        atmosphere.Config = Settings.Atmosphere; atmosphere.Day = DayNight; atmosphere.Wind = Wind;
        atmosphere.Actors = actors; atmosphere.Torch = torch; atmosphere.WorldBounds = world.Bounds; AddChild(atmosphere); _atmosphere = atmosphere;
        Snow = new WorldSnow { Name = "Snow", Config = Settings.Snow, Weather = Weather, Actors = actors, WorldBounds = world.Bounds }; AddChild(Snow);
        Initialized = true;
    }
    // Per-chunk adapters share the one world clock/wind/weather and release their exact caches on unload.
    // =========================================================
    public void RegisterChunk(Node2D chunk)
    {
        foreach (int step in RegisterChunkSteps(chunk)) { }
    }
    // Yield between adapters so streamed completion shares the same application budget as scenery.
    // =========================================================
    public System.Collections.Generic.IEnumerable<int> RegisterChunkSteps(Node2D chunk)
    {
        var wind = new VegetationWind { Name = "ChunkWind", Wind = Wind }; AddChild(wind); wind.Attach(chunk);
        chunk.SetMeta("chunk_wind",wind.GetInstanceId()); yield return 0;
        var wetness = new VegetationWetness { Weather = Weather }; AddChild(wetness); wetness.Attach(chunk, _wetnessShaders);
        wetness.StartAt(GetNode<VegetationWetness>("VegetationWetness").Coverage);
        chunk.SetMeta("chunk_wetness",wetness.GetInstanceId()); yield return 0;
        var shadow = new SunShadowRenderer { Wind = Wind }; DayNight.Shadows.AddChild(shadow); shadow.Attach(chunk);
        chunk.SetMeta("chunk_shadow",shadow.GetInstanceId()); yield return 0;
        Snow.RegisterChunk(chunk); yield return 0;
        _chunkVisuals.Add(chunk,new ChunkVisuals { Bounds = SceneryVisualBounds.Chunk(chunk), Wind = wind, Wetness = wetness });
    }
    public void UnregisterChunk(Node2D chunk)
    {
        _chunkVisuals.Remove(chunk);
        Snow.UnregisterChunk(chunk);
        foreach (string key in new[] { "chunk_shadow","chunk_wetness","chunk_wind" })
            if (chunk.HasMeta(key) && GodotObject.InstanceFromId(chunk.GetMeta(key).AsUInt64()) is Node node) node.Free();
    }
    // One prop's visual-source lifecycle is shared by trees, rocks and future scenery templates.
    // =========================================================
    public void RegisterScenery(Node2D source)
    {
        _rainSources.RegisterSource(source);
        _atmosphere.RegisterSource(source);
    }
    public void UnregisterScenery(Node2D source)
    {
        _rainSources.UnregisterSource(source);
        _atmosphere.UnregisterSource(source);
    }
    public void RefreshScenery(Node2D source) { UnregisterScenery(source); RegisterScenery(source); }

    // Evaluate cached chunk bounds only; source discovery is handled by explicit prop lifecycle events.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("EnvironmentRuntime.Process");
        if (!Initialized) return;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        Rect2 view = SceneryVisualBounds.View(this).Grow(Mathf.Max(64,Config.SceneryPrepareMargin));
        CosmeticChunkCount = 0;
        foreach (var pair in _chunkVisuals)
        {
            var state = pair.Value;
            bool active = !Config.CullOffscreenSceneryEffects || state.Bounds.Intersects(state.Active ? view.Grow(Mathf.Max(0,Config.SceneryRetentionMargin)) : view);
            if (active != state.Active)
            {
                state.Active = active; state.Wind.SetProcess(active); state.Wetness.SetProcess(active);
                Snow.SetChunkVisualActive(pair.Key,active);
                if (active)
                {
                    state.Wind._Process(0);
                    state.Wetness.StartAt(GetNode<VegetationWetness>("VegetationWetness").Coverage);
                    state.Wetness._Process(0);
                }
            }
            if (active) CosmeticChunkCount++;
        }
        DayNight.Shadows.CullOffscreen = Config.CullOffscreenSceneryEffects;
        DayNight.Shadows.PrepareMargin = Mathf.Max(64,Config.SceneryPrepareMargin);
        LastVisibilityMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }
    #endregion
}
