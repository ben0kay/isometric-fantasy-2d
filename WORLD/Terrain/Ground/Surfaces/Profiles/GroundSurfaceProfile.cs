// Surface data only: shared shaders render palettes without knowing biome names.
using Godot;
using System;

public enum GroundDetailStyle { Organic, Granular, Mineral }

[GlobalClass]
public partial class GroundSurfaceProfile : Resource
{
    [Export] public Texture2D Texture { get; set; }
    [Export] public ShaderMaterial Material { get; set; }
    [ExportGroup("Base colour")]
    [Export] public Color BaseColour { get; set; } = new(0.2196f, 0.2745f, 0.2078f);
    [ExportGroup("Extra surface / legacy patch defaults - explicit base stacks ignore these")]
    [Export] public Color ShadeColour { get; set; } = new(0.18f, 0.245f, 0.18f);
    [Export] public Color FreshColour { get; set; } = new(0.28f, 0.335f, 0.23f);
    [Export] public Color DryColour { get; set; } = new(0.32f, 0.32f, 0.22f);
    [Export] public float Variation { get; set; } = 0.7f;
    [Export] public float PatchSize { get; set; } = 320f;
    [ExportGroup("Base artwork detail")]
    [Export] public float DetailStrength { get; set; } = 0.065f;
    [ExportGroup("Shared streamed composition")]
    [Export] public GroundDetailStyle DetailStyle { get; set; } = GroundDetailStyle.Organic;
    [Export(PropertyHint.Range,"0,1")] public float ArtworkInfluence { get; set; } = 0.22f;

    // Fail during compilation, not after uploading NaN/invalid data to the GPU.
    // =========================================================
    public void Validate()
    {
        if (Texture == null || Material == null || !Enum.IsDefined(DetailStyle)
            || !float.IsFinite(Variation) || Variation < 0 || Variation > 1
            || !float.IsFinite(PatchSize) || PatchSize < 80 || PatchSize > 8192
            || !float.IsFinite(DetailStrength) || DetailStrength < 0 || DetailStrength > 1
            || !float.IsFinite(ArtworkInfluence) || ArtworkInfluence < 0 || ArtworkInfluence > 1)
            throw new ArgumentException("Invalid ground surface appearance settings.");
        foreach (var colour in new[] { BaseColour,ShadeColour,FreshColour,DryColour })
            if (!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B) || !float.IsFinite(colour.A))
                throw new ArgumentException("Ground palettes must be finite.");
    }
}
