// Detached feature geometry/permission values for future SAVE serialization; no live Resource references.
using System.Linq;

public sealed record WorldFeatureSnapshot(string Id, bool Required, int Priority, bool IsStartingArea,
    float AnchorX, float AnchorY, float Clearance, int Kind, int AllowedPlacements, float[] PolygonXY)
{
    // Preserve exact placed outcomes instead of rolling directives again when a saved world resumes.
    // =========================================================
    public static WorldFeatureSnapshot From(WorldFeatureReservation feature) => new(feature.Id, feature.Required, feature.Priority,
        feature.IsStartingArea, feature.Anchor.X, feature.Anchor.Y, feature.Clearance, (int)feature.Kind, (int)feature.AllowedPlacements,
        feature.CopyPolygon().SelectMany(p => new[] { p.X, p.Y }).ToArray());
}
