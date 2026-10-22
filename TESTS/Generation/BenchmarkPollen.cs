// Repeatable CPU submission diagnostic. Headless has no GPU timing and cannot establish game FPS.
using Godot;
using System;
using System.Threading.Tasks;
public partial class BenchmarkPollen : Node
{
    public override async void _Ready()
    {
        try { await Run(); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        var atmosphere=env.GetNode<WorldAtmosphere>("Atmosphere"); var config=atmosphere.Config;
        config.Haze=false; config.Leaves=false; config.Smoke=false; config.Embers=false; env.Wind.Config.Strength=0;
        await Frames(200);
        RuntimeFrameTrace.Start(GetInstanceId());
        foreach(int count in new[]{35,0,100,0})
        {
            config.Pollen=count>0; config.PollenCount=count; await Frames(60);
            double draw=0,process=0; int draws=0; double[] values=new double[180];
            for(int i=0;i<values.Length;i++)
            {
                await Frames(1);
                RuntimeFrameTrace.TryGetLatest("WorldAtmosphere.Draw",out double d,out int calls);
                RuntimeFrameTrace.TryGetLatest("WorldAtmosphere.Process",out double p,out _);
                values[i]=d; draw+=d; process+=p; draws+=calls;
            }
            Array.Sort(values);
            GD.Print($"POLLEN CPU SAMPLE count={count} drawMean={draw/values.Length:F5}ms drawP95={values[(int)(values.Length*0.95)]:F5}ms processMean={process/values.Length:F5}ms draws={draws}/{values.Length}");
        }
        RuntimeFrameTrace.Release(GetInstanceId()); world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
}
