// Frozen macro water geometry. No Nodes/Resources or mutable array references escape into runtime queries.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public sealed record WaterBodySnapshot(string Id, string LiquidId, float[] PolygonXY);
public sealed record WaterPlanSnapshot(int Version, int Seed, string Hash, WaterBodySnapshot[] Bodies);
public sealed class PlannedWaterBody
{
    private readonly Vector2[] _polygon;
    public string Id { get; }
    public string LiquidId { get; }
    public Rect2 Bounds { get; }
    // Validate simple polygons before accepting generated or disk-supplied geometry.
    // =========================================================
    internal PlannedWaterBody(string id, string liquidId, Vector2[] polygon)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || string.IsNullOrWhiteSpace(liquidId) || liquidId.Length > 128
            || polygon == null || polygon.Length < 3 || polygon.Length > 64 || polygon.Any(p => !p.IsFinite()))
            throw new ArgumentException("Invalid water body identity/geometry.");
        Bounds = FeatureGeometry.Bounds(polygon);
        float extent = Mathf.Max(Bounds.Size.X, Bounds.Size.Y);
        if (!float.IsFinite(extent) || extent < 1f) throw new ArgumentException("Degenerate water geometry.");
        FeatureGeometry.Validate(polygon.Select(p => (p - Bounds.GetCenter()) / (extent * 1.001f)).ToArray(), id);
        Id = id; LiquidId = liquidId; _polygon = (Vector2[])polygon.Clone();
    }
    public Vector2[] CopyPolygon() => (Vector2[])_polygon.Clone();
    // Bounded, allocation-free footprint query; positive distance means underwater.
    // =========================================================
    public float SignedDistance(Vector2 point)
    {
        if (!point.IsFinite()) return float.NegativeInfinity;
        float minimum = float.MaxValue;
        for (int i = 0; i < _polygon.Length; i++)
            minimum = Mathf.Min(minimum, point.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(point, _polygon[i], _polygon[(i+1)%_polygon.Length])));
        return Mathf.Sqrt(minimum) * (Geometry2D.IsPointInPolygon(point, _polygon) ? 1f : -1f);
    }
    // Serialize detached values; snapshot mutation cannot change the active plan.
    // =========================================================
    internal WaterBodySnapshot Snapshot() => new(Id, LiquidId, _polygon.SelectMany(p => new[] { p.X,p.Y }).ToArray());
}
public sealed class WaterBodyPlan
{
    public const int Version = 1;
    public int Seed { get; }
    public string Hash { get; }
    public IReadOnlyList<PlannedWaterBody> Bodies { get; }
    public IReadOnlyList<string> Warnings { get; }
    // Hash exact outcomes and world ownership independently from biome/feature streams.
    // =========================================================
    internal WaterBodyPlan(int seed, Rect2 bounds, IEnumerable<PlannedWaterBody> bodies, IEnumerable<string> warnings)
    {
        Seed = seed; Bodies = Array.AsReadOnly(bodies.ToArray()); Warnings = Array.AsReadOnly(warnings.ToArray());
        if (Bodies.Count > 32 || Bodies.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != Bodies.Count
            || Bodies.Any(b => !bounds.Encloses(b.Bounds))) throw new ArgumentException("Invalid frozen water plan.");
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        writer.Write(Version); writer.Write(seed); writer.Write(bounds.Position.X); writer.Write(bounds.Position.Y);
        writer.Write(bounds.Size.X); writer.Write(bounds.Size.Y); writer.Write(Bodies.Count);
        foreach (var body in Bodies)
        {
            var snapshot = body.Snapshot(); writer.Write(snapshot.Id); writer.Write(snapshot.LiquidId); writer.Write(snapshot.PolygonXY.Length);
            foreach (float value in snapshot.PolygonXY) writer.Write(value);
        }
        writer.Flush(); Hash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
    // Caller-owned destination list avoids allocation during future chunk intersection queries.
    // =========================================================
    public void Query(Rect2 bounds, List<PlannedWaterBody> destination)
    {
        foreach (var body in Bodies) if (body.Bounds.Intersects(bounds, true)) destination.Add(body);
    }
    public WaterPlanSnapshot Snapshot() => new(Version, Seed, Hash, Bodies.Select(b => b.Snapshot()).ToArray());
    // Missing optional data means an old world with NO generated water; never rerun authoring settings.
    // =========================================================
    public static WaterBodyPlan Restore(WaterPlanSnapshot snapshot, int seed, Rect2 bounds)
    {
        if (snapshot == null) return new(seed, bounds, Array.Empty<PlannedWaterBody>(), Array.Empty<string>());
        if (snapshot.Version != Version || snapshot.Seed != seed || snapshot.Bodies == null || snapshot.Bodies.Length > 32)
            throw new InvalidDataException("Unsupported/invalid frozen water plan.");
        try
        {
            var bodies = snapshot.Bodies.Select(b =>
            {
                if (b == null || b.PolygonXY == null || b.PolygonXY.Length < 6 || b.PolygonXY.Length > 128 || b.PolygonXY.Length%2 != 0)
                    throw new ArgumentException("Invalid water snapshot.");
                var polygon = new Vector2[b.PolygonXY.Length/2];
                for (int i = 0; i < polygon.Length; i++) polygon[i] = new(b.PolygonXY[i*2],b.PolygonXY[i*2+1]);
                return new PlannedWaterBody(b.Id,b.LiquidId,polygon);
            });
            var plan = new WaterBodyPlan(seed,bounds,bodies,Array.Empty<string>());
            if (plan.Hash != snapshot.Hash) throw new InvalidDataException("Frozen water hash mismatch.");
            return plan;
        }
        catch (ArgumentException error) { throw new InvalidDataException("Invalid frozen water geometry.",error); }
    }
}
