// WG3 production streaming and durable river restore fixture, isolated from real campaign storage.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateStreamedRiver : Node
{
    private int _checks;
    // Exercise the same world/runtime used by normal gameplay, with explicit opt-in cloned settings.
    // =========================================================
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"WG3 STREAM PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.Generation=(WorldGenerationMasterConfig)world.Config.Generation.Duplicate();
        Check(world.Config.Generation.Water?.Enabled==true,"Production World config has generated water disabled");
        world.Config.RandomSeedOnLoad=false; world.Config.FixedSeed=42;
        AddChild(world); Check(world.StartupError==null,"World failed to initialize: "+world.StartupError);
        Check(world.GeographyPlan!=null && (world.MacroMap.GenerationVersion==MacroWorldMap.ClimateGeneratorVersion || world.MacroMap.GenerationVersion==MacroWorldMap.ScalableGeneratorVersion) && (world.Blueprint.SchemaVersion==9 || world.Blueprint.SchemaVersion==10 || world.Blueprint.SchemaVersion==11 || world.Blueprint.SchemaVersion==12) && world.WaterPlan.PlanVersion==4,"River world did not use the river-capable save contract");
        var body=world.WaterPlan.Bodies.Single(b => b.River!=null); var path=body.River;
        int middle=path.Points.Count/2;
        foreach (int index in new[] { 0,middle,path.Points.Count-1,middle })
        {
            var point=path.Points[index]; world.Player.GlobalPosition=point; world.Player.Velocity=Vector2.Zero;
            await Settled(world,point);
            var region=world.GeneratedWater.RegionAt(point);
            Check(region!=null && region.PlannedBody.Id==body.Id,"River section missing or using a duplicate water body");
            Check(world.Player.SurfaceSpeedMultiplier<1,"Player did not wade in river");
            Check(world.GeneratedWater.TileCount<=world.Stream.LoadedCount*world.WaterPlan.Bodies.Count,"Unbounded river tile ownership");
            foreach (var tile in region.GetChildren().OfType<LiquidSurfaceTile>())
            {
                Check(body.Intersects(tile.TileBounds,40),"River tiled a dry bounding-box chunk");
                using var field=tile.Field.GetImage();
                for (int y=0;y<field.GetHeight();y+=17) for (int x=0;x<field.GetWidth();x+=17)
                {
                    var sample=tile.FieldBounds.Position+new Vector2(x+0.5f,y+0.5f)*4f;
                    float expected=Mathf.Clamp(0.5f+body.SignedDistance(sample)/512f,0,1);
                    Check(Mathf.Abs(field.GetPixel(x,y).R-expected)<0.0001f,"Chunk distance field disagrees with authoritative shoreline");
                }
            }
            var data=new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing);
            data.LiquidBlocked=world.GeneratedWater.Contains;
            foreach (var item in data.Build(ChunkCoord.At(point,world.Config.Streaming.ChunkSize)))
                Check(!world.GeneratedWater.Contains(item.Position,item.Radius+(item.Content.GrassLike ? 30 : 15)),"Scenery spawned in river");
        }
        var root=Path.Combine(Path.GetTempPath(),"wg3-"+Guid.NewGuid().ToString("N"));
        var blueprint=world.Blueprint;
        var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,blueprint.ProfileId,blueprint.RunId,blueprint.WorldId,CampaignSession.ContentFingerprint(),DateTime.UtcNow.ToString("O"),blueprint,
            new SavedPlayer(world.Player.GlobalPosition.X,world.Player.GlobalPosition.Y,Health:100,Inventory:world.Harvesting.Inventory.Capture()),new SavedClock(12,600,true,world.Environment.Seasons.Capture(),world.Environment.RegionalClimate.Capture()),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
        try
        {
            var store=new CampaignStore(root); store.Save(snapshot,true); var loaded=store.Load(snapshot.ProfileId).Snapshot;
            var restored=WaterBodyPlan.Restore(loaded.Blueprint.Water,loaded.Blueprint.Seed,world.MacroMap.Bounds);
            Check(restored.Hash==world.WaterPlan.Hash && restored.Bodies.Single(b => b.River!=null).River.Points.SequenceEqual(path.Points),"Disk checkpoint lost river route");
            Reject(() => store.Validate(snapshot with { Blueprint=blueprint with { SchemaVersion=6 } }),"Dense river accepted by older river schema");
            Reject(() => store.Validate(snapshot with { Blueprint=blueprint with { SchemaVersion=5 } }),"River data accepted by lake-only schema");
            Reject(() => store.Validate(snapshot with { Blueprint=blueprint with { SchemaVersion=4 } }),"River data accepted by metadata-only schema");
            var legacyConfig=(WorldGenerationMasterConfig)world.Config.Generation.Duplicate(true);
            if(legacyConfig.ScalableMacro) foreach(var biome in legacyConfig.Biomes) { biome.MinimumRegionDiameter*=legacyConfig.RegionDiameterScale; biome.MaximumRegionDiameter*=legacyConfig.RegionDiameterScale; }
            legacyConfig.ScalableMacro=false;
            var legacyMap=MacroWorldBuilder.Build(legacyConfig,blueprint.Seed);
            var legacyWater=new WaterBodyPlan(blueprint.Seed,legacyMap.Bounds,world.WaterPlan.Bodies.Where(b=>!b.IsOcean),Array.Empty<string>());
            var legacy=loaded with { Blueprint=blueprint with { SchemaVersion=7,RefinementStrength=0,Climate=null,AuthorPolicy=null,GeneratorVersion=1,GeographyVersion=0,Water=legacyWater.Snapshot(),MapHash=legacyMap.Hash,
                RegionCells=legacyMap.CopyRegionCells(),RegionBiomes=legacyMap.Regions.Select(r=>r.BiomeIndex).ToArray(),
                MacroRegions=legacyMap.Regions.Select(r=>new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray() } };
            world.Free(); await Frames(3);
            CampaignSession.TestStorageRoot=root;
            await ContinueFrozen(loaded,restored.Hash);
            await ContinueFrozen(legacy,legacyWater.Hash);
        }
        finally
        {
            CampaignSession.TestStorageRoot=null; CampaignSession.Clear();
            if (Directory.Exists(root)) Directory.Delete(root,true);
        }
    }
    // Neither new nor legacy Continue may consult newly forbidden biome hydrology or run the current water planner.
    // =========================================================
    private async Task ContinueFrozen(CampaignSnapshot snapshot,string waterHash)
    {
        CampaignSession.Continue(snapshot);
        var continued=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        continued.Config=(WorldMasterConfig)continued.Config.Duplicate(); continued.Config.Generation=(WorldGenerationMasterConfig)continued.Config.Generation.Duplicate(true);
        continued.Config.Generation.Water=null;
        foreach (var biome in continued.Config.Generation.Biomes) biome.Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=false,AllowFreshwaterRivers=false };
        AddChild(continued);
        for (int i=0;i<240 && continued.StartupError==null && continued.Saves?.ReadyToSave!=true && string.IsNullOrEmpty(continued.Saves?.LastError);i++) await Frames(1);
        Check(continued.StartupError==null && continued.Saves?.ReadyToSave==true,"Continue failed: "+continued.StartupError+continued.Saves?.LastError);
        await Frames(10);
        Check(continued.Blueprint.SchemaVersion==snapshot.Blueprint.SchemaVersion && continued.Blueprint.WorldId==snapshot.WorldId,"Continue changed river world identity");
        Check(continued.MacroMap.Hash==snapshot.Blueprint.MapHash && continued.WaterPlan.Hash==waterHash && continued.GeneratedWater.RegionAt(continued.Player.GlobalPosition)!=null,"Continue rerolled/failed to bind saved geography");
        Check((continued.GeographyPlan!=null)==(snapshot.Blueprint.SchemaVersion>=8),"Legacy river promoted to geography-first generation");
        Check(continued.Saves.LastError=="","Saved river arrival blocked: "+continued.Saves.LastError);
        continued.Free(); await Frames(3);
    }
    // Wait for the actual existing stream scheduler; a teleport does not itself create loaded geometry.
    // =========================================================
    private async Task Settled(WorldScene world,Vector2 point)
    {
        for (int i=0;i<2000;i++)
        { await Frames(1); if (world.Stream.CanMoveTo(point) && world.Stream.PendingCount==0) { await Frames(12); return; } }
        throw new Exception("River streaming did not settle at "+point);
    }
    private async Task Frames(int count) { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool value,string message) { _checks++; if (!value) throw new Exception(message); }
    private void Reject(Action action,string message) { bool rejected=false; try { action(); } catch (InvalidDataException) { rejected=true; } Check(rejected,message); }
}
