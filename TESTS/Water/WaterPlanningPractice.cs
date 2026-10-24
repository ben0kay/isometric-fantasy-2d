// Disposable F6 world with opt-in WG2 lakes; authored settings and campaign content stay untouched.
using Godot;
public partial class WaterPlanningPractice : WorldScene
{
    // Clone only configs we edit; no production Resource writes or automatic campaign save.
    // =========================================================
    public override void _EnterTree()
    {
        Config=(WorldMasterConfig)Config.Duplicate();
        Config.Generation=(WorldGenerationMasterConfig)Config.Generation.Duplicate();
        Config.Generation.Water=new WaterGenerationMasterConfig { Enabled=true,LakeCount=3 };
        Config.RandomSeedOnLoad=false; Config.FixedSeed=42;
        base._EnterTree();
    }
    private int _lake;
    // Start at a real shoreline; L cycles the same frozen bodies without rerolling the world.
    // =========================================================
    public override void _Ready()
    {
        base._Ready();
        if (StartupError!=null) return;
        JumpToLake();
        var ui=new CanvasLayer(); AddChild(ui);
        ui.AddChild(new Label { Position=new Vector2(16,46),Text="WG2 generated lakes | L: next lake | F7: map | F9: environment / rain" });
    }
    // A disposable teleport also exercises chunk unload/return and water field reconstruction.
    // =========================================================
    public void JumpToLake()
    {
        if (WaterPlan?.Bodies.Count is not >0) return;
        var body=WaterPlan.Bodies[_lake++%WaterPlan.Bodies.Count];
        Vector2 edge=body.CopyPolygon()[0];
        Player.GlobalPosition=edge+(edge-body.Bounds.GetCenter()).Normalized()*70f;
        Player.Velocity=Vector2.Zero; Player.ResetPhysicsInterpolation();
        Player.GetNode<Camera2D>("Camera").ResetSmoothing();
    }
    // Reuse normal input; only this developer navigation key is added by the fixture.
    // =========================================================
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed:true,Echo:false,Keycode:Key.L }) JumpToLake();
    }
}
