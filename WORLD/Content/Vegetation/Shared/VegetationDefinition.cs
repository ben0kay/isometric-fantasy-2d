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
    // PL3 will supply this sample from the existing candidate sequence without changing legacy paths.
    // =========================================================
    public int SelectVariant(float seededUnitSample)
    {
        if (VisualVariants.Count == 0 || !float.IsFinite(seededUnitSample) || seededUnitSample < 0 || seededUnitSample >= 1)
            throw new ArgumentException("Vegetation needs artwork and a seeded sample in [0, 1).");
        return Math.Min(VisualVariants.Count - 1, (int)(seededUnitSample * VisualVariants.Count));
    }
}
