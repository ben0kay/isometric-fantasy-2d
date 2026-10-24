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
        world.Config.Generation.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=2,RiverCount=1,RequireMultipleRiverBiomes=true };
        world.Config.RandomSeedOnLoad=false; world.Config.FixedSeed=42;
        AddChild(world); Check(world.StartupError==null,"World failed to initialize: "+world.StartupError);
        Check(world.Blueprint.SchemaVersion==6 && world.WaterPlan.PlanVersion==2,"River world did not use the river-capable save contract");
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
                Check(body.Intersects(tile.TileBounds,40),"River tiled a dry bounding-box chunk");
            var data=new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing);
            data.LiquidBlocked=world.GeneratedWater.Contains;
            foreach (var item in data.Build(ChunkCoord.At(point,world.Config.Streaming.ChunkSize)))
                Check(!world.GeneratedWater.Contains(item.Position,item.Radius+(item.Content.GrassLike ? 30 : 15)),"Scenery spawned in river");
        }
        var root=Path.Combine(Path.GetTempPath(),"wg3-"+Guid.NewGuid().ToString("N"));
        var blueprint=world.Blueprint;
        var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,blueprint.ProfileId,blueprint.RunId,blueprint.WorldId,CampaignSession.ContentFingerprint(),DateTime.UtcNow.ToString("O"),blueprint,
            new SavedPlayer(world.Player.GlobalPosition.X,world.Player.GlobalPosition.Y,Health:100),new SavedClock(12,600,true),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
        try
        {
            var store=new CampaignStore(root); store.Save(snapshot,true); var loaded=store.Load(snapshot.ProfileId).Snapshot;
            var restored=WaterBodyPlan.Restore(loaded.Blueprint.Water,loaded.Blueprint.Seed,world.MacroMap.Bounds);
            Check(restored.Hash==world.WaterPlan.Hash && restored.Bodies.Single(b => b.River!=null).River.Points.SequenceEqual(path.Points),"Disk checkpoint lost river route");
            Reject(() => store.Validate(snapshot with { Blueprint=blueprint with { SchemaVersion=5 } }),"River data accepted by lake-only schema");
            Reject(() => store.Validate(snapshot with { Blueprint=blueprint with { SchemaVersion=4 } }),"River data accepted by metadata-only schema");
            world.Free(); await Frames(3);
            CampaignSession.TestStorageRoot=root; CampaignSession.Continue(loaded);
            var continued=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
            continued.Config=(WorldMasterConfig)continued.Config.Duplicate(); continued.Config.Generation=(WorldGenerationMasterConfig)continued.Config.Generation.Duplicate();
            continued.Config.Generation.Water=null; AddChild(continued); await Frames(10);
            Check(continued.StartupError==null,"Continue failed: "+continued.StartupError);
            Check(continued.Blueprint.SchemaVersion==6 && continued.Blueprint.WorldId==snapshot.WorldId,"Continue changed river world identity");
            Check(continued.WaterPlan.Hash==restored.Hash && continued.GeneratedWater.RegionAt(continued.Player.GlobalPosition)!=null,"Continue rerolled/failed to bind saved river");
            Check(continued.Saves.LastError=="","Saved river arrival blocked: "+continued.Saves.LastError);
            continued.Free(); await Frames(3);
        }
        finally
        {
            CampaignSession.TestStorageRoot=null; CampaignSession.Clear();
            if (Directory.Exists(root)) Directory.Delete(root,true);
        }
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
