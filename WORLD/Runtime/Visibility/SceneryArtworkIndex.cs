// Shared static artwork queries for lightning and surface effects, independent of rain activation.
using Godot;
using System.Collections.Generic;
public sealed class SceneryArtworkIndex
{
    public Rect2 WorldBounds { get; set; }
    private Sprite2D _player;
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
    public void Initialize(Node2D actors,Rect2 bounds)
    {
        WorldBounds = bounds;
        foreach (Node child in actors.GetChildren())
        {
            if (child.Name == "Player") { _player = child.GetNode<Sprite2D>("Sprite"); continue; }
            if (child is Node2D source && child is not CharacterBody2D && !source.HasMeta("world_chunk_id")) RegisterSource(source);
        }
    }
    public Rect2 PlayerBounds(float padding) => GodotObject.IsInstanceValid(_player) ? SceneryVisualBounds.Sprite(_player).Grow(padding) : new Rect2();
    // Cache only one changed prop, then index its bounds for local point queries. Repeated registration is safe.
    // =========================================================
    public void RegisterSource(Node2D owner)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("Ground artwork registration");
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
    public bool IsClear(Vector2 point,Rect2 player)
    {
        if (player.HasPoint(point)) return false;
        if (_cells.TryGetValue(CellAt(point),out var bucket))
            foreach (var source in bucket) foreach (Rect2 area in source.Areas) if (area.HasPoint(point)) return false;
        return true;
    }
    public bool TryGroundPoint(Rect2 requested,RandomNumberGenerator random,out Vector2 point)
    {
        Rect2 area = requested.Intersection(WorldBounds), player = PlayerBounds(10);
        point = Vector2.Zero; if (!area.HasArea()) return false;
        for (int i=0;i<40;i++)
        {
            point = area.Position+new Vector2(random.Randf(),random.Randf())*area.Size;
            if (IsClear(point,player)) return true;
        }
        return false;
    }
    public void Clear() { _sources.Clear(); _cells.Clear(); _player = null; }
}
