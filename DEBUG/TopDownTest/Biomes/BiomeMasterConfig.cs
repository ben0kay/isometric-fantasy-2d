// Two adjacent test regions share one curved boundary for drawing and placement.
using Godot;

[GlobalClass]
public partial class BiomeMasterConfig : Resource
{
    [Export] public BiomeDefinition West { get; set; }
    [Export] public BiomeDefinition East { get; set; }
    [Export] public float BoundaryX { get; set; } = 1760f;
    [Export(PropertyHint.Range, "0,160,1")] public float CurveAmplitude { get; set; } = 70f;
    [Export(PropertyHint.Range, "100,1000,10")] public float CurveScale { get; set; } = 340f;
    [Export(PropertyHint.Range, "1,480,1")] public float BlendWidth { get; set; } = 240f;

    // Return the definition of the side containing this point, without allocation.
    // =========================================================
    public BiomeDefinition At(Vector2 point) => point.X < EdgeAt(point.Y) ? West : East;

    // Keep this analytical curve identical to BiomeBlend.gdshaderinc; no sampled grid edges.
    // =========================================================
    public float EdgeAt(float y) => BoundaryX + CurveAmplitude *
        (Mathf.Sin(y / Mathf.Max(100f, CurveScale)) + 0.35f * Mathf.Sin(y / Mathf.Max(100f, CurveScale) * 2.17f + 1.3f));

    // Reserve the full transition plus sprite width so oak foliage never enters pink ground.
    // =========================================================
    public bool Allows(Vector2 point, bool tree, bool grass, float margin = 0f)
    {
        float distance = point.X - EdgeAt(point.Y);
        BiomeDefinition definition = distance < 0f ? West : East;
        if (definition == null) return true;
        bool allowed = grass ? definition.AllowGrassTufts : tree ? definition.AllowTrees : definition.AllowRocks;
        if (!allowed) return false;
        BiomeDefinition other = distance < 0f ? East : West;
        bool otherAllowed = other == null || (grass ? other.AllowGrassTufts : tree ? other.AllowTrees : other.AllowRocks);
        return otherAllowed || Mathf.Abs(distance) > Mathf.Max(1f, BlendWidth) * 0.5f + margin;
    }
}
