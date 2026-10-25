// Resource-only vegetation type; all cosmetic variants share one lightweight category scene.
using Godot;
using System;

[GlobalClass]
public partial class VegetationDefinition : SceneryDefinition
{
    [ExportGroup("Shared template and artwork")]
    [Export] public PackedScene Template { get; set; }
    [Export] public Godot.Collections.Array<VegetationVisualVariant> VisualVariants { get; set; } = new();
    [Export] public ShaderMaterial WindMaterial { get; set; }

    // Consume the planner's existing coordinate-seeded unit sample; never advance an instance RNG.
    // Streaming supplies this sample from the existing candidate sequence without changing legacy paths.
    // =========================================================
    public int SelectVariant(float seededUnitSample)
    {
        if (VisualVariants.Count == 0 || !float.IsFinite(seededUnitSample) || seededUnitSample < 0 || seededUnitSample >= 1)
            throw new ArgumentException("Vegetation needs artwork and a seeded sample in [0, 1).");
        return Math.Min(VisualVariants.Count - 1, (int)(seededUnitSample * VisualVariants.Count));
    }
    // Fail during population setup rather than publishing broken or non-conservative artwork.
    // =========================================================
    public void Validate()
    {
        if (Template == null || VisualVariants.Count == 0)
            throw new ArgumentException($"Vegetation {Id} requires a template and artwork.");
        foreach (var art in VisualVariants)
        {
            if (art?.Texture == null || !art.SourceAnchor.IsFinite() || !art.GroundOffset.IsFinite()
                || !art.SpriteScale.IsFinite() || art.SpriteScale.X <= 0 || art.SpriteScale.Y <= 0
                || !art.VisualMinimum.IsFinite() || !art.VisualMaximum.IsFinite()
                || art.VisualMinimum.X > art.VisualMaximum.X || art.VisualMinimum.Y > art.VisualMaximum.Y
                || !float.IsFinite(art.SpacingRadius))
                throw new ArgumentException($"Vegetation {Id} has invalid artwork/placement data.");
            Vector2 minimum = art.GroundOffset - art.SourceAnchor * art.SpriteScale;
            Vector2 maximum = minimum + art.Texture.GetSize() * art.SpriteScale;
            if (art.VisualMinimum.X > minimum.X || art.VisualMinimum.Y > minimum.Y
                || art.VisualMaximum.X < maximum.X || art.VisualMaximum.Y < maximum.Y)
                throw new ArgumentException($"Vegetation {Id} bounds must contain the full rooted artwork.");
        }
    }
}
