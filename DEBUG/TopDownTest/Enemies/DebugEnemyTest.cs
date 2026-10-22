// Isolated skeleton demonstration: bake tree/rock footprints once and spawn one chaser.
// Does not modify production enemy systems, player movement or equipped axe behaviour.
using Godot;

public partial class DebugEnemyTest : Node2D
{
    [Export] public EnemyMasterConfig Config { get; set; }
    [Export] public PackedScene SkeletonScene { get; set; }
    private DebugEnemyNavigation _navigation;

    #region Test setup
    // Actors has already run its seeded scenery generation when this sibling enters _Ready.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new EnemyMasterConfig();
        if (!Config.Enabled || SkeletonScene == null) return;

        var actors = GetNodeOrNull<DebugWorldScatter>("../Actors");
        var player = GetNodeOrNull<CharacterBody2D>("../Actors/Player");
        if (actors == null || player == null || actors.Config == null)
        {
            GD.PushWarning("Skeleton test could not find the existing player/scatter config.");
            return;
        }

        _navigation = new DebugEnemyNavigation();
        _navigation.Build(actors, actors.Config.WorldBounds, Config);

        var skeleton = SkeletonScene.Instantiate<DebugSkeleton>();
        skeleton.Config = Config;
        skeleton.Navigation = _navigation;
        skeleton.Player = player;
        skeleton.Position = _navigation.FindSpawnPosition(Config.SpawnPosition);
        actors.AddChild(skeleton);
        GD.Print("DEBUG skeleton | spawned at " + skeleton.Position + " | HP " + skeleton.Health);
    }
    #endregion
}
