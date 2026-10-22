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
    internal MacroWorldMap(int seed, Rect2 bounds, float cellSize, int columns, int rows, int[] cells, MacroBiomeInfo[] biomes, MacroRegion[] regions)
    {
        Seed = seed; Bounds = bounds; CellSize = cellSize; Columns = columns; Rows = rows;
        _cells = (int[])cells.Clone(); Biomes = Array.AsReadOnly(biomes); Regions = Array.AsReadOnly(regions); Hash = ComputeHash();
    }
    // Finite-map edges are exclusive at right/bottom; outside positions have no biome.
    // =========================================================
    public MacroRegion RegionAt(Vector2 point)
    {
        if (!point.IsFinite() || !Bounds.HasPoint(point)) return null;
        int x = Mathf.FloorToInt((point.X - Bounds.Position.X) / CellSize), y = Mathf.FloorToInt((point.Y - Bounds.Position.Y) / CellSize);
        return Regions[_cells[y * Columns + x]];
    }
    public MacroBiomeInfo BiomeAt(Vector2 point) => RegionAt(point) is { } region ? Biomes[region.BiomeIndex] : null;
    public int RegionIdAtCell(int x, int y) => x >= 0 && y >= 0 && x < Columns && y < Rows ? _cells[y * Columns + x] : -1;
    public int[] CopyRegionCells() => (int[])_cells.Clone();
    // Explicit byte-order hash excludes random world-instance identity.
    // =========================================================
    private string ComputeHash()
    {
        using var stream = new System.IO.MemoryStream(); using var writer = new System.IO.BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(GeneratorVersion); writer.Write(Seed); writer.Write(Bounds.Position.X); writer.Write(Bounds.Position.Y);
        writer.Write(Bounds.Size.X); writer.Write(Bounds.Size.Y); writer.Write(CellSize); writer.Write(Columns); writer.Write(Rows);
        foreach (var biome in Biomes) writer.Write(biome.Id);
        foreach (var region in Regions) { writer.Write(region.BiomeIndex); writer.Write(region.Centre.X); writer.Write(region.Centre.Y); writer.Write(region.TargetDiameter); }
        foreach (int id in _cells) writer.Write(id);
        writer.Flush(); return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
