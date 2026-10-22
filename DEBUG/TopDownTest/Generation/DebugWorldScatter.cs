// Generates one seeded, bounded arrangement of debug trees and rocks before lighting caches them.
// All artwork and collision polygons come from reusable packed scenes; this is NOT terrain generation.
using Godot;
using System.Collections.Generic;

public partial class DebugWorldScatter : Node2D
{
    [ExportGroup("Templates")]
    [Export] public PackedScene TreeScene { get; set; }
    [Export] public PackedScene RockScene { get; set; }
    [Export] public ScatterMasterConfig Config { get; set; }

    private readonly List<(Vector2 Center, float Radius)> _occupied = new();
    private readonly List<LiquidRegion> _liquids = new();

    // Keep the debug trail open from the northern edge through the starting clearing and east.
    // Coordinates align with the authored Polygon2D path in TopDownVisualTest.tscn.
    private static readonly Vector2[] MainPath =
    {
        new(453f, -544f), new(460f, 120f), new(467f, 280f),
        new(575f, 395f), new(680f, 441f), new(960f, 446f), new(1760f, 469f)
    };

    #region Setup
    // Spawn synchronously during Actors._Ready, before sibling DayNight/Atmosphere nodes cache actors.
    // =========================================================
    public override void _Ready()
    {
        if (Config == null || TreeScene == null || RockScene == null)
        {
            GD.PushWarning("Debug scatter needs a config and both packed scenery scenes.");
            return;
        }
        foreach (Node node in GetTree().GetNodesInGroup("debug_liquid_regions"))
            if (node is LiquidRegion region) _liquids.Add(region);
        var random = new RandomNumberGenerator { Seed = (ulong)(uint)Config.Seed };
        int trees = SpawnSet(random, TreeScene, true, Mathf.Max(0, Config.TreeCount));
        int rocks = SpawnSet(random, RockScene, false, Mathf.Max(0, Config.RockCount));
        GD.Print($"DEBUG world scatter | seed {Config.Seed} | trees {trees}/{Config.TreeCount}, rocks {rocks}/{Config.RockCount}");
        if (trees < Config.TreeCount || rocks < Config.RockCount)
            GD.PushWarning("Some debug props could not fit safely. Reduce counts/spacing, or increase the map size.");
    }

    // Create matching visuals, collider orientation and uniformly scaled ground anchors.
    // =========================================================
    private int SpawnSet(RandomNumberGenerator random, PackedScene template, bool tree, int requested)
    {
        int placed = 0;
        for (int i = 0; i < requested; i++)
        {
            float minimum = tree ? Config.TreeScaleMin : Config.RockScaleMin;
            float maximum = tree ? Config.TreeScaleMax : Config.RockScaleMax;
            float scale = random.RandfRange(Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));
            float radius = (tree ? Config.TreeSpacingRadius : Config.RockSpacingRadius) * scale;
            if (!FindPlacement(random, tree, radius, out Vector2 position)) continue;

            StaticBody2D body = template.Instantiate<StaticBody2D>();
            body.Name = (tree ? "Tree" : "Rock") + placed;
            body.Position = position;
            body.Scale = Vector2.One * scale;

            if (random.Randf() < Config.MirrorChance)
            {
                body.GetNode<Sprite2D>("Sprite").FlipH = true;
                MirrorFootprint(body.GetNode<CollisionPolygon2D>(tree ? "TrunkFootprint" : "RockFootprint"));
            }
            AddChild(body);
            _occupied.Add((position, radius));
            placed++;
        }
        return placed;
    }

    // Flip only the art and footprint: negative StaticBody2D scales cause physics problems.
    // Reverse point ordering so the polygon keeps its original winding and remains convex.
    // =========================================================
    private static void MirrorFootprint(CollisionPolygon2D collision)
    {
        Vector2[] points = collision.Polygon;
        var mirrored = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p = points[points.Length - 1 - i];
            mirrored[i] = new Vector2(-p.X, p.Y);
        }
        collision.Polygon = mirrored;
    }
    #endregion

    #region Placement
    // Allow non-collidable grass near trees without intersecting physical bases or paths.
    // =========================================================
    public bool CanPlaceGrassDecoration(Vector2 point, float clearance)
    {
        if (Config == null || !Config.WorldBounds.HasPoint(point)) return false;
        float extra = Mathf.Max(0f, clearance);
        foreach (LiquidRegion region in _liquids)
            if (region.Contains(point, extra + 32f)) return false;
        float clearing = Config.StartClearingRadius + extra;
        if (point.DistanceSquaredTo(Config.PlayerSpawn) < clearing * clearing) return false;
        if (TooNearPath(point, Config.PathHalfWidth + extra)) return false;
        foreach (var obstacle in _occupied)
        {
            float radius = obstacle.Radius * 0.72f + extra;
            if (point.DistanceSquaredTo(obstacle.Center) < radius * radius) return false;
        }
        return true;
    }

    // Bounded rejection sampling: avoid existing roots/rocks, central clearing and the main path.
    // =========================================================
    private bool FindPlacement(RandomNumberGenerator random, bool tree, float radius, out Vector2 position)
    {
        Rect2 world = Config.WorldBounds;
        float margin = Config.EdgeMargin;
        float visualRadius = tree ? 83f : 48f; // Larger than physical root to keep art inside the map.
        float minX = world.Position.X + margin + visualRadius;
        float maxX = world.End.X - margin - visualRadius;
        float minY = world.Position.Y + margin + (tree ? 105f : 5f);
        float maxY = world.End.Y - margin;
        position = Vector2.Zero;
        if (maxX <= minX || maxY <= minY) return false;

        for (int attempt = 0; attempt < Mathf.Clamp(Config.AttemptsPerObject, 1, 120); attempt++)
        {
            Vector2 candidate = new(random.RandfRange(minX, maxX), random.RandfRange(minY, maxY));
            float clearing = Config.StartClearingRadius + radius;
            if (candidate.DistanceSquaredTo(Config.PlayerSpawn) < clearing * clearing) continue;
            if (TooNearPath(candidate, radius + Config.PathHalfWidth)) continue;
            bool nearWater = false;
            foreach (LiquidRegion region in _liquids)
                if (region.Contains(candidate, Mathf.Max(radius, visualRadius) + 36f)) { nearWater = true; break; }
            if (nearWater) continue;

            bool blocked = false;
            foreach (var other in _occupied)
            {
                float distance = radius + other.Radius + Config.ExtraSpacing;
                if (candidate.DistanceSquaredTo(other.Center) >= distance * distance) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;
            position = candidate;
            return true;
        }
        return false;
    }

    // Squared point-to-segment distance avoids trigonometry and allocations during generation.
    // =========================================================
    private static bool TooNearPath(Vector2 point, float clearance)
    {
        float requiredSquared = clearance * clearance;
        for (int i = 1; i < MainPath.Length; i++)
        {
            Vector2 start = MainPath[i - 1];
            Vector2 path = MainPath[i] - start;
            float t = Mathf.Clamp((point - start).Dot(path) / Mathf.Max(1f, path.LengthSquared()), 0f, 1f);
            if (point.DistanceSquaredTo(start + path * t) < requiredSquared) return true;
        }
        return false;
    }
    #endregion
}
