// World-scoped bounded chunk lifecycle: pure candidate data, budgeted scene application and explicit cache cleanup.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using FantasyGame.WorldInteractions.Fire;

public partial class WorldChunkStream : Node
{
    private sealed class Chunk
    {
        public ChunkCoord Coord; public Node2D Actors; public MeshInstance2D Ground;
        public WorldChunkState State; public List<ChunkSceneryItem> Items; public int Applied;
        public readonly Dictionary<string,Node2D> Objects = new();
    }
    private readonly Dictionary<ChunkCoord,Chunk> _chunks = new();
    private readonly Dictionary<string,float> _burns = new();
    private readonly HashSet<string> _removed = new();
    private WorldScene _world; private Node2D _actors; private PlayerMovement _player;
    private WorldMasterConfig _config; private WorldStreamingConfig _settings; private ChunkSceneryData _data;
    private ShaderMaterial _ground; private Chunk _applying; private bool _bound;
    private EnvironmentRuntime _environment; private FireRuntime _fire; private OcclusionController _visibility;
    private double _navTimer; private bool _navDirty;
    private ChunkCoord? _desiredCentre; private ChunkCoord[] _prefetch = Array.Empty<ChunkCoord>();
    public string WorldId { get; private set; }
    public int LoadedCount => _chunks.Values.Count(c => c.State == WorldChunkState.Loaded);
    public double LastDataMilliseconds { get; private set; }
    public double LastFrameMilliseconds { get; private set; }
    public MacroRegion PlayerRegion => _world.MacroMap.RegionAt(_player.GlobalPosition);
    public string PlayerBiome => _world.MacroMap.BiomeAt(_player.GlobalPosition)?.Id ?? "outside";
    public int PendingCount => Desired(_settings.PreloadRadius).Count(c => !_chunks.ContainsKey(c)) + ApplyingCount;
    public int ApplyingCount => _applying == null ? 0 : 1;
    public ChunkCoord PlayerChunk => ChunkCoord.At(_player.GlobalPosition,_settings.ChunkSize);
    public Rect2 LocalBounds => new Rect2(new Vector2((PlayerChunk.X-_settings.ActiveRadius)*_settings.ChunkSize,(PlayerChunk.Y-_settings.ActiveRadius)*_settings.ChunkSize),Vector2.One*((_settings.ActiveRadius*2+1)*_settings.ChunkSize)).Intersection(_world.MacroMap.Bounds);

    #region Startup
    // Load only the initial active square before input; no whole-world scenery or navigation is built.
    // =========================================================
    public void Initialize(WorldScene world, Node2D actors, PlayerMovement player, WorldMasterConfig config, WorldLiquidRegion liquid)
    {
        _world = world; _actors = actors; _player = player; _config = config; _settings = config.Streaming; WorldId = world.Blueprint.WorldId;
        if (_settings.ChunkSize < 256 || _settings.ChunkSize > 2048 || _settings.ActiveRadius < 2 || _settings.PreloadRadius < _settings.ActiveRadius
            || _settings.UnloadRadius <= _settings.PreloadRadius || _settings.UnloadRadius > 6 || _settings.CandidateCellSize < 32 || _settings.CandidateCellSize > 256
            || _settings.NodesPerFrame < 1 || _settings.NodesPerFrame > 128 || !float.IsFinite(_settings.ApplyBudgetMilliseconds) || _settings.ApplyBudgetMilliseconds <= 0)
            throw new ArgumentException("Invalid streaming size, radii, candidate grid or apply budget.");
        float artMargin = 0;
        foreach (var biome in config.Generation.Biomes.Where(b => b != null && b.EnabledForGeneration)) foreach (var p in biome.Populations)
        {
            if (p?.Content == null) continue;
            artMargin = Mathf.Max(artMargin,Mathf.Max(p.Content.VisualMinimum.Length(),p.Content.VisualMaximum.Length())*p.MaximumScale);
            if (p.Content is TreeDefinition tree) foreach (var variant in tree.VisualVariants)
                artMargin = Mathf.Max(artMargin,Mathf.Max(variant.VisualMinimum.Length(),variant.VisualMaximum.Length())*p.MaximumScale);
        }
        var lighting = config.Biome.Environment?.Lighting ?? world.GetNode<EnvironmentRuntime>("Environment").Config.Defaults.Lighting;
        artMargin *= Mathf.Max(1,lighting.MaximumLength);
        Vector2 halfView = player.GetViewportRect().Size.Max(new Vector2(1920,1080)) / (player.Config.CameraZoom / player.Config.CameraMaxViewMultiplier) * 0.5f;
        if (_settings.ChunkSize*_settings.ActiveRadius < Mathf.Max(halfView.X,halfView.Y)+artMargin+64)
            throw new ArgumentException("Active chunk radius is too small for maximum camera view plus scenery artwork.");
        _data = new ChunkSceneryData(world.MacroMap,world.FeaturePlan,_settings,config.Generation.Biomes,config.ExtraSpacing);
        if (liquid != null) _data.LiquidBlocked = liquid.Contains;
        _ground = StreamedGroundPalette.Build(world.MacroMap,config.Generation.Biomes);
        foreach (var coord in Desired(_settings.ActiveRadius))
        {
            var chunk = Begin(coord);
            while (chunk.Applied < chunk.Items.Count) ApplyOne(chunk);
            Complete(chunk);
        }
        player.Streaming = this;
    }
    // Initial environment composition sees the player/lamps; each chunk supplies its own bounded adapters.
    // =========================================================
    public void Bind(EnvironmentRuntime environment, FireRuntime fire, OcclusionController visibility)
    {
        _environment = environment; _fire = fire; _visibility = visibility; _bound = true;
        foreach (var chunk in _chunks.Values) BindChunk(chunk);
    }
    #endregion

