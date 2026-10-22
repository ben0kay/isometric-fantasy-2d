// P2 acceptance checks for required placement, exact geometry, permissions, independent seeds and snapshots.
using Godot;
using System;
using System.Linq;
using System.Text.Json;
public partial class ValidateFeatureReservations : Node
{
    private int _checks;
    // Exercise the production data and bounded failure cases; never write profiles or mutate game assets.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"P2 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Required default features must survive every tested seed and stay fully inside their allowed zones.
    // =========================================================
    private void Run()
    {
        var config = GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres");
        var timer = System.Diagnostics.Stopwatch.StartNew(); string previous = null; var anchors = new System.Collections.Generic.HashSet<Vector2>();
        for (int seed = 0; seed < 16; seed++)
        {
            var plan = WorldFeaturePlanner.Build(config, seed);
            Check(plan.Reservations.Features.Count == 2 && plan.StartingArea != null, "Required default feature missing");
            Check(plan.Hash == WorldFeaturePlanner.Build(config, seed).Hash, "Features are nondeterministic");
            var southeast = plan.Reservations.Features.Single(f => f.Id == "feature.southeast_test");
            anchors.Add(southeast.Anchor);
            var request = config.Layout.Features.Single(f => f.Id == southeast.Id);
            var zone = new Rect2(config.Bounds.Position + request.NormalizedZone.Position * config.Bounds.Size, request.NormalizedZone.Size * config.Bounds.Size);
            Check(zone.Encloses(southeast.Bounds.Grow(southeast.Clearance)), "Required footprint escaped its quadrant");
            Check(previous == null || previous != plan.Hash, "Different seeds did not vary plan"); previous = plan.Hash;
            var start = plan.StartingArea;
            Check(!plan.Reservations.AllowsDisk(start.Anchor, 20, WorldPlacementKind.SolidScenery), "Tree overwrote protected start");
            Check(plan.Reservations.AllowsDisk(start.Anchor, 20, WorldPlacementKind.LowVegetation), "Grass incorrectly blocked at start");
            Check(plan.Reservations.AllowsDisk(start.Anchor, 20, WorldPlacementKind.Path), "Starting approach blocked");
            Check(!plan.Reservations.AllowsDisk(start.Anchor, 20, WorldPlacementKind.Water), "Water overwrote start");
            Check(plan.Reservations.AllowsDisk(start.Anchor, 20, WorldPlacementKind.SolidScenery, start.Id), "Owner's authored decoration blocked");
        }
        Check(anchors.Count > 1, "Seeds changed hash but not actual feature positions");
        GD.Print($"P2 16 default seeds plus repeats: {timer.ElapsedMilliseconds}ms");
        using var preset = new WorldLayoutPreset();
        using var local = new WorldGenerationMasterConfig { Bounds = new Rect2(0, 0, 10000, 10000), Layout = preset, Biomes = config.Biomes };
        using var high = Fixed("high", new Vector2(0.5f, 0.5f)); high.Priority = 500; preset.Features.Add(high);
        using var low = Fixed("low", new Vector2(0.5f, 0.5f)); low.Priority = 100; preset.Features.Add(low);
        ExpectInvalid(local, "low");
        low.Required = false;
        var optional = WorldFeaturePlanner.Build(local, 99);
        Check(optional.Reservations.Features.Single().Id == high.Id && optional.Warnings.Count == 1, "Preferred conflict did not skip/warn");
        low.Priority = 9999;
        Check(WorldFeaturePlanner.Build(local, 99).Reservations.Features.Single().Id == high.Id, "Preferred request displaced required request");
        low.Placement = WorldFeaturePlacement.AnywhereInZone; low.NormalizedZone = new Rect2(0.46f,0.46f,0.08f,0.08f); low.Size = new Vector2(100,100);
        Check(WorldFeaturePlanner.Build(local, 99).Warnings.Count == 1, "Blocked broad-zone preferred request did not terminate/skip");
        preset.Features.Remove(low);
        var baseline = WorldFeaturePlanner.Build(local, 99);
        using var unrelated = Fixed("unrelated", new Vector2(0.1f, 0.1f)); preset.Features.Add(unrelated); preset.Features.Reverse();
        var extended = WorldFeaturePlanner.Build(local, 99);
        Check(extended.Reservations.Features.Single(f => f.Id == high.Id).CopyPolygon().SequenceEqual(baseline.Reservations.Features[0].CopyPolygon()), "Unrelated request shifted feature RNG");
        var reordered = WorldFeaturePlanner.Build(local, 99); preset.Features.Reverse();
        Check(reordered.Hash == WorldFeaturePlanner.Build(local, 99).Hash, "Inspector order changed plan");
        string macroHash = MacroWorldBuilder.Build(local, 99).Hash;
        local.Layout = null;
        Check(MacroWorldBuilder.Build(local, 99).Hash == macroHash, "Feature planning changed biome RNG");
        Check(WorldFeaturePlanner.Build(local, 99).Reservations.Features.Count == 0, "Null layout did not preserve P1 mode");
        local.Layout = preset;
        preset.Features.Clear(); preset.Features.Add(high);
        high.NormalizedPolygon = new[] { new Vector2(-0.5f,-0.5f), new Vector2(0.5f,-0.5f), new Vector2(0.5f,-0.1f), new Vector2(-0.1f,-0.1f), new Vector2(-0.1f,0.5f), new Vector2(-0.5f,0.5f) };
        high.Size = new Vector2(2000, 2000); high.Clearance = 100;
        var concave = WorldFeaturePlanner.Build(local, 7);
        Check(concave.Reservations.AllowsDisk(new Vector2(5500,5500), 30, WorldPlacementKind.SolidScenery), "Concave empty corner treated as occupied AABB");
        Check(!concave.Reservations.AllowsDisk(new Vector2(5500,4300), 30, WorldPlacementKind.SolidScenery), "Actual footprint not protected");
        Check(!concave.Reservations.AllowsDisk(new Vector2(4820,5500), 1, WorldPlacementKind.SolidScenery), "Edge clearance ignored");
        Check(!concave.Reservations.AllowsDisk(new Vector2(-1,0), 0, WorldPlacementKind.LowVegetation), "Outside bounds accepted");
        Check(!concave.Reservations.AllowsDisk(new Vector2(float.NaN,0), 0, WorldPlacementKind.LowVegetation), "NaN query accepted");
        var copy = concave.Reservations.Features[0].CopyPolygon(); copy[0] = Vector2.Zero;
        Check(concave.Reservations.Features[0].CopyPolygon()[0] != Vector2.Zero, "Caller mutated frozen geometry");
        // Both shapes share an AABB but the smaller feature fits the concave empty corner.
        using var corner = Fixed("corner", new Vector2(0.55f,0.55f)); corner.Size = new Vector2(100,100); preset.Features.Add(corner);
        Check(WorldFeaturePlanner.Build(local, 7).Reservations.Features.Count == 2, "Exact concave feature placement failed");
        corner.NormalizedAnchor = new Vector2(0.55f,0.43f); ExpectInvalid(local, "corner");
        preset.Features.Remove(corner);
        high.NormalizedPolygon = new[] { new Vector2(-0.5f,-0.5f), new Vector2(0.5f,-0.5f), new Vector2(0.5f,0.5f), new Vector2(-0.5f,0.5f) };
        high.Size = new Vector2(2000,100);
        corner.NormalizedAnchor = new Vector2(0.5f,0.5f); corner.Size = new Vector2(100,2000); preset.Features.Add(corner);
        ExpectInvalid(local, "corner"); // Crossing outlines with all vertices outside the other polygon.
        high.AllowedPlacements = WorldPlacementKind.PointOfInterest; corner.AllowedPlacements = WorldPlacementKind.PointOfInterest;
        Check(WorldFeaturePlanner.Build(local, 7).Reservations.Features.Count == 2, "Mutually permitted overlap blocked");
        corner.AllowedPlacements = WorldPlacementKind.None; ExpectInvalid(local, "corner");
        preset.Features.Remove(corner); high.Size = new Vector2(2000,2000); high.Clearance = 0;
        high.NormalizedZone = new Rect2(0,0,0.01f,0.01f); ExpectInvalid(local, "high");
        high.Required = false;
        Check(WorldFeaturePlanner.Build(local, 7).Warnings.Count == 1, "Impossible preferred zone did not warn");
        high.Required = true; high.NormalizedZone = new Rect2(0,0,1,1); high.NormalizedAnchor = Vector2.Zero; ExpectInvalid(local, "high");
        high.NormalizedAnchor = new Vector2(0.5f,0.5f);
        high.NormalizedPolygon = new[] { new Vector2(-0.5f,-0.5f),new Vector2(0.5f,0.5f),new Vector2(-0.5f,0.5f),new Vector2(0.5f,-0.5f) }; ExpectInvalid(local, "high");
        high.NormalizedPolygon = corner.NormalizedPolygon; high.Size = new Vector2(float.NaN,100); ExpectInvalid(local, "high");
        high.Size = new Vector2(100,100); corner.Id = high.Id; preset.Features.Add(corner); ExpectInvalid(local, "unique");
        preset.Features.Clear(); preset.Features.Add(high); high.IsStartingArea = true; high.Required = false; ExpectInvalid(local, "high");
        high.Required = true;
        var map = MacroWorldBuilder.Build(config, 42); var features = WorldFeaturePlanner.Build(config, 42);
        var snapshot = WorldBlueprintManifest.CreatePreview(map, features);
        var roundtrip = JsonSerializer.Deserialize<WorldBlueprintManifest>(JsonSerializer.Serialize(snapshot));
        Check(roundtrip.Features.Length == 2 && roundtrip.FeatureHash == features.Hash && roundtrip.SchemaVersion == 2, "Feature manifest lost metadata");
        Check(roundtrip.Features[1].PolygonXY.SequenceEqual(snapshot.Features[1].PolygonXY), "Feature geometry did not round-trip");
        snapshot.Features[0].PolygonXY[0] = float.NaN;
        Check(features.StartingArea.CopyPolygon().All(p => p.IsFinite()), "Snapshot changed live reservation geometry");
        Check(WorldFeaturePlanner.Build(config, 42).Hash == features.Hash, "Tests mutated production resources");
    }
    // Small fixed requests make overlap/failure tests deterministic rather than relying on lucky samples.
    // =========================================================
    private static WorldFeatureDirective Fixed(string id, Vector2 anchor) => new() { Id = id, Placement = WorldFeaturePlacement.FixedAnchor, NormalizedAnchor = anchor, Size = new Vector2(1000,1000) };
    // All required failures must explain the affected feature or invalid authoring constraint.
    // =========================================================
    private void ExpectInvalid(WorldGenerationMasterConfig config, string fragment)
    {
        try { WorldFeaturePlanner.Build(config, 1); }
        catch (ArgumentException error) { Check(error.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase), "Failure omitted useful feature/conflict context: " + error.Message); return; }
        throw new Exception("Impossible/invalid required request was silently accepted");
    }
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new Exception(message); }
}
