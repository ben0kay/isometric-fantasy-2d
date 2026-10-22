// Reusable FIRE2 GPU emission points sampled from a tree's ACTUAL alpha silhouette.
// Samples once per sprite shape/profile and caches the small RG32F point texture.
// No per-frame sprite sampling, GPU readback, per-leaf nodes or physics checks.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;
using System.Collections.Generic;

public readonly struct CanopyFireMask
{
    public Texture2D Points { get; }
    public int Count { get; }
    public CanopyFireMask(Texture2D points, int count) { Points = points; Count = count; }
}

public static class CanopyFireEmissionMask
{
    private const int SampleCellPixels = 28;
    private const int MaxEmissionPoints = 512;

    // A sprite's authored local transform and flip are part of the cache key; trees
    // sharing the same scene and artwork reuse the identical GPU point texture.
    private readonly record struct MaskKey(
        ulong TextureId, bool FlipH, bool FlipV, bool Centered,
        Transform2D SpriteTransform, Vector2 Offset,
        float Top, float Bottom, float Alpha);
    private static readonly Dictionary<MaskKey, CanopyFireMask> Cache = new();

    #region Cache and build
    // Use a tree's healthy texture and its current layout, not a hardcoded oak canopy box.
    // =========================================================
    public static bool TryGet(Sprite2D sprite, TreeFireProfile config, out CanopyFireMask mask)
    {
        mask = default;
        if (sprite == null || config == null || !config.UseCanopyEmissionMask) return false;
        Texture2D texture = config.HealthyTexture ?? sprite.Texture;
        if (texture == null) return false;

        float top = Mathf.Clamp(config.CanopyTopFraction, 0f, 0.95f);
        float bottom = Mathf.Clamp(config.CanopyBottomFraction, top + 0.01f, 1f);
        float alpha = Mathf.Clamp(config.CanopyAlphaThreshold, 0f, 1f);
        var key = new MaskKey(texture.GetInstanceId(), sprite.FlipH, sprite.FlipV, sprite.Centered,
            sprite.Transform, sprite.Offset, top, bottom, alpha);
        if (!Cache.TryGetValue(key, out mask))
        {
            mask = Build(texture, sprite, top, bottom, alpha);
            Cache.Add(key, mask); // Cache empty results too; invalid art won't be reread each ignition.
        }
        return mask.Points != null && mask.Count > 0;
    }

    // Sample visible sprite pixels in a bounded canopy-height band; outer branch
    // curves, gaps and species-specific widths all follow the imported PNG's alpha.
    // =========================================================
    private static CanopyFireMask Build(Texture2D texture, Sprite2D sprite,
        float top, float bottom, float opacityThreshold)
    {
        Image image = texture.GetImage();
        if (image == null || image.IsEmpty()) return default;
        if (image.IsCompressed() && image.Decompress() != Error.Ok) return default;

        int width = image.GetWidth(), height = image.GetHeight();
        if (width == 0 || height == 0) return default;
        int fromY = Mathf.Clamp(Mathf.RoundToInt(height * top), 0, height - 1);
        int toY = Mathf.Clamp(Mathf.RoundToInt(height * bottom), fromY + 1, height);

        var candidates = new List<Vector2>(MaxEmissionPoints * 3);
        for (int y = fromY; y < toY; y += SampleCellPixels)
        {
            for (int x = SampleCellPixels / 2; x < width; x += SampleCellPixels)
            {
                if (image.GetPixel(x, y).A < opacityThreshold) continue;
                float drawX = sprite.FlipH ? width - x - 0.5f : x + 0.5f;
                float drawY = sprite.FlipV ? height - y - 0.5f : y + 0.5f;
                Vector2 imageOrigin = sprite.Centered ? new Vector2(-width * 0.5f, -height * 0.5f) : Vector2.Zero;
                candidates.Add(sprite.Transform * (new Vector2(drawX, drawY) + sprite.Offset + imageOrigin));
            }
        }
        if (candidates.Count == 0) return default;

        // Evenly thin a large mask instead of taking its first N points, which
        // would bias fire to the top-left of broad or asymmetric canopies.
        int count = Mathf.Min(MaxEmissionPoints, candidates.Count);
        Image points = Image.CreateEmpty(count, 1, false, Image.Format.Rgf);
        for (int i = 0; i < count; i++)
        {
            int sourceIndex = Mathf.Min(candidates.Count - 1,
                (int)((i + 0.5f) * candidates.Count / count));
            Vector2 p = candidates[sourceIndex];
            points.SetPixel(i, 0, new Color(p.X, p.Y, 0f, 1f));
        }
        return new CanopyFireMask(ImageTexture.CreateFromImage(points), count);
    }
    #endregion

    #region GPU particle hookup
    // Point coordinates are already in parent-tree local space; all emitters
    // share this mask without needing dozens of scene nodes for foliage branches.
    // =========================================================
    public static void Apply(CanopyFireMask mask, GpuParticles2D emitter, ParticleProcessMaterial motion)
    {
        if (mask.Points == null || mask.Count == 0 || emitter == null || motion == null) return;
        motion.EmissionPointTexture = mask.Points;
        motion.EmissionPointCount = mask.Count;
        motion.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Points;
        emitter.Position = Vector2.Zero;
    }
    #endregion
}
