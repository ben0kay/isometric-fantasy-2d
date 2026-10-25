// Headless P1 acceptance checks: coverage, connectivity, determinism, authoring and world isolation.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
public partial class ValidateMacroGeneration : Node
{
    private int _checks;
    // Run with the companion scene; any failed invariant returns a nonzero process exit code.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"P1 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Exercise authored resources and temporary additions without modifying the content catalog.
    // =========================================================
    private void Run()
    {
        var config = (WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres").Duplicate(true);
        config.Bounds=new Rect2(-22500,-16875,45000,33750); config.CellSize=256;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string previous = null;
        for (int seed = 0; seed < 16; seed++)
        {
            var map = MacroWorldBuilder.Build(config, seed); Validate(map);
            Check(map.Hash == MacroWorldBuilder.Build(config, seed).Hash, "Repeated seed/settings changed map");
            Check(map.Hash != previous, "Different seeds did not vary"); previous = map.Hash;
        }
        GD.Print($"16 default seeds + repeats: {watch.ElapsedMilliseconds}ms");
        var original = MacroWorldBuilder.Build(config, 123);
        using var editable = new WorldGenerationMasterConfig { Bounds = new Rect2(-3500, -2700, 12000, 9000), CellSize = 256 };
        using var a = new WorldBiomeDefinition { Id = "a", MinimumRegionDiameter = 1200, MaximumRegionDiameter = 2400 };
        using var b = new WorldBiomeDefinition { Id = "b", MinimumRegionDiameter = 1200, MaximumRegionDiameter = 2400 };
        using var c = new WorldBiomeDefinition { Id = "c", MinimumRegionDiameter = 1200, MaximumRegionDiameter = 2400 };
        using var d = new WorldBiomeDefinition { Id = "d", MinimumRegionDiameter = 1200, MaximumRegionDiameter = 2400 };
        editable.Biomes.Add(a); Validate(MacroWorldBuilder.Build(editable, -42));
        editable.Biomes.Add(b); editable.Biomes.Add(c); editable.Biomes.Add(d);
        var four = MacroWorldBuilder.Build(editable, 123); Validate(four);
        Check(four.Regions.Select(r => four.Biomes[r.BiomeIndex].Id).Distinct().Count() == 4, "Four-biome pool did not work");
        editable.Biomes.Reverse(); Check(four.Hash == MacroWorldBuilder.Build(editable, 123).Hash, "List order changed generation");
        b.GenerationWeight = c.GenerationWeight = d.GenerationWeight = 0;
        var weighted = MacroWorldBuilder.Build(editable, 123);
        Check(weighted.Regions.All(r => weighted.Biomes[r.BiomeIndex].Id == "a"), "Zero-weight biome spawned");
        int smallRegions = weighted.Regions.Count;
        a.MinimumRegionDiameter = 3000; a.MaximumRegionDiameter = 4000;
        Check(MacroWorldBuilder.Build(editable, 123).Regions.Count < smallRegions, "Diameter did not affect region count");
        a.GenerationWeight = 0; ExpectInvalid(editable);
        a.GenerationWeight = float.NaN; ExpectInvalid(editable);
        a.GenerationWeight = 1; a.MaximumRegionDiameter = 1; ExpectInvalid(editable);
        a.MaximumRegionDiameter = 4000;
        editable.Biomes.Add(null); ExpectInvalid(editable); editable.Biomes.RemoveAt(editable.Biomes.Count - 1);
        b.Id = "a"; ExpectInvalid(editable); b.Id = "b";
        a.EnabledForGeneration = b.EnabledForGeneration = c.EnabledForGeneration = d.EnabledForGeneration = false; ExpectInvalid(editable);
        a.EnabledForGeneration = true; editable.MaximumCells = 1; ExpectInvalid(editable);
        editable.MaximumCells = 262144; editable.MaximumRegions = 1; ExpectInvalid(editable);
        editable.MaximumRegions = 4096; editable.Bounds = new Rect2(-7, -5, 10, 8); Validate(MacroWorldBuilder.Build(editable, int.MinValue));
        Check(MacroWorldBuilder.Build(config, 123).Hash == original.Hash, "Test authoring changed original resource");
        string profile = Guid.NewGuid().ToString("N"), run = Guid.NewGuid().ToString("N");
        var first = WorldBlueprintManifest.Create(profile, run, original);
        var second = WorldBlueprintManifest.Create(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), original);
        Check(first.WorldId != second.WorldId && first.MapHash == second.MapHash, "Same seed merged world instances");
        var restored = JsonSerializer.Deserialize<WorldBlueprintManifest>(JsonSerializer.Serialize(first));
        Check(restored.ProfileId == profile && restored.RunId == run && restored.Seed == 123
            && restored.RegionCells.SequenceEqual(first.RegionCells), "Manifest contract did not round-trip");
        first.RegionCells[0] = -1; Check(original.RegionIdAtCell(0, 0) >= 0, "Manifest mutated frozen geography");
        GD.Print($"Default map {original.Columns}x{original.Rows}, {original.Regions.Count} regions; hash {original.Hash}");
    }
    // Traverse each region independently to detect missing, disconnected or misidentified cells.
    // =========================================================
    private void Validate(MacroWorldMap map)
    {
        foreach (var region in map.Regions) Check(map.RegionAt(region.Centre)?.Id == region.Id, "Region centre is outside its owned cells");
        int[] counts = new int[map.Regions.Count];
        for (int y = 0; y < map.Rows; y++) for (int x = 0; x < map.Columns; x++)
        {
            int id = map.RegionIdAtCell(x, y); Check(id >= 0 && id < counts.Length, "Unassigned cell"); counts[id]++;
        }
        var visited = new HashSet<int>(); var queue = new Queue<int>();
        for (int index = 0; index < map.Columns * map.Rows; index++)
        {
            if (!visited.Add(index)) continue;
            int region = map.RegionIdAtCell(index % map.Columns, index / map.Columns), reached = 0; queue.Enqueue(index);
            while (queue.TryDequeue(out int cell))
            {
                reached++; int x = cell % map.Columns, y = cell / map.Columns;
                Add(x - 1, y); Add(x + 1, y); Add(x, y - 1); Add(x, y + 1);
                void Add(int nx, int ny)
                {
                    if (map.RegionIdAtCell(nx, ny) != region) return;
                    int next = ny * map.Columns + nx; if (visited.Add(next)) queue.Enqueue(next);
                }
            }
            Check(reached == counts[region] && reached == map.Regions[region].CellCount, "Disconnected region or bad area count");
        }
        Check(map.BiomeAt(map.Bounds.Position) != null && map.BiomeAt(map.Bounds.End - Vector2.One * 0.01f) != null, "Clipped map edge missing");
        Check(map.BiomeAt(map.Bounds.End) == null && map.BiomeAt(map.Bounds.Position - Vector2.One) == null, "Out-of-bounds accepted");
    }
    // Invalid authoring must fail clearly and quickly, rather than silently inventing a fallback biome.
    // =========================================================
    private void ExpectInvalid(WorldGenerationMasterConfig config)
    {
        try { MacroWorldBuilder.Build(config, 1); } catch (ArgumentException) { _checks++; return; }
        throw new Exception("Invalid configuration accepted");
    }
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new Exception(message); }
}
