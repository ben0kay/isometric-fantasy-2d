// Verify the bounded pollen batch, dormant lifecycle and independence from haze/leaves/torch effects.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidatePollenBatch : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"POLLEN BATCH PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        var atmosphere=env.GetNode<WorldAtmosphere>("Atmosphere"); var config=atmosphere.Config;
        env.DayNight.Clock.Config.Paused=true; env.DayNight.Clock.SetHour(12); env.Wind.Config.Strength=0;
        config.Pollen=false; config.Haze=false; config.Leaves=false; config.Smoke=false; config.Embers=false;
        await Frames(180);
        Check(!atmosphere.IsProcessing() && atmosphere.GetNodeOrNull("PollenDust")==null,"All-off atmosphere keeps polling or retains pollen batch");
        int redraws=atmosphere.RedrawRequests, haze=atmosphere.HazeUpdates;
        await Frames(5);
        Check(atmosphere.RedrawRequests==redraws && atmosphere.HazeUpdates==haze,"All-off atmosphere redraws or rewrites haze");
        RuntimeFrameTrace.Start(GetInstanceId()); config.Pollen=true; config.PollenCount=35; await Frames(5);
        var pollen=atmosphere.GetNode<PollenDust>("PollenDust");
        Check(!atmosphere.IsProcessing() && !pollen.IsProcessing(),"Screen dust alone creates an idle owner/helper process loop");
        Check(pollen.Batch.Multimesh.InstanceCount==100 && pollen.Batch.Multimesh.VisibleInstanceCount==35,"Pollen budget is not bounded/compatible");
        Check(pollen.GetChildCount()==1 && pollen.Batch.GetChildCount()==0 && !pollen.FollowViewportEnabled,"Screen dust creates per-speck nodes or follows world canvas");
        Check(atmosphere.RedrawRequests==redraws,"Pollen-only animation requests C# redraws");
        RuntimeFrameTrace.TryGetLatest("WorldAtmosphere.Draw",out _,out int drawCalls);
        Check(drawCalls==0,"Pollen-only atmosphere still executes immediate-mode draws");
        int updates=pollen.AppliedUpdates; await Frames(5);
        Check(pollen.AppliedUpdates==updates,"Stable pollen rewrites unchanged batch inputs");
        config.PollenCount=1000;
        Check(pollen.Batch.Multimesh.VisibleInstanceCount==100,"Pollen count escapes the 100-instance cap");
        config.PollenCount=0;
        Check(atmosphere.GetNodeOrNull("PollenDust")==null && !atmosphere.IsProcessing(),"Zero count retains pollen GPU state or ticking");
        config.PollenCount=12; pollen=atmosphere.GetNode<PollenDust>("PollenDust");
        Check(pollen.Batch.Multimesh.VisibleInstanceCount==12,"Count/profile cannot wake dormant pollen");
        updates=pollen.AppliedUpdates; env.Wind.Config.Strength=1; await Frames(3);
        Check(pollen.AppliedUpdates==updates,"Screen dust incorrectly follows world wind");
        var material=(ShaderMaterial)pollen.Batch.Material; env.DayNight.Clock.SetHour(0); await Frames(3);
        Check(pollen.AppliedUpdates==updates,"Screen dust incorrectly follows day/night");
        var viewport=GetViewport(); var transform=viewport.CanvasTransform; var dustTransform=pollen.Batch.Transform;
        viewport.CanvasTransform=new Transform2D(0,new Vector2(0.5f,0.5f),0,new Vector2(100,50)); atmosphere._Process(0);
        Check(pollen.AppliedUpdates==updates && pollen.Batch.Transform==dustTransform,"Camera movement/zoom moves the screen overlay");
        viewport.CanvasTransform=transform; config.Pollen=false;
        Check(!atmosphere.IsProcessing() && atmosphere.GetNodeOrNull("PollenDust")==null,"Pollen master off leaves active batch");
        config.Haze=true;
        Check(atmosphere.IsProcessing() && atmosphere.GetNode<Polygon2D>("Haze").Visible,"Sleeping atmosphere cannot wake haze independently");
        config.HazeDensity=0;
        Check(!atmosphere.IsProcessing() && !atmosphere.GetNode<Polygon2D>("Haze").Visible,"Zero haze density does not sleep");
        config.HazeDensity=0.22f;
        Check(atmosphere.IsProcessing() && atmosphere.GetNode<Polygon2D>("Haze").Visible,"Haze density change cannot wake haze");
        config.Haze=false; config.Leaves=true; await Frames(3);
        Check(atmosphere.IsProcessing() && atmosphere.RedrawRequests>redraws,"Pollen off disables shared leaf drawing");
        config.Leaves=false; config.Smoke=true; await Frames(3);
        Check(atmosphere.IsProcessing(),"Pollen off disables independent torch smoke");
        config.Smoke=false; config.Embers=true; await Frames(3);
        Check(atmosphere.IsProcessing(),"Pollen off disables independent embers");
        atmosphere.Torch.Visible=false;
        Check(!atmosphere.IsProcessing(),"Hidden torch leaves the atmosphere ticking with only embers enabled");
        atmosphere.Torch.Visible=true;
        Check(atmosphere.IsProcessing(),"Torch visibility event cannot wake sleeping embers");
        config.Embers=false; config.Pollen=true; config.PollenCount=35; await Frames(3); config.Pollen=false;
        Check(!atmosphere.IsProcessing() && atmosphere.GetNodeOrNull("PollenDust")==null,"Repeated toggle leaves pollen processing/batch");
        var sub=new SubViewport { Size=new Vector2I(320,180) }; AddChild(sub);
        var resized=new PollenDust(); sub.AddChild(resized); resized.UpdateSettings(10);
        Check(((ShaderMaterial)resized.Batch.Material).GetShaderParameter("view_size").AsVector2()==new Vector2(320,180),"Dust ignores its viewport dimensions");
        int resizeUpdates=resized.AppliedUpdates; sub.Size=new Vector2I(640,360); await Frames(2);
        Check(resized.AppliedUpdates>resizeUpdates && resized.Batch.Position==new Vector2(320,180),"Viewport resize does not reposition screen batch");
        resized.Free(); sub.Size=new Vector2I(800,600); sub.QueueFree();
        RuntimeFrameTrace.Release(GetInstanceId()); world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
