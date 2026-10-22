// Editable production AtmosphereProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class AtmosphereProfile : Resource
{
    [ExportGroup("Switches")]
    [Export] public bool Haze { get; set; } = true;
    [Export] public bool Pollen { get; set; } = true;
    [Export] public bool Leaves { get; set; } = true;
    [Export] public bool Smoke { get; set; } = true;
    [Export] public bool Embers { get; set; } = true;
    [ExportGroup("Haze")]
    [Export(PropertyHint.Range, "0,0.6,0.01")] public float HazeDensity { get; set; } = 0.22f;
    [Export(PropertyHint.Range, "60,500,5")] public float PatchSize { get; set; } = 190f;
    [Export] public Color HazeColour { get; set; } = new(0.74f, 0.80f, 0.79f);
    [ExportGroup("Particle intensity")]
    [Export(PropertyHint.Range, "0,100,1")] public int PollenCount { get; set; } = 35;
    [Export(PropertyHint.Range, "0,40,1")] public int LeafCount { get; set; } = 10;
    [ExportGroup("Leaves / wind")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float LeafWindCountBoost { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,6,0.25")] public float LeafWindDrift { get; set; } = 2f;
    [Export(PropertyHint.Range, "0,40,1")] public int SmokeCount { get; set; } = 14;
    [Export(PropertyHint.Range, "0,40,1")] public int EmberCount { get; set; } = 10;
}
