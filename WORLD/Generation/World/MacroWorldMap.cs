// Frozen coarse geography that can be queried without loading scenery or retaining mutable Resources.
using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
public sealed record MacroBiomeInfo(string Id, string Name, Color Colour);
public sealed record MacroRegion(int Id, int BiomeIndex, Vector2 Centre, float TargetDiameter, int CellCount);
public sealed class MacroWorldMap
{
    private readonly int[] _cells;
    public const int GeneratorVersion = 1;
    public const int GeographyGeneratorVersion = 2;
    public const int OceanGeneratorVersion = 3;
    public const int ClimateGeneratorVersion = 4;
    public const int ScalableGeneratorVersion = 5;
    public float RefinementStrength { get; }
    public MacroBuildCounters BuildCounters { get; internal set; }
    public int NonLandCells { get; }
    public int GenerationVersion { get; }
    public int Seed { get; }
    public Rect2 Bounds { get; }
    public float CellSize { get; }
    public int Columns { get; }
    public int Rows { get; }
    public IReadOnlyList<MacroBiomeInfo> Biomes { get; }
    public IReadOnlyList<MacroRegion> Regions { get; }
    public string Hash { get; }
    // Copy all owned data; later authoring edits cannot change the generated blueprint.
    // =========================================================
    internal MacroWorldMap(int seed, Rect2 bounds, float cellSize, int columns, int rows, int[] cells, MacroBiomeInfo[] biomes, MacroRegion[] regions,int generationVersion=GeneratorVersion,float refinementStrength=0)
    {
        if(!float.IsFinite(refinementStrength) || refinementStrength<0 || refinementStrength>0.25f || (generationVersion!=ScalableGeneratorVersion && refinementStrength!=0))
            throw new ArgumentException("Invalid frozen macro refinement contract.");
        RefinementStrength=refinementStrength; GenerationVersion=generationVersion; Seed = seed; Bounds = bounds; CellSize = cellSize; Columns = columns; Rows = rows;
        _cells = (int[])cells.Clone(); foreach (int cell in _cells) if (cell<0) NonLandCells++; Biomes = Array.AsReadOnly(biomes); Regions = Array.AsReadOnly(regions); Hash = ComputeHash();
    }
    // Finite-map edges are exclusive at right/bottom; outside positions have no biome.
    // =========================================================
    public MacroRegion RegionAt(Vector2 point)
    {
        if (!point.IsFinite() || !Bounds.HasPoint(point)) return null;
        int x = Mathf.FloorToInt((point.X - Bounds.Position.X) / CellSize), y = Mathf.FloorToInt((point.Y - Bounds.Position.Y) / CellSize);
        x=Math.Clamp(x,0,Columns-1); y=Math.Clamp(y,0,Rows-1);
        int region=_cells[y*Columns+x];
        if(region>=0 && RefinementStrength>0)
        {
            Vector2 refined=(point+MacroBorderRefinement.Offset(point-Bounds.Position,CellSize,Seed,RefinementStrength))
                .Clamp(Bounds.Position,Bounds.End-Vector2.One*0.25f);
            int rx=Mathf.FloorToInt((refined.X-Bounds.Position.X)/CellSize),ry=Mathf.FloorToInt((refined.Y-Bounds.Position.Y)/CellSize);
            rx=Math.Clamp(rx,0,Columns-1); ry=Math.Clamp(ry,0,Rows-1);
            int candidate=_cells[ry*Columns+rx]; if(candidate>=0) region=candidate;
        }
        return region<0 ? null : Regions[region];
    }
    public MacroBiomeInfo BiomeAt(Vector2 point) => RegionAt(point) is { } region ? Biomes[region.BiomeIndex] : null;
    public int RegionIdAtCell(int x, int y) => x >= 0 && y >= 0 && x < Columns && y < Rows ? _cells[y * Columns + x] : -1;
    public int[] CopyRegionCells() => (int[])_cells.Clone();
    // Explicit byte-order hash excludes random world-instance identity.
    // =========================================================
    private string ComputeHash()
    {
        using var stream = new System.IO.MemoryStream(); using var writer = new System.IO.BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(GenerationVersion); writer.Write(Seed); writer.Write(Bounds.Position.X); writer.Write(Bounds.Position.Y);
        writer.Write(Bounds.Size.X); writer.Write(Bounds.Size.Y); writer.Write(CellSize); writer.Write(Columns); writer.Write(Rows);
        if(GenerationVersion==ScalableGeneratorVersion) writer.Write(RefinementStrength);
        foreach (var biome in Biomes) writer.Write(biome.Id);
        foreach (var region in Regions) { writer.Write(region.BiomeIndex); writer.Write(region.Centre.X); writer.Write(region.Centre.Y); writer.Write(region.TargetDiameter); }
        foreach (int id in _cells) writer.Write(id);
        writer.Flush(); return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
