// Editable production SnowProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class SnowProfile : Resource
{
    [Export] public bool Enabled { get; set; } = false;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Coverage { get; set; } = 0.6f;
    [Export] public bool Snowfall { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float SnowfallRate { get; set; } = 0.65f;
    [Export] public bool AutoAccumulate { get; set; } = false;
    [Export(PropertyHint.Range, "5,120,1")] public float AccumulationSeconds { get; set; } = 30f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float TransitionSeconds { get; set; } = 3f;
    [ExportGroup("Shallow snow depth")]
    [Export] public bool PlayerSinks { get; set; } = true;
    [Export(PropertyHint.Range, "0,8,0.25")] public float MaximumSinkPixels { get; set; } = 4f;
    [Export(PropertyHint.Range, "60,500,10")] public float DriftScale { get; set; } = 220f;
    [Export(PropertyHint.Range, "0,0.15,0.005")] public float DriftContrast { get; set; } = 0.065f;
    [Export] public Color SnowColour { get; set; } = new(0.87f, 0.91f, 0.94f);
    [ExportGroup("Rock snow appearance")]
    [Export(PropertyHint.Range, "0.25,0.85,0.01")] public float RockCapDepth { get; set; } = 0.58f;
}
