// Draws a bounded splash pool below every actor, excluding static prop artwork rectangles.
using Godot;
using System.Collections.Generic;

public partial class GroundRainSplashes : Node2D
{
    public WorldWeather Weather { get; set; }
    private struct Splash { public Vector2 Position; public float Age; public bool Active; }
    private readonly Splash[] _pool = new Splash[96];
    private sealed class Source
    {
        public readonly List<Rect2> Areas = new();
        public readonly List<Vector2I> Cells = new();
    }
    private const int CellSize = 256;
    private readonly Dictionary<Node2D,Source> _sources = new();
    private readonly Dictionary<Vector2I,List<Source>> _cells = new();
    public int SourceCount => _sources.Count;
    public int SpatialCellCount => _cells.Count;
    public int RegistrationCount { get; private set; }
    private readonly RandomNumberGenerator _random = new() { Seed = 71391 };
    public Node2D Actors { get; set; }
    public Rect2 WorldBounds { get; set; }
    private Sprite2D _player;
    private Rect2 _world;
    private float _budget;
    private int _next;

    #region Setup
    // Static props are already generated; cache conservative artwork bounds once, not per frame.
    // =========================================================
    public override void _Ready()
    {
        // Match rain visibility at night without a separate light for every splash.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        RefreshActors();
    }

