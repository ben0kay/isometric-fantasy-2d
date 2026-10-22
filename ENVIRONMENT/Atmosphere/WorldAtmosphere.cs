// One production bounded 2D renderer handles haze, pollen, leaves and torch emissions.
using Godot;
using System.Collections.Generic;
using WorldTree = FantasyGame.World.Trees.Tree;

public partial class WorldAtmosphere : Node2D
{
    [Export] public AtmosphereProfile Config { get; set; }
    [Export] public Texture2D SmokeTexture { get; set; }
    public WorldDayNight Day { get; set; }
    public Node2D Torch { get; set; }
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    private WorldDayNight _day;
    public WindController Wind { get; set; }
    private WindController _weather;
    private Node2D _torch;
    private Polygon2D _haze;
    private ShaderMaterial _mistMaterial;
    private readonly List<(WorldTree Tree, TreeHitResponse Hits, Vector2 Origin, Color Foliage)> _trees = new();
    private readonly Dictionary<WorldTree,int> _indices = new();
    public int TreeSourceCount => _trees.Count;
    public int RegistrationCount { get; private set; }
    private static readonly Vector2[] LeafShape = { new(-3, 0), new(0, -1.5f), new(3, 0), new(0, 1.5f) };
    private PollenDust _pollen;
    private bool _sharedDrawActive, _ready;
    private Rect2 _hazeBounds;
    private Vector2 _lastHazeDrift = new(float.NaN,float.NaN);
    private float _lastDaylight = float.NaN, _lastDensity = float.NaN, _lastPatch = float.NaN;
    private Color _lastHazeColour;
    public int RedrawRequests { get; private set; }
    public int HazeUpdates { get; private set; }
    private double _time;
    private Vector2 _drift;
    private Rect2 _worldBounds;
    private float _leafWindMagnitude; // Smooth count/animation changes when the wind slider moves.
    private readonly Vector2[] _leafDrift = new Vector2[40], _smokeDrift = new Vector2[40], _emberDrift = new Vector2[40];
    private readonly float[] _leafAge = new float[40], _smokeAge = new float[40], _emberAge = new float[40];