    #region Scheduler
    // Cancel obsolete partial loads; prefetch nearest first, then unload beyond a larger hysteresis ring.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!_bound) return;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        if (_applying != null && _applying.Coord.Distance(PlayerChunk) > _settings.UnloadRadius) Release(_applying);
        foreach (var chunk in _chunks.Values.Where(c => c.Coord.Distance(PlayerChunk) > _settings.UnloadRadius).OrderByDescending(c => c.Coord.Distance(PlayerChunk)).Take(2).ToArray()) Release(chunk);
        if (_applying == null)
        {
            if (_desiredCentre != PlayerChunk)
            { _desiredCentre = PlayerChunk; _prefetch = Desired(_settings.PreloadRadius).ToArray(); _navDirty = true; }
            foreach (var coord in _prefetch)
                if (!_chunks.ContainsKey(coord)) { _applying = Begin(coord); break; }
        }
        int count = 0;
        while (_applying != null && count++ < _settings.NodesPerFrame && timer.Elapsed.TotalMilliseconds < _settings.ApplyBudgetMilliseconds)
        {
            if (_applying.Applied < _applying.Items.Count) ApplyOne(_applying);
            else { Complete(_applying); _applying = null; }
        }
        _navTimer += delta;
        if (_navDirty && _navTimer > 0.5)
        {
            _world.GetNodeOrNull<WorldEntityPlacement>("EntityPlacement")?.RefreshNavigation(_actors,_player,LocalBounds);
            _navDirty = false; _navTimer = 0;
        }
        LastFrameMilliseconds = timer.Elapsed.TotalMilliseconds;
    }
    // Camera coverage plus canopy margin must be materialized before allowing movement into it.
    // =========================================================
    public bool CanMoveTo(Vector2 point)
    {
        if (!point.IsFinite() || !_world.MacroMap.Bounds.HasPoint(point)) return false;
        var coord = ChunkCoord.At(point,_settings.ChunkSize);
        for (int y = coord.Y-_settings.ActiveRadius; y <= coord.Y+_settings.ActiveRadius; y++)
        for (int x = coord.X-_settings.ActiveRadius; x <= coord.X+_settings.ActiveRadius; x++)
        {
            var wanted = new ChunkCoord(x,y);
            if (!wanted.Bounds(_settings.ChunkSize).Intersects(_world.MacroMap.Bounds)) continue;
            if (!_chunks.TryGetValue(wanted,out var chunk) || chunk.State != WorldChunkState.Loaded) return false;
        }
        return true;
    }
    // Deterministic nearest-first enumeration works at clipped edges and negative chunk coordinates.
    // =========================================================
    private IEnumerable<ChunkCoord> Desired(int radius, ChunkCoord? around = null)
    {
        var centre = around ?? PlayerChunk; var result = new List<ChunkCoord>();
        for (int y = centre.Y-radius; y <= centre.Y+radius; y++) for (int x = centre.X-radius; x <= centre.X+radius; x++)
        { var coord = new ChunkCoord(x,y); if (coord.Bounds(_settings.ChunkSize).Intersects(_world.MacroMap.Bounds)) result.Add(coord); }
        return result.OrderBy(c => c.Distance(centre)).ThenBy(c => c.Y).ThenBy(c => c.X);
    }
    #endregion

    #region Materialization
    // Pure data generation is kept main-thread in this pass; each scene application is separately budgeted.
    // =========================================================
    private Chunk Begin(ChunkCoord coord)
    {
        var chunk = new Chunk { Coord = coord, State = WorldChunkState.Queued };
        var timer = System.Diagnostics.Stopwatch.StartNew();
        _chunks.Add(coord,chunk); chunk.State = WorldChunkState.GeneratingData; chunk.Items = _data.Build(coord); LastDataMilliseconds = timer.Elapsed.TotalMilliseconds;
        chunk.Actors = new Node2D { Name = $"Chunk_{coord.X}_{coord.Y}", YSortEnabled = true, Visible = false, ProcessMode = ProcessModeEnum.Disabled };
        chunk.Actors.SetMeta("world_chunk_id",FormattableString.Invariant($"{WorldId}/surface/{coord.X}/{coord.Y}")); _actors.AddChild(chunk.Actors);
        chunk.Ground = StreamedGroundPalette.Create(coord.Bounds(_settings.ChunkSize).Intersection(_world.MacroMap.Bounds),_ground);
        chunk.Ground.Visible = false; _world.AddChild(chunk.Ground); chunk.State = WorldChunkState.ApplyingScene;
        return chunk;
    }
    // Generic tree/scenery templates retain the canonical data-owned artwork, metadata and colliders.
    // =========================================================
    private void ApplyOne(Chunk chunk)
    {
        var item = chunk.Items[chunk.Applied++]; if (_removed.Contains(item.Id)) return;
        var content = item.Content; var tree = content as TreeDefinition;
        var scene = tree != null ? GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn") : content.Variants[item.Variant];
        var prop = scene.Instantiate<Node2D>(); prop.Position = item.Position; prop.Scale = Vector2.One * item.Scale;
        if (prop is FantasyGame.World.Trees.Tree instance) instance.Configure(tree,item.Variant,item.Mirrored);
        else if (item.Mirrored) WorldSceneryPlacement.Mirror(prop,content);
        prop.SetMeta("world_instance_id",item.Id); prop.SetMeta("world_biome_id",item.BiomeId); prop.SetMeta("world_content_id",content.Id);
        prop.SetMeta("environment_snow",(int)content.SnowResponse); prop.SetMeta("environment_grass",content.GrassLike);
        prop.SetMeta("environment_shadows",content.CastSunShadow); prop.SetMeta("environment_contact",content.ContactShadowRadius);
        if (content.GrassLike) prop.ZIndex = -6;
        chunk.Actors.AddChild(prop); chunk.Objects.Add(item.Id,prop);
        prop.TreeExiting += () =>
        {
            if (!_bound || !GodotObject.IsInstanceValid(_fire) || !_fire.IsInsideTree()) return;
            var burn = prop.GetNodeOrNull<Burnable>("Burnable"); if (burn != null) _fire.Unregister(burn);
            _visibility.Unregister(prop.GetNodeOrNull<Sprite2D>("Sprite"));
            if (chunk.State == WorldChunkState.Loaded) _removed.Add(item.Id);
        };
    }
    // Publish only complete chunks; adapters/physics are ready together before the player may enter.
    // =========================================================
    private void Complete(Chunk chunk)
    {
        chunk.State = WorldChunkState.Loaded;
        if (_bound) BindChunk(chunk);
        chunk.Actors.ResetPhysicsInterpolation();
        chunk.Actors.ProcessMode = ProcessModeEnum.Inherit; chunk.Actors.Visible = true; chunk.Ground.Visible = true; _navDirty = true;
    }
    private void BindChunk(Chunk chunk)
    {
        _environment.RegisterChunk(chunk.Actors);
        foreach (var pair in chunk.Objects)
        {
            var prop = pair.Value;
            if (prop is FantasyGame.World.Trees.Tree tree && tree.Definition.VisibilityProfile is { } profile)
                _visibility.Register(tree,tree.GetNodeOrNull<Sprite2D>("Sprite"),profile);
            var burn = prop.GetNodeOrNull<Burnable>("Burnable");
            if (burn == null) continue;
            _fire.Register(burn,_environment.DayNight.Shadows);
            if (_burns.TryGetValue(pair.Key,out float progress)) burn.RestoreProgress(progress);
        }
    }
    // Save only touched runtime deltas; release references before freeing nodes. Disk persistence is P4.
    // =========================================================
    private void Release(Chunk chunk)
    {
        bool loaded = chunk.State == WorldChunkState.Loaded; chunk.State = WorldChunkState.Unloading;
        if (loaded)
        {
            foreach (var pair in chunk.Objects)
            {
                if (!GodotObject.IsInstanceValid(pair.Value) || pair.Value.IsQueuedForDeletion()) { _removed.Add(pair.Key); continue; }
                var burn = pair.Value.GetNodeOrNull<Burnable>("Burnable");
                if (burn != null)
                { if (burn.State != BurnState.Healthy) _burns[pair.Key] = Mathf.Max(0.000001f,burn.BurnProgress); else _burns.Remove(pair.Key); _fire.Unregister(burn); }
                _visibility.Unregister(pair.Value.GetNodeOrNull<Sprite2D>("Sprite"));
            }
            _environment.UnregisterChunk(chunk.Actors);
        }
        _actors.RemoveChild(chunk.Actors); chunk.Actors.QueueFree(); chunk.Ground.QueueFree(); _chunks.Remove(chunk.Coord);
        if (_applying == chunk) _applying = null;
        _environment.RefreshLocalActors(); _navDirty = true;
    }
    #endregion
}
