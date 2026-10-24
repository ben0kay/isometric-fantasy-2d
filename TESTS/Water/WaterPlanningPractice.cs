// Disposable F6 world with opt-in WG1 metadata; authored settings and campaign content stay untouched.
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
}
