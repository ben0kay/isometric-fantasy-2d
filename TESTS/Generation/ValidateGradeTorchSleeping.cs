// Verify event-driven grading and static torch settings, dormant off states and active flicker.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateGradeTorchSleeping : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"GRADE TORCH PASS: {_checks} checks"); GetTree().Quit(); }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false; AddChild(world);
        var env=world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        var grade=env.GetNode<WorldColourGrade>("ColourGrade"); var rect=grade.GetNode<ColorRect>("Post/Grade");
        var torches=env.GetChildren().OfType<TorchLight>().ToArray(); var profile=torches[0].Config;
        await Frames(180);
        Check(!grade.IsProcessing() && rect.Visible,"Grading still polls or default visibility is wrong");
        int writes=grade.MaterialWrites, statics=torches.Sum(t=>t.StaticWrites), energies=torches.Sum(t=>t.EnergyWrites);
        await Frames(5);
        Check(grade.MaterialWrites==writes,"Stable grade rewrites uniforms");
        Check(torches.Sum(t=>t.StaticWrites)==statics,"Active flicker rewrites static light settings");
        Check(torches.Sum(t=>t.EnergyWrites)>energies,"Active torch flicker froze");
        grade.Config.Contrast=1.23f;
        Check(grade.MaterialWrites==writes+1 && ((ShaderMaterial)rect.Material).GetShaderParameter("contrast").AsSingle()==1.23f,"Grade edit does not apply exactly one changed uniform");
        grade.Config.Enabled=false; writes=grade.MaterialWrites; grade.Config.Saturation=0.6f; grade.Config.Vignette=0.2f; await Frames(3);
        Check(!rect.Visible && !grade.IsProcessing() && grade.MaterialWrites==writes,"Disabled grade uploads settings or ticks");
        grade.Config.Enabled=true;
        Check(rect.Visible && grade.MaterialWrites==writes+2,"Grade re-enable misses deferred settings");
        grade.Config.Enabled=true;
        Check(grade.MaterialWrites==writes+2,"Identical profile assignment uploads again");
        profile.Enabled=false; statics=torches.Sum(t=>t.StaticWrites); energies=torches.Sum(t=>t.EnergyWrites);
        Check(torches.All(t=>!t.IsProcessing() && !t.Torch.Visible && !t.Torch.GetNode<PointLight2D>("Light").Enabled),"Torch master off does not stop all shared controllers/lights");
        profile.TextureScale=1.7f; profile.Colour=Colors.Cyan; profile.CastShadows=false; profile.ShadowSoftness=3; profile.Energy=1.2f;
        await Frames(4);
        Check(torches.Sum(t=>t.StaticWrites)==statics && torches.Sum(t=>t.EnergyWrites)==energies,"Off torch profile edits/frames upload native properties");
        profile.Enabled=true;
        Check(torches.All(t=>t.IsProcessing() && t.Torch.Visible && t.Torch.GetNode<PointLight2D>("Light").Enabled),"Torch profile cannot wake all lamps");
        Check(torches.All(t=>t.Torch.GetNode<PointLight2D>("Light").TextureScale==1.7f && t.Torch.GetNode<PointLight2D>("Light").Color==Colors.Cyan),"Torch re-enable loses deferred appearance");
        Check(torches.All(t=>!t.Torch.GetNode<PointLight2D>("Light").ShadowEnabled && t.Torch.GetNode<PointLight2D>("Light").ShadowFilterSmooth==3),"Torch re-enable loses deferred shadows");
        profile.ShadowFilter=Light2D.ShadowFilterEnum.None; profile.CastShadows=true;
        Check(torches.All(t=>t.Torch.GetNode<PointLight2D>("Light").ShadowEnabled && t.Torch.GetNode<PointLight2D>("Light").ShadowFilter==Light2D.ShadowFilterEnum.None),"Live filter/shadow toggle fails");
        statics=torches.Sum(t=>t.StaticWrites); profile.Energy=0;
        Check(torches.All(t=>!t.IsProcessing() && t.Torch.GetNode<PointLight2D>("Light").Energy==0 && !t.Torch.GetNode<PointLight2D>("Light").Enabled),"Zero energy leaves flicker/native lights running");
        energies=torches.Sum(t=>t.EnergyWrites); await Frames(4);
        Check(torches.Sum(t=>t.EnergyWrites)==energies,"Zero-energy torch still uploads flicker");
        profile.Energy=0.9f; await Frames(3);
        Check(torches.All(t=>t.IsProcessing() && t.Torch.GetNode<PointLight2D>("Light").Energy>0.8f),"Energy slider cannot reactivate flicker");
        var dormantGrade=GD.Load<PackedScene>("res://ENVIRONMENT/Lighting/Colour/ColourGrade.tscn").Instantiate<WorldColourGrade>();
        dormantGrade.Config=new ColourGradeProfile { Enabled=false }; AddChild(dormantGrade);
        Check(!dormantGrade.IsProcessing() && dormantGrade.MaterialWrites==0,"Never-enabled grade uploads or processes");
        var dormantProfile=dormantGrade.Config; dormantGrade.Free(); dormantProfile.Enabled=true;
        Check(!GodotObject.IsInstanceValid(dormantGrade),"Grade profile retains a freed component");
        var artwork=GD.Load<PackedScene>("res://WORLD/Content/Props/Torch/Torch.tscn").Instantiate<Node2D>(); AddChild(artwork);
        var dormantTorch=new TorchLight { Torch=artwork, Config=new TorchProfile { Enabled=false } }; AddChild(dormantTorch);
        Check(!dormantTorch.IsProcessing() && !artwork.Visible,"Never-enabled torch processes or flashes on startup");
        var dormantTorchProfile=dormantTorch.Config; dormantTorch.Free(); dormantTorchProfile.Enabled=true;
        Check(!GodotObject.IsInstanceValid(dormantTorch),"Torch profile retains a freed component");
        profile.Enabled=false; world.QueueFree(); await Frames(3);
    }
    private async Task Frames(int n) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool ok,string message) { _checks++; if(!ok) throw new Exception(message); }
}
