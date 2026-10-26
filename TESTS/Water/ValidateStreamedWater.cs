// WG2 contracts: cropped shared fields, bounded/cancelled lifecycle, ground-nav and production interaction/save integration.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateStreamedWater : Node
{
    private int _checks;
    // Use disposable worlds/storage only; no real profiles or authored Resource writes.
    // =========================================================
    public override async void _Ready()
    {
        try { GeometryAndLifetime(); await WorldIntegration(); GD.Print($"WG2 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Adjacent tiles must share scalar samples and release resources, including partial preparation.
    // =========================================================
    private void GeometryAndLifetime()
    {
        var body=new PlannedWaterBody("test.lake","water",new[] { new Vector2(-180,-180),new Vector2(180,-180),new Vector2(180,180),new Vector2(-180,180) });
        var plan=new WaterBodyPlan(5,new Rect2(-2000,-2000,4000,4000),new[] { body },Array.Empty<string>());
        var config=new WorldLiquidMasterConfig { Animate=false };
        var runtime=new GeneratedWaterRuntime(); AddChild(runtime); runtime.Initialize(plan,config);
        var left=new ChunkCoord(-1,0); var right=new ChunkCoord(0,0);
        foreach (int step in runtime.PrepareChunkSteps(left,left.Bounds(256))) { }
        Check(runtime.TileCount==0 && runtime.RegionAt(new Vector2(-60,60))==null,"Partial chunk published water early");
        foreach (int step in runtime.PrepareChunkSteps(right,right.Bounds(256))) { }
        runtime.PublishChunk(left); runtime.PublishChunk(right);
        Check(runtime.TileCount==2 && runtime.ResidentCount==1,"Per-chunk duplicate region/incorrect tile count");
        var region=runtime.RegionAt(new Vector2(60,60));
        Check(region!=null && Mathf.Abs(region.SignedDistance(new Vector2(60,60))-120)<0.001f,"Supplied shoreline/distance changed");
        var tiles=region.GetChildren().OfType<LiquidSurfaceTile>().ToArray();
        Check(tiles.All(t => t.TileBounds.Size==Vector2.One*256),"Water quad escaped chunk");
        using (var a=tiles[0].Field.GetImage()) using (var b=tiles[1].Field.GetImage())
        {
            for (int y=6;y<240;y+=8)
            {
                // Compare the same world texel center in the shared four-unit halo.
                var point=new Vector2(2,y);
                float Sample(LiquidSurfaceTile tile,Image image)
                {
                    var pixel=((point-tile.FieldBounds.Position)/4f).Floor();
                    return image.GetPixel((int)pixel.X,(int)pixel.Y).R;
                }
                Check(Mathf.Abs(Sample(tiles[0],a)-Sample(tiles[1],b))<0.000001f,"Chunk seam distance samples disagree");
            }
        }
        var actors=new Node2D(); AddChild(actors);
        var nav=new StaticObstacleNavigation(); nav.Build(actors,new Rect2(-600,-600,1200,1200),24,17,8,new[] { body.CopyPolygon() });
        Check(!nav.IsWalkable(Vector2.Zero),"Enemy navigation accepts water");
        Check(nav.IsWalkable(new Vector2(350,0)),"Lake rectangle blocked unrelated land");
        var path=nav.FindPath(new Vector2(-400,0),new Vector2(400,0));
        Check(path.Length>0 && path.All(p => body.SignedDistance(p)<0),"Enemy route crosses lake"); actors.Free();
        config.Enabled=false; Check(!runtime.IsProcessing() && !region.Visible,"Disabled water did not sleep");
        config.Enabled=true; Check(region.Visible,"Enabled water did not reactivate");
        runtime.ReleaseChunk(left); Check(runtime.TileCount==1 && runtime.ResidentCount==1,"Unloading one tile lost shared body");
        runtime.ReleaseChunk(right); Check(runtime.TileCount==0 && runtime.ResidentCount==0 && !runtime.IsProcessing(),"Last unload leaked presentation");
        using (var pending=runtime.PrepareChunkSteps(left,left.Bounds(256)).GetEnumerator()) Check(pending.MoveNext(),"Expected row-sliced field preparation");
        runtime.ReleaseChunk(left); Check(runtime.ResidentCount==0 && runtime.TileCount==0,"Cancellation retained region or texture");
        foreach (int step in runtime.PrepareChunkSteps(right,right.Bounds(256))) { }
        runtime.PublishChunk(right); Check(runtime.RegionAt(new Vector2(60,60)).Contains(new Vector2(60,60)),"Reload changed water");
        runtime.ReleaseChunk(right); runtime.Free();
    }
    // Exercise the actual streaming scheduler and single player surface owner around a generated lake.
    // =========================================================
    private async Task WorldIntegration()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate();
        world.Config.Generation=(WorldGenerationMasterConfig)world.Config.Generation.Duplicate();
        world.Config.Generation.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=3 };
        world.Config.RandomSeedOnLoad=false; world.Config.FixedSeed=42;
        AddChild(world);
        Check(world.StartupError==null,"World startup: "+world.StartupError);
        Check((world.Blueprint.SchemaVersion==9 || world.Blueprint.SchemaVersion==10 || world.Blueprint.SchemaVersion==11) && world.GeneratedWater!=null,"New world lacks playable-water version/runtime");
        var body=world.WaterPlan.Bodies[0]; var centre=body.Bounds.GetCenter();
        world.Player.GlobalPosition=centre; world.Player.Velocity=Vector2.Zero;
        await WaitLoaded(world,centre);
        var runtime=world.GeneratedWater;
        Check(runtime.TileCount>0 && runtime.ResidentCount<=world.WaterPlan.Bodies.Count,"Streaming did not bind nearby water");
        Check(world.GetTree().GetNodesInGroup("world_liquid_regions").Count<=4,"Duplicate resident liquid regions");
        Check(world.Player.SurfaceSpeedMultiplier<1,"Generated lake did not slow player");
        var wading=world.GetNode<WorldLiquidWading>("TerrainRoot/Liquids/Wading");
        Check(wading.Immersion>0,"Generated lake did not immerse player");
        var data=new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing);
        data.LiquidBlocked=runtime.Contains;
        var coord=ChunkCoord.At(centre,world.Config.Streaming.ChunkSize);
        for (int y=-1;y<=1;y++) for (int x=-1;x<=1;x++)
            foreach (var item in data.Build(new ChunkCoord(coord.X+x,coord.Y+y)))
                Check(!runtime.Contains(item.Position,item.Radius+(item.Content.GrassLike ? 30 : 15)),"Generated scenery overlaps water");
        var pond=world.GetNode<WorldLiquidRegion>("TerrainRoot/Liquids/Pond");
        pond.Config.Animate=false;
        world.Environment.Weather.Config.RainIntensity=0;
        await Frames(4); Check(!runtime.IsProcessing(),"Static/dry generated water kept processing");
        world.Environment.Weather.Config.RainIntensity=1;
        await Frames(4); Check(runtime.IsProcessing(),"Rain did not wake resident cosmetics");
        Check(runtime.RegionAt(centre).GetNode<WorldLiquidRainSplashes>("RainSplashes").IsProcessing(),"Rain pool did not wake");
        world.Environment.Weather.Config.RainIntensity=0;
        await Frames(4); Check(!runtime.RegionAt(centre).GetNode<WorldLiquidRainSplashes>("RainSplashes").IsProcessing(),"Dry rain pool kept processing");
        CampaignSnapshot frozen=null;
        var root=Path.Combine(Path.GetTempPath(),"wg2-"+Guid.NewGuid().ToString("N"));
        try
        {
            var b=world.Blueprint; var snapshot=new CampaignSnapshot(CampaignSnapshot.CurrentSchema,b.ProfileId,b.RunId,b.WorldId,CampaignSession.ContentFingerprint(),DateTime.UtcNow.ToString("O"),b,
                new SavedPlayer(centre.X,centre.Y,Health:100,Inventory:world.Harvesting.Inventory.Capture()),new SavedClock(12,600,true),new SavedTestEntity(false,0,0,0),Array.Empty<SavedChunk>());
            var store=new CampaignStore(root); store.Save(snapshot,true);
            frozen=store.Load(b.ProfileId).Snapshot;
            var loaded=frozen.Blueprint;
            Check((loaded.SchemaVersion==9 || loaded.SchemaVersion==10 || loaded.SchemaVersion==11) && WaterBodyPlan.Restore(loaded.Water,loaded.Seed,world.MacroMap.Bounds).Hash==world.WaterPlan.Hash,"Playable water lost disk identity");
            store.Validate(Legacy(snapshot,4)); // WG1 metadata stays legacy, never promoted on restore.
            Reject(() => store.Validate(snapshot with { Blueprint=b with { Water=null } }),"Playable save accepted missing water");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
        world.Player.GlobalPosition=Vector2.Zero; await WaitLoaded(world,Vector2.Zero); await Frames(100);
        Check(runtime.TileCount==0 && runtime.ResidentCount==0,"Distant lake stayed resident after unload");
        Check(world.Player.SurfaceSpeedMultiplier==1 && !wading.IsProcessing(),"Leaving water retained speed/visual callback");
        world.Player.GlobalPosition=centre; await WaitLoaded(world,centre);
        Check(runtime.RegionAt(centre)!=null && world.Player.SurfaceSpeedMultiplier<1,"Travel/return failed to restore lake");
        world.Free(); await Frames(3);
        CampaignSession.TestStorageRoot=root;
        try
        {
            await RestoreWorld(frozen,true);
            await RestoreWorld(Legacy(frozen,5),true);
            await RestoreWorld(Legacy(frozen,4) with { Player=frozen.Player with { X=0,Y=0 } },false);
            await RestoreWorld(Legacy(frozen,4) with { Blueprint=Legacy(frozen,4).Blueprint with { Water=null },Player=frozen.Player with { X=0,Y=0 } },false);
        }
        finally
        {
            CampaignSession.TestStorageRoot=null; CampaignSession.Clear();
            if (Directory.Exists(root)) Directory.Delete(root,true);
        }
    }
    // Build a genuine version-1 macro snapshot for legacy Continue checks, rather than relabeling a version-2 hash.
    // =========================================================
    private static CampaignSnapshot Legacy(CampaignSnapshot source,int schema)
    {
        var config=GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres");
        var map=MacroWorldBuilder.Build(config,source.Blueprint.Seed); var b=source.Blueprint;
        return source with { Blueprint=b with { SchemaVersion=schema,Climate=null,AuthorPolicy=null,GeneratorVersion=1,GeographyVersion=0,MapHash=map.Hash,
            RegionCells=map.CopyRegionCells(),RegionBiomes=map.Regions.Select(r=>r.BiomeIndex).ToArray(),
            MacroRegions=map.Regions.Select(r=>new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray() } };
    }
    // Real Continue uses only saved outcomes even when the current new-world planner is disabled.
    // =========================================================
    private async Task RestoreWorld(CampaignSnapshot snapshot,bool playable)
    {
        CampaignSession.Continue(snapshot);
        var restored=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        restored.Config=(WorldMasterConfig)restored.Config.Duplicate();
        restored.Config.Generation=(WorldGenerationMasterConfig)restored.Config.Generation.Duplicate();
        restored.Config.Generation.Water=null;
        AddChild(restored);
        for (int i=0;i<240 && restored.StartupError==null && restored.Saves?.ReadyToSave!=true && string.IsNullOrEmpty(restored.Saves?.LastError);i++) await Frames(1);
        Check(restored.Saves?.ReadyToSave==true,"Continue did not finish save/input initialization: "+restored.Saves?.LastError);
        await Frames(8);
        Check(restored.StartupError==null,"Continue failed: "+restored.StartupError);
        Check((restored.GeneratedWater!=null)==playable,"Continue promoted legacy water or lost playable water");
        Check(restored.Blueprint.WorldId==snapshot.WorldId && restored.MacroMap.Hash==snapshot.Blueprint.MapHash,"Continue changed world identity/map");
        if (playable) Check(restored.GeneratedWater.RegionAt(restored.Player.GlobalPosition)!=null,"Continue did not materialize saved lake at arrival");
        Check(restored.Saves.LastError=="","Continue blocked player arrival: "+restored.Saves.LastError);
        if (!playable && snapshot.Blueprint.Water==null)
        {
            var pond=restored.GetNode<WorldLiquidRegion>("TerrainRoot/Liquids/Pond");
            restored.Player.GlobalPosition=pond.GlobalPosition;
            await WaitLoaded(restored,pond.GlobalPosition);
            Check(restored.Player.SurfaceSpeedMultiplier<1,"Original fixed pond wading regressed");
        }
        restored.Free(); await Frames(3);
    }
    // Use bounded waits with explicit failure rather than claiming a partial streamed area is loaded.
    // =========================================================
    private async Task WaitLoaded(WorldScene world,Vector2 point)
    {
        for (int i=0;i<1600;i++)
        { await Frames(1); if (world.Stream.CanMoveTo(point) && world.Stream.PendingCount==0) { await Frames(12); return; } }
        throw new Exception("Streaming did not settle at "+point);
    }
    private async Task Frames(int count) { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool value,string message) { _checks++; if (!value) throw new Exception(message); }
    private void Reject(Action action,string message) { bool bad=false; try { action(); } catch (InvalidDataException) { bad=true; } Check(bad,message); }
}
