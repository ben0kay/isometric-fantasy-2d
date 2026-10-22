// Validate nearby-only cosmetics/occlusion, idle skips, exact restoration and index edge cases.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class ValidateSpatialScheduling : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"SPATIAL SCHEDULING PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        env.Clock.Config.Paused=true; env.Clock.SetHour(12); await Frames(200);
        long checks=env.VisibilityChunkChecks,passes=env.VisibilityPasses;
        await Frames(5);
        Check(env.VisibilityChunkChecks==checks && env.VisibilityPasses==passes,"Stationary camera repeats cosmetic chunk checks");
        env.Config.CullOffscreenSceneryEffects=false; await Frames(2);
        Check(env.CosmeticChunkCount==env.RegisteredChunkCount,"Culling disable does not wake every chunk");
        checks=env.VisibilityChunkChecks; var viewport=GetViewport(); var canvas=viewport.CanvasTransform;
        var shifted=canvas; shifted.Origin-=new Vector2(10000,10000); viewport.CanvasTransform=shifted; env._Process(0);
        Check(env.VisibilityChunkChecks==checks,"Disabled culling checks chunks for camera movement");
        env.Config.CullOffscreenSceneryEffects=true; env._Process(0);
        Check(env.CosmeticChunkCount<env.RegisteredChunkCount,"Camera teleport cannot suspend previous active chunks");
        viewport.CanvasTransform=canvas; env._Process(0);
        Check(env.CosmeticChunkCount>0 && env.CosmeticChunkCount<env.RegisteredChunkCount,"Camera re-entry fails to reactivate nearby cosmetics");
        passes=env.VisibilityPasses; env.Config.SceneryPrepareMargin+=16; env._Process(0);
        Check(env.VisibilityPasses==passes+1,"Margin edits do not invalidate cached view");
        checks=env.VisibilityChunkChecks; var tiny=canvas; tiny.Origin-=canvas.X*2; viewport.CanvasTransform=tiny; env._Process(0);
        Check(env.VisibilityChunkChecks==checks,"Tiny camera movement ignores the safe visibility envelope");
        viewport.CanvasTransform=canvas; env.Config.SceneryVisibilitySlack=0; env._Process(0); checks=env.VisibilityChunkChecks;
        viewport.CanvasTransform=tiny; env._Process(0);
        Check(env.VisibilityChunkChecks>checks,"Zero slack cannot restore exact view checks");
        viewport.CanvasTransform=canvas; env.Config.SceneryVisibilitySlack=32; env._Process(0);
        // An inactive chunk enters at prepare bounds and leaves only beyond retention bounds.
        Rect2 prepared=SceneryVisualBounds.View(env).Grow(Mathf.Max(64,env.Config.SceneryPrepareMargin));
        float band=env.Config.SceneryRetentionMargin;
        var chunk=new Node2D(); world.ChunkActorsRoot.AddChild(chunk);
        var marker=new StaticBody2D { Position=new Vector2(prepared.End.X+band+64,prepared.GetCenter().Y) }; chunk.AddChild(marker);
        marker.AddChild(new Sprite2D { Name="Sprite",Texture=new GradientTexture2D { Width=20,Height=20 } });
        env.RegisterChunk(chunk);
        var wind=(VegetationWind)GodotObject.InstanceFromId(chunk.GetMeta("chunk_wind").AsUInt64());
        marker.Position=new Vector2(prepared.End.X+band/2,marker.Position.Y); env.RefreshScenery(marker);
        Check(!wind.IsProcessing(),"Inactive chunk enters through retention margin instead of prepare margin");
        marker.Position=new Vector2(prepared.End.X-32,marker.Position.Y); env.RefreshScenery(marker);
        Check(wind.IsProcessing(),"Inactive chunk cannot enter prepared view");
        marker.Position=new Vector2(prepared.End.X+band/2,marker.Position.Y); env.RefreshScenery(marker);
        Check(wind.IsProcessing(),"Active chunk loses retention hysteresis");
        marker.Position=new Vector2(prepared.End.X+band+64,marker.Position.Y); env.RefreshScenery(marker);
        Check(!wind.IsProcessing(),"Active chunk does not exit beyond retention");
        marker.Position=new Vector2(prepared.End.X+24,marker.Position.Y); env.RefreshScenery(marker);
        checks=env.VisibilityChunkChecks; var inside=canvas; inside.Origin-=canvas.X*20; viewport.CanvasTransform=inside; env._Process(0);
        Check(wind.IsProcessing() && env.VisibilityChunkChecks==checks,"Refreshed scenery is missed while the camera moves inside cached envelope");
        viewport.CanvasTransform=canvas;
        env.UnregisterScenery(marker); env.UnregisterChunk(chunk); chunk.Free();
        world.QueueFree(); await Frames(3);
        // Shared bounds index handles negative cells, duplicates, updates and very large entries.
        var index=new SpatialBoundsIndex<object>(256); var a=new object(); var b=new object(); var result=new HashSet<object>();
        index.Set(a,new Rect2(-300,-300,600,600)); index.Set(a,new Rect2(-300,-300,600,600));
        index.Query(new Rect2(-257,-257,2,2),result);
        Check(result.Count==1 && index.Count==1,"Index loses negative/multi-cell entry or duplicates it");
        index.Set(a,new Rect2(5000,5000,10,10)); result.Clear(); index.Query(new Rect2(-257,-257,2,2),result);
        Check(result.Count==0,"Moved bounds leave stale bucket membership");
        index.Set(b,new Rect2(-100000,-100000,200000,200000)); result.Clear(); index.Query(new Rect2(0,0,1,1),result);
        Check(result.SetEquals(new[]{b}) && index.CellCount<10,"Oversized artwork explodes bucket count or becomes unqueryable");
        result.Clear(); index.Query(new Rect2(-1000000,-1000000,2000000,2000000),result);
        Check(result.Count==2 && index.LastQueryVisits==2,"Large query fallback scans empty space or loses entries");
        index.Remove(a); index.Remove(b); Check(index.Count==0 && index.CellCount==0,"Unregister leaks spatial buckets");
        // Isolated props: non-unit source alpha, offsets, large artwork and far-away clutter.
        var actors=new Node2D(); AddChild(actors); var player=new Node2D { Position=new Vector2(0,90) }; actors.AddChild(player);
        var controller=new OcclusionController(); AddChild(controller); controller.Initialize(player); controller.SetProcess(false);
        var profile=new OcclusionProfile { MinimumAlpha=0.4f, FadeOutSeconds=0.05f, FadeInSeconds=0.05f };
        var body=new StaticBody2D { Position=new Vector2(0,100) }; actors.AddChild(body);
        var sprite=new Sprite2D { Name="Sprite", Position=new Vector2(0,-50), Texture=new GradientTexture2D { Width=100,Height=100 }, Modulate=new Color(0.5f,0.6f,0.7f,0.8f) }; body.AddChild(sprite);
        controller.Register(body,sprite,profile); controller.SetProcess(false);
        for(int i=0;i<250;i++)
        {
            var distant=new StaticBody2D { Position=new Vector2(5000+i*300,5000) }; actors.AddChild(distant);
            var visual=new Sprite2D { Name="Sprite",Texture=sprite.Texture }; distant.AddChild(visual);
            controller.Register(distant,visual,profile);
        }
        controller.SetProcess(false); controller._Process(0.2);
        Check(Mathf.Abs(sprite.Modulate.A-0.32f)<0.0001f,"Nearby blocker does not fade relative to original alpha");
        Check(controller.CandidateChecks<10,"Nearby detection scans far-away registered props");
        Check(controller.ActiveFadeCount==0,"Settled obstruction stays in fade collection");
        player.Position=new Vector2(0,110); controller._Process(0.2);
        Check(sprite.Modulate.A==0.8f,"Tree fades while player is in front of its anchor");
        player.Position=new Vector2(0,90); controller._Process(0.2);
        body.Visible=false; controller._Process(0.2);
        Check(sprite.Modulate.A==0.8f,"Hidden parent/chunk remains an obstruction");
        body.Visible=true; controller._Process(0.2);
        long queries=controller.DetectionPasses,writes=controller.FadeWrites;
        for(int i=0;i<30;i++) controller._Process(0.1);
        Check(controller.DetectionPasses==queries && controller.FadeWrites==writes,"Stationary settled occlusion keeps querying/writing");
        player.Position=new Vector2(2000,90); controller._Process(0.2);
        Check(sprite.Modulate.A==0.8f,"Teleport leaves former blocker transparent");
        player.Position=new Vector2(0,90); controller._Process(0.2); sprite.Visible=false; controller._Process(0.2);
        Check(sprite.Modulate.A==0.8f,"Hidden/stream-unpublished blocker stays obstructing");
        sprite.Visible=true; controller._Process(0.2); profile.MinimumAlpha=0.5f; controller._Process(0.2);
        Check(sprite.Modulate.A==0.4f,"Shared profile edit misses settled obstruction");
        profile.Enabled=false; controller._Process(0.2);
        Check(sprite.Modulate.A==0.8f,"Disabled profile does not restore original alpha");
        profile.Enabled=true; controller._Process(0.2); controller.Unregister(sprite);
        Check(sprite.Modulate.A==0.8f && controller.ActiveFadeCount==0,"Unregister fails alpha/fade cleanup");
        controller.Register(body,sprite,profile); controller._Process(0.2); sprite.QueueFree(); await Frames(2);
        Check(controller.RegisteredCount==250,"Source exit leaves spatial registration");
        // Dynamic opt-in props reindex at the shared detection interval without per-prop processors.
        var moving=new Node2D { Position=new Vector2(2000,100) }; actors.AddChild(moving);
        var movingVisual=new Sprite2D { Name="Sprite",Texture=new GradientTexture2D { Width=100,Height=100 }, Position=new Vector2(0,-50) }; moving.AddChild(movingVisual);
        controller.Register(moving,movingVisual,profile); controller.SetProcess(false); controller._Process(0.2);
        moving.Position=new Vector2(0,100); controller._Process(0.2);
        Check(movingVisual.Modulate.A<1,"Moving generic prop never enters nearby index");
        controller.Free(); Check(movingVisual.Modulate.A==1,"Controller exit leaves a faded visual");
        profile.MinimumAlpha=0.3f; actors.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
