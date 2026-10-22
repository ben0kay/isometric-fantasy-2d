// Surface data only: shared shaders render palettes without knowing biome names.
using Godot;

[GlobalClass]
public partial class GroundSurfaceProfile : Resource
{
    [Export] public Texture2D Texture { get; set; }
    [Export] public ShaderMaterial Material { get; set; }
    [ExportGroup("Palette")]
    [Export] public Color BaseColour { get; set; } = new(0.2196f, 0.2745f, 0.2078f);
    [Export] public Color ShadeColour { get; set; } = new(0.18f, 0.245f, 0.18f);
    [Export] public Color FreshColour { get; set; } = new(0.28f, 0.335f, 0.23f);
    [Export] public Color DryColour { get; set; } = new(0.32f, 0.32f, 0.22f);
    [ExportGroup("Surface detail")]
    [Export] public float Variation { get; set; } = 0.7f;
    [Export] public float PatchSize { get; set; } = 320f;
    [Export] public float DetailStrength { get; set; } = 0.065f;
}
