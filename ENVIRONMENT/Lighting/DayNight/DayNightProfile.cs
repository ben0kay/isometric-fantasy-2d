// Editable production DayNightProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class DayNightProfile : Resource
{
    [ExportGroup("Ambient colours")]
    [Export] public Color NoonColour { get; set; } = new(1f, 0.98f, 0.91f);
    [Export] public Color HorizonColour { get; set; } = new(0.83f, 0.56f, 0.38f);
    [Export] public Color NightColour { get; set; } = new(0.18f, 0.23f, 0.36f);
    [Export] public bool SoftShadows { get; set; } = true;
    [Export] public float ShadowSoftness { get; set; } = 0.008f;
    [ExportGroup("Sun shadows")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ShadowOpacity { get; set; } = 0.36f;
    [Export] public float MinimumLength { get; set; } = 0.18f;
    [Export] public float MaximumLength { get; set; } = 1.65f;
    [Export] public float SunDirectionOffsetDegrees { get; set; } = 0f;
}
