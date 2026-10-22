// Small two-region composition copied from the debug boundary; not a macro world generator.
using Godot;

[GlobalClass]
public partial class AdjacentBiomeLayout : Resource
{
    [Export] public WorldBiomeDefinition Primary { get; set; }
    [Export] public WorldBiomeDefinition Secondary { get; set; }
    [ExportGroup("Boundary")]
    [Export] public float BoundaryX { get; set; } = 1760f;
    [Export] public float CurveAmplitude { get; set; } = 70f;
    [Export] public float CurveScale { get; set; } = 340f;
    [Export] public float BlendWidth { get; set; } = 240f;

    // Keep the continuous curve identical to AdjacentBiomeBlend.gdshaderinc.
    // =========================================================
    public float EdgeAt(float y)
    {
        float t = y / Mathf.Max(100f, CurveScale);
        return BoundaryX + CurveAmplitude * (Mathf.Sin(t) + 0.35f * Mathf.Sin(t * 2.17f + 1.3f));
    }
    // Pure coordinate lookup needs no loaded scenery or rendering state.
    // =========================================================
    public bool IsSecondary(Vector2 point) => Secondary != null && point.X >= EdgeAt(point.Y);

    // Return a biome from coordinates without loading world actors or terrain.
    // =========================================================
    public WorldBiomeDefinition At(Vector2 point) => IsSecondary(point) ? Secondary : Primary;

    // Restrict bounded sampling to each side's conservative rectangle, then reject by the curve.
    // =========================================================
    public Rect2 SamplingBounds(Rect2 world, bool secondary)
    {
        if (Secondary == null) return world;
        float bend = Mathf.Abs(CurveAmplitude) * 1.35f;
        float left = secondary ? Mathf.Max(world.Position.X, BoundaryX - bend) : world.Position.X;
        float right = secondary ? world.End.X : Mathf.Min(world.End.X, BoundaryX + bend);
        return new Rect2(new Vector2(left, world.Position.Y), new Vector2(Mathf.Max(0f, right - left), world.Size.Y));
    }

    // Trees/tufts reserve blend width plus artwork and worst-case curve change over their height.
    // Shared rocks can remain within the transition, selected by their centre's biome.
    // =========================================================
    public bool Allows(Vector2 point, bool secondary, Vector2 minimum, Vector2 maximum, bool avoidTransition)
    {
        if (Secondary == null) return !secondary;
        if (IsSecondary(point) != secondary) return false;
        if (!avoidTransition) return true;
        float slope = Mathf.Abs(CurveAmplitude) * (1f + 0.35f * 2.17f) / Mathf.Max(100f, CurveScale);
        float margin = Mathf.Max(Mathf.Abs(minimum.X), Mathf.Abs(maximum.X))
            + Mathf.Max(Mathf.Abs(minimum.Y), Mathf.Abs(maximum.Y)) * slope;
        return Mathf.Abs(point.X - EdgeAt(point.Y)) > Mathf.Max(1f, BlendWidth) * 0.5f + margin;
    }
}
