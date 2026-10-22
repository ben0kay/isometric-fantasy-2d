// Coordinate-keyed candidates and local priority thinning; no node creation or advancing global RNG.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record ChunkSceneryItem(string Id, string BiomeId, SceneryDefinition Content, Vector2 Position, float Scale, bool Mirrored, int Variant, float Radius, ulong Rank);
public sealed class ChunkSceneryData
{
    public const int GeneratorVersion = 1;
    private readonly MacroWorldMap _map;
    private readonly WorldFeaturePlan _features;
    private readonly WorldStreamingConfig _settings;
    private readonly Dictionary<string, WorldBiomeDefinition> _biomes;
    private readonly Dictionary<(int,int), ChunkSceneryItem> _cache = new();
    private readonly Dictionary<string,BiomePopulation[]> _populations = new();
    private readonly float _spacing;
    private readonly int _halo;
    public Func<Vector2,float,bool> LiquidBlocked { get; set; }
    // Validate population identities and conservative spacing before any chunks materialize.
    // =========================================================
    public ChunkSceneryData(MacroWorldMap map, WorldFeaturePlan features, WorldStreamingConfig settings, IEnumerable<WorldBiomeDefinition> biomes, float spacing)
    {
        _map = map; _features = features; _settings = settings; _spacing = spacing;
        _biomes = biomes.Where(b => b != null && b.EnabledForGeneration).ToDictionary(b => b.Id, StringComparer.Ordinal);
        float maxRadius = 0;
        foreach (var biome in _biomes.Values)
        {
            _populations.Add(biome.Id,biome.Populations.OrderBy(p => p?.Content?.Id,StringComparer.Ordinal).ToArray());
            var seen = new HashSet<string>(); double density = 0;
            foreach (var p in biome.Populations)
            {
                if (p?.Content == null || string.IsNullOrWhiteSpace(p.Content.Id) || !float.IsFinite(p.Content.SpacingRadius) || p.Content.SpacingRadius < 0 || !seen.Add(p.Content.Id) || !float.IsFinite(p.InstancesPerMillionUnits) || p.InstancesPerMillionUnits < 0
                    || !float.IsFinite(p.MinimumScale) || !float.IsFinite(p.MaximumScale) || p.MinimumScale <= 0 || p.MaximumScale < p.MinimumScale)
                    throw new ArgumentException($"Invalid/duplicate streaming population in {biome.Id}.");
                density += p.InstancesPerMillionUnits;
                float radius = p.Content.SpacingRadius;
                if (p.Content is TreeDefinition tree) foreach (var v in tree.VisualVariants) radius = Mathf.Max(radius, v.PlacementRadius(radius));
                maxRadius = Mathf.Max(maxRadius, radius * p.MaximumScale);
            }
            if (density * settings.CandidateCellSize * settings.CandidateCellSize > 1000000)
                throw new ArgumentException($"Population density exceeds candidate capacity in {biome.Id}; reduce CandidateCellSize.");
        }
        _halo = Mathf.CeilToInt((maxRadius * 2 + spacing) / settings.CandidateCellSize) + 1;
        if (_halo > 12) throw new ArgumentException("Scenery footprints exceed the bounded candidate halo; increase CandidateCellSize or reduce spacing/scale.");
    }
    // Inspect neighbour candidates across all chunk edges; rejected candidates never depend on live nodes.
    // =========================================================
    public List<ChunkSceneryItem> Build(ChunkCoord coord)
    {
        _cache.Clear(); var result = new List<ChunkSceneryItem>(); Rect2 bounds = coord.Bounds(_settings.ChunkSize).Intersection(_map.Bounds);
        if (!bounds.HasArea()) return result;
        int cell = _settings.CandidateCellSize;
        for (int y = Mathf.FloorToInt(bounds.Position.Y / cell) - 1; y <= Mathf.FloorToInt(bounds.End.Y / cell); y++)
        for (int x = Mathf.FloorToInt(bounds.Position.X / cell) - 1; x <= Mathf.FloorToInt(bounds.End.X / cell); x++)
        {
            var candidate = Candidate(x, y);
            if (candidate == null || ChunkCoord.At(candidate.Position, _settings.ChunkSize) != coord) continue;
            bool blocked = false;
            for (int ny = y - _halo; ny <= y + _halo && !blocked; ny++) for (int nx = x - _halo; nx <= x + _halo; nx++)
            {
                if (nx == x && ny == y) continue;
                var other = Candidate(nx, ny); if (other == null) continue;
                bool earlier = other.Content.GrassLike != candidate.Content.GrassLike ? !other.Content.GrassLike : other.Rank < candidate.Rank || (other.Rank == candidate.Rank && string.CompareOrdinal(other.Id, candidate.Id) < 0);
                float radius = other.Radius + candidate.Radius + _spacing;
                if (earlier && other.Position.DistanceSquaredTo(candidate.Position) < radius * radius) { blocked = true; break; }
            }
            if (!blocked) result.Add(candidate);
        }
        _cache.Clear(); return result;
    }
    // One globally owned jittered sample per cell; stable IDs are independent of chunk size/load order.
    // =========================================================
    private ChunkSceneryItem Candidate(int x, int y)
    {
        if (_cache.TryGetValue((x,y), out var found)) return found;
        _cache[(x,y)] = null;
        ulong state = Key(_map.Seed, x, y); ulong rank = Next(ref state);
        float Unit() => (Next(ref state) >> 40) / 16777216f;
        Vector2 point = new((x + Unit()) * _settings.CandidateCellSize, (y + Unit()) * _settings.CandidateCellSize);
        var info = _map.BiomeAt(point); if (info == null) return null;
        var biome = _biomes[info.Id];
        float choice = Unit() * 1000000f / (_settings.CandidateCellSize * _settings.CandidateCellSize);
        BiomePopulation population = null;
        foreach (var p in _populations[biome.Id])
        { choice -= p.InstancesPerMillionUnits; if (choice < 0) { population = p; break; } }
        if (population == null) return null;
        var content = population.Content; var trees = content as TreeDefinition;
        int variants = trees?.VisualVariants.Count ?? content.Variants.Count; if (variants == 0) return null;
        int variant = Math.Min(variants - 1, (int)(Unit() * variants)); bool mirror = Unit() < population.MirrorChance;
        float scale = Mathf.Lerp(population.MinimumScale, population.MaximumScale, Unit());
        var art = trees?.VisualVariants[variant];
        float radius = (art?.PlacementRadius(content.SpacingRadius) ?? content.SpacingRadius) * scale;
        Vector2 minimum = (art?.VisualMinimum ?? content.VisualMinimum) * scale, maximum = (art?.VisualMaximum ?? content.VisualMaximum) * scale;
        if (mirror) (minimum.X, maximum.X) = (-maximum.X,-minimum.X);
        foreach (Vector2 offset in new[] { minimum, maximum, new Vector2(minimum.X, maximum.Y), new Vector2(maximum.X, minimum.Y) })
            if (!_map.Bounds.HasPoint(point + offset) || (content.AvoidBiomeTransition && _map.BiomeAt(point + offset)?.Id != info.Id)) return null;
        if (content.AvoidBiomeTransition)
        {
            float margin = _settings.BiomeBlendWidth;
            foreach (Vector2 offset in new[] { Vector2.Left,Vector2.Right,Vector2.Up,Vector2.Down })
                if (_map.BiomeAt(point + offset * margin)?.Id != info.Id) return null;
        }
        if (!_features.Reservations.AllowsDisk(point, radius, content.GrassLike ? WorldPlacementKind.LowVegetation : WorldPlacementKind.SolidScenery)
            || (LiquidBlocked?.Invoke(point, radius + (content.GrassLike ? 30 : 15)) ?? false)) return null;
        var item = new ChunkSceneryItem(FormattableString.Invariant($"surface/{x}/{y}/{content.Id}"),info.Id,content,point,scale,mirror,variant,radius,rank);
        _cache[(x,y)] = item; return item;
    }
    // Fixed integer mixing is independent of platform string hashes and runtime Random implementations.
    // =========================================================
    private static ulong Key(int seed, int x, int y) { unchecked { return (uint)seed ^ (ulong)(uint)x * 0x9e3779b97f4a7c15UL ^ (ulong)(uint)y * 0xbf58476d1ce4e5b9UL; } }
    private static ulong Next(ref ulong state)
    { unchecked { state += 0x9e3779b97f4a7c15UL; ulong z = state; z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL; z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL; return z ^ (z >> 31); } }
}
