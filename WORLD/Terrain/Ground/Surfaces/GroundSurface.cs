// Applies a shared profile once; no comparison UI or per-frame uniform updates.
using Godot;

public partial class GroundSurface : Sprite2D
{
    // Tile the original artwork across the configured world and preserve debug palette/detail math.
    // =========================================================
    public void Configure(Rect2 bounds, GroundSurfaceProfile profile)
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
        Material = material;
    }
}
