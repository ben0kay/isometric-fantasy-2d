// Bounded startup scatter from biome data. One deterministic RNG controls every generated choice.
using Godot;
using System.Collections.Generic;

public partial class WorldSceneryPlacement : Node
{
    private readonly List<(Vector2 Point, float Radius)> _occupied = new();
    public WorldLiquidRegion ExcludedLiquid { get; set; }

    private static PackedScene _treeTemplate;
    private Node2D _destination;

    #region Generation
    // Called once by world composition after config validation; order of populations is significant.
    // =========================================================
    public void Generate(WorldMasterConfig config, int seed, Node2D destination)
    {
        _destination = destination; _occupied.Clear();
        var random = new RandomNumberGenerator { Seed = (ulong)(uint)seed };
        GenerateBiome(config, random, config.Biome, false);
        if (config.Biomes.Secondary != null) GenerateBiome(config, random, config.Biomes.Secondary, true);
    }

    // Layout defines membership; content stays data-driven and shared across region boundaries.
    // =========================================================
    private void GenerateBiome(WorldMasterConfig config, RandomNumberGenerator random, WorldBiomeDefinition biome, bool secondary)
    {
        Rect2 sampleBounds = config.Biomes.SamplingBounds(config.Bounds, secondary);
        if (!sampleBounds.HasArea()) { GD.PushWarning($"Biome {biome.Id} has no sampling area inside world bounds."); return; }
        foreach (BiomePopulation population in biome.Populations)
        {
            SceneryDefinition content = population?.Content;
            if (content == null || (content is TreeDefinition trees ? trees.VisualVariants.Count == 0 : content is VegetationDefinition plants ? plants.VisualVariants.Count == 0 : content.Variants.Count == 0))
            {
                GD.PushWarning("Skipped a world population without content/scene variants.");
                continue;
            }
            if (content is VegetationDefinition validated) validated.Validate();
            int placed = 0, requested = Mathf.Clamp(population.Count, 0, 5000);
            Vector2 center = Vector2.Zero;
            for (int i = 0; i < requested; i++)
            {
                int clusterSize = Mathf.Clamp(population.ClusterSize, 1, 50);
                if (i % clusterSize == 0) center = RandomPoint(random, sampleBounds);
                float scale = random.RandfRange(Mathf.Max(0.01f, Mathf.Min(population.MinimumScale, population.MaximumScale)),
                    Mathf.Max(0.01f, Mathf.Max(population.MinimumScale, population.MaximumScale)));
                bool mirrored = random.Randf() < Mathf.Clamp(population.MirrorChance, 0f, 1f);
                TreeDefinition treeDefinition = content as TreeDefinition;
                var vegetation = content as VegetationDefinition;
                int variantIndex = random.RandiRange(0, (treeDefinition?.VisualVariants.Count ?? vegetation?.VisualVariants.Count ?? content.Variants.Count) - 1);
                TreeVisualVariant treeVariant = treeDefinition?.VisualVariants[variantIndex];
                var plantVariant = vegetation?.VisualVariants[variantIndex];
                PackedScene scene = treeDefinition == null ? vegetation?.Template ?? content.Variants[variantIndex]
                    : (_treeTemplate ??= GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn"));
                if (scene == null || (treeDefinition != null && treeVariant?.Texture == null)
                    || !FindPosition(config, random, content, treeVariant, plantVariant, scale, mirrored,
                        clusterSize > 1, center, population.ClusterRadius, secondary, out Vector2 point)) continue;
                Node2D prop = scene.Instantiate<Node2D>();
                prop.Position = _destination.ToLocal(point); prop.Scale = Vector2.One * scale;
                if (prop is FantasyGame.World.Trees.Tree tree) tree.Configure(treeDefinition, variantIndex, mirrored);
                else if (prop is VegetationInstance plant) plant.Configure(vegetation, variantIndex, mirrored);
                else if (mirrored) Mirror(prop, content);
                prop.SetMeta("world_visual_variant", variantIndex);
                prop.SetMeta("world_biome_id", biome.Id);
                prop.SetMeta("world_content_id", content.Id);
                prop.SetMeta("environment_snow", (int)content.SnowResponse);
                prop.SetMeta("environment_grass", content.GrassLike);
                prop.SetMeta("environment_shadows", content.CastSunShadow);
                prop.SetMeta("environment_contact", content.ContactShadowRadius);
                if (content.GrassLike) prop.ZIndex = -6;
                _destination.AddChild(prop);
                _occupied.Add((point, (treeVariant?.PlacementRadius(content.SpacingRadius) ?? plantVariant?.PlacementRadius(content.SpacingRadius) ?? Mathf.Max(0f, content.SpacingRadius)) * scale));
                placed++;
            }
            GD.Print($"World scenery | {biome.Id} | {content.Id} | {placed}/{requested}");
            if (placed < requested) GD.PushWarning($"{content.Id}: some objects could not fit; reduce count/spacing or enlarge bounds.");
        }
    }
    #endregion

    #region Placement
    // Reject overlaps, map-edge artwork clipping and the configured clear player spawn.
    // =========================================================
    private bool FindPosition(WorldMasterConfig config, RandomNumberGenerator random, SceneryDefinition content, TreeVisualVariant variant, VegetationVisualVariant plantVariant,
        float scale, bool mirrored, bool clustered, Vector2 center, float clusterRadius, bool secondary, out Vector2 point)
    {
        point = Vector2.Zero;
        float radius = (variant?.PlacementRadius(content.SpacingRadius) ?? plantVariant?.PlacementRadius(content.SpacingRadius) ?? Mathf.Max(0f, content.SpacingRadius)) * scale;
        Vector2 minimum = (variant?.VisualMinimum ?? plantVariant?.VisualMinimum ?? content.VisualMinimum) * scale,
            maximum = (variant?.VisualMaximum ?? plantVariant?.VisualMaximum ?? content.VisualMaximum) * scale;
        if (mirrored) (minimum.X, maximum.X) = (-maximum.X, -minimum.X);
        Rect2 bounds = config.Bounds.Grow(-Mathf.Max(0f, config.EdgeMargin));
        Rect2 sampleBounds = config.Biomes.SamplingBounds(bounds, secondary);
        for (int attempt = 0; attempt < Mathf.Clamp(config.AttemptsPerObject, 1, 120); attempt++)
        {
            Vector2 candidate = clustered
                ? center + Vector2.FromAngle(random.RandfRange(0f, Mathf.Tau))
                    * Mathf.Pow(random.Randf(), 0.75f) * Mathf.Max(0f, clusterRadius)
                : RandomPoint(random, sampleBounds);
            if (!bounds.HasPoint(candidate + minimum) || !bounds.HasPoint(candidate + maximum)) continue;
            if (!config.Biomes.Allows(candidate, secondary, minimum, maximum, content.AvoidBiomeTransition)) continue;
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
            if (ExcludedLiquid != null && ExcludedLiquid.Contains(candidate, radius + (content.GrassLike ? 30f : 15f))) continue;
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
    public static void Mirror(Node2D prop, SceneryDefinition content)
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
