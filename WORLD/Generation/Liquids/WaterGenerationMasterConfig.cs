// Opt-in bounded macro planning only: no rendering, terrain, physics or per-frame work.
using Godot;
[GlobalClass]
public partial class WaterGenerationMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; }
    [Export] public int LakeCount { get; set; } = 1;
    [Export] public string LiquidId { get; set; } = "water";
    [ExportGroup("Major oceans - world units")]
    [Export] public int OceanCount { get; set; }
    [Export] public bool OceansRequired { get; set; } = true;
    [Export] public float MinimumOceanDiameter { get; set; } = 32000f;
    [Export] public float MaximumOceanDiameter { get; set; } = 48000f;
    [Export] public int AttemptsPerOcean { get; set; } = 64;
    [ExportGroup("World-unit geometry")]
    [Export] public float MinimumDiameter { get; set; } = 600f;
    [Export] public float MaximumDiameter { get; set; } = 1200f;
    [Export] public float Separation { get; set; } = 128f;
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float Irregularity { get; set; } = 0.18f;
    [ExportGroup("Rivers - opt-in, world units")]
    [Export] public int RiverCount { get; set; }
    [Export] public float MinimumRiverSpan { get; set; } = 6000f;
    [Export] public float MaximumRiverSpan { get; set; } = 11000f;
    [Export] public float MinimumRiverWidth { get; set; } = 100f;
    [Export] public float MaximumRiverWidth { get; set; } = 180f;
    [Export] public float RiverBendStrength { get; set; } = 280f;
    [Export] public float RiverMeanderLength { get; set; } = 2400f;
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float RiverBankIrregularity { get; set; } = 0.15f;
    [ExportGroup("Legacy biome-first validation only")]
    [Export] public bool RequireMultipleRiverBiomes { get; set; }
    [ExportGroup("River route limits")]
    [Export] public int RiverRouteCellSize { get; set; } = 512;
    [Export] public int AttemptsPerRiver { get; set; } = 12;
    [Export] public int MaximumRiverSearchNodes { get; set; } = 8192;
    [ExportGroup("Planning limits")]
    [Export] public int AttemptsPerLake { get; set; } = 64;
}
