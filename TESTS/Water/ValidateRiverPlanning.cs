// WG3 deterministic river/corridor/save checks. No authored data or real profiles are modified.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
public partial class ValidateRiverPlanning : Node
{
    private int _checks;
    // Exercise real planners and explicit failure cases with bounded test seeds.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"WG3 PLAN PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private void Run()
    {
        var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres").Duplicate(true);
        config.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=2,RiverCount=1,RequireMultipleRiverBiomes=true };
        var timer=System.Diagnostics.Stopwatch.StartNew(); string previous=null;
        for (int seed=0;seed<12;seed++)
        {
            var map=MacroWorldBuilder.Build(config,seed); var features=WorldFeaturePlanner.Build(config,seed);
            var pond=new Rect2(650,500,860,600);
            var plan=WaterBodyPlanner.Build(config,map,features,pond);
            Check(plan.Bodies.Count(b => b.River!=null)==1,"No feasible default river at seed "+seed+": "+string.Join(";",plan.Warnings));
            var river=plan.Bodies.Single(b => b.River!=null); var path=river.River;
            Check(plan.PlanVersion==2 && path.Points.Count<=24 && river.CopyPolygon().Length<=64,"River exceeded frozen geometry limits");
            Check(path.Source==RiverEndpointKind.SpringPool && path.Destination==RiverEndpointKind.TerminalBasin,"Missing supported river terminals");
            Check(path.Points.Select(p => map.BiomeAt(p).Id).Distinct().Count()>=2,"River did not cross biome types");
            Check(path.Widths.Max()>path.Widths.Min() && path.Widths.Min()>0,"River width is uniform or has gaps");
            Check(path.Points[0].DistanceTo(path.Points[^1])>=config.Water.MinimumRiverSpan-0.01f,"River endpoints too close");
            for (int i=1;i<path.Points.Count;i++) for (int sample=0;sample<=8;sample++)
            {
                var p=path.Points[i-1].Lerp(path.Points[i],sample/8f);
                Check(river.SignedDistance(p)>0,"Disconnected corridor along saved centerline");
                Check(features.Reservations.AllowsDisk(p,0,WorldPlacementKind.Water),"River entered protected reservation");
                Check(!pond.Grow(config.Water.Separation).HasPoint(p),"River entered authored pond exclusion");
            }
            Check(map.Bounds.Encloses(river.Bounds),"River escaped finite world");
            Check(plan.Hash==WaterBodyPlanner.Build(config,map,features,pond).Hash,"River rerolled on same seed");
            Check(previous==null || previous!=plan.Hash,"Different seeds reused same river outcomes"); previous=plan.Hash;
            config.Biomes.Reverse(); Check(plan.Hash==WaterBodyPlanner.Build(config,map,features,pond).Hash,"Biome Resource order shifted river"); config.Biomes.Reverse();
            config.Water.RiverCount=0;
            var lakeOnly=WaterBodyPlanner.Build(config,map,features,pond);
            Check(lakeOnly.PlanVersion==1 && lakeOnly.Bodies.All(b => plan.Bodies.Single(p => p.Id==b.Id).CopyPolygon().SequenceEqual(b.CopyPolygon())),"River changed existing lake geometry/version");
            config.Water.RiverCount=1;
            var json=JsonSerializer.Serialize(plan.Snapshot());
            Check(WaterBodyPlan.Restore(JsonSerializer.Deserialize<WaterPlanSnapshot>(json),seed,map.Bounds).Hash==plan.Hash,"River JSON changed frozen outcomes");
            var bad=plan.Snapshot(); bad.Bodies.Single(b => b.River!=null).River.Widths[0]+=1;
            Reject(() => WaterBodyPlan.Restore(bad,seed,map.Bounds),"Corrupt river width accepted");
            Reject(() => WaterBodyPlan.Restore(plan.Snapshot() with { Version=1 },seed,map.Bounds),"River accepted under legacy water version");
            var points=path.Points.ToArray(); points[0]+=Vector2.One*10000;
            Check(points[0]!=path.Points[0],"Caller mutated river path");
        }
        GD.Print($"WG3 12 seed repeat/order/eligibility/JSON checks: {timer.ElapsedMilliseconds}ms");
        var fixedMap=MacroWorldBuilder.Build(config,42); var fixedFeatures=WorldFeaturePlanner.Build(config,42);
        var initial=WaterBodyPlanner.Build(config,fixedMap,fixedFeatures);
        var sampleBody=initial.Bodies.Single(b => b.River!=null);
        var smooth=RiverPlanner.Outline(sampleBody.River);
        var noisy=RiverPlanner.Outline(sampleBody.River,0.3f);
        Check(smooth.SequenceEqual(RiverPlanner.Outline(sampleBody.River,0)),"Zero bank noise changed the smooth outline");
        Check(!smooth.SequenceEqual(noisy),"Bank irregularity did not affect geometry");
        Check(noisy.SequenceEqual(RiverPlanner.Outline(sampleBody.River,0.3f)),"Bank noise was not deterministic");
        var noisyBody=new PlannedWaterBody("noisy","water",noisy,sampleBody.River);
        foreach (var point in sampleBody.River.Points) Check(noisyBody.SignedDistance(point)>0,"Bank noise excluded the centerline");
        config.Water.RiverBankIrregularity=float.NaN;
        Reject(() => WaterBodyPlanner.Build(config,fixedMap,fixedFeatures),"Nonfinite bank irregularity accepted");
        config.Water.RiverBankIrregularity=0.31f;
        Reject(() => WaterBodyPlanner.Build(config,fixedMap,fixedFeatures),"Excessive bank irregularity accepted");
        config.Water.RiverBankIrregularity=0.15f;
        config.Water.RiverCount=2; var extra=WaterBodyPlanner.Build(config,fixedMap,fixedFeatures);
        Check(initial.Bodies.Where(b => b.River!=null).All(b => extra.Bodies.Any(e => e.Id==b.Id && e.CopyPolygon().SequenceEqual(b.CopyPolygon()))),"Later river slot moved first river");
        config.Water.RiverCount=1; config.Water.LakeCount=0;
        foreach (var biome in config.Biomes) biome.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterRivers=false };
        var denied=WaterBodyPlanner.Build(config,fixedMap,fixedFeatures);
        Check(denied.Bodies.Count==0 && denied.Warnings.Count==1,"Hard river exclusions failed or skipped silently");
        foreach (var biome in config.Biomes) biome.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false };
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Any(b => b.River!=null),"River used lake permissions instead of river permissions");
        foreach (var biome in config.Biomes) biome.Hydrology=new BiomeHydrologyProfile { RiverSuitability=0 };
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Count==0,"Zero-suitability endpoints were chosen");
        config.Biomes[0].Hydrology.RiverSuitability=1;
        Check(WaterBodyPlanner.Build(config,fixedMap,fixedFeatures).Bodies.Count==0,"Zero-suitability biome supplied a cross-biome endpoint");
        config.Water.MaximumRiverWidth=float.NaN;
        Reject(() => WaterBodyPlanner.Build(config,fixedMap,fixedFeatures),"Invalid river width was accepted");
        BarrierAndSparseChunks();
    }
    // A forbidden strip spanning the world separates source/terminal biomes; the planner must reject, not fade or jump it.
    // =========================================================
    private void BarrierAndSparseChunks()
    {
        var a=new WorldBiomeDefinition { Id="west" }; var b=new WorldBiomeDefinition { Id="east" };
        var barrier=new WorldBiomeDefinition { Id="blocked",Hydrology=new BiomeHydrologyProfile { AllowFreshwaterRivers=false } };
        int[] cells=new int[40*40]; var counts=new int[3];
        for (int y=0;y<40;y++) for (int x=0;x<40;x++) { int id=x<18 ? 0 : x>21 ? 1 : 2; cells[y*40+x]=id; counts[id]++; }
        var map=new MacroWorldMap(15,new Rect2(-5000,-5000,10000,10000),250,40,40,cells,
            new[] { new MacroBiomeInfo(a.Id,"",Colors.Green),new MacroBiomeInfo(b.Id,"",Colors.Red),new MacroBiomeInfo(barrier.Id,"",Colors.Black) },
            new[] { new MacroRegion(0,0,new Vector2(-2500,0),4000,counts[0]),new MacroRegion(1,1,new Vector2(2500,0),4000,counts[1]),new MacroRegion(2,2,Vector2.Zero,1000,counts[2]) });
        var config=new WorldGenerationMasterConfig { Bounds=map.Bounds,Biomes=new() { a,b,barrier },Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=0,RiverCount=1,RequireMultipleRiverBiomes=true } };
        var features=new WorldFeaturePlan(15,map.Bounds,Array.Empty<WorldFeatureReservation>(),Array.Empty<string>());
        Check(WaterBodyPlanner.Build(config,map,features).Bodies.Count==0,"River crossed a world-spanning forbidden biome");
        // A U-shaped corridor leaves its bounding-box interior dry; shared queries must not publish all those chunks.
        var path=new PlannedRiverPath(new[] { new Vector2(0,0),new Vector2(2000,0),new Vector2(2000,2000),new Vector2(0,2000) },new[] { 100f,100f,100f,100f });
        var body=new PlannedWaterBody("u-river","water",RiverPlanner.Outline(path),path);
        var dry=new Rect2(900,900,128,128);
        Check(body.Bounds.Encloses(dry) && !body.Intersects(dry,40),"Sparse river index retained bounding-box interior");
        var plan=new WaterBodyPlan(1,new Rect2(-1000,-1000,5000,5000),new[] { body },Array.Empty<string>());
        var hits=new List<PlannedWaterBody>(); plan.Query(dry,hits); Check(hits.Count==0,"Shared chunk query ignored segment index");
        var runtime=new GeneratedWaterRuntime(); AddChild(runtime); runtime.Initialize(plan,new WorldLiquidMasterConfig { Animate=false });
        var chunk=new ChunkCoord(3,3); foreach (int step in runtime.PrepareChunkSteps(chunk,chunk.Bounds(256))) { }
        runtime.PublishChunk(chunk); Check(runtime.TileCount==0 && runtime.ResidentCount==0,"Dry bounding-box chunk materialized water");
        runtime.ReleaseChunk(chunk); runtime.Free();
        // A single allowed biome should remain supported even when multi-biome preference is enabled.
        config.Biomes=new() { a }; config.Bounds=new Rect2(-10000,-10000,20000,20000); config.Water.RequireMultipleRiverBiomes=true;
        var single=MacroWorldBuilder.Build(config,19);
        Check(WaterBodyPlanner.Build(config,single,WorldFeaturePlanner.Build(config,19)).Bodies.Count==1,"One-biome river generation failed");
    }
    private void Check(bool value,string message) { _checks++; if (!value) throw new Exception(message); }
    private void Reject(Action action,string message)
    {
        bool rejected=false; try { action(); } catch (ArgumentException) { rejected=true; } catch (InvalidDataException) { rejected=true; }
        Check(rejected,message);
    }
}
