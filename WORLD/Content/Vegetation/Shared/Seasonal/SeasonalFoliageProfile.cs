// Species-owned seasonal targets; gradient RGB is leaf colour and alpha is recolour influence.
using Godot;

[GlobalClass]
public partial class SeasonalFoliageProfile : Resource
{
    [Export] public Gradient Palette { get; set; } = new();
    [ExportGroup("Tone: red = saturation, green = brightness lift")]
    [Export] public Gradient ToneControls { get; set; }
    [Export(PropertyHint.Range, "0,2,0.05")] public float TimingVariationScale { get; set; } = 1f;
}
