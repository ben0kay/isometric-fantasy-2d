// Frozen exact polygon reservations with category permissions; usable without loading nearby chunks.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class WorldFeatureReservation
{
    private readonly Vector2[] _polygon;
    public string Id { get; }
    public string Name { get; }
    public bool Required { get; }
    public int Priority { get; }
    public bool IsStartingArea { get; }
    public Vector2 Anchor { get; }
    public float Clearance { get; }
    public WorldPlacementKind Kind { get; }
    public WorldPlacementKind AllowedPlacements { get; }
    public Color Colour { get; }
    public Rect2 Bounds { get; }

    // Copy authoring data and geometry; editing a Resource never moves an already planned feature.
    // =========================================================
    internal WorldFeatureReservation(WorldFeatureDirective request, Vector2 anchor, Vector2[] polygon)
    {
        Id = request.Id; Name = request.DisplayName; Required = request.Required; Priority = request.Priority;
        IsStartingArea = request.IsStartingArea; Anchor = anchor; Clearance = request.Clearance;
        Kind = request.Kind; AllowedPlacements = request.AllowedPlacements; Colour = request.OverviewColour;
        _polygon = (Vector2[])polygon.Clone(); Bounds = FeatureGeometry.Bounds(_polygon);
    }
    // Rehydrate frozen values without a mutable authoring Resource or new placement decisions.
    // =========================================================
    internal WorldFeatureReservation(WorldFeatureSnapshot f)
    {
        Id = f.Id; Name = f.Id; Required = f.Required; Priority = f.Priority; IsStartingArea = f.IsStartingArea;
        Anchor = new(f.AnchorX,f.AnchorY); Clearance = f.Clearance; Kind = (WorldPlacementKind)f.Kind;
        AllowedPlacements = (WorldPlacementKind)f.AllowedPlacements; Colour = new Color(1,0.75f,0.15f);
        _polygon = new Vector2[f.PolygonXY.Length/2];
        for (int i = 0; i < _polygon.Length; i++) _polygon[i] = new(f.PolygonXY[i*2],f.PolygonXY[i*2+1]);
        Bounds = FeatureGeometry.Bounds(_polygon);
    }
    public Vector2[] CopyPolygon() => (Vector2[])_polygon.Clone();

    // Cheap bounding rejection precedes precise concave-polygon and edge-distance checks.
    // =========================================================
    public bool OverlapsDisk(Vector2 centre, float radius)
    {
        if (!centre.IsFinite() || !float.IsFinite(radius) || radius < 0) return false;
        float extent = radius + Clearance;
        var b = Bounds.Grow(extent);
        if (centre.X < b.Position.X || centre.Y < b.Position.Y || centre.X > b.End.X || centre.Y > b.End.Y) return false;
        if (Geometry2D.IsPointInPolygon(centre, _polygon)) return true;
        for (int i = 0; i < _polygon.Length; i++)
            if (centre.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(centre, _polygon[i], _polygon[(i + 1) % _polygon.Length])) <= extent * extent) return true;
        return false;
    }
    // Clearance applies around real edges, not the entire bounding rectangle.
    // =========================================================
    public bool Overlaps(WorldFeatureReservation other) => FeatureGeometry.Overlaps(_polygon, Bounds, Clearance, other._polygon, other.Bounds, other.Clearance);
}

public sealed class WorldReservationMap
{
    public Rect2 Bounds { get; }
    public IReadOnlyList<WorldFeatureReservation> Features { get; }
    internal WorldReservationMap(Rect2 bounds, IEnumerable<WorldFeatureReservation> features)
    { Bounds = bounds; Features = Array.AsReadOnly(features.ToArray()); }

