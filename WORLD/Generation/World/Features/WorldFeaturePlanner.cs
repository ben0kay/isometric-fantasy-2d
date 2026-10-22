// Plan mandatory then preferred major footprints, with bounded retries and independent feature RNG streams.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class WorldFeaturePlanner
{
    #region Planning
    // Plan before random scenery/terrain application; never modify source Resources or partial runtime state.
    // =========================================================
    public static WorldFeaturePlan Build(WorldGenerationMasterConfig config, int seed)
    {
        if (config == null || !config.Bounds.Position.IsFinite() || !config.Bounds.Size.IsFinite() || !config.Bounds.End.IsFinite()
            || config.Bounds.Size.X <= 0 || config.Bounds.Size.Y <= 0) throw new ArgumentException("Features require finite positive world bounds.");
        var preset = config.Layout;
        if (preset == null) return new WorldFeaturePlan(seed, config.Bounds, Array.Empty<WorldFeatureReservation>(), Array.Empty<string>());
        if (preset.Features == null || preset.Features.Any(f => f == null) || preset.MaximumFeatures < 1 || preset.MaximumFeatures > 256
            || preset.Features.Count > preset.MaximumFeatures || preset.AttemptsPerFeature < 1 || preset.AttemptsPerFeature > 256)
            throw new ArgumentException("Layout requires valid feature references and bounded feature/attempt limits.");
        var requests = preset.Features.Where(f => f.Enabled).OrderByDescending(f => f.Required).ThenByDescending(f => f.Priority).ThenBy(f => f.Id, StringComparer.Ordinal).ToArray();
        if (requests.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != requests.Length) throw new ArgumentException("Enabled feature IDs must be unique.");
        if (requests.Count(f => f.IsStartingArea) > 1) throw new ArgumentException("Only one feature may designate the player starting area.");
        foreach (var request in requests) Validate(request);
        var placed = new List<WorldFeatureReservation>(); var warnings = new List<string>();
        foreach (var request in requests)
        {
            var random = new FeatureRandom(seed, request.Id);
            float scale = request.MinimumScale + (float)random.Unit() * (request.MaximumScale - request.MinimumScale);
            Vector2[] offsets = request.NormalizedPolygon.Select(p => p * request.Size * scale).ToArray();
            Rect2 footprint = FeatureGeometry.Bounds(offsets).Grow(request.Clearance);
            Rect2 zone = new(config.Bounds.Position + request.NormalizedZone.Position * config.Bounds.Size, request.NormalizedZone.Size * config.Bounds.Size);
            Vector2 minimum = zone.Position - footprint.Position, maximum = zone.End - footprint.End;
            string reason = "footprint and clearance do not fit the allowed zone"; bool success = false; int tried = 0;
            if (minimum.X <= maximum.X && minimum.Y <= maximum.Y)
            {
                int attempts = request.Placement == WorldFeaturePlacement.FixedAnchor ? 1 : preset.AttemptsPerFeature;
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    tried++;
                    Vector2 anchor = request.Placement == WorldFeaturePlacement.FixedAnchor
                        ? config.Bounds.Position + request.NormalizedAnchor * config.Bounds.Size
                        : new Vector2(Mathf.Lerp(minimum.X, maximum.X, (float)random.Unit()), Mathf.Lerp(minimum.Y, maximum.Y, (float)random.Unit()));
                    if (anchor.X < minimum.X || anchor.Y < minimum.Y || anchor.X > maximum.X || anchor.Y > maximum.Y)
                    { reason = "fixed anchor places the footprint/clearance outside its allowed zone"; break; }
                    var polygon = offsets.Select(p => anchor + p).ToArray();
                    var candidate = new WorldFeatureReservation(request, anchor, polygon);
                    string conflict = WorldReservationMap.Conflict(candidate, placed);
                    if (conflict != null) { reason = $"conflicts with reserved feature '{conflict}'"; continue; }
                    placed.Add(candidate); success = true; break;
                }
            }
            if (success) continue;
            string message = $"Feature '{request.Id}' could not be placed: {reason} (attempts {tried}/{preset.AttemptsPerFeature}).";
            if (request.Required) throw new ArgumentException(message);
            warnings.Add(message + " Preferred request skipped.");
        }
        return new WorldFeaturePlan(seed, config.Bounds, placed, warnings);
    }
    // Malformed authoring fails even for preferred requests; only valid but infeasible requests may skip.
    // =========================================================
    private static void Validate(WorldFeatureDirective f)
    {
        var zone = f.NormalizedZone;
        if (string.IsNullOrWhiteSpace(f.Id) || !zone.Position.IsFinite() || !zone.Size.IsFinite() || zone.Size.X <= 0 || zone.Size.Y <= 0
            || zone.Position.X < 0 || zone.Position.Y < 0 || zone.End.X > 1 || zone.End.Y > 1
            || !f.NormalizedAnchor.IsFinite() || f.NormalizedAnchor.X < 0 || f.NormalizedAnchor.Y < 0 || f.NormalizedAnchor.X > 1 || f.NormalizedAnchor.Y > 1
            || !f.Size.IsFinite() || f.Size.X <= 0 || f.Size.Y <= 0 || !float.IsFinite(f.MinimumScale) || !float.IsFinite(f.MaximumScale)
            || f.MinimumScale <= 0 || f.MaximumScale < f.MinimumScale || !(f.Size * f.MaximumScale).IsFinite()
            || !float.IsFinite(f.Clearance) || f.Clearance < 0 || f.Kind == WorldPlacementKind.None || (f.Kind & ~WorldPlacementKind.All) != 0
            || (f.AllowedPlacements & ~WorldPlacementKind.All) != 0 || !Enum.IsDefined(f.Placement) || (f.IsStartingArea && !f.Required))
            throw new ArgumentException($"Feature '{f.Id}': invalid identity, normalized placement, size, permissions or starting-area policy.");
        FeatureGeometry.Validate(f.NormalizedPolygon, f.Id);
    }
    #endregion

    #region Deterministic feature randomness
    // Feature ID, seed and planner version key an explicit stream; unrelated requests consume no draws.
    // =========================================================
    private sealed class FeatureRandom
    {
        private uint _state;
        public FeatureRandom(int seed, string id)
        {
            using var stream = new System.IO.MemoryStream(); using var writer = new System.IO.BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(seed); writer.Write(WorldFeaturePlan.Version); writer.Write("world.features"); writer.Write(id); writer.Flush();
            _state = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(stream.ToArray()));
        }
        public double Unit()
        {
            unchecked { _state += 0x9e3779b9u; uint v = _state; v ^= v >> 16; v *= 0x7feb352du; v ^= v >> 15; v *= 0x846ca68bu; return (v ^ (v >> 16)) / 4294967296.0; }
        }
    }
    #endregion
}
