// Applies a shared profile once; no comparison UI or per-frame uniform updates.
using Godot;

public partial class GroundSurface : Sprite2D
{
    // Tile the original artwork across the configured world and preserve debug palette/detail math.
    // =========================================================
    public void Configure(Rect2 bounds, GroundSurfaceProfile profile, AdjacentBiomeLayout biomes = null)
    {
        Texture = profile.Texture;
        TextureRepeat = TextureRepeatEnum.Enabled;
        Centered = false; RegionEnabled = true;
        Position = bounds.Position;
        RegionRect = new Rect2(Vector2.Zero, bounds.Size);
        var material = (ShaderMaterial)profile.Material.Duplicate();
        material.SetShaderParameter("base_colour", profile.BaseColour);
        material.SetShaderParameter("shade_colour", profile.ShadeColour);
        material.SetShaderParameter("fresh_colour", profile.FreshColour);
        material.SetShaderParameter("dry_colour", profile.DryColour);
        material.SetShaderParameter("variation", Mathf.Clamp(profile.Variation, 0f, 1f));
        material.SetShaderParameter("patch_size", Mathf.Max(80f, profile.PatchSize));
        material.SetShaderParameter("detail_strength", Mathf.Max(0f, profile.DetailStrength));
        GroundSurfaceProfile other = biomes?.Secondary?.Surface ?? profile;
        material.SetShaderParameter("adjacent_biome", biomes?.Secondary != null);
        material.SetShaderParameter("secondary_base", other.BaseColour);
        material.SetShaderParameter("secondary_shade", other.ShadeColour);
        material.SetShaderParameter("secondary_fresh", other.FreshColour);
        material.SetShaderParameter("secondary_dry", other.DryColour);
        material.SetShaderParameter("secondary_variation", Mathf.Clamp(other.Variation, 0f, 1f));
        material.SetShaderParameter("secondary_patch_size", Mathf.Max(80f, other.PatchSize));
        material.SetShaderParameter("secondary_detail_strength", Mathf.Max(0f, other.DetailStrength));
        if (biomes != null)
        {
            material.SetShaderParameter("biome_boundary_x", biomes.BoundaryX);
            material.SetShaderParameter("biome_curve_amplitude", biomes.CurveAmplitude);
            material.SetShaderParameter("biome_curve_scale", biomes.CurveScale);
            material.SetShaderParameter("biome_blend_width", biomes.BlendWidth);
        }
        Material = material;
    }
}
