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
    [ExportGroup("Optional World Author - new worlds only")]
    [Export] public WorldAuthorPreset Authoring { get; set; }
    [ExportGroup("Optional water planning")]
    [Export] public WaterGenerationMasterConfig Water { get; set; }
    [ExportGroup("BC4 bounded macro / local refinement")]
    [Export] public bool ScalableMacro { get; set; }
    [Export] public float RegionDiameterScale { get; set; } = 1;
    [Export(PropertyHint.Range,"0,0.25,0.01")] public float RefinementStrength { get; set; } = 0.2f;
    [ExportGroup("Region shape")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float BoundaryIrregularity { get; set; } = 0.45f;
    [ExportGroup("Limits")]
    [Export] public int MaximumCells { get; set; } = 262144;
    [Export] public int MaximumRegions { get; set; } = 4096;
}
