// Disposable C2.5 practice room: real player/equipment, a separate 50-HP skeleton and explicit reset UI. Never saves.
using Godot;
public partial class PlayerActionsPractice : Node2D
{
    [Export] public float TargetDefense { get; set; }
    private PlayerMovement _player;
    private Entity _target;
    private Label _status;
    private readonly StaticObstacleNavigation _navigation=new();
    // A small repeatable room keeps balance tuning outside real campaigns and production entity data.
    // =========================================================
    public override void _Ready()
    {
        YSortEnabled=true;
        _player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); _player.ZIndex=10; AddChild(_player);
        _navigation.Build(this,new Rect2(-800,-544,2560,1728));
        var ui=new CanvasLayer(); AddChild(ui);
        var column=new VBoxContainer { Position=new Vector2(24,24) }; ui.AddChild(column);
        column.AddChild(new Label { Text="C2.5 DISPOSABLE PRACTICE — LMB slash · MMB thrust · 1 axe · 2 temporary sword · 3 shield\nRMB hop / hold with shield to block · arrows move · RShift sprint / double-tap dodge\nTarget: 50 HP, matching Skeleton Basic. No saves. Blue arc is a debug guard marker." });
        _status=new Label(); column.AddChild(_status);
        var reset=new Button { Text="Reset target and player health",FocusMode=Control.FocusModeEnum.None }; column.AddChild(reset); reset.Pressed+=ResetTarget;
        ResetTarget(); QueueRedraw();
    }
    // Recreate one target, avoiding live definition mutations or changing the authored skeleton resource.
    // =========================================================
    private void ResetTarget()
    {
        if (GodotObject.IsInstanceValid(_target)) { _target.HealthState.Changed-=UpdateStatus; _target.QueueFree(); }
        _player.HealthState.Restore(_player.HealthState.Maximum); _player.Position=Vector2.Zero;
        _target=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        var definition=(EntityDefinition)GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Basic/SkeletonBasic.tres").Duplicate();
        definition.MaxHealth=50; definition.CombatStats=new CombatStatProfile { Defense=Mathf.Clamp(TargetDefense,0,100) };
        _target.Configure(definition); _target.ZIndex=10; _target.Position=new Vector2(120,0); AddChild(_target); _target.ConfigureNavigation(_player,_navigation);
        _target.HealthState.Changed+=UpdateStatus; UpdateStatus(default);
    }
    // Event-driven health readout; no idle UI polling.
    // =========================================================
    private void UpdateStatus(HealthChange change) => _status.Text=$"Target HP: {_target.Health} / 50 — defense {TargetDefense}";
    // Simple static floor art is test-only and redraws once.
    // =========================================================
    public override void _Draw()
    {
        DrawRect(new Rect2(-800,-544,2560,1728),new Color(0.12f,0.17f,0.14f));
        for(int x=-800;x<1760;x+=64) DrawLine(new Vector2(x,-544),new Vector2(x,1184),new Color(0.2f,0.25f,0.22f));
        for(int y=-544;y<1184;y+=64) DrawLine(new Vector2(-800,y),new Vector2(1760,y),new Color(0.2f,0.25f,0.22f));
    }
}