    #region Setup
    // Cache generated production emitters and bounds; no developer controls are created here.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AtmosphereProfile();
        _day = Day;
        _weather = Wind;
        _leafWindMagnitude = Mathf.Abs(_weather.Strength);
        _torch = Torch;
        _haze = GetNode<Polygon2D>("Haze");
        _mistMaterial = (ShaderMaterial)_haze.Material.Duplicate(); _haze.Material = _mistMaterial;
        // The world finishes seeded scenery generation before initializing environment modules.
        _worldBounds = WorldBounds;
        _mistMaterial.SetShaderParameter("world_min", _worldBounds.Position);
        _mistMaterial.SetShaderParameter("world_max", _worldBounds.End);
        RefreshTrees();
        _ready = true; Config.Changed += RefreshState;
        if (GodotObject.IsInstanceValid(_torch)) _torch.VisibilityChanged += RefreshState;
        RefreshState();
    }
    // Persistent/preview trees register once; streamed trees use the same explicit lifecycle API.
    // =========================================================
    public void RefreshTrees()
    {
        foreach (Node child in Actors.GetChildren())
            if (child is Node2D source && !source.HasMeta("world_chunk_id")) RegisterSource(source);
    }
    // Cache references, position and species colour once, without enumerating unrelated scene descendants.
    // =========================================================
    public void RegisterSource(Node2D source)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("Atmosphere source registration");
        if (source is not WorldTree tree || !tree.Definition.HasFoliage || _indices.ContainsKey(tree)) return;
        _indices.Add(tree,_trees.Count);
        _trees.Add((tree,tree.GetNode<TreeHitResponse>("HitResponse"),tree.GlobalPosition,tree.Definition.FoliageColour));
        RegistrationCount++;
        if (_ready && _trees.Count == 1) RefreshState();
    }
    // Swap removal retains dense bounded storage and repairs the moved source's index.
    // =========================================================
    public void UnregisterSource(Node2D source)
    {
        if (source is not WorldTree tree || !_indices.Remove(tree,out int index)) return;
        int last = _trees.Count-1;
        if (index != last) { _trees[index] = _trees[last]; _indices[_trees[index].Tree] = index; }
        _trees.RemoveAt(last);
        if (_ready && _trees.Count == 0) RefreshState();
    }
    public override void _ExitTree()
    {
        Config.Changed -= RefreshState;
        if (GodotObject.IsInstanceValid(_torch)) _torch.VisibilityChanged -= RefreshState;
        _trees.Clear(); _indices.Clear();
    }
    // Explicit profile/source/visibility events wake effects; all-off does not poll for changes.
    // =========================================================
    private void RefreshState()
    {
        bool pollen = Config.Pollen && Config.PollenCount > 0;
        if (pollen && _pollen == null) { _pollen = new PollenDust { Name = "PollenDust" }; AddChild(_pollen); }
        else if (!pollen && _pollen != null) { RemoveChild(_pollen); _pollen.Free(); _pollen = null; }
        bool haze = Config.Haze && Config.HazeDensity > 0;
        if (_haze.Visible != haze) _haze.Visible = haze;
        bool torch = GodotObject.IsInstanceValid(_torch) && _torch.IsVisibleInTree();
        bool shared = (Config.Leaves && Config.LeafCount > 0 && _trees.Count > 0)
            || (torch && ((Config.Smoke && Config.SmokeCount > 0 && SmokeTexture != null) || (Config.Embers && Config.EmberCount > 0)));
        if (_sharedDrawActive != shared) RequestRedraw();
        _sharedDrawActive = shared;
        SetProcess(pollen || haze || shared);
        if (pollen || haze) UpdateAmbient();
    }
    private void RequestRedraw() { RedrawRequests++; QueueRedraw(); }

    // Cache changed haze geometry/settings and batch inputs; no per-pollen C# calculation.
    // =========================================================
    private void UpdateAmbient()
    {
        _worldBounds = Day.Weather.VisibleWorldRect().Grow(200).Intersection(WorldBounds);
        _drift = _weather.Displacement;
        _pollen?.UpdateState(Config.PollenCount,_worldBounds,_drift,_day.Daylight);
        if (!Config.Haze || Config.HazeDensity <= 0) return;
        if (_hazeBounds != _worldBounds)
        {
            _hazeBounds = _worldBounds;
            _haze.Polygon = new[] { _worldBounds.Position,new Vector2(_worldBounds.End.X,_worldBounds.Position.Y),_worldBounds.End,new Vector2(_worldBounds.Position.X,_worldBounds.End.Y) };
            _mistMaterial.SetShaderParameter("world_min",_worldBounds.Position); _mistMaterial.SetShaderParameter("world_max",_worldBounds.End); HazeUpdates++;
        }
        if (_lastDensity != Config.HazeDensity) { _lastDensity=Config.HazeDensity; _mistMaterial.SetShaderParameter("density",_lastDensity); HazeUpdates++; }
        if (_lastPatch != Config.PatchSize) { _lastPatch=Config.PatchSize; _mistMaterial.SetShaderParameter("patch_size",_lastPatch); HazeUpdates++; }
        if (_lastHazeColour != Config.HazeColour) { _lastHazeColour=Config.HazeColour; _mistMaterial.SetShaderParameter("haze_colour",_lastHazeColour); HazeUpdates++; }
        if (_lastDaylight != _day.Daylight) { _lastDaylight=_day.Daylight; _mistMaterial.SetShaderParameter("daylight",_lastDaylight); HazeUpdates++; }
        if (_lastHazeDrift != _drift) { _lastHazeDrift=_drift; _mistMaterial.SetShaderParameter("drift",-_drift); HazeUpdates++; }
    }
    #endregion

    #region Simulation and drawing
    // Integrate drift so changing wind does not teleport the mist.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldAtmosphere.Process");
        _time += delta;
        UpdateAmbient();
        // Fade in extra leaves over roughly a second rather than popping on a slider change.
        if (Config.Leaves && Config.LeafCount > 0 && _trees.Count > 0)
        {
            _leafWindMagnitude = Mathf.MoveToward(_leafWindMagnitude, Mathf.Abs(_weather.Strength), (float)delta * 1.5f);
            IntegrateParticleWind(_leafDrift, _leafAge, 14f, 5.37f, Mathf.Clamp(Config.LeafWindDrift, 0f, 6f), (float)delta);
        }
        if (Config.Smoke && Config.SmokeCount > 0 && SmokeTexture != null) IntegrateParticleWind(_smokeDrift, _smokeAge, 6f, 0.43f, 0.7f, (float)delta);
        if (Config.Embers && Config.EmberCount > 0) IntegrateParticleWind(_emberDrift, _emberAge, 2.2f, 0.31f, 0.3f, (float)delta);
        if (_sharedDrawActive) RequestRedraw();
    }

    // Integrate per-cycle drift so wind changes move existing particles without position jumps.
    // =========================================================
    private void IntegrateParticleWind(Vector2[] drift, float[] previousAge, float period, float phase, float response, float delta)
    {
        for (int i = 0; i < drift.Length; i++)
        {
            float age = Mathf.PosMod((float)(_time % 100000) + i * phase, period);
            drift[i] = age < previousAge[i] ? Vector2.Zero : drift[i] + _weather.Velocity * delta * response;
            previousAge[i] = age;
        }
    }

    // Fixed-count deterministic trajectories avoid spawning nodes or allocating per particle.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WorldAtmosphere.Draw");
        if (_day == null) return;
        if (Config.Leaves && _trees.Count > 0)
        {
            // The existing LeafCount is the calm-weather base; stronger wind reveals
            // more staggered leaf trajectories, capped by the 40 pooled particles.
            float count = Mathf.Clamp(Config.LeafCount * (1f + _leafWindMagnitude * Mathf.Max(0f, Config.LeafWindCountBoost)), 0f, _leafDrift.Length);
            for (int i = 0; i < Mathf.CeilToInt(count); i++)
                DrawLeaf(i, Mathf.Clamp(count - i, 0f, 1f));
        }
        if (GodotObject.IsInstanceValid(_torch) && _torch.IsVisibleInTree())
        {
            if (Config.Smoke && SmokeTexture != null)
                for (int i = 0; i < Mathf.Clamp(Config.SmokeCount, 0, 40); i++) DrawSmoke(i);
            if (Config.Embers)
                for (int i = 0; i < Mathf.Clamp(Config.EmberCount, 0, 40); i++) DrawEmber(i);
        }
    }

    // Wind drives direction and flutter while the original tree-origin fall cycle stays intact.
    // =========================================================
    private void DrawLeaf(int i, float visibility)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 5.37f, 14f);
        if (age > 9f || visibility <= 0f) return;
        var source = _trees[(i * 17) % _trees.Count];
        WorldTree tree = source.Tree;
        if (!_worldBounds.HasPoint(source.Origin) || !GodotObject.IsInstanceValid(tree) || !GodotObject.IsInstanceValid(source.Hits) || !tree.IsVisibleInTree() || !source.Hits.HealthyLeaves) return;
        Color foliage = source.Foliage;
        Vector2 start = ToLocal(source.Origin) + new Vector2(Mathf.Sin(i * 7.1f) * 28f, -100f);
        float flutter = 7f + _leafWindMagnitude * 2.5f;
        Vector2 p = start + _leafDrift[i] + new Vector2(Mathf.Sin(age * (2f + _leafWindMagnitude * 0.15f) + i) * flutter, age * 11f);
        float alpha = Mathf.Min(age, 1f) * Mathf.Clamp((9f - age) / 2f, 0f, 1f);
        alpha *= (0.25f + 0.65f * _day.Daylight) * visibility;
        DrawSetTransform(p, age * (2f + _leafWindMagnitude * 0.3f) + i, new Vector2(1f, 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(age * 3f + i))));
        DrawColoredPolygon(LeafShape, new Color(foliage, foliage.A * alpha));
        DrawSetTransform(Vector2.Zero);
    }

    // A small radial texture produces soft expanding smoke without external image assets.
    // =========================================================
    private void DrawSmoke(int i)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 0.43f, 6f);
        Vector2 p = _torch.Position + new Vector2(0, -43) +
            new Vector2(_smokeDrift[i].X + Mathf.Sin(age + i) * age, -age * 12f);
        float size = 7f + age * 4f;
        float alpha = Mathf.Min(age * 2f, 1f) * (1f - age / 6f) * 0.13f;
        alpha *= 0.4f + 0.6f * _day.Daylight;
        DrawTextureRect(SmokeTexture, new Rect2(p - Vector2.One * size / 2f, Vector2.One * size), false,
            new Color(0.65f, 0.66f, 0.64f, alpha));
    }

    // Tiny warm sparks lift from the flame and fade within two seconds.
    // =========================================================
    private void DrawEmber(int i)
    {
        float age = Mathf.PosMod((float)(_time % 100000) + i * 0.31f, 2.2f);
        Vector2 p = _torch.Position + new Vector2(0, -42) +
            new Vector2(Mathf.Sin(age * 3f + i) * 3f + _emberDrift[i].X, -age * (14f + i % 4));
        float alpha = Mathf.Min(age * 5f, 1f) * (1f - age / 2.2f) * 0.75f;
        DrawCircle(p, 0.6f, new Color(1f, 0.48f + age * 0.1f, 0.10f, alpha));
    }
    #endregion
}
