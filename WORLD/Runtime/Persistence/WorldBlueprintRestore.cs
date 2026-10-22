// Restore frozen coarse outcomes directly; Continue never reruns the macro/feature planners.
using Godot;
using System;
using System.IO;
using System.Linq;
public sealed record SavedMacroRegion(int Id, int BiomeIndex, float X, float Y, float Diameter, int Cells);
public sealed record SavedMacroBiome(string Id, string Name, float R, float G, float B, float A);
public static class WorldBlueprintRestore
{
    // Validate array bounds before allocation/indexing and retain exact region metadata for map hashing.
    // =========================================================
    public static MacroWorldMap Map(WorldBlueprintManifest b)
    {
        if (b == null || b.MacroBiomes == null || b.MacroRegions == null || b.RegionCells == null || b.BiomeIds == null || b.RegionBiomes == null
            || b.Columns < 1 || b.Rows < 1 || (long)b.Columns*b.Rows > 262144 || b.RegionCells.Length != (long)b.Columns*b.Rows
            || b.MacroBiomes.Length < 1 || b.MacroBiomes.Length > 4096 || b.MacroRegions.Length < 1 || b.MacroRegions.Length > 4096
            || b.BiomeIds.Length != b.MacroBiomes.Length || b.RegionBiomes.Length != b.MacroRegions.Length
            || !float.IsFinite(b.CellSize) || b.CellSize <= 0 || !float.IsFinite(b.Width) || !float.IsFinite(b.Height)
            || !float.IsFinite(b.OriginX) || !float.IsFinite(b.OriginY) || b.Width <= 0 || b.Height <= 0
            || Math.Ceiling((double)b.Width/b.CellSize) != b.Columns || Math.Ceiling((double)b.Height/b.CellSize) != b.Rows
            || b.ChunkSize < 256 || b.ChunkSize > 2048 || b.CandidateCellSize < 32 || b.CandidateCellSize > 256)
            throw new InvalidDataException("Invalid frozen world dimensions/arrays.");
        var biomes = b.MacroBiomes.Select((v,i) =>
        {
            if (v == null || string.IsNullOrWhiteSpace(v.Id) || v.Id != b.BiomeIds[i]
                || !float.IsFinite(v.R) || !float.IsFinite(v.G) || !float.IsFinite(v.B) || !float.IsFinite(v.A)) throw new InvalidDataException("Invalid biome snapshot.");
            return new MacroBiomeInfo(v.Id,v.Name,new Color(v.R,v.G,v.B,v.A));
        }).ToArray();
        if (biomes.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != biomes.Length) throw new InvalidDataException("Duplicate biome IDs.");
        var counts = new int[b.MacroRegions.Length];
        foreach (int id in b.RegionCells)
        { if (id < 0 || id >= counts.Length) throw new InvalidDataException("Invalid region cell."); counts[id]++; }
        var regions = b.MacroRegions.Select((v,i) =>
        {
            if (v == null || v.Id != i || v.BiomeIndex < 0 || v.BiomeIndex >= biomes.Length || b.RegionBiomes[i] != v.BiomeIndex
                || !float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Diameter) || v.Diameter <= 0 || v.Cells != counts[i])
                throw new InvalidDataException("Invalid region snapshot.");
            return new MacroRegion(v.Id,v.BiomeIndex,new Vector2(v.X,v.Y),v.Diameter,v.Cells);
        }).ToArray();
        var map = new MacroWorldMap(b.Seed,new Rect2(b.OriginX,b.OriginY,b.Width,b.Height),b.CellSize,b.Columns,b.Rows,b.RegionCells,biomes,regions);
        if (map.Hash != b.MapHash) throw new InvalidDataException("Frozen macro map hash mismatch.");
        return map;
    }
    // Feature construction copies raw values and creates no Resource/Node; usable during worker validation.
    // =========================================================
    public static WorldFeaturePlan Features(WorldBlueprintManifest b)
    {
        if (b.Features == null || b.Features.Length > 4096) throw new InvalidDataException("Invalid feature count.");
        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        int starts = 0;
        var features = b.Features.Select(f =>
        {
            if (f == null || string.IsNullOrWhiteSpace(f.Id) || !ids.Add(f.Id) || f.PolygonXY == null || f.PolygonXY.Length < 6
                || f.PolygonXY.Length > 128 || f.PolygonXY.Length%2 != 0 || !float.IsFinite(f.AnchorX) || !float.IsFinite(f.AnchorY)
                || !float.IsFinite(f.Clearance) || f.Clearance < 0 || (f.Kind & ~(int)WorldPlacementKind.All) != 0
                || (f.AllowedPlacements & ~(int)WorldPlacementKind.All) != 0 || f.PolygonXY.Any(v => !float.IsFinite(v)))
                throw new InvalidDataException("Invalid feature snapshot.");
            if (f.IsStartingArea && ++starts > 1) throw new InvalidDataException("Duplicate starting anchor.");
            return new WorldFeatureReservation(f);
        }).ToArray();
        var plan = new WorldFeaturePlan(b.Seed,new Rect2(b.OriginX,b.OriginY,b.Width,b.Height),features,Array.Empty<string>());
        if (plan.Hash != b.FeatureHash) throw new InvalidDataException("Frozen feature hash mismatch.");
        return plan;
    }
}
