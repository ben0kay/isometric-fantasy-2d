// Disposable R1 room: one real shared-Entity archer, real player, cover and an explicit reset; no campaigns.
using Godot;
public partial class ArcherPractice : Node2D
{
    public Entity Archer { get; private set; }
    public PlayerMovement Player { get; private set; }
    public ProjectileSpawner Projectiles { get; private set; }
    private readonly Rect2 _bounds=new(-640,-400,1280,800);
    private readonly StaticObstacleNavigation _navigation=new();
    private Label _status;
    // Setup is local to this practice scene; production World's five-melee layout is untouched.
    // =========================================================
    public override void _Ready()
    {
        YSortEnabled=true;
        Player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); AddChild(Player);
        Projectiles=new ProjectileSpawner { Name="Projectiles",Bounds=_bounds }; AddChild(Projectiles);
        Wall(new Vector2(0,-405),new Vector2(1300,10)); Wall(new Vector2(0,405),new Vector2(1300,10));
        Wall(new Vector2(-645,0),new Vector2(10,800)); Wall(new Vector2(645,0),new Vector2(10,800));
        Wall(new Vector2(0,100),new Vector2(60,90));
        _navigation.Build(this,_bounds);
        var ui=new CanvasLayer(); AddChild(ui);
        var box=new VBoxContainer { Position=new Vector2(20,20) }; ui.AddChild(box);
        box.AddChild(new Label { Text="R1 ARCHER PRACTICE — no saves\nArrow keys: move/dodge · LMB/MMB: attack · 3: shield · hold RMB: block\nWatch the bow draw, then sidestep the locked shot. Brown cover blocks arrows.\nThe normal World still has its original five melee skeletons." });
        _status=new Label(); box.AddChild(_status);
        var reset=new Button { Text="Reset archer, arrows and player",FocusMode=Control.FocusModeEnum.None }; box.AddChild(reset); reset.Pressed+=Reset;
        Player.HealthState.Changed+=Status; Reset(); QueueRedraw();
    }
    // A button/event owns resets; no polling UI or independent enemy mover is introduced.
    // =========================================================
    private void Reset()
    {
        if(GodotObject.IsInstanceValid(Archer)) { Archer.HealthState.Changed-=Status; Archer.QueueFree(); }
        foreach(Node child in Projectiles.GetChildren()) if(child is Projectile shot) shot.Retire();
        Player.Position=new Vector2(80,0); Player.HealthState.Restore(Player.HealthState.Maximum); Player.Reaction.Clear();
        Archer=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        Archer.Configure(GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Archer/SkeletonArcher.tres"));
        Archer.Position=new Vector2(-280,0); AddChild(Archer); Archer.ConfigureNavigation(Player,_navigation); Archer.ConfigureRanged(Projectiles);
        Archer.HealthState.Changed+=Status; Status(default);
    }
    // =========================================================
    private void Status(HealthChange change) => _status.Text=$"Player HP {Player.HealthState.Current} | Archer HP {(GodotObject.IsInstanceValid(Archer) ? Archer.Health : 0)} / 50";
    // =========================================================
    private void Wall(Vector2 center,Vector2 size)
    {
        var body=new StaticBody2D { Position=center,CollisionLayer=1,CollisionMask=3 };
        body.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=size } }); AddChild(body);
        var art=new Polygon2D { Polygon=new[] { -size/2,new Vector2(size.X/2,-size.Y/2),size/2,new Vector2(-size.X/2,size.Y/2) },Color=new Color(0.3f,0.24f,0.17f) }; body.AddChild(art);
    }
    // Static ground draws once; all moving sprites use the shared Y-sort chain.
    // =========================================================
    public override void _Draw()
    {
        DrawRect(_bounds,new Color(0.1f,0.15f,0.12f));
        for(int x=-640;x<=640;x+=64) DrawLine(new Vector2(x,-400),new Vector2(x,400),new Color(0.16f,0.21f,0.18f));
        for(int y=-400;y<=400;y+=64) DrawLine(new Vector2(-640,y),new Vector2(640,y),new Color(0.16f,0.21f,0.18f));
    }
    // =========================================================
    public override void _ExitTree()
    { if(GodotObject.IsInstanceValid(Player)) Player.HealthState.Changed-=Status; if(GodotObject.IsInstanceValid(Archer)) Archer.HealthState.Changed-=Status; }
}
