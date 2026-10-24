// Small world-owned lookup table packs two ordered layers per biome; never updates per frame.
using Godot;
public static class GroundOverlayPalette
{
    public const int Columns=9;
    // Zero/disabled slots cost no mask or artwork work in the shader.
    // =========================================================
    public static bool IsVisible(BiomeGroundLayer layer) => layer.Enabled && layer.Coverage>0 && layer.Opacity>0;

    // Authored-off previews preload data once; visibility remains off until explicitly requested.
    // =========================================================
    public static bool CanCompile(BiomeGroundLayer layer) => (layer.Enabled || layer.AllowSessionPreview) && layer.Coverage>0 && layer.Opacity>0;

    // Row = biome*2 + slot. Artwork indices select the legacy 32px atlas or the native 256px grayscale atlas by recipe.
    // =========================================================
    public static void Bind(ShaderMaterial material,CompiledGroundBiome[] biomes,int[,] artwork)
    {
        using var table=Image.CreateEmpty(Columns,biomes.Length*2,false,Image.Format.Rgbaf);
        for(int i=0;i<biomes.Length;i++) for(int slot=0;slot<biomes[i].Overlays.Length;slot++)
        {
            var layer=biomes[i].Overlays[slot]; if(!CanCompile(layer)) continue;
            var p=layer.Surface; int row=i*2+slot;
            table.SetPixel(0,row,p.BaseColour); table.SetPixel(1,row,p.ShadeColour);
            table.SetPixel(2,row,p.FreshColour); table.SetPixel(3,row,p.DryColour);
            table.SetPixel(4,row,new Color(p.Variation,p.PatchSize,p.DetailStrength,p.ArtworkInfluence));
            table.SetPixel(5,row,layer.Tint);
            table.SetPixel(6,row,new Color(layer.Coverage,layer.Opacity,layer.PatchSize,layer.EdgeSoftness));
            uint salt=unchecked((uint)layer.SeedSalt*747796405u+2891336453u);
            float offsetX=(salt&65535)/65536f; salt=unchecked(salt*747796405u+2891336453u);
            table.SetPixel(7,row,new Color(layer.WarpStrength,offsetX,(salt&65535)/65536f,(float)p.DetailStyle));
            table.SetPixel(8,row,new Color(artwork[i,slot],layer.Enabled ? 1f : 2f,(float)p.Recipe,p.MaterialFeatureSize));
        }
        material.SetShaderParameter("overlay_table",ImageTexture.CreateFromImage(table));
        material.SetShaderParameter("overlay_rows",(float)(biomes.Length*2));
    }
}
