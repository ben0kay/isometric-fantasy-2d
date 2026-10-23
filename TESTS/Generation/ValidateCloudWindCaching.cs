// Validate dormant cloud controls and changed-only chunk wind uploads without freezing shader animation.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateCloudWindCaching : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"CLOUD WIND PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        var clouds=env.GetNode<CloudShade>("Clouds");
        env.DayNight.Clock.Config.Paused=true; env.DayNight.Clock.SetHour(12); env.Wind.Config.Strength=0;
        await Frames(180);
        var winds=env.GetChildren().OfType<VegetationWind>().ToArray();
        int writes=winds.Sum(w=>w.MaterialWrites), cloudWrites=clouds.MaterialWrites;
        // Shared foliage wind must remain source-resolution-independent with a draw margin.
        var sway=GD.Load<Shader>("res://ENVIRONMENT/Wind/Visuals/VegetationSway.gdshader").Code;
        Check(sway.Contains("strength_world_pixels") && sway.Contains("MODEL_MATRIX[0].xy")
            && sway.Contains("VERTEX.x +=") && sway.Contains("UV.x ="),"Production sway lost world-unit scaling or anti-clip margin");
        foreach(string path in new[] { "res://WORLD/Content/Vegetation/Trees/Oak/OakWind.tres",
            "res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossomWind.tres",
            "res://WORLD/Content/Vegetation/Grass/GrassWind.tres" })
            Check(GD.Load<ShaderMaterial>(path).GetShaderParameter("strength_world_pixels").AsSingle()>0,
                "Vegetation material missing world-unit strength: "+path);
        await Frames(5);
        Check(winds.Sum(w=>w.MaterialWrites)==writes,"Unchanged wind keeps uploading materials");
        Check(clouds.MaterialWrites==cloudWrites,"Stable clouds keep uploading uniforms");
        clouds.Config.Enabled=false;
        Check(!clouds.Visible && !clouds.IsProcessing(),"Cloud toggle does not hide/sleep");
        cloudWrites=clouds.MaterialWrites; env.Wind.Config.Strength=1; await Frames(5);
        Check(clouds.MaterialWrites==cloudWrites,"Disabled clouds still upload material settings");
        Check(winds.Sum(w=>w.MaterialWrites)>writes,"Wind changes never reach active chunk materials");
        clouds.Config.Opacity=0.3f;
        Check(!clouds.IsProcessing() && clouds.MaterialWrites==cloudWrites,"Disabled opacity edit wakes cloud work");
        clouds.Config.Enabled=true;
        var material=(ShaderMaterial)clouds.Material;
        Check(clouds.IsProcessing() && clouds.Visible,"Profile signal cannot wake clouds");
        Check(material.GetShaderParameter("opacity").AsSingle()==0.3f && material.GetShaderParameter("drift").AsVector2()==-env.Wind.Displacement,"Cloud re-enable applies stale settings/wind");
        clouds.Config.Opacity=0;
        Check(!clouds.Visible && !clouds.IsProcessing(),"Zero opacity clouds do not sleep");
        clouds.Config.Opacity=0.12f;
        Check(clouds.Visible && clouds.IsProcessing(),"Opacity cannot wake zero-opacity clouds");
        env.DayNight.Clock.SetHour(0); await Frames(3);
        Check(material.GetShaderParameter("daylight").AsSingle()<0.01f,"Cloud daylight freezes after reactivation");
        var wind=winds.First(w=>w.MaterialWrites>0); wind.SetProcess(false); writes=wind.MaterialWrites;
        env.Wind.Config.Strength=-2; await Frames(3);
        Check(wind.MaterialWrites==writes,"Inactive chunk wind still uploads");
        wind.SetProcess(true); wind._Process(0);
        Check(wind.MaterialWrites>writes,"Re-entering chunk misses changed wind");
        writes=wind.MaterialWrites; wind._Process(0);
        Check(wind.MaterialWrites==writes,"Repeated re-entry/upload rewrites same strength");
        var actors=new Node2D(); AddChild(actors);
        var sprite=new Sprite2D { Material=new ShaderMaterial { Shader=new Shader { Code="shader_type canvas_item; uniform float wind_multiplier=0.0; void fragment() { COLOR=vec4(wind_multiplier); }" } } };
        sprite.Name="Sprite"; var prop=new Node2D(); actors.AddChild(prop); prop.AddChild(sprite);
        var original=sprite.Material; var fresh=new VegetationWind { Wind=env.Wind }; AddChild(fresh); fresh.Attach(actors);
        Check(((ShaderMaterial)sprite.Material).GetShaderParameter("wind_multiplier").AsSingle()==-2,"New material misses initial unchanged global wind");
        fresh.Free(); Check(sprite.Material==original,"Unloaded wind adapter fails to restore source material"); actors.QueueFree();
        clouds.Config.Enabled=false; await Frames(3);
        Check(!clouds.IsProcessing(),"Repeated cloud stop leaves polling");
        var dormantProfile=new CloudShadeProfile { Enabled=false };
        var dormant=new CloudShade { Config=dormantProfile, Day=env.DayNight, Wind=env.Wind };
        dormant.Configure(env.Snow.WorldBounds); AddChild(dormant);
        Check(!dormant.IsProcessing() && !dormant.Visible && dormant.MaterialWrites==0,"Never-enabled clouds upload or tick at startup");
        dormantProfile.Opacity=0.4f;
        Check(dormant.MaterialWrites==0 && !dormant.IsProcessing(),"Never-enabled profile edit performs hidden uploads");
        dormant.Free(); dormantProfile.Enabled=true;
        Check(!GodotObject.IsInstanceValid(dormant),"Removed cloud still responds to profile changes");
        world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