    // Own authored decorations may opt into their reservation; unrelated owners obey every permission.
    // =========================================================
    public bool AllowsDisk(Vector2 centre, float radius, WorldPlacementKind kind, string ownerFeatureId = null)
    {
        if (!centre.IsFinite() || !float.IsFinite(radius) || radius < 0 || kind == WorldPlacementKind.None
            || (kind & ~WorldPlacementKind.All) != 0 || !Bounds.HasPoint(centre)
            || centre.X - radius < Bounds.Position.X || centre.Y - radius < Bounds.Position.Y
            || centre.X + radius > Bounds.End.X || centre.Y + radius > Bounds.End.Y) return false;
        foreach (var feature in Features)
            if (feature.Id != ownerFeatureId && (feature.AllowedPlacements & kind) != kind && feature.OverlapsDisk(centre, radius)) return false;
        return true;
    }
    // Symmetric compatibility prevents a later footprint from excluding an earlier feature's own kind.
    // =========================================================
    internal static string Conflict(WorldFeatureReservation candidate, IEnumerable<WorldFeatureReservation> placed)
    {
        foreach (var other in placed)
            if (((other.AllowedPlacements & candidate.Kind) != candidate.Kind || (candidate.AllowedPlacements & other.Kind) != other.Kind)
                && candidate.Overlaps(other)) return other.Id;
        return null;
    }
}

internal static class FeatureGeometry
{
    // Preserve arbitrary concave outlines instead of reserving their rectangular empty corners.
    // =========================================================
    internal static Rect2 Bounds(Vector2[] polygon)
    {
        var bounds = new Rect2(polygon[0], Vector2.Zero);
        foreach (var p in polygon) bounds = bounds.Expand(p);
        return bounds;
    }
    // Reject degenerate/self-crossing normalized footprints at authoring boundaries.
    // =========================================================
    internal static void Validate(Vector2[] polygon, string id,int maximumVertices=64)
    {
        if (polygon == null || polygon.Length < 3 || polygon.Length > maximumVertices) throw new ArgumentException($"Feature {id}: polygon needs 3–{maximumVertices} vertices.");
        double area = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
            if (!a.IsFinite() || Math.Abs(a.X) > 0.5f || Math.Abs(a.Y) > 0.5f || a == b) throw new ArgumentException($"Feature {id}: invalid normalized polygon vertex.");
            area += (double)a.X * b.Y - (double)b.X * a.Y;
            for (int j = i + 2; j < polygon.Length; j++)
            {
                if ((j + 1) % polygon.Length == i) continue;
                if (!new Rect2(a,b-a).Abs().Grow(0.000001f).Intersects(new Rect2(polygon[j],polygon[(j+1)%polygon.Length]-polygon[j]).Abs(),true)) continue;
                if (SegmentDistanceSquared(a, b, polygon[j], polygon[(j + 1) % polygon.Length]) <= 1e-12)
                    throw new ArgumentException($"Feature {id}: polygon crosses or touches itself.");
            }
        }
        if (Math.Abs(area) < 1e-8) throw new ArgumentException($"Feature {id}: polygon has no area.");
    }
    // Disk/edge separation also detects contact and concave gaps that AABB overlap would misclassify.
    // =========================================================
    internal static bool Overlaps(Vector2[] a, Rect2 aBounds, float aClearance, Vector2[] b, Rect2 bBounds, float bClearance)
    {
        float clearance = aClearance + bClearance;
        if (!aBounds.Grow(clearance).Intersects(bBounds, true)) return false;
        if (Geometry2D.IsPointInPolygon(a[0], b) || Geometry2D.IsPointInPolygon(b[0], a)) return true;
        for (int i = 0; i < a.Length; i++) for (int j = 0; j < b.Length; j++)
            if (SegmentDistanceSquared(a[i], a[(i + 1) % a.Length], b[j], b[(j + 1) % b.Length]) <= (double)clearance * clearance) return true;
        return false;
    }
    // Crossing segments have zero separation; otherwise the closest pair contains an endpoint.
    // =========================================================
    private static double SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        double abC = Cross(b - a, c - a), abD = Cross(b - a, d - a), cdA = Cross(d - c, a - c), cdB = Cross(d - c, b - c);
        if (((abC > 0 && abD < 0) || (abC < 0 && abD > 0)) && ((cdA > 0 && cdB < 0) || (cdA < 0 && cdB > 0))) return 0;
        return Math.Min(Math.Min(a.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(a, c, d)), b.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(b, c, d))),
            Math.Min(c.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(c, a, b)), d.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(d, a, b))));
    }
    private static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;
}