    // Capture persistent/preview roots once. Streamed roots register individually while binding.
    // =========================================================
    public void RefreshActors()
    {
        _world = WorldBounds;
        foreach (Node child in Actors.GetChildren())
        {
            if (child.Name == "Player") { _player = child.GetNode<Sprite2D>("Sprite"); continue; }
            if (child is Node2D source && child is not CharacterBody2D && !source.HasMeta("world_chunk_id")) RegisterSource(source);
        }
    }
    // Cache only one changed prop, then index its bounds for local point queries. Repeated registration is safe.
    // =========================================================
    public void RegisterSource(Node2D owner)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("Rain source registration");
        if (_sources.ContainsKey(owner)) return;
        var source = new Source(); CacheArtwork(owner,source.Areas);
        if (source.Areas.Count == 0) return;
        var cells = new HashSet<Vector2I>();
        foreach (Rect2 area in source.Areas)
        {
            Vector2I first = CellAt(area.Position), last = CellAt(area.End);
            for (int y=first.Y;y<=last.Y;y++) for (int x=first.X;x<=last.X;x++) cells.Add(new Vector2I(x,y));
        }
        foreach (Vector2I cell in cells)
        {
            if (!_cells.TryGetValue(cell,out var bucket)) { bucket = new List<Source>(); _cells.Add(cell,bucket); }
            bucket.Add(source); source.Cells.Add(cell);
        }
        _sources.Add(owner,source); RegistrationCount++;
    }
    // Removal uses reverse cell membership, never a scan of other chunks or engine scene nodes.
    // =========================================================
    public void UnregisterSource(Node2D owner)
    {
        if (!_sources.Remove(owner,out var source)) return;
        foreach (Vector2I cell in source.Cells)
        {
            var bucket = _cells[cell]; bucket.Remove(source);
            if (bucket.Count == 0) _cells.Remove(cell);
        }
    }
    // Includes polygon-authored torch artwork; gameplay component subtrees cannot block rain splashes.
    // =========================================================
    private void CacheArtwork(Node node,List<Rect2> areas)
    {
        if (node is Sprite2D sprite && sprite.Texture != null) areas.Add(WorldRect(sprite,sprite.GetRect()).Grow(8));
        if (node is Polygon2D polygon)
        {
            Vector2[] points = polygon.Polygon; // One native-array copy per polygon, not one per vertex.
            if (points.Length > 0)
            {
                Rect2 rect = new(points[0],Vector2.Zero);
                foreach (Vector2 point in points) rect = rect.Expand(point);
                areas.Add(WorldRect(polygon,rect).Grow(8));
            }
        }
        foreach (Node child in node.GetChildren()) if (child is CanvasItem) CacheArtwork(child,areas);
    }
    private static Vector2I CellAt(Vector2 point) => new(Mathf.FloorToInt(point.X/CellSize),Mathf.FloorToInt(point.Y/CellSize));
    // Four transformed corners preserve scaled/mirrored prop bounds without texture pixel reads.
    // =========================================================
    private static Rect2 WorldRect(Node2D node, Rect2 local)
    {
        return SceneryVisualBounds.Transform(local,node.GlobalTransform);
    }
    #endregion

    #region Simulation and drawing
    // Bounded attempts and a reusable pool keep dense forests and frame stalls inexpensive.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundRainSplashes.Process");
        for (int i = 0; i < _pool.Length; i++)
            if (_pool[i].Active) { _pool[i].Age += (float)delta; if (_pool[i].Age >= Mathf.Clamp(Weather.Config.SplashLifetime, 0.15f, 0.6f)) _pool[i].Active = false; }
        if (Weather.Config.Splashes && Weather.Rain > 0.001f)
        {
            _budget = Mathf.Min(8f, _budget + (float)delta * Mathf.Clamp(Weather.Config.SplashesPerSecond, 0f, 200f) * Weather.Rain);
            Rect2 area = Weather.VisibleWorldRect().Intersection(_world);
            Rect2 player = _player == null ? new Rect2() : WorldRect(_player, _player.GetRect()).Grow(8);
            while (_budget >= 1f && area.HasArea())
            {
                _budget -= 1f;
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    Vector2 p = area.Position + new Vector2(_random.Randf(), _random.Randf()) * area.Size;
                    if (!ClearGround(p, player)) continue;
                    _pool[_next] = new Splash { Position = p, Active = true };
                    _next = (_next + 1) % _pool.Length;
                    break;
                }
            }
        }
        else _budget = 0;
        QueueRedraw();
    }

    // Reuse cached exclusions for rare lightning impacts; never rebuild scenery bounds per strike.
    // =========================================================
    public bool TryGroundPoint(Rect2 requested, RandomNumberGenerator random, out Vector2 point)
    {
        Rect2 area = requested.Intersection(_world);
        Rect2 player = _player == null ? new Rect2() : WorldRect(_player, _player.GetRect()).Grow(10);
        point = Vector2.Zero;
        if (!area.HasArea()) return false;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            point = area.Position + new Vector2(random.Randf(), random.Randf()) * area.Size;
            if (ClearGround(point, player)) return true;
        }
        return false;
    }

    // Reject full prop artwork bounds, not only their small ground colliders.
    // =========================================================
    private bool ClearGround(Vector2 point, Rect2 player)
    {
        if (player.HasPoint(point)) return false;
        if (_cells.TryGetValue(CellAt(point),out var bucket))
            foreach (var source in bucket) foreach (Rect2 rect in source.Areas) if (rect.HasPoint(point)) return false;
        return true;
    }

    // These are ground-level arcs and tiny upward droplets, never surface hits on scenery.
    // =========================================================
    public override void _Draw()
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundRainSplashes.Draw");
        if (!Weather.Config.Splashes) return;
        foreach (Splash splash in _pool)
        {
            if (!splash.Active) continue;
            float t = Mathf.Clamp(splash.Age / Mathf.Clamp(Weather.Config.SplashLifetime, 0.15f, 0.6f), 0f, 1f);
            float size = Mathf.Clamp(Weather.Config.SplashSize, 0.5f, 2f), radius = (0.5f + t * 2.5f) * size;
            var colour = new Color(0.72f, 0.81f, 0.86f, (1f - t) * Mathf.Sqrt(Weather.Rain) * Mathf.Clamp(Weather.Config.SplashOpacity, 0f, 1f));
            DrawSetTransform(splash.Position, 0, new Vector2(1, 0.4f));
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 10, colour, 0.8f, true);
            DrawSetTransform(splash.Position);
            float rise = Mathf.Sin(t * Mathf.Pi) * 3f * size;
            DrawLine(new Vector2(-radius, -rise), new Vector2(-radius - 0.4f, -rise - 0.9f), colour, 0.7f, true);
            DrawLine(new Vector2(radius, -rise), new Vector2(radius + 0.4f, -rise - 0.9f), colour, 0.5f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
    // Drop every world-owned reference on exit; no source survives a scene/profile change.
    // =========================================================
    public override void _ExitTree() { _sources.Clear(); _cells.Clear(); }
    #endregion
}
