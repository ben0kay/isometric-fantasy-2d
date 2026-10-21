// Reversible look-development settings for the disposable debug clearing.
using Godot;

[GlobalClass]
public partial class PolishMasterConfig : Resource
{
    [ExportGroup("Switches")]
    [Export] public bool ColourGrading { get; set; } = true;
    [Export] public bool SoftShadows { get; set; } = true;
    [Export] public bool CloudShade { get; set; } = true;
    [Export] public bool Torch { get; set; } = true;
    [ExportGroup("Colour")]
    [Export(PropertyHint.Range, "0.8,1.5,0.01")] public float Contrast { get; set; } = 1.10f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Saturation { get; set; } = 1.08f;
    [Export(PropertyHint.Range, "0,0.3,0.01")] public float Vignette { get; set; } = 0.08f;
    [ExportGroup("Atmosphere")]
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float CloudOpacity { get; set; } = 0.12f;
    [Export(PropertyHint.Range, "0,0.025,0.001")] public float ShadowSoftness { get; set; } = 0.008f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float TorchEnergy { get; set; } = 0.85f;
}
