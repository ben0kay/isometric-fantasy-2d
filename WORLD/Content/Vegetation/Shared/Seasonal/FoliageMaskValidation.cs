// Cheap binding-time checks only; pixel/import audits belong to the explicit offline authoring tool.
using Godot;

public static class FoliageMaskValidation
{
    // Missing is normal. Unsupported/invalid inputs return a diagnostic and use colour detection.
    // Masks use full source-image UVs; atlas regions need a separate future UV contract.
    // =========================================================
    public static Texture2D Resolve(Texture2D healthy, Texture2D mask, out string diagnostic)
    {
        diagnostic = null;
        if (mask == null) return null;
        if (healthy == null)
        { diagnostic = "healthy artwork is missing"; return null; }
        if (healthy is AtlasTexture || mask is AtlasTexture)
        { diagnostic = "atlas regions are unsupported; use matching full-canvas PNGs"; return null; }
        Vector2 size = healthy.GetSize(), maskSize = mask.GetSize();
        if (size.X <= 0 || size.Y <= 0 || maskSize.X <= 0 || maskSize.Y <= 0)
        { diagnostic = "artwork/mask has no valid source dimensions"; return null; }
        if (size != maskSize)
        { diagnostic = $"mask {maskSize.X}×{maskSize.Y} must match artwork {size.X}×{size.Y}"; return null; }
        return mask;
    }
}
