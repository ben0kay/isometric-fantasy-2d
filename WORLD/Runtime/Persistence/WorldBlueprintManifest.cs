// P1/P2 value-only handoff contract for SAVE: world identity and frozen coarse geography, no disk I/O.
using System;
using System.Linq;

public sealed record WorldBlueprintManifest(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    int GeneratorVersion, int Seed, string MapHash, float OriginX, float OriginY, float Width, float Height,
    float CellSize, int Columns, int Rows, string[] BiomeIds, int[] RegionBiomes, int[] RegionCells,
    int FeaturePlanVersion = 0, string FeatureHash = "", WorldFeatureSnapshot[] Features = null)
{
    public int LocalGeneratorVersion { get; init; }
    public int ChunkSize { get; init; }
    public int CandidateCellSize { get; init; }
    // Production SAVE will supply the selected profile/run; a seed never identifies mutable state.
    // =========================================================
    public static WorldBlueprintManifest Create(string profileId, string runId, MacroWorldMap map, WorldFeaturePlan features = null)
    {
        if (!Guid.TryParse(profileId, out _) || !Guid.TryParse(runId, out _) || map == null)
            throw new ArgumentException("A blueprint requires stable profile/run GUIDs and a completed map.");
        if (features != null && (features.Seed != map.Seed || features.Reservations.Bounds != map.Bounds))
            throw new ArgumentException("Feature plan and biome map must belong to the same seed and bounds.");
        var biomes = new string[map.Biomes.Count]; var regions = new int[map.Regions.Count];
        for (int i = 0; i < biomes.Length; i++) biomes[i] = map.Biomes[i].Id;
        for (int i = 0; i < regions.Length; i++) regions[i] = map.Regions[i].BiomeIndex;
        return new(2, Guid.Parse(profileId).ToString("N"), Guid.Parse(runId).ToString("N"), Guid.NewGuid().ToString("N"), MacroWorldMap.GeneratorVersion, map.Seed,
            map.Hash, map.Bounds.Position.X, map.Bounds.Position.Y, map.Bounds.Size.X, map.Bounds.Size.Y,
            map.CellSize, map.Columns, map.Rows, biomes, regions, map.CopyRegionCells(),
            features == null ? 0 : WorldFeaturePlan.Version, features?.Hash ?? "",
            features == null ? System.Array.Empty<WorldFeatureSnapshot>() : features.Reservations.Features.Select(WorldFeatureSnapshot.From).ToArray());
    }
    // Disposable preview ownership only; Continue/save selection is deliberately not implemented here.
    // =========================================================
    public static WorldBlueprintManifest CreatePreview(MacroWorldMap map, WorldFeaturePlan features = null) => Create(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), map, features);
}
