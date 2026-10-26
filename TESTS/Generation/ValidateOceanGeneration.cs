// P5c ocean/non-land, enlarged production world, whole-world F7 and frozen new/legacy save integration.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateOceanGeneration : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { Plans(); await World(); GD.Print($"P5C OCEAN PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Real production presets, repeated seeds, full coverage excluding ocean and immutable save contracts.
    // =========================================================
    private void Plans()
    {
        var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/StandardWorldGeneration.tres").Duplicate(true);
        double ratio=(double)config.Bounds.Size.X*config.Bounds.Size.Y/(45000d*33750);
        Check(Math.Abs(ratio-10)<0.001 && config.CellSize==512,"World is not 10x area with bounded macro resolution");
        Check(config.Water.OceanCount==1,"Production ocean is not enabled");
        for (int seed=0;seed<4;seed++)
        {
            var world=WorldGenerationPipeline.Build(config,seed,new Rect2(650,500,860,600)); var map=world.Map; var water=world.Geography.Water;
            var ocean=water.Bodies.Single(b=>b.IsOcean);
            Check(water.PlanVersion==4 && map.GenerationVersion==MacroWorldMap.ClimateGeneratorVersion && map.NonLandCells>0,"Ocean did not use non-land/versioned contracts");
            Check(map.Columns*map.Rows<=config.MaximumCells && map.Regions.Count<config.MaximumRegions,"Large world exceeded macro bounds");
            Check(map.BiomeAt(world.Features.StartingArea.Anchor)!=null,"Ocean swallowed protected start");
            Check(world.Geography.PlanVersion==2,"Missing geography contract version");
            Reject(()=>WaterBodyPlanner.Build(config,map,world.Features),"Legacy water planner accepted non-land ocean cells");
            var repeat=WorldGenerationPipeline.Build(config,seed,new Rect2(650,500,860,600));
            Check(repeat.Map.Hash==map.Hash && repeat.Geography.Water.Hash==water.Hash,"Ocean/world rerolled on repeated seed");
            int land=0;
            for (int y=0;y<map.Rows;y++) for (int x=0;x<map.Columns;x++)
            {
                int id=map.RegionIdAtCell(x,y); var point=map.Bounds.Position+new Vector2(x+0.5f,y+0.5f)*map.CellSize;
                point=point.Clamp(map.Bounds.Position,map.Bounds.End-Vector2.One*0.01f);
                bool wet=ocean.Bounds.HasPoint(point) && ocean.SignedDistance(point)>=0;
                Check((id<0)==wet,"Ocean center cell has a land biome or dry cell became void");
                Check((map.BiomeAt(point)==null)==wet,"Ocean biome lookup did not return non-land");
                if (id>=0) land++;
            }
            Check(land+map.NonLandCells==map.Columns*map.Rows && map.Regions.Sum(r=>r.CellCount)==land,"Land coverage metadata includes ocean");
            Check(!FeatureGeometry.Overlaps(ocean.CopyPolygon(),ocean.Bounds,config.Water.Separation,world.Features.StartingArea.CopyPolygon(),world.Features.StartingArea.Bounds,world.Features.StartingArea.Clearance),"Ocean violated spawn clearance");
            var restored=WaterBodyPlan.Restore(water.Snapshot(),seed,map.Bounds);
            Check(restored.Hash==water.Hash && restored.Bodies.Single(b=>b.IsOcean).CopyPolygon().SequenceEqual(ocean.CopyPolygon()),"Ocean lost frozen identity");
            Reject(()=>WaterBodyPlan.Restore(water.Snapshot() with { Version=3 },seed,map.Bounds),"Ocean data accepted by old water version");
        }
        config.Water.MinimumOceanDiameter=199000;config.Water.MaximumOceanDiameter=200000;
        Reject(()=>WorldGenerationPipeline.Build(config,1),"Impossible required ocean silently became land");
        config.Water.OceansRequired=false;
        var optional=WorldGenerationPipeline.Build(config,1);
        Check(optional.Geography.Water.Bodies.All(b=>!b.IsOcean) && optional.Geography.Water.Warnings.Count>0,"Optional ocean skip was silent");
    }
    // Actual normal World, without replacing the ocean/size config; visit deep ocean and coast, then real Continue.
    // =========================================================
    private async Task World()
    {
        var scene=GD.Load<PackedScene>("res://SCENES/World/World.tscn"); var world=scene.Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate();world.Config.RandomSeedOnLoad=false;world.Config.FixedSeed=42;
        AddChild(world);Check(world.StartupError==null,"Production ocean startup: "+world.StartupError);
        Check((world.Blueprint.SchemaVersion==9 || world.Blueprint.SchemaVersion==10 || world.Blueprint.SchemaVersion==11 || world.Blueprint.SchemaVersion==12) && world.Blueprint.GeographyVersion==2,"Wrong ocean blueprint contract");
        var ocean=world.WaterPlan.Bodies.Single(b=>b.IsOcean);var centre=ocean.Bounds.GetCenter();var shore=ocean.CopyPolygon()[0];
        await Frames(4);
        var panel=world.GetNode<MacroMapDebugPanel>("DebugUIRoot/MacroMapDebugPanel");
        panel._UnhandledKeyInput(new InputEventKey { Pressed=true,Keycode=Key.F7 });await Frames(3);
        var control=panel.GetChildren().OfType<PanelContainer>().Single();
        Check(control.Visible && world.GetViewport().GetVisibleRect().Encloses(control.GetGlobalRect()),"F7 full map panel exceeds viewport");
        var picture=control.FindChildren("*","TextureRect",true,false).OfType<TextureRect>().Single();
        Check(picture.Texture.GetWidth()==world.MacroMap.Columns && picture.Texture.GetHeight()==world.MacroMap.Rows
            && picture.StretchMode==TextureRect.StretchModeEnum.KeepAspectCentered,"F7 cropped the larger map");
        foreach (var point in new[] { centre,shore,centre })
        {
            world.Player.GlobalPosition=point;world.Player.Velocity=Vector2.Zero;
            for(int i=0;i<2200 && (!world.Stream.CanMoveTo(point) || world.Stream.PendingCount>0);i++) await Frames(1);
            await Frames(15);
            Check(world.Stream.CanMoveTo(point) && world.GeneratedWater.RegionAt(point)!=null,"Ocean section did not stream/publish");
            Check(world.GeneratedWater.TileCount<=world.Stream.LoadedCount*world.WaterPlan.Bodies.Count,"Ocean materialized full-world tiles");
        }
        Check(world.MacroMap.BiomeAt(centre)==null && world.Player.SurfaceSpeedMultiplier<1,"Deep ocean assigned land or lost water interaction");
        var data=new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing) { LiquidBlocked=world.GeneratedWater.Contains };
        Check(data.Build(ChunkCoord.At(centre,world.Config.Streaming.ChunkSize)).Count==0,"Land scenery spawned in deep ocean");
        var b=world.Blueprint;
        var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,b.ProfileId,b.RunId,b.WorldId,CampaignSession.ContentFingerprint(),DateTime.UtcNow.ToString("O"),b,
            new SavedPlayer(centre.X,centre.Y,Health:100,Inventory:world.Harvesting.Inventory.Capture()),new SavedClock(12,600,true,world.Environment.Seasons.Capture(),world.Environment.RegionalClimate.Capture()),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
        var root=Path.Combine(Path.GetTempPath(),"ocean-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new CampaignStore(root);store.Save(snapshot,true);var loaded=store.Load(b.ProfileId).Snapshot;
            Reject(()=>store.Validate(snapshot with { Blueprint=b with { SchemaVersion=8 } }),"Ocean accepted under older blueprint schema");
            string mapHash=world.MacroMap.Hash,waterHash=world.WaterPlan.Hash;
            world.Free();await Frames(3);CampaignSession.TestStorageRoot=root;CampaignSession.Continue(loaded);
            var continued=scene.Instantiate<WorldScene>();continued.Config=(WorldMasterConfig)continued.Config.Duplicate();
            continued.Config.Generation=(WorldGenerationMasterConfig)continued.Config.Generation.Duplicate(true);
            continued.Config.Generation.Water=null;continued.Config.Generation.Bounds=new Rect2(-500,-500,1000,1000);
            AddChild(continued);
            for(int i=0;i<300 && continued.StartupError==null && continued.Saves?.ReadyToSave!=true && string.IsNullOrEmpty(continued.Saves?.LastError);i++) await Frames(1);
            Check(continued.StartupError==null && continued.Saves?.ReadyToSave==true,"Deep ocean Continue failed: "+continued.StartupError+continued.Saves?.LastError);
            Check(continued.MacroMap.Hash==mapHash && continued.WaterPlan.Hash==waterHash && continued.MacroMap.BiomeAt(centre)==null,"Continue resized/rerolled ocean geography");
            await Frames(10);continued.Free();await Frames(3);
            // A real pre-ocean geography-first campaign keeps the smaller bounds and has no ocean inserted.
            var oldConfig=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/StandardWorldGeneration.tres").Duplicate(true);
            oldConfig.Bounds=new Rect2(-22500,-16875,45000,33750);oldConfig.CellSize=256;oldConfig.Water=null;
            var oldFeatures=WorldFeaturePlanner.Build(oldConfig,42);var oldWater=new WaterBodyPlan(42,oldConfig.Bounds,Array.Empty<PlannedWaterBody>(),Array.Empty<string>());
            var oldGeo=new WorldGeographyPlan(42,oldConfig.Bounds,oldWater,1);var oldMap=MacroWorldBuilder.Build(oldConfig,42,oldGeo);
            var oldBlueprint=WorldBlueprintManifest.Create(b.ProfileId,b.RunId,oldMap,oldFeatures) with { SchemaVersion=8,WorldId=b.WorldId,GeographyVersion=1,Water=oldWater.Snapshot(),
                LocalGeneratorVersion=ChunkSceneryData.GeneratorVersion,ChunkSize=512,CandidateCellSize=96,
                MacroBiomes=oldMap.Biomes.Select(v=>new SavedMacroBiome(v.Id,v.Name,v.Colour.R,v.Colour.G,v.Colour.B,v.Colour.A)).ToArray(),
                MacroRegions=oldMap.Regions.Select(v=>new SavedMacroRegion(v.Id,v.BiomeIndex,v.Centre.X,v.Centre.Y,v.TargetDiameter,v.CellCount)).ToArray() };
            var oldSnapshot=snapshot with { Blueprint=oldBlueprint,Player=snapshot.Player with { X=0,Y=0 } };
            store.Save(oldSnapshot,true);CampaignSession.Continue(store.Load(b.ProfileId).Snapshot);
            var legacy=scene.Instantiate<WorldScene>();AddChild(legacy);
            for(int i=0;i<300 && legacy.StartupError==null && legacy.Saves?.ReadyToSave!=true && string.IsNullOrEmpty(legacy.Saves?.LastError);i++) await Frames(1);
            Check(legacy.StartupError==null && legacy.Saves?.ReadyToSave==true,"Pre-ocean Continue failed: "+legacy.StartupError+legacy.Saves?.LastError);
            Check(legacy.MacroMap.GenerationVersion==2 && legacy.GeographyPlan.PlanVersion==1 && legacy.Config.Bounds==oldConfig.Bounds
                && legacy.MacroMap.Hash==oldMap.Hash && legacy.WaterPlan.Bodies.Count==0 && legacy.GeneratedWater==null,"Old campaign was enlarged or gained an ocean");
            await Frames(10);legacy.Free();await Frames(3);
        }
        finally { CampaignSession.TestStorageRoot=null;CampaignSession.Clear();if(Directory.Exists(root)) Directory.Delete(root,true); }
    }
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool value,string message) { _checks++;if(!value) throw new Exception(message); }
    private void Reject(Action action,string message) { bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidDataException){rejected=true;}Check(rejected,message); }
}
