// Exercise snow master shutdown, profile wake, settled coverage, GPU caching and composed foliage.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateSnowSleeping : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"SNOW SLEEP PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        GetTree().Root.Size = new Vector2I(1920,1080);
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment"); var snow=env.Snow; var config=snow.Config;
        Check(!config.Enabled && !snow.IsProcessing() && snow.GetNodeOrNull("Snowfall")==null,"Default snow is not dormant");
        await Frames(180); int applied=snow.AppliedUpdates; await Frames(5);
        Check(snow.AppliedUpdates==applied,"Disabled snow rewrites materials");
        config.TransitionSeconds=0.1f; config.Enabled=true;
        Check(snow.IsProcessing() && snow.GetNodeOrNull("Snowfall")!=null,"Master switch cannot wake snow");
        snow.SetCoverage(0.7f); await Frames(5);
        Check(snow.Coverage==0.7f && !snow.IsProcessing(),"Settled coverage does not sleep");
        var flakes=snow.GetNode<WorldSnowfall>("Snowfall");
        Check(flakes.Emitting && flakes.Visible,"Enabled snowfall does not emit");
        applied=flakes.AppliedUpdates; await Frames(4);
        Check(flakes.AppliedUpdates==applied,"Static snowfall rewrites GPU properties");
        // Headless camera rendering is inactive; exercise its canvas transform contract directly.
        var viewport=GetViewport(); var originalTransform=viewport.CanvasTransform;
        viewport.CanvasTransform=new Transform2D(0,new Vector2(0.5f,0.5f),0,new Vector2(100,50)); flakes._Process(0);
        Check(flakes.AppliedUpdates>applied && flakes.VisibilityRect.Size.X>0,"Active snowfall ignores changed canvas bounds");
        viewport.CanvasTransform=originalTransform;
        config.SnowfallRate=0;
        Check(snow.GetNodeOrNull("Snowfall")==null && snow.Coverage==0.7f,"Zero rate retains GPU flakes or erases settled cover");
        config.SnowfallRate=0.4f;
        Check(snow.GetNode<WorldSnowfall>("Snowfall").Emitting,"Rate change cannot recreate flakes");
        config.Snowfall=false;
        Check(snow.GetNodeOrNull("Snowfall")==null,"Snowfall toggle leaves GPU state");
        applied=snow.AppliedUpdates; config.SnowColour=Colors.White;
        Check(snow.AppliedUpdates==applied+1 && !snow.IsProcessing(),"Sleeping coverage misses profile change");
        var overlay=world.GetNode("Actors").FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>()
            .Select(s=>s.Material).OfType<ShaderMaterial>().First(m=>m.Shader.Code.Contains("tree_snow_colour"));
        env.Wind.Config.Strength=-2; await Frames(3);
        Check(Mathf.Abs(overlay.GetShaderParameter("wind_multiplier").AsSingle()+2)<0.001f && !snow.IsProcessing(),"Sleeping snow freezes wind");
        var chunkWithSnow=world.GetNode("Actors").GetChildren().OfType<Node2D>().First(n=>n.Name.ToString().StartsWith("Chunk_") &&
            n.FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>().Any(v=>v.Material is ShaderMaterial m && m.Shader.Code.Contains("tree_snow_colour")));
        var chunkMaterial=chunkWithSnow.FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>()
            .Select(v=>v.Material).OfType<ShaderMaterial>().First(m=>m.Shader.Code.Contains("tree_snow_colour"));
        snow.SetChunkVisualActive(chunkWithSnow,true); snow.SetChunkVisualActive(chunkWithSnow,false);
        float beforeWind=chunkMaterial.GetShaderParameter("wind_multiplier").AsSingle();
        env.Wind.Config.Strength=1; env.Wind._Process(0);
        Check(chunkMaterial.GetShaderParameter("wind_multiplier").AsSingle()==beforeWind,"Culled snow keeps following wind writes");
        snow.SetChunkVisualActive(chunkWithSnow,true);
        Check(chunkMaterial.GetShaderParameter("wind_multiplier").AsSingle()==1,"Re-entering snow misses current wind");
        env.Weather.Config.WetnessBuildSeconds=0.1f; env.Weather.Config.WetnessDrySeconds=0.1f;
        env.Weather.Config.TransitionSeconds=0.1f; env.Weather.Config.RainIntensity=0.5f; await Frames(40);
        var wetOverlays=world.GetNode("Actors").FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>()
            .Select(s=>s.Material).OfType<ShaderMaterial>().Where(m=>m.Shader.Code.Contains("tree_snow_colour") && m.Shader.Code.Contains("wet_foliage_colour")).ToArray();
        Check(wetOverlays.Any(m=>m.GetShaderParameter("wet_coverage").AsSingle()>0.4f),"Sleeping snow freezes wet foliage");
        env.Weather.Config.RainIntensity=0; await Frames(30);
        Check(wetOverlays.All(m=>m.GetShaderParameter("wet_coverage").AsSingle()==0),"Snow overlay never dries");
        config.Enabled=false;
        Check(!world.GetNode("Actors").FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>()
            .Select(s=>s.Material).OfType<ShaderMaterial>().Any(m=>m.Shader.Code.Contains("tree_snow_colour")),"Master off leaves hidden chunk snow materials active");
        Check(snow.Coverage==0 && !snow.IsProcessing() && !snow.GetNode<MeshInstance2D>("GroundCover").Visible,"Master off does not clear/sleep cover");
        config.Enabled=true; snow.SetCoverage(0.6f);
        var chunk=new Node2D(); world.GetNode("Actors").AddChild(chunk); snow.RegisterChunk(chunk);
        snow.SetChunkVisualActive(chunk,false); snow.SetCoverage(0.3f); snow.SetChunkVisualActive(chunk,true); snow.UnregisterChunk(chunk); chunk.QueueFree();
        Check(!snow.IsProcessing() && snow.Coverage==0.3f,"Chunk lifecycle wakes settled simulation");
        config.AutoAccumulate=true; config.Snowfall=true;
        Check(snow.IsProcessing(),"Automatic accumulation does not wake");
        snow.SetCoverage(1); Check(!snow.IsProcessing(),"Accumulated endpoint does not sleep");
        config.SnowfallRate=0; Check(snow.IsProcessing(),"Automatic melting does not wake");
        snow.SetCoverage(0); Check(!snow.IsProcessing(),"Melted endpoint does not sleep");
        config.Enabled=false; world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
