// Optional single-actor production placement, after scenery generation.
// This stays separate from biome populations until an entity-spawning system exists.
using Godot;

public partial class WorldEntityPlacement : Node
{
    [ExportGroup("Entity")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public PackedScene EntityScene { get; set; }
    [Export] public EntityDefinition EntityDefinition { get; set; }
    [Export] public Vector2 SpawnOffsetFromPlayer { get; set; } = new(0, -210);

    [ExportGroup("Static navigation")]
    [Export(PropertyHint.Range, "12,48,2")] public int GridCellSize { get; set; } = 24;
    [Export(PropertyHint.Range, "5,32,1")] public float ColliderClearance { get; set; } = 17f;
    [Export(PropertyHint.Range, "1,16,1")] public int FreeCellSearchRadius { get; set; } = 8;

    #region Placement
    // Use the existing random forest's actual obstacles, never a separately randomized map.
    // =========================================================
    public void Populate(Node2D actors, CharacterBody2D player, Rect2 bounds)
    {
        if (!Enabled || EntityScene == null) return;
        var navigation = new StaticObstacleNavigation();
        navigation.Build(actors, bounds, GridCellSize, ColliderClearance, FreeCellSearchRadius);

        Vector2 wanted = player.GlobalPosition + SpawnOffsetFromPlayer;
        if (!navigation.TryGetWalkablePoint(wanted, out Vector2 safeSpawn))
        {
            GD.PushWarning("World entity: no free spawn point near the requested location.");
            return;
        }

        Node2D instance = EntityScene.Instantiate<Node2D>();
        if (instance is not IWorldNavigationAgent agent)
        {
            GD.PushError("World entity scene must implement IWorldNavigationAgent.");
            instance.Free();
            return;
        }

        if (instance is Entity entity)
        {
            if (EntityDefinition == null) { GD.PushError("Shared entity scene requires EntityDefinition."); instance.Free(); return; }
            entity.Configure(EntityDefinition);
        }
        instance.Position = safeSpawn;
        actors.AddChild(instance);
        agent.ConfigureNavigation(player, navigation);
        GD.Print($"World entity: spawned {instance.Name} at {safeSpawn}.");
    }
    #endregion
}
