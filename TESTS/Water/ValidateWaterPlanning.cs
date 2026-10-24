// WG1 regression fixture: deterministic outcomes, whole-footprint eligibility and frozen save round trips.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
public partial class ValidateWaterPlanning : Node
{
    private int _checks;
    // Never change production Resources or write player profiles.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"WG1 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Check outcomes through production entry points rather than reproducing planner math.
    // =========================================================
    private void Run()
    {
        var source = GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres");
        var config = (WorldGenerationMasterConfig)source.Duplicate(true);
        config.Water = new WaterGenerationMasterConfig { Enabled=true,LakeCount=3 };
        string previous = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (int seed = 0; seed < 12; seed++)
        {
            var map = MacroWorldBuilder.Build(config,seed); var features = WorldFeaturePlanner.Build(config,seed);
            var pond = new Rect2(650,500,860,600);
            var plan = WaterBodyPlanner.Build(config,map,features,pond);
            Check(plan.Bodies.Count == 3,"Default-size map should find requested lakes");
            Check(plan.Hash == WaterBodyPlanner.Build(config,map,features,pond).Hash,"Same seed changed water");
            Check(previous == null || previous != plan.Hash,"Different seeds did not move geometry"); previous = plan.Hash;
            Check(map.Hash == MacroWorldBuilder.Build(config,seed).Hash,"Water changed biome stream");
            config.Biomes.Reverse();
            Check(plan.Hash == WaterBodyPlanner.Build(config,map,features,pond).Hash,"Resource order changed water");
            config.Biomes.Reverse();
            foreach (var body in plan.Bodies)
            {
                Check(map.Bounds.Encloses(body.Bounds),"Water outside finite bounds");
                Check(!pond.Grow(config.Water.Separation).Intersects(body.Bounds,true),"Water overlaps authored pond");
                foreach (var point in body.CopyPolygon()) Check(features.Reservations.AllowsDisk(point,0,WorldPlacementKind.Water),"Water overwrote reservation");
                var copy = body.CopyPolygon(); copy[0] += Vector2.One*10000;
                Check(!copy.SequenceEqual(body.CopyPolygon()),"Caller mutated frozen geometry");
                var hits = new System.Collections.Generic.List<PlannedWaterBody>(); plan.Query(body.Bounds,hits);
                Check(hits.Contains(body),"Chunk intersection query lost body");
            }
            var json = JsonSerializer.Serialize(plan.Snapshot());
            Check(WaterBodyPlan.Restore(JsonSerializer.Deserialize<WaterPlanSnapshot>(json),seed,map.Bounds).Hash == plan.Hash,"Frozen JSON changed water");
            var bad = plan.Snapshot(); bad.Bodies[0].PolygonXY[0] += 1;
            Reject(() => WaterBodyPlan.Restore(bad,seed,map.Bounds),"Corrupt geometry accepted");
            Reject(() => WaterBodyPlan.Restore(plan.Snapshot() with { Version=99 },seed,map.Bounds),"Unknown version accepted");
            int count = config.Water.LakeCount; config.Water.LakeCount++;
            var more = WaterBodyPlanner.Build(config,map,features,pond);
            Check(plan.Bodies.All(b => more.Bodies.Any(m => m.Id == b.Id && m.CopyPolygon().SequenceEqual(b.CopyPolygon()))),"Adding slot changed existing lakes");
            config.Water.LakeCount=count;
        }
        GD.Print($"WG1 12 seeds, repeat/order/JSON/extension checks: {timer.ElapsedMilliseconds}ms");
        var fixedMap = MacroWorldBuilder.Build(config,42); var fixedFeatures = WorldFeaturePlanner.Build(config,42);
        var persisted = WaterBodyPlanner.Build(config,fixedMap,fixedFeatures);
        var manifest = WorldBlueprintManifest.CreatePreview(fixedMap,fixedFeatures) with {
            SchemaVersion=4, Water=persisted.Snapshot(), LocalGeneratorVersion=ChunkSceneryData.GeneratorVersion,
            ChunkSize=512, CandidateCellSize=96,
            MacroRegions=fixedMap.Regions.Select(v => new SavedMacroRegion(v.Id,v.BiomeIndex,v.Centre.X,v.Centre.Y,v.TargetDiameter,v.CellCount)).ToArray(),
            MacroBiomes=fixedMap.Biomes.Select(v => new SavedMacroBiome(v.Id,v.Name,v.Colour.R,v.Colour.G,v.Colour.B,v.Colour.A)).ToArray() };
        var root=Path.Combine(Path.GetTempPath(),"wg1-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new CampaignStore(root);
            var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,manifest.ProfileId,manifest.RunId,manifest.WorldId,
                new string('A',64),DateTime.UtcNow.ToString("O"),manifest,new SavedPlayer(0,0,Health:100),
                new SavedClock(12,600,true),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
            store.Save(snapshot,true);
            var loaded=store.Load(manifest.ProfileId).Snapshot;
            Check(WaterBodyPlan.Restore(loaded.Blueprint.Water,42,fixedMap.Bounds).Hash==persisted.Hash,"Disk checkpoint lost generated water");
            var corrupt=persisted.Snapshot(); corrupt.Bodies[0].PolygonXY[0]+=1;
            Reject(() => store.Validate(snapshot with { Blueprint=manifest with { Water=corrupt } }),"Store accepted corrupt water");
            store.Save(snapshot with { Blueprint=manifest with { Water=null } });
            Check(store.Load(manifest.ProfileId).Snapshot.Blueprint.Water==null,"Legacy empty water did not round-trip");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
        foreach (var biome in config.Biomes) biome.Hydrology = new BiomeHydrologyProfile { AllowFreshwaterLakes=false };
        var forbidden = WaterBodyPlanner.Build(config,fixedMap,fixedFeatures);
        Check(forbidden.Bodies.Count == 0 && forbidden.Warnings.Count == 3,"Hard exclusions failed or skip was silent");
        foreach (var biome in config.Biomes) biome.Hydrology = new BiomeHydrologyProfile { LakeSuitability=0 };
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Count == 0,"Zero suitability accepted water");
        config.Water.Enabled=false;
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Count == 0,"Disabled water planned geometry");
        config.Water=null;
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Count == 0,"Null config compatibility failed");
        Check(WaterBodyPlan.Restore(null,42,fixedMap.Bounds).Bodies.Count == 0,"Old save acquired generated water");
        config.Water=new WaterGenerationMasterConfig { Enabled=true,MinimumDiameter=float.NaN };
        Reject(() => WaterBodyPlanner.Build(config,fixedMap,fixedFeatures),"NaN config accepted");
        // Interior forbidden cell surrounded by permissive cells must still reject a containing lake.
        var a = new WorldBiomeDefinition { Id="land",Hydrology=null };
        var b = new WorldBiomeDefinition { Id="island",Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false } };
        config.Biomes = new() { a,b }; config.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=1,MinimumDiameter=1500,MaximumDiameter=1500,Separation=0,AttemptsPerLake=128,Irregularity=0 };
        var cells = new int[64]; cells[27]=1;
        var islandMap = new MacroWorldMap(7,new Rect2(0,0,2000,2000),250,8,8,cells,
            new[] { new MacroBiomeInfo("land","",Colors.Green),new MacroBiomeInfo("island","",Colors.Red) },
            new[] { new MacroRegion(0,0,new Vector2(1000,1000),2000,63),new MacroRegion(1,1,new Vector2(875,875),250,1) });
        var emptyFeatures = new WorldFeaturePlan(7,islandMap.Bounds,Array.Empty<WorldFeatureReservation>(),Array.Empty<string>());
        Check(WaterBodyPlanner.Build(config,islandMap,emptyFeatures).Bodies.Count==0,"Interior forbidden biome was ignored");
        config.Biomes=new() { a }; config.Bounds=new Rect2(0,0,5000,5000); config.Layout=null;
        config.Water=new WaterGenerationMasterConfig { Enabled=true };
        var one=MacroWorldBuilder.Build(config,9);
        Check(WaterBodyPlanner.Build(config,one,WorldFeaturePlanner.Build(config,9)).Bodies.Count==1,"One-biome/null profile failed");
    }
    // Report a useful invariant on the first failed case.
    // =========================================================
    private void Check(bool value,string message) { _checks++; if (!value) throw new Exception(message); }
    // Both malformed authoring and corrupt disk input must fail explicitly.
    // =========================================================
    private void Reject(Action action,string message)
    {
        bool rejected=false;
        try { action(); } catch (ArgumentException) { rejected=true; } catch (InvalidDataException) { rejected=true; }
        Check(rejected,message);
    }
}
