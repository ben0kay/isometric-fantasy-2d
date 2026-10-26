// P5a/P5b: real pipeline order, biome constraints, connected coverage, frozen saves and legacy compatibility.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public partial class ValidateGeographyFirst : Node
{
    private int _checks;
    // All configs/storage are disposable; no authored Resources or real campaigns are changed.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"P5AB PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private void Run()
    {
        const string sourcePath="res://WORLD/Generation/World/StandardWorldGeneration.tres";
        string authored=Godot.FileAccess.GetFileAsString(sourcePath).Replace("\r\n","\n");
        string previous=Godot.FileAccess.GetFileAsString("res://SAVE/Migrations/References/WaterDefaultsBefore.txt").Replace("\r\n","\n");
        Check(CampaignSession.NormalizeDefaultWaterGenerationSource(sourcePath,authored)==previous,"Shipped water defaults lost legacy content compatibility");
        string changed=authored.Replace("CellSize = 512.0","CellSize = 128.0");
        Check(CampaignSession.NormalizeDefaultWaterGenerationSource(sourcePath,changed)==changed,"Unrelated config edit bypassed compatibility");
        Check(CampaignSession.NormalizeDefaultWaterGenerationSource("res://other.tres",authored)==authored,"Water compatibility leaked to another dependency");
        var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/StandardWorldGeneration.tres").Duplicate(true);
        config.Authoring=null; // This suite exercises the pre-climate geography generator.
        config.Bounds=new Rect2(-22500,-16875,45000,33750); config.CellSize=256;
        config.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=2,RiverCount=1 };
        var pond=new Rect2(650,500,860,600);
        for (int seed=0;seed<6;seed++)
        {
            var world=WorldGenerationPipeline.Build(config,seed,pond); var water=world.Geography.Water;
            Check(water.Bodies.Count(b=>b.River!=null)==1 && water.Bodies.Count(b=>b.River==null)==2,"Expected feasible major water");
            Check(world.Map.GenerationVersion==3,"New pipeline did not version biome generation");
            var repeat=WorldGenerationPipeline.Build(config,seed,pond);
            Check(world.Map.Hash==repeat.Map.Hash && water.Hash==repeat.Geography.Water.Hash,"Pipeline rerolled same seed");
            var features=WorldFeaturePlanner.Build(config,seed);
            Check(features.Hash==world.Features.Hash,"Geography changed required reservations");
            config.Biomes.Reverse(); var reordered=WorldGenerationPipeline.Build(config,seed,pond); config.Biomes.Reverse();
            Check(reordered.Map.Hash==world.Map.Hash && reordered.Geography.Water.Hash==water.Hash,"Inspector ordering changed outcomes");
            var savedBiomes=config.Biomes;
            config.Biomes=null;
            Check(WaterBodyPlanner.BuildGeography(config,seed,features,pond).Hash==water.Hash,"Major water read biome Resources");
            config.Biomes=savedBiomes;
            foreach (var biome in config.Biomes) biome.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false,AllowFreshwaterRivers=false };
            Check(WaterBodyPlanner.BuildGeography(config,seed,features,pond).Hash==water.Hash,"Biome exclusions reshaped geography");
            Reject(()=>MacroWorldBuilder.Build(config,seed,world.Geography),"All-forbidden biome set erased/claimed water");
            config.Biomes[0].Hydrology=null; // One compatible biome must cover all water cells; others can still fill dry land.
            var constrained=MacroWorldBuilder.Build(config,seed,world.Geography);
            ValidateCoverage(constrained,world.Geography,config.Biomes[0].Id);
            foreach (var body in water.Bodies)
            {
                var polygon=body.CopyPolygon();
                foreach (var feature in features.Reservations.Features)
                    Check((feature.AllowedPlacements&WorldPlacementKind.Water)!=0 || !FeatureGeometry.Overlaps(polygon,body.Bounds,config.Water.Separation,feature.CopyPolygon(),feature.Bounds,feature.Clearance),"Water entered required reservation");
            }
            foreach (var biome in config.Biomes) biome.Hydrology=null;
            SnapshotChecks(world,config);
        }
        Check(MacroWorldBuilder.Build(config,42).Hash=="56C4356337CDF50D921A19205D761E107D4CD44FECC9D092C60F09DDAA369DBF","Legacy generator hash changed");
        SyntheticConstraints();
        config.Water=null;
        var empty=WorldGenerationPipeline.Build(config,42);
        Check(empty.Geography.Water.Bodies.Count==0 && empty.Map.GenerationVersion==3,"Disabled geography did not produce empty versioned world");
        Check(empty.Map.CopyRegionCells().SequenceEqual(MacroWorldBuilder.Build(config,42).CopyRegionCells()),"Unconstrained world needlessly changed biome layout");
        SnapshotChecks(empty,config);
    }
    // Prove every cell belongs to exactly one valid, four-connected region with consistent retained metadata.
    // =========================================================
    private void ValidateCoverage(MacroWorldMap map,WorldGeographyPlan geography,string wetBiome)
    {
        var mask=geography.BuildWaterMask(map.CellSize,map.Columns,map.Rows);
        int[] cells=map.CopyRegionCells(); var seen=new bool[cells.Length]; var counts=new int[map.Regions.Count];
        for (int i=0;i<cells.Length;i++)
        {
            Check(cells[i]>=0 && cells[i]<counts.Length,"Unassigned geography cell"); counts[cells[i]]++;
            if (mask[i]!=GeographyWaterKind.None) Check(map.Biomes[map.Regions[cells[i]].BiomeIndex].Id==wetBiome,"Forbidden biome overlaps established water");
        }
        var queue=new Queue<int>();
        for (int start=0;start<cells.Length;start++)
        {
            if (seen[start]) continue;
            int region=cells[start],total=0; queue.Enqueue(start); seen[start]=true;
            while (queue.Count>0)
            {
                int cell=queue.Dequeue(); total++; int x=cell%map.Columns,y=cell/map.Columns;
                if (x>0) Add(cell-1); if (x+1<map.Columns) Add(cell+1); if (y>0) Add(cell-map.Columns); if (y+1<map.Rows) Add(cell+map.Columns);
            }
            Check(total==counts[region] && total==map.Regions[region].CellCount,"Region disconnected or metadata mismatched");
            void Add(int id) { if (!seen[id] && cells[id]==region) { seen[id]=true; queue.Enqueue(id); } }
        }
        Check(counts.All(n=>n>0),"Empty region survived");
    }
    // Small exact fixtures cover separate lake/river rules, mixed cells, zero preferences and constrained components.
    // =========================================================
    private void SyntheticConstraints()
    {
        var bounds=new Rect2(-800,-800,1600,1600);
        var riverPath=new PlannedRiverPath(new[] { new Vector2(-650,0),new Vector2(650,0) },new[] { 100f,100f });
        var river=new PlannedWaterBody("river","water",RiverPlanner.Outline(riverPath),riverPath);
        var lake=new PlannedWaterBody("lake","water",new[] { new Vector2(-550,-550),new Vector2(-300,-550),new Vector2(-300,-300),new Vector2(-550,-300) });
        var geography=new WorldGeographyPlan(17,bounds,new WaterBodyPlan(17,bounds,new[] { river,lake },Array.Empty<string>()));
        var allowed=new WorldBiomeDefinition { Id="wet",MinimumRegionDiameter=2000,MaximumRegionDiameter=2000,Hydrology=new BiomeHydrologyProfile() };
        var dry=new WorldBiomeDefinition { Id="dry",MinimumRegionDiameter=2000,MaximumRegionDiameter=2000,Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false,AllowFreshwaterRivers=false } };
        var config=new WorldGenerationMasterConfig { Bounds=bounds,CellSize=100,Biomes=new() { allowed,dry } };
        Reject(()=>new WorldGeographyPlan(17,bounds.Grow(1),geography.Water),"Mismatched geography/water bounds accepted");
        var exactMask=geography.BuildWaterMask(100,16,16);
        for (int y=0;y<16;y++) for (int x=0;x<16;x++)
            Check(exactMask[y*16+x]==geography.WaterAt(new Rect2(bounds.Position+new Vector2(x,y)*100,Vector2.One*100)),"Cell mask differs from conservative geometry query");
        ValidateCoverage(MacroWorldBuilder.Build(config,17,geography),geography,"wet");
        allowed.Hydrology.LakeSuitability=0; allowed.Hydrology.RiverSuitability=0;
        ValidateCoverage(MacroWorldBuilder.Build(config,17,geography),geography,"wet");
        var rules=new GeographyBiomeRules(geography,100,16,16,new[] { allowed,dry }); var mask=geography.BuildWaterMask(100,16,16);
        int lakeCell=Array.FindIndex(mask,m=>m==GeographyWaterKind.Lake),riverCell=Array.FindIndex(mask,m=>m==GeographyWaterKind.River);
        Check(lakeCell>=0 && riverCell>=0 && rules.Preference(lakeCell,0)>0 && rules.Preference(riverCell,0)>0,"Zero suitability became hard exclusion");
        dry.Hydrology.AllowFreshwaterLakes=true;
        rules=new GeographyBiomeRules(geography,100,16,16,new[] { allowed,dry });
        Check(rules.Preference(lakeCell,1)>0 && rules.Preference(riverCell,1)==0,"Lake and river permissions were conflated");
        allowed.GenerationWeight=0; Reject(()=>MacroWorldBuilder.Build(config,17,geography),"Zero-weight compatible biome supplied river coverage"); allowed.GenerationWeight=1;
        allowed.Hydrology.LakeSuitability=float.NaN; Reject(()=>MacroWorldBuilder.Build(config,17,geography),"Invalid soft preference accepted"); allowed.Hydrology.LakeSuitability=0;
        config.Biomes=new() { allowed }; ValidateCoverage(MacroWorldBuilder.Build(config,17,geography),geography,"wet");
        Reject(()=>MacroWorldBuilder.Build(config,18,geography),"Mismatched geography ownership accepted");
        // A cross-world strip is a constraint for dry regions, not a gap where the planner may leave unassigned cells.
        config.Biomes=new() { allowed,dry }; dry.Hydrology.AllowFreshwaterLakes=false;
        config.MaximumRegions=1;
        bool boundedFailure=false;
        for (int seed=0;seed<16 && !boundedFailure;seed++)
        {
            var geo=new WorldGeographyPlan(seed,bounds,new WaterBodyPlan(seed,bounds,new[] { river,lake },Array.Empty<string>()));
            try { MacroWorldBuilder.Build(config,seed,geo); } catch (ArgumentException e) { boundedFailure=e.Message.Contains("MaximumRegions"); }
        }
        Check(boundedFailure,"Disconnected constrained growth did not respect region budget");
        var overlappingLake=new PlannedWaterBody("mixed.lake","water",new[] { new Vector2(-100,-100),new Vector2(100,-100),new Vector2(100,100),new Vector2(-100,100) });
        var mixed=new WorldGeographyPlan(17,bounds,new WaterBodyPlan(17,bounds,new[] { river,overlappingLake },Array.Empty<string>()));
        config.MaximumRegions=4096; allowed.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterRivers=false };
        dry.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false };
        Reject(()=>MacroWorldBuilder.Build(config,17,mixed),"Mixed lake/river cell ignored one hard permission");
        config.Biomes.Add(new WorldBiomeDefinition { Id="both",MinimumRegionDiameter=2000,MaximumRegionDiameter=2000 });
        Check(MacroWorldBuilder.Build(config,17,mixed).Regions.Count>0,"Compatible mixed-water fallback failed");
    }
    // Durable snapshots carry separate geography/generator versions; Continue must never need current biome rules.
    // =========================================================
    private void SnapshotChecks(WorldGenerationResult world,WorldGenerationMasterConfig config)
    {
        var map=world.Map; var features=world.Features; var water=world.Geography.Water;
        var b=Blueprint(map,features,water,9) with { GeographyVersion=WorldGeographyPlan.Version };
        Check(WorldBlueprintRestore.Map(b).Hash==map.Hash,"New map failed frozen restore");
        Check(WaterBodyPlan.Restore(b.Water,b.Seed,map.Bounds).Hash==water.Hash,"Geography failed frozen restore");
        SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,b);
        Reject(()=>SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,b with { GeographyVersion=99 }),"Unknown geography version accepted");
        Reject(()=>SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,b with { SchemaVersion=8 }),"New generator accepted under older blueprint schema");
        Reject(()=>SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,b with { GeneratorVersion=1 }),"New schema accepted older generator identity");
        var root=Path.Combine(Path.GetTempPath(),"p5ab-"+Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,b.ProfileId,b.RunId,b.WorldId,new string('A',64),DateTime.UtcNow.ToString("O"),b,
                new SavedPlayer(0,0,Health:100,Inventory:new SavedInventory(GD.Load<PlayerInventoryProfile>("res://ENTITIES/Player/Inventory/DefaultPlayerInventory.tres").Storage.StorageSlots,Array.Empty<SavedItemStack>(),new long[9],-1,false)),new SavedClock(12,600,true,new SavedCalendar(60,120,true,false,0.5f,true,1,14)),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
            var store=new CampaignStore(root); store.Save(snapshot,true); var loaded=store.Load(b.ProfileId).Snapshot;
            Check(loaded.Blueprint.MapHash==b.MapHash && loaded.Blueprint.Water.Hash==water.Hash,"Disk changed geography/map identity");
            var legacyMap=MacroWorldBuilder.Build(config,map.Seed);
            var lake=new PlannedWaterBody("legacy.lake","water",new[] { new Vector2(-500,-500),new Vector2(-200,-500),new Vector2(-200,-200),new Vector2(-500,-200) });
            var path=new PlannedRiverPath(new[] { new Vector2(1000,1000),new Vector2(3000,1000) },new[] { 100f,100f });
            var river=new PlannedWaterBody("legacy.river","water",RiverPlanner.Outline(path),path);
            for (int schema=4;schema<=7;schema++)
            {
                var oldWater=new WaterBodyPlan(map.Seed,map.Bounds,schema>=6 ? new[] { river } : new[] { lake },Array.Empty<string>(),schema==6 ? 2 : schema==7 ? 3 : 1);
                var old=Blueprint(legacyMap,features,oldWater,schema) with { ProfileId=b.ProfileId,RunId=b.RunId,WorldId=b.WorldId };
                store.Save(snapshot with { Blueprint=old },false); var restored=store.Load(b.ProfileId).Snapshot.Blueprint;
                Check(WorldBlueprintRestore.Map(restored).Hash==legacyMap.Hash && WaterBodyPlan.Restore(restored.Water,restored.Seed,map.Bounds).Hash==oldWater.Hash,"Legacy schema geography changed");
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
    }
    private static WorldBlueprintManifest Blueprint(MacroWorldMap map,WorldFeaturePlan features,WaterBodyPlan water,int schema)
        =>WorldBlueprintManifest.Create(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),map,features) with {
            SchemaVersion=schema,Water=water.Snapshot(),LocalGeneratorVersion=ChunkSceneryData.GeneratorVersion,ChunkSize=512,CandidateCellSize=96,
            MacroBiomes=map.Biomes.Select(v=>new SavedMacroBiome(v.Id,v.Name,v.Colour.R,v.Colour.G,v.Colour.B,v.Colour.A)).ToArray(),
            MacroRegions=map.Regions.Select(r=>new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray() };
    private void Check(bool value,string message) { _checks++; if (!value) throw new Exception(message); }
    private void Reject(Action action,string message)
    {
        bool rejected=false; try { action(); } catch (ArgumentException) { rejected=true; } catch (InvalidDataException) { rejected=true; }
        Check(rejected,message);
    }
}
