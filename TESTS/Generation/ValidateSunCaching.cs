// Validate paused sun caches, independent actor/hit/wind updates and bounded running-time projections.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateSunCaching : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"SUN CACHE PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment"); var day=env.DayNight;
        env.Clock.Config.Paused=true; env.Clock.SetHour(12); env.Wind.Config.Strength=0;
        await Frames(200);
        int curves=day.SolarCalculations, ambient=day.AmbientWrites;
        long visits=day.Shadows.StaticCasterVisits;
        await Frames(5);
        Check(day.SolarCalculations==curves && day.AmbientWrites==ambient,"Paused solar curves/ambient keep rewriting");
        Check(day.Shadows.StaticCasterVisits==visits,"Paused static casters keep being visited");
        long mats=day.Shadows.MaterialWrites; env.Wind.Config.Strength=-2; await Frames(3);
        Check(day.Shadows.MaterialWrites>mats && day.Shadows.StaticCasterVisits==visits,"Paused wind freezes shadow sway or reprojects scenery");
        mats=day.Shadows.MaterialWrites; await Frames(3);
        Check(day.Shadows.MaterialWrites==mats,"Unchanged shadow materials keep uploading");
        long writes=day.Shadows.ProjectionWrites; env.Clock.SetHour(12.001f); await Frames(2);
        Check(day.Shadows.ProjectionWrites>writes,"Tiny manual time scrub is suppressed by tolerance");
        writes=day.Shadows.ProjectionWrites; day.Config.MinimumLength+=0.001f; await Frames(2);
        Check(day.Shadows.ProjectionWrites>writes,"Profile edit does not refresh solar projection immediately");
        curves=day.SolarCalculations; ambient=day.AmbientWrites; env.Weather.Lightning.Trigger(true); await Frames(3);
        Check(day.AmbientWrites>ambient && day.SolarCalculations==curves,"Paused sun cache freezes lightning or recalculates solar curves for it");
        var tree=GetTree().GetNodesInGroup("world_trees").OfType<FantasyGame.World.Trees.Tree>().First(t=>t.GetParent().HasMeta("chunk_shadow"));
        var renderer=(SunShadowRenderer)GodotObject.InstanceFromId(tree.GetParent().GetMeta("chunk_shadow").AsUInt64());
        renderer.CullOffscreen=false; day.Shadows.CullOffscreen=false; day._Process(0);
        writes=renderer.ProjectionWrites;
        var hits=tree.GetNode<TreeHitResponse>("HitResponse"); hits.ReceiveHit(Vector2.Right); hits._Process(0.05); day._Process(0);
        Check(renderer.ProjectionWrites>writes,"Paused hit wobble leaves the tree shadow frozen");
        hits._Process(5); hits._Process(5); day._Process(0);
        // Isolate geometry policy from world actors, AI, streaming and clock callbacks.
        env.Clock.SetProcess(false); day.SetProcess(false);
        var actors=new Node2D(); AddChild(actors);
        var body=new StaticBody2D(); actors.AddChild(body);
        var sprite=new Sprite2D { Name="Sprite", Texture=new GradientTexture2D { Width=20,Height=200 }, Position=new Vector2(0,-100) }; body.AddChild(sprite);
        var shadows=new SunShadowRenderer { Wind=env.Wind, CullOffscreen=false }; AddChild(shadows); shadows.Attach(actors);
        shadows.UpdateProjection(0,1,0.5f,0,0.25f);
        var silhouette=shadows.GetChildren().OfType<Sprite2D>().Single(); writes=shadows.ProjectionWrites;
        shadows.UpdateProjection(0,1,0.5f,0,0.25f);
        Check(shadows.ProjectionWrites==writes,"Unchanged fixture projection is uploaded again");
        double maxError=0;
        var originalCanvas=GetViewport().CanvasTransform; GetViewport().CanvasTransform=Transform2D.Identity;
        for(int i=1;i<=100;i++)
        {
            float angle=i*0.00001f; shadows.UpdateProjection(angle,1,0.5f,0,0.25f);
            var exact=new Transform2D(Vector2.Right,-Vector2.FromAngle(angle),Vector2.Zero)*sprite.Transform;
            maxError=Math.Max(maxError,(exact*new Vector2(0,-100)-silhouette.GlobalTransform*new Vector2(0,-100)).Length());
        }
        Check(maxError<=0.25 && shadows.ProjectionWrites<writes+100,"Tolerance fails to bound error or skip tiny motion");
        writes=shadows.ProjectionWrites; shadows.UpdateProjection(0.1f,1,0.5f,0,0.25f);
        Check(shadows.ProjectionWrites>writes,"Accelerated/large sun movement is delayed");
        writes=shadows.ProjectionWrites; shadows.UpdateProjection(0.100001f,1,0.5f,0,0);
        Check(shadows.ProjectionWrites>writes,"Zero tolerance cannot restore exact updates");
        sprite.Visible=false; shadows.UpdateProjection(0.100001f,1,0.5f,0,0.25f);
        Check(!silhouette.Visible,"Paused cache ignores hidden scenery");
        sprite.Visible=true; shadows.UpdateProjection(0.100001f,1,0.5f,0,0.25f);
        Check(silhouette.Visible,"Paused cache cannot restore scenery visibility");
        shadows.UpdateProjection(0.100001f,1,0,0,0.25f);
        Check(!silhouette.Visible,"Night/zero opacity leaves silhouettes visible");
        shadows.UpdateProjection(0.100001f,1,0.5f,0,0.25f); Check(silhouette.Visible,"Dawn cannot restore cached silhouettes");
        shadows.CullOffscreen=true;
        GetViewport().CanvasTransform=new Transform2D(0,Vector2.One,0,new Vector2(-100000,-100000));
        shadows.UpdateProjection(0.100001f,1,0.5f,0,0.25f);
        Check(!silhouette.Visible,"Paused camera movement leaves off-screen shadow visible");
        GetViewport().CanvasTransform=Transform2D.Identity; shadows.UpdateProjection(0.100001f,1,0.5f,0,0.25f);
        Check(silhouette.Visible,"Paused camera re-entry fails to restore silhouette");
        shadows.CullOffscreen=false;
        var zoomed=new Transform2D(0.4f,new Vector2(4,2),0,Vector2.Zero); GetViewport().CanvasTransform=zoomed;
        maxError=0; writes=shadows.ProjectionWrites;
        for(int i=0;i<50;i++)
        {
            float angle=0.2f+i*0.00001f; shadows.UpdateProjection(angle,1,0.5f,0,0.25f);
            var exact=new Transform2D(Vector2.Right,-Vector2.FromAngle(angle),Vector2.Zero)*sprite.Transform;
            maxError=Math.Max(maxError,((zoomed*(exact*new Vector2(0,-100)))-(zoomed*(silhouette.GlobalTransform*new Vector2(0,-100)))).Length());
        }
        Check(maxError<=0.25 && shadows.ProjectionWrites<writes+50,"Rotated/nonuniform zoom breaks pixel error bound");
        GetViewport().CanvasTransform=originalCanvas;
        shadows.Free(); sprite.Visible=false; actors.QueueFree();
        // Dynamic actors follow current position even while solar geometry is reused.
        var dynamicActors=new Node2D(); AddChild(dynamicActors); var actor=new Node2D(); dynamicActors.AddChild(actor);
        actor.AddChild(new Sprite2D { Name="Sprite",Texture=new GradientTexture2D() });
        var dynamicShadows=new SunShadowRenderer { Wind=env.Wind,CullOffscreen=false }; AddChild(dynamicShadows); dynamicShadows.Attach(dynamicActors);
        dynamicShadows.UpdateProjection(0,1,0.5f,0,0.25f); var dynamicSprite=dynamicShadows.GetChildren().OfType<Sprite2D>().Single();
        actor.Position=new Vector2(50,20); dynamicShadows.UpdateProjection(0,1,0.5f,0,0.25f);
        Check(dynamicSprite.GlobalPosition==actor.GlobalPosition,"Paused projection cache freezes moving actors");
        dynamicShadows.Free(); dynamicActors.QueueFree(); world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
