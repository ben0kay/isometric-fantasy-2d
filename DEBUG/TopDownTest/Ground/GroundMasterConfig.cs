// Disposable grass appearance only; weather and lighting keep their own controllers/configs.
using Godot;

[GlobalClass]
public partial class GroundMasterConfig : Resource
{
    [ExportGroup("Comparison")]
    [Export] public bool NaturalGrass { get; set; } = true;
    [Export] public bool ColourPatches { get; set; } = true;
    [Export] public bool FineDetail { get; set; } = true;
    [ExportGroup("Colour patches")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float Variation { get; set; } = 0.7f;
    [Export(PropertyHint.Range, "80,700,10")] public float PatchSize { get; set; } = 320f;
    [Export] public Color ShadeColour { get; set; } = new(0.18f, 0.245f, 0.18f);
    [Export] public Color FreshColour { get; set; } = new(0.28f, 0.335f, 0.23f);
    [Export] public Color DryColour { get; set; } = new(0.32f, 0.32f, 0.22f);
    [ExportGroup("Fine surface detail")]
    [Export(PropertyHint.Range, "0,0.2,0.005")] public float DetailStrength { get; set; } = 0.065f;
}
