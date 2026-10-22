// Frozen feature outcomes and deterministic hash; carries reservations into future chunk/save consumers.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public sealed class WorldFeaturePlan
{
    public const int Version = 1;
    public int Seed { get; }
    public WorldReservationMap Reservations { get; }
    public IReadOnlyList<string> Warnings { get; }
    public WorldFeatureReservation StartingArea => Reservations.Features.FirstOrDefault(f => f.IsStartingArea);
    public string Hash { get; }

    // Hash placed geometry and permissions, independently of biome RNG or display colours/names.
    // =========================================================
    internal WorldFeaturePlan(int seed, Rect2 bounds, IEnumerable<WorldFeatureReservation> features, IEnumerable<string> warnings)
    {
        Seed = seed; Reservations = new WorldReservationMap(bounds, features); Warnings = Array.AsReadOnly(warnings.ToArray());
        using var stream = new System.IO.MemoryStream(); using var writer = new System.IO.BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(Version); writer.Write(seed); writer.Write(bounds.Position.X); writer.Write(bounds.Position.Y); writer.Write(bounds.Size.X); writer.Write(bounds.Size.Y);
        writer.Write(Reservations.Features.Count);
        foreach (var f in Reservations.Features)
        {
            writer.Write(f.Id); writer.Write(f.Required); writer.Write(f.Priority); writer.Write(f.IsStartingArea);
            writer.Write(f.Anchor.X); writer.Write(f.Anchor.Y); writer.Write(f.Clearance); writer.Write((int)f.Kind); writer.Write((int)f.AllowedPlacements);
            var polygon = f.CopyPolygon(); writer.Write(polygon.Length);
            foreach (var p in polygon) { writer.Write(p.X); writer.Write(p.Y); }
        }
        writer.Flush(); Hash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
