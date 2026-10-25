// Surface data only: shared shaders render palettes without knowing biome names.
using Godot;
using System;

public enum GroundDetailStyle { Organic, Granular, Mineral }
public enum GroundMaterialRecipe { Artwork, Dust, Basalt, GrayscaleArtwork, GrayscaleMineral }

[GlobalClass]
public partial class GroundSurfaceProfile : Resource
{
    [Export] public Texture2D Texture { get; set; }
    [Export] public ShaderMaterial Material { get; set; }
    [ExportGroup("Grayscale texture variants - optional replacement for Texture")]
    [Export] public Godot.Collections.Array<Texture2D> TextureVariants { get; set; } = new();
    [Export(PropertyHint.Range,"80,8192")] public float VariantPatchSize { get; set; } = 512f;
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

    [ExportGroup("Extra-layer material recipe")]
    [Export] public GroundMaterialRecipe Recipe { get; set; } = GroundMaterialRecipe.Artwork;
    [Export(PropertyHint.Range,"1,256")] public float MaterialFeatureSize { get; set; } = 32f;

    public bool UsesGrayscaleArtwork => Recipe==GroundMaterialRecipe.GrayscaleArtwork || Recipe==GroundMaterialRecipe.GrayscaleMineral;

    // Resolve once during palette compilation; existing single-texture surfaces retain their path.
    // =========================================================
    public Godot.Collections.Array<Texture2D> ResolveTextureVariants() =>
        TextureVariants.Count>0 ? TextureVariants : new Godot.Collections.Array<Texture2D> { Texture };

    // Fail during compilation, not after uploading NaN/invalid data to the GPU.
    // =========================================================
    public void Validate()
    {
        if (((Recipe==GroundMaterialRecipe.Artwork && Texture==null) || (UsesGrayscaleArtwork && Texture==null && (TextureVariants==null || TextureVariants.Count==0))) || Material == null || !Enum.IsDefined(DetailStyle)
            || !Enum.IsDefined(Recipe) || !float.IsFinite(MaterialFeatureSize) || MaterialFeatureSize<1 || MaterialFeatureSize>256
            || !float.IsFinite(Variation) || Variation < 0 || Variation > 1
            || !float.IsFinite(PatchSize) || PatchSize < 80 || PatchSize > 8192
            || !float.IsFinite(DetailStrength) || DetailStrength < 0 || DetailStrength > 1
            || !float.IsFinite(ArtworkInfluence) || ArtworkInfluence < 0 || ArtworkInfluence > 1)
            throw new ArgumentException("Invalid ground surface appearance settings.");
        if(TextureVariants==null || TextureVariants.Count>4 || (!UsesGrayscaleArtwork && TextureVariants.Count>0)
            || !float.IsFinite(VariantPatchSize) || VariantPatchSize<80 || VariantPatchSize>8192)
            throw new ArgumentException("Up to four texture variants are supported on grayscale extra layers.");
        if(UsesGrayscaleArtwork)
            foreach(var texture in ResolveTextureVariants())
                if(texture==null || texture.GetWidth()!=256 || texture.GetHeight()!=256)
                    throw new ArgumentException("Grayscale artwork variants must be nonnull native 256x256 images.");
        foreach (var colour in new[] { BaseColour,ShadeColour,FreshColour,DryColour })
            if (!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B) || !float.IsFinite(colour.A))
                throw new ArgumentException("Ground palettes must be finite.");
    }
}
