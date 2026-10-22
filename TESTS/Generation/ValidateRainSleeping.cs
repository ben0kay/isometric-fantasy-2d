// Exercise event-driven rain off/wake, cleared GPU/splash state, dry wetness sleep and independent lightning.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateRainSleeping : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"RAIN SLEEP PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env = world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment"); var weather = env.Weather;
        var ground = weather.GetNode<GroundRainSplashes>("GroundSplashes");
        var liquid = world.GetNode<WorldLiquidRainSplashes>("TerrainRoot/Liquids/Pond/RainSplashes");
        Check(!weather.RainActive && !weather.IsProcessing(),"Default dry world polls rain");
        Check(weather.GetNodeOrNull("RainLayers") == null,"Dry startup allocates rain GPU emitters");
        await Frames(220);
        Check(!ground.IsProcessing() && !ground.Visible && !liquid.IsProcessing() && !liquid.Visible,"Dry splash pools keep ticking/drawing");
        Check(!weather.Lightning.IsProcessing(),"Dry idle lightning keeps ticking");
        Check(env.GetChildren().OfType<VegetationWetness>().All(w => !w.IsProcessing()),"Dry vegetation wetness keeps polling");
        int sources = env.GroundQueries.SourceCount;
        weather.Config.TransitionSeconds=0.1f; weather.Config.WetnessBuildSeconds=0.1f; weather.Config.WetnessDrySeconds=0.1f;
        weather.Config.RainIntensity=0.65f;
        Check(weather.IsProcessing() && weather.RainActive,"Resource intensity setter cannot wake stopped rain");
        Check(weather.GetNode<Node>("RainLayers").GetChildCount()==3,"Rain did not lazily create shared GPU layers");
        await Frames(25);
        Check(weather.Rain>0.6f && ground.IsProcessing() && liquid.IsProcessing(),"Rain/splash consumers did not resume");
        var layers = weather.GetNode<RainVisualLayers>("RainLayers");
        Check(layers.GetChildren().OfType<GpuParticles2D>().All(e=>e.Emitting),"Active rain emitters remain stopped");
        await Frames(20); int applied=layers.AppliedUpdates; await Frames(4);
        Check(layers.AppliedUpdates==applied,"Unchanged active rain keeps rewriting GPU state");
        var wetness=env.GetNode<VegetationWetness>("VegetationWetness");
        Check(wetness.Coverage>0.5f,"Wetness did not wake and accumulate");
        weather.Config.Splashes=false;
        Check(!ground.IsProcessing() && !ground.Visible && liquid.IsProcessing(),"Ground-only splash switch broke independent liquid splashes");
        weather.Config.Splashes=true;
        weather.Config.RainIntensity=0;
        Check(weather.Rain==0 && weather.CloudCover==0 && !weather.IsProcessing(),"Zero intensity does not stop immediately");
        Check(weather.GetNodeOrNull("RainLayers")==null,"Rain GPU layer survives hard stop");
        Check(!ground.IsProcessing() && !ground.Visible && !liquid.IsProcessing() && !liquid.Visible,"Zero leaves active splash callbacks");
        Check(wetness.IsProcessing(),"Stopping rain incorrectly discards foliage drying");
        await Frames(25);
        Check(wetness.Coverage==0 && env.GetChildren().OfType<VegetationWetness>().All(w=>!w.IsProcessing()),"Drying never goes to sleep");
        env.Wind.Config.Strength=-2; await Frames(3);
        var material=world.GetNode("Actors").FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>()
            .Select(s=>s.Material).OfType<ShaderMaterial>().First(m=>m.Shader.Code.Contains("wet_foliage_colour"));
        Check(Mathf.Abs(material.GetShaderParameter("wind_multiplier").AsSingle()+2)<0.001f,"Sleeping wetness freezes shared vegetation wind");
        Check(!wetness.IsProcessing() && env.GroundQueries.SourceCount==sources,"Dry wind updates wake rain or discard shared ground queries");
        weather.Lightning.Trigger(true); await Frames(3);
        Check(weather.Lightning.Flash>0 && !weather.RainActive && !weather.IsProcessing(),"Manual dry lightning needs active rain");
        await ToSignal(GetTree().CreateTimer(1.0),SceneTreeTimer.SignalName.Timeout); await Frames(3);
        Check(weather.Lightning.Flash==0 && !weather.Lightning.IsProcessing(),"Completed dry lightning never sleeps");
        weather.Config.RainIntensity=0.25f; await Frames(4); weather.Config.RainIntensity=0; await Frames(25);
        Check(weather.GetNodeOrNull("RainLayers")==null && !weather.IsProcessing(),"Repeated rain start/stop leaks layers or polling");
        world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
