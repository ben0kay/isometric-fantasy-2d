// Optional authored test-actor placement, after scenery generation.
// This stays separate from biome populations until an entity-spawning system exists.
using Godot;
using System.Collections.Generic;

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

    private StaticObstacleNavigation _navigation;
    private Node2D _instance;
    private readonly List<Entity> _actors = new();
    #region Placement
    // Use the existing random forest's actual obstacles, never a separately randomized map.
    // =========================================================
    public void Populate(Node2D destination, Node2D navigationSource, CharacterBody2D player, Rect2 bounds, SavedTestEntity restored = null, bool includePractice = false, ProjectileSpawner projectiles = null)
    {
        if (!destination.CanProcess() || restored?.Exists == false) return;
        if (!Enabled || EntityScene == null) return;
        var navigation = _navigation = new StaticObstacleNavigation();
        navigation.Build(navigationSource, bounds, GridCellSize, ColliderClearance, FreeCellSearchRadius);

        Vector2 wanted = restored == null ? player.GlobalPosition + SpawnOffsetFromPlayer : new Vector2(restored.X,restored.Y);
        Vector2 safeSpawn = wanted;
        if (restored == null && !navigation.TryGetWalkablePoint(wanted, out safeSpawn))
        {
            GD.PushWarning("World entity: no free spawn point near the requested location.");
            return;
        }

        Node2D instance = _instance = EntityScene.Instantiate<Node2D>();
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
        instance.Position = destination.ToLocal(safeSpawn);
        destination.AddChild(instance);
        agent.ConfigureNavigation(player, navigation);
        if(instance is Entity rangedEntity && rangedEntity.Config.Ranged!=null) rangedEntity.ConfigureRanged(projectiles);
        if (restored != null && instance is Entity savedEntity) savedEntity.RestoreHealth(restored.Health);
        if (instance is Entity tracked) _actors.Add(tracked);
        instance.ProcessMode = bounds.HasPoint(instance.GlobalPosition) ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        // Optional authored markers belong to a removable practice root; additional actors are never saved.
        if (includePractice && EntityDefinition!=null)
            foreach (Node child in destination.GetChildren())
            {
                if (child is not Marker2D marker || _actors.Count>=5) continue;
                if (!TryPracticePoint(player.GlobalPosition+marker.Position,player.GlobalPosition,bounds,out Vector2 point))
                { GD.PushWarning("Practice actor skipped: no separated walkable marker position: "+marker.Name); continue; }
                var extra=EntityScene.Instantiate<Entity>(); extra.Configure(EntityDefinition);
                extra.Name=marker.Name+"Skeleton"; extra.Position=destination.ToLocal(point); destination.AddChild(extra);
                extra.ConfigureNavigation(player,navigation); if(extra.Config.Ranged!=null) extra.ConfigureRanged(projectiles); _actors.Add(extra);
            }
        GD.Print($"World entity: spawned {instance.Name} at {safeSpawn}.");
    }
    // Bound startup-only search when scenery maps several markers onto one free cell.
    // =========================================================
    private bool TryPracticePoint(Vector2 wanted,Vector2 player,Rect2 bounds,out Vector2 point)
    {
        point=default;
        for(int ring=0;ring<=3;ring++)
        for(int y=-ring;y<=ring;y++)
        for(int x=-ring;x<=ring;x++)
        {
            if (ring>0 && System.Math.Abs(x)!=ring && System.Math.Abs(y)!=ring) continue;
            if (!_navigation.TryGetWalkablePoint(wanted+new Vector2(x,y)*48,out var candidate) || !bounds.HasPoint(candidate)
                || candidate.DistanceSquaredTo(player)<64*64) continue;
            bool occupied=false;
            foreach(var other in _actors)
                if (candidate.DistanceSquaredTo(other.GlobalPosition)<48*48) { occupied=true; break; }
            if (occupied) continue;
            point=candidate; return true;
        }
        return false;
    }
    // Rebuild one shared local grid; test actors keep identity while far away.
    // =========================================================
    public void RefreshNavigation(Node2D actors, CharacterBody2D player, Rect2 bounds)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("Navigation rebuild");
        if (_navigation == null) return;
        bool anyActive=false;
        for(int i=_actors.Count-1;i>=0;i--)
        {
            var actor=_actors[i];
            if (!GodotObject.IsInstanceValid(actor) || actor.IsQueuedForDeletion()) { _actors.RemoveAt(i); continue; }
            bool active=bounds.HasPoint(actor.GlobalPosition);
            // Preserve a disabled practice-root ancestor; inherited actors cannot override it.
            actor.ProcessMode=active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            anyActive |= active && actor.HealthState.IsAlive && actor.CanProcess();
        }
        if (anyActive) _navigation.Build(actors,bounds,GridCellSize,ColliderClearance,FreeCellSearchRadius);
    }
    // One authored test actor is persisted until population ownership arrives in P6.
    // =========================================================
    public SavedTestEntity Capture() => GodotObject.IsInstanceValid(_instance) && !_instance.IsQueuedForDeletion() && _instance is Entity entity && entity.Health > 0
        ? new(true,entity.GlobalPosition.X,entity.GlobalPosition.Y,entity.Health) : new(false,0,0,0);
    #endregion
}
