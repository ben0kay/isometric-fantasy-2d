// Disposable WG3 river traversal/streaming fixture. Generation config changes are cloned and never saved to authored files.
using Godot;
using System.Linq;
public partial class RiverPractice : WorldScene
{
    private int _station=2;
    // Keep production defaults off; this fixture explicitly requests two lakes and one cross-biome river.
    // =========================================================
    public override void _EnterTree()
    {
        Config=(WorldMasterConfig)Config.Duplicate(); Config.Generation=(WorldGenerationMasterConfig)Config.Generation.Duplicate();
        Config.Generation.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=2,RiverCount=1,RequireMultipleRiverBiomes=true };
        Config.RandomSeedOnLoad=false; Config.FixedSeed=42;
        base._EnterTree();
    }
    // Begin beside a middle section instead of requiring a long walk to find the generated river.
    // =========================================================
    public override void _Ready()
    {
        base._Ready(); if (StartupError!=null) return;
        JumpToStation();
        var ui=new CanvasLayer(); AddChild(ui);
        ui.AddChild(new Label { Position=new Vector2(16,46),Text="WG3 river | R: source / river sections / terminal basin | F7: map | F9: rain" });
    }
    // Five stations follow the frozen source-to-destination direction, including both widened terminal pools.
    // =========================================================
    public void JumpToStation()
    {
        var body=WaterPlan?.Bodies.FirstOrDefault(b => b.River!=null); if (body==null) { GD.PushWarning("River practice: no river was feasible for this seed/settings."); return; }
        var path=body.River; int index=Mathf.RoundToInt((_station++%5)*(path.Points.Count-1)/4f);
        var direction=(path.Points[System.Math.Min(index+1,path.Points.Count-1)]-path.Points[System.Math.Max(0,index-1)]).Normalized();
        Player.GlobalPosition=path.Points[index]+new Vector2(-direction.Y,direction.X)*(path.Widths[index]*0.5f+70);
        Player.Velocity=Vector2.Zero; Player.ResetPhysicsInterpolation(); Player.GetNode<Camera2D>("Camera").ResetSmoothing();
    }
    // Reuse ordinary controls; the practice-only R key teleports without rerolling the route.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed:true,Echo:false,Keycode:Key.R }) { JumpToStation(); GetViewport().SetInputAsHandled(); }
    }
}
