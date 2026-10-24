// Applies a shared profile once; no comparison UI or per-frame uniform updates.
using Godot;

public partial class GroundSurface : Sprite2D
{
    public GroundSurfacePreview Preview { get; private set; }
    // Tile the original artwork across the configured world and preserve debug palette/detail math.
    // =========================================================
    public void Configure(Rect2 bounds, GroundSurfaceProfile profile, AdjacentBiomeLayout biomes = null, Color? tint = null, BiomeGroundStack groundStack = null)
    {
        var stack=groundStack ?? biomes?.Primary?.GroundStack;
        var patches=stack!=null ? stack.AccompanyingPatches : GroundPatchLayer.FromLegacy(profile);
        if(patches?.Enabled!=true || patches.Strength<=0) patches=null;
        Texture = profile.Texture;
        TextureRepeat = TextureRepeatEnum.Enabled;
        Centered = false; RegionEnabled = true;
        Position = bounds.Position;
        RegionRect = new Rect2(Vector2.Zero, bounds.Size);
        var material = (ShaderMaterial)profile.Material.Duplicate();
        material.SetShaderParameter("surface_tint",tint ?? biomes?.Primary?.GroundTint ?? Colors.White);
        material.SetShaderParameter("secondary_surface_tint",biomes?.Secondary?.GroundTint ?? tint ?? Colors.White);
        material.SetShaderParameter("base_colour", profile.BaseColour);
        material.SetShaderParameter("shade_colour", patches?.ShadeColour ?? profile.BaseColour);
        material.SetShaderParameter("fresh_colour", patches?.FreshColour ?? profile.BaseColour);
        material.SetShaderParameter("dry_colour", patches?.DryColour ?? profile.BaseColour);
        material.SetShaderParameter("variation", patches?.Enabled==true ? patches.Strength : 0f);
        material.SetShaderParameter("patch_size", patches?.PatchSize ?? 320f);
        material.SetShaderParameter("detail_strength", Mathf.Max(0f, profile.DetailStrength));
        GroundSurfaceProfile other = biomes?.Secondary?.Surface ?? profile;
        var otherStack=biomes?.Secondary?.GroundStack;
        var otherPatches=biomes?.Secondary==null ? patches : otherStack!=null ? otherStack.AccompanyingPatches : GroundPatchLayer.FromLegacy(other);
        if(otherPatches?.Enabled!=true || otherPatches.Strength<=0) otherPatches=null;
        material.SetShaderParameter("adjacent_biome", biomes?.Secondary != null);
        material.SetShaderParameter("secondary_base", other.BaseColour);
        material.SetShaderParameter("secondary_shade", otherPatches?.ShadeColour ?? other.BaseColour);
        material.SetShaderParameter("secondary_fresh", otherPatches?.FreshColour ?? other.BaseColour);
        material.SetShaderParameter("secondary_dry", otherPatches?.DryColour ?? other.BaseColour);
        material.SetShaderParameter("secondary_variation", otherPatches?.Enabled==true ? otherPatches.Strength : 0f);
        material.SetShaderParameter("secondary_patch_size", otherPatches?.PatchSize ?? 320f);
        material.SetShaderParameter("secondary_detail_strength", Mathf.Max(0f, other.DetailStrength));
        if (biomes != null)
        {
            material.SetShaderParameter("biome_boundary_x", biomes.BoundaryX);
            material.SetShaderParameter("biome_curve_amplitude", biomes.CurveAmplitude);
            material.SetShaderParameter("biome_curve_scale", biomes.CurveScale);
            material.SetShaderParameter("biome_blend_width", biomes.BlendWidth);
        }
        Material = material;
        Preview=new GroundSurfacePreview(material,false);
    }
}
