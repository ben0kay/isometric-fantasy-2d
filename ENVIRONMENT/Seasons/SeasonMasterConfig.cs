// Authored defaults for the manual season preview; runtime commands never mutate this Resource.
using Godot;

[GlobalClass]
public partial class SeasonMasterConfig : Resource
{
    [ExportGroup("Calendar")]
    [Export] public bool Automatic { get; set; }
    [Export(PropertyHint.Range,"30,730,1")] public int YearLengthDays { get; set; } = 120;
    [Export] public double StartingDay { get; set; } = 60;
    [ExportGroup("Seasonal foliage")]
    [Export] public bool Enabled { get; set; }
    [Export(PropertyHint.Range, "0,1,0.001")] public float YearPosition { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ColourStrength { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,45,1")] public float TimingVariationDays { get; set; } = 14f;
}
