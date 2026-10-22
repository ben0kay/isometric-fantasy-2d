// Finite macro geography settings, independent of preview bounds and future streaming chunks.
using Godot;
[GlobalClass]
public partial class WorldGenerationMasterConfig : Resource
{
    [ExportGroup("Finite map")]
    [Export] public Rect2 Bounds { get; set; } = new(-22500, -16875, 45000, 33750);
    [Export] public float CellSize { get; set; } = 256f;
    [Export] public Godot.Collections.Array<WorldBiomeDefinition> Biomes { get; set; } = new();
    [ExportGroup("Guided layout and reservations")]
    [Export] public WorldLayoutPreset Layout { get; set; }
    [ExportGroup("Region shape")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float BoundaryIrregularity { get; set; } = 0.45f;
    [ExportGroup("Limits")]
    [Export] public int MaximumCells { get; set; } = 262144;
    [Export] public int MaximumRegions { get; set; } = 4096;
}
