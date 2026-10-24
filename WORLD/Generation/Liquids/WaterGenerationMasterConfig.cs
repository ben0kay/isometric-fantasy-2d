// Opt-in bounded macro planning only: no rendering, terrain, physics or per-frame work.
using Godot;
[GlobalClass]
public partial class WaterGenerationMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; }
    [Export] public int LakeCount { get; set; } = 1;
    [Export] public string LiquidId { get; set; } = "water";
    [ExportGroup("World-unit geometry")]
    [Export] public float MinimumDiameter { get; set; } = 600f;
    [Export] public float MaximumDiameter { get; set; } = 1200f;
    [Export] public float Separation { get; set; } = 128f;
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float Irregularity { get; set; } = 0.18f;
    [ExportGroup("Planning limits")]
    [Export] public int AttemptsPerLake { get; set; } = 64;
}
