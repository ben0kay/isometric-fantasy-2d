// Fixed-seed CPU-only projection benchmark at simulated 120Hz; no GPU/FPS conclusions.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
public partial class BenchmarkSunShadows : Node
{
    public override async void _Ready()
    {
        try { await Run(); GetTree().Quit(); } catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        for(int i=0;i<200;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        env.Wind.Config.Strength=0; env.Clock.SetProcess(false); env.DayNight.SetProcess(false);
        bool policy=env.DayNight.Config.GetPropertyList().Any(p=>p["name"].AsString()=="ShadowProjectionTolerancePixels");
        foreach(var sample in new[]{(true,1440f,0.25f),(false,30f,0.25f),(false,1440f,0.25f),(false,1440f,0f)})
        {
            env.Clock.Config.Paused=sample.Item1; env.Clock.Config.CycleSeconds=sample.Item2; env.Clock.SetHour(12);
            if(policy) env.DayNight.Config.Set("ShadowProjectionTolerancePixels",sample.Item3);
            for(int i=0;i<8;i++) { env.Clock._Process(1.0/120); env.DayNight._Process(1.0/120); }
            double[] times=new double[240]; long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<times.Length;i++)
            { env.Clock._Process(1.0/120); env.DayNight._Process(1.0/120); times[i]=env.DayNight.Shadows.LastProjectionMilliseconds; }
            bytes=GC.GetAllocatedBytesForCurrentThread()-bytes; Array.Sort(times);
            GD.Print($"SUN CPU SAMPLE paused={sample.Item1} cycle={sample.Item2} tolerance={sample.Item3} mean={times.Average():F5}ms p95={times[228]:F5}ms bytes={bytes} casters={env.DayNight.Shadows.TotalCasterCount}");
        }
        world.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
}
