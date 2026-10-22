// Fixed-seed CPU-only scheduling diagnostic. Simulated camera/player travel does not stream new chunks.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
public partial class BenchmarkSpatialScheduling : Node
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
        var occlusion=world.GetNode<OcclusionController>("SystemsRoot/Occlusion");
        var player=world.GetNode<Node2D>("Actors/Player"); Vector2 start=player.GlobalPosition;
        var viewport=GetViewport(); var canvas=viewport.CanvasTransform;
        foreach(bool moving in new[]{false,true})
        {
            player.GlobalPosition=start; viewport.CanvasTransform=canvas;
            for(int i=0;i<16;i++) { env._Process(1.0/120); occlusion._Process(1.0/120); }
            double[] visibility=new double[360],fade=new double[360]; long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<visibility.Length;i++)
            {
                if(moving) { player.GlobalPosition=start+new Vector2(i,0); var shifted=canvas; shifted.Origin-=new Vector2(i*2,0); viewport.CanvasTransform=shifted; }
                long t=Stopwatch.GetTimestamp(); env._Process(1.0/120); visibility[i]=Stopwatch.GetElapsedTime(t).TotalMilliseconds;
                t=Stopwatch.GetTimestamp(); occlusion._Process(1.0/120); fade[i]=Stopwatch.GetElapsedTime(t).TotalMilliseconds;
            }
            bytes=GC.GetAllocatedBytesForCurrentThread()-bytes; Array.Sort(visibility); Array.Sort(fade);
            GD.Print($"SPATIAL CPU moving={moving} chunks={env.RegisteredChunkCount} visibilityMean={visibility.Average():F5}ms p95={visibility[342]:F5}ms occlusionMean={fade.Average():F5}ms p95={fade[342]:F5}ms bytes={bytes}");
        }
        viewport.CanvasTransform=canvas; world.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
}
