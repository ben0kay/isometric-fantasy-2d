// P1 value-only handoff contract for SAVE: world identity and frozen coarse geography, no disk I/O.
using System;

public sealed record WorldBlueprintManifest(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    int GeneratorVersion, int Seed, string MapHash, float OriginX, float OriginY, float Width, float Height,
    float CellSize, int Columns, int Rows, string[] BiomeIds, int[] RegionBiomes, int[] RegionCells)
{
    // Production SAVE will supply the selected profile/run; a seed never identifies mutable state.
    // =========================================================
    public static WorldBlueprintManifest Create(string profileId, string runId, MacroWorldMap map)
    {
        if (!Guid.TryParse(profileId, out _) || !Guid.TryParse(runId, out _) || map == null)
            throw new ArgumentException("A blueprint requires stable profile/run GUIDs and a completed map.");
        var biomes = new string[map.Biomes.Count]; var regions = new int[map.Regions.Count];
        for (int i = 0; i < biomes.Length; i++) biomes[i] = map.Biomes[i].Id;
        for (int i = 0; i < regions.Length; i++) regions[i] = map.Regions[i].BiomeIndex;
        return new(1, Guid.Parse(profileId).ToString("N"), Guid.Parse(runId).ToString("N"), Guid.NewGuid().ToString("N"), MacroWorldMap.GeneratorVersion, map.Seed,
            map.Hash, map.Bounds.Position.X, map.Bounds.Position.Y, map.Bounds.Size.X, map.Bounds.Size.Y,
            map.CellSize, map.Columns, map.Rows, biomes, regions, map.CopyRegionCells());
    }
    // Disposable preview ownership only; Continue/save selection is deliberately not implemented here.
    // =========================================================
    public static WorldBlueprintManifest CreatePreview(MacroWorldMap map) => Create(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), map);
}
