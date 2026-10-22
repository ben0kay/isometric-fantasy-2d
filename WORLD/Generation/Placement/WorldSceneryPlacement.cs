// Bounded startup scatter from biome data. One deterministic RNG controls every generated choice.
using Godot;
using System.Collections.Generic;

public partial class WorldSceneryPlacement : Node2D
{
    private readonly List<(Vector2 Point, float Radius)> _occupied = new();

    #region Generation
    // Called once by world composition after config validation; order of populations is significant.
    // =========================================================
    public void Generate(WorldMasterConfig config, int seed)
    {
        var random = new RandomNumberGenerator { Seed = (ulong)(uint)seed };
        foreach (BiomePopulation population in config.Biome.Populations)
        {
            SceneryDefinition content = population?.Content;
            if (content == null || content.Variants.Count == 0)
            {
                GD.PushWarning("Skipped a world population without content/scene variants.");
                continue;
            }
            int placed = 0, requested = Mathf.Clamp(population.Count, 0, 5000);
            Vector2 center = Vector2.Zero;
            for (int i = 0; i < requested; i++)
            {
                int clusterSize = Mathf.Clamp(population.ClusterSize, 1, 50);
                if (i % clusterSize == 0) center = RandomPoint(random, config.Bounds);
                float scale = random.RandfRange(Mathf.Max(0.01f, Mathf.Min(population.MinimumScale, population.MaximumScale)),
                    Mathf.Max(0.01f, Mathf.Max(population.MinimumScale, population.MaximumScale)));
                bool mirrored = random.Randf() < Mathf.Clamp(population.MirrorChance, 0f, 1f);
                PackedScene scene = content.Variants[random.RandiRange(0, content.Variants.Count - 1)];
                if (scene == null || !FindPosition(config, random, content, scale, mirrored,
                    clusterSize > 1, center, population.ClusterRadius, out Vector2 point)) continue;
                Node2D prop = scene.Instantiate<Node2D>();
                prop.Position = point; prop.Scale = Vector2.One * scale;
                if (mirrored) Mirror(prop, content);
                AddChild(prop);
                _occupied.Add((point, Mathf.Max(0f, content.SpacingRadius) * scale));
                placed++;
            }
            GD.Print($"World scenery | {content.Id} | {placed}/{requested}");
            if (placed < requested) GD.PushWarning($"{content.Id}: some objects could not fit; reduce count/spacing or enlarge bounds.");
        }
    }
    #endregion

    #region Placement
    // Reject overlaps, map-edge artwork clipping and the configured clear player spawn.
    // =========================================================
    private bool FindPosition(WorldMasterConfig config, RandomNumberGenerator random, SceneryDefinition content,
        float scale, bool mirrored, bool clustered, Vector2 center, float clusterRadius, out Vector2 point)
    {
        point = Vector2.Zero;
        float radius = Mathf.Max(0f, content.SpacingRadius) * scale;
        Vector2 minimum = content.VisualMinimum * scale, maximum = content.VisualMaximum * scale;
        if (mirrored) (minimum.X, maximum.X) = (-maximum.X, -minimum.X);
        Rect2 bounds = config.Bounds.Grow(-Mathf.Max(0f, config.EdgeMargin));
        for (int attempt = 0; attempt < Mathf.Clamp(config.AttemptsPerObject, 1, 120); attempt++)
        {
            Vector2 candidate = clustered
                ? center + Vector2.FromAngle(random.RandfRange(0f, Mathf.Tau))
                    * Mathf.Pow(random.Randf(), 0.75f) * Mathf.Max(0f, clusterRadius)
                : RandomPoint(random, bounds);
            if (!bounds.HasPoint(candidate + minimum) || !bounds.HasPoint(candidate + maximum)) continue;
            float clearing = Mathf.Max(0f, config.SpawnClearance) + radius;
            if (candidate.DistanceSquaredTo(config.PlayerSpawn) < clearing * clearing) continue;
            bool blocked = false;
            foreach (var other in _occupied)
            {
                float distance = radius + other.Radius + Mathf.Max(0f, config.ExtraSpacing);
                if (candidate.DistanceSquaredTo(other.Point) >= distance * distance) continue;
                blocked = true; break;
            }
            if (blocked) continue;
            point = candidate; return true;
        }
        return false;
    }

    // Uniform world-space sampling does not depend on frame time or machine speed.
    // =========================================================
    private static Vector2 RandomPoint(RandomNumberGenerator random, Rect2 bounds) =>
        new(random.RandfRange(bounds.Position.X, bounds.End.X), random.RandfRange(bounds.Position.Y, bounds.End.Y));

    // Reflect art and actual footprint together without negative physics-body scales.
    // =========================================================
    private static void Mirror(Node2D prop, SceneryDefinition content)
    {
        if (prop.GetNodeOrNull<Sprite2D>(content.SpritePath) is Sprite2D sprite) sprite.FlipH = !sprite.FlipH;
        if (content.FootprintPath.IsEmpty || prop.GetNodeOrNull<CollisionPolygon2D>(content.FootprintPath) is not CollisionPolygon2D collision) return;
        Vector2[] source = collision.Polygon, result = new Vector2[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Vector2 p = source[source.Length - 1 - i]; result[i] = new Vector2(-p.X, p.Y);
        }
        collision.Polygon = result;
    }
    #endregion
}
