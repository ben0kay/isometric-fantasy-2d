// R1 production-component checks: ranged intent, aim lock, services, LOS, reactions and shared motor lifecycle.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateRangedCombat : Node2D
{
    private int _checks;
    private readonly StaticObstacleNavigation _navigation=new();
    private ProjectileSpawner _spawner;
    private Entity _target;
    private readonly Rect2 _bounds=new(-600,-400,1200,800);
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            _spawner=new ProjectileSpawner { Bounds=_bounds }; AddChild(_spawner);
            _target=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
            _target.Configure(new EntityDefinition { MaxHealth=10000,Faction=CombatFaction.Player }); AddChild(_target);
            _navigation.Build(this,_bounds);
            var unbound=Make(new Vector2(-150,0),false); var dormant=unbound.GetNode<GroundChase>("GroundChase"); await Frames(3);
            Check(dormant.Ranged==null && !dormant.IsPhysicsProcessing(),"Unbound ranged actor did not sleep");
            using var unboundHit=new AttackProfile { KnockbackDistance=12,StaggerSeconds=0.1f };
            unbound.ReceiveReaction(unboundHit.Snapshot(1),Vector2.Left); await Frames(45);
            Check(!dormant.IsPhysicsProcessing() && !unbound.Reaction.NeedsUpdate,"Unbound ranged motor failed to sleep after recoil");
            unbound.ConfigureRanged(_spawner); await Frames(3); Check(dormant.Ranged!=null && dormant.IsPhysicsProcessing(),"Service binding did not wake ranged actor");
            unbound.ConfigureRanged(null); Check(dormant.Ranged==null && !dormant.IsPhysicsProcessing(),"Unbinding ranged services did not sleep");
            unbound.ConfigureRanged(_spawner); await Frames(3); Check(dormant.Ranged!=null && dormant.IsPhysicsProcessing(),"Rebinding services did not reactivate");
            unbound.QueueFree(); ClearShots(); await Frames(3);
            var archer=Make(new Vector2(-150,0)); var motor=archer.GetNode<GroundChase>("GroundChase");
            await Frames(120);
            Check(motor.Ranged?.ShotsFired>0 && _target.Health<10000,"Archer failed to fire/damage");
            Check(motor.Attack==null && !motor.EngagementState.HasPosition,"Ranged allocated melee/engagement state");
            var weapon=archer.GetNode<EntityRangedWeapon>("RangedWeapon");
            Check(!weapon.IsProcessing() && !weapon.IsPhysicsProcessing(),"Bow owns ticking callback");
            Check(motor.Ranged.LOSQueries<40,"Unthrottled LOS");
            var master=GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres"); master.EnemyRangedEnabled=false;
            await Frames(3); int fired=motor.Ranged.ShotsFired,queries=motor.Ranged.LOSQueries;
            await Frames(30); Check(motor.Ranged.ShotsFired==fired && motor.Ranged.LOSQueries==queries && archer.Velocity==Vector2.Zero,"Ranged master Off");
            master.EnemyRangedEnabled=true; await Frames(150); Check(motor.Ranged.ShotsFired>fired,"Ranged master resume");
            await Until(()=>motor.Ranged.Drawing,180); fired=motor.Ranged.ShotsFired;
            using var interrupt=new AttackProfile { KnockbackDistance=20,StaggerSeconds=0.3f,InterruptAttack=true };
            float x=archer.Position.X; archer.ReceiveReaction(interrupt.Snapshot(1),Vector2.Left);
            Check(!motor.Ranged.Drawing,"Stagger interruption retained pending shot"); await Frames(15);
            Check(motor.Ranged.ShotsFired==fired && archer.Position.X<x,"Ranged overwrote recoil or fired during interruption");
            await Frames(160); Check(motor.Ranged.ShotsFired>fired,"Ranged failed to resume after stagger");
            await Until(()=>motor.Ranged.Drawing,180); fired=motor.Ranged.ShotsFired;
            archer.ReceiveReaction(interrupt.Snapshot(1) with { InterruptAttack=false },Vector2.Left); await Frames(10);
            Check(motor.Ranged.Drawing && motor.Ranged.ShotsFired==fired,"Non-interrupting stagger did not pause draw"); await Frames(120);
            Check(motor.Ranged.ShotsFired>fired,"Paused draw never resumed");
            ClearShots(); archer.Position=new Vector2(-150,0); _target.Position=Vector2.Zero; var reused=motor.Ranged; archer.ConfigureNavigation(_target,_navigation);
            Check(ReferenceEquals(motor.Ranged,reused),"Navigation refresh recreated ranged resources");
            await Until(()=>motor.Ranged?.AimLocked==true,180); Vector2 locked=motor.Ranged.Aim; int health=_target.Health; int beforeShot=motor.Ranged.ShotsFired;
            _target.Position=new Vector2(0,100); await Until(()=>motor.Ranged.ShotsFired>beforeShot,60);
            var arrow=_spawner.GetChildren().OfType<Projectile>().First();
            Check(Vector2.FromAngle(arrow.GlobalRotation).Dot(locked)>0.999f,"Aim tracked target after lock");
            await Frames(40); Check(_target.Health==health,"Locked shot could not miss moving target");
            archer.ProcessMode=ProcessModeEnum.Disabled; Check(!motor.Ranged.Drawing,"Disabled actor retained draw"); fired=motor.Ranged.ShotsFired;
            await Frames(60); Check(motor.Ranged.ShotsFired==fired,"Disabled actor fired");
            archer.ProcessMode=ProcessModeEnum.Inherit; await Frames(170); Check(motor.Ranged.ShotsFired>fired,"Disabled actor did not resume");
            await Until(()=>motor.Ranged.Drawing,180); var oldState=motor.Ranged; fired=oldState.ShotsFired;
            archer.TakeDamage(99999); await Frames(60);
            Check(oldState.ShotsFired==fired && _spawner.ActiveCount==0,"Death retained intent/arrows");
            _target.Position=Vector2.Zero; ClearShots(); await Frames(3);
            var wall=new StaticBody2D { Position=new Vector2(-75,0),CollisionLayer=1 };
            wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(10,800) } }); AddChild(wall);
            _navigation.Build(this,_bounds); archer=Make(new Vector2(-150,0)); motor=archer.GetNode<GroundChase>("GroundChase");
            for(int i=0;i<180;i++) { _target.Position=new Vector2(0,Mathf.Sin(i*0.3f)*80); await Frames(1); }
            _target.Position=Vector2.Zero; Check(motor.Ranged.ShotsFired==0 && archer.Position.X<-89,"Archer fired/walked through wall");
            Check(motor.Ranged.PositionSearches<=4 && motor.Ranged.LOSQueries<40,"Unbounded blocked-position queries");
            wall.QueueFree(); await Frames(3); _navigation.Build(this,_bounds); archer.ConfigureNavigation(_target,_navigation);
            await Frames(140); Check(motor.Ranged.ShotsFired>0,"Obstacle removal did not resume firing");
            archer.QueueFree(); ClearShots(); await Frames(3);
            var back=Make(new Vector2(-200,0)); var front=Make(new Vector2(-100,0));
            await Frames(300);
            Check(back.GetNode<GroundChase>("GroundChase").Ranged.ShotsFired>0 && front.GetNode<GroundChase>("GroundChase").Ranged.ShotsFired>0,"Collinear archers locked each other indefinitely");
            back.QueueFree(); front.QueueFree(); ClearShots(); await Frames(3);
            var many=Enumerable.Range(0,5).Select(i=>Make(new Vector2(-180,i*30-60))).ToArray(); await Frames(150);
            Check(many.All(e=>e.GetNode<GroundChase>("GroundChase").Ranged.ShotsFired>0),"Five archers starved shots");
            foreach(var actor in many) actor.QueueFree(); ClearShots(); await Frames(3);
            var fast=Make(new Vector2(-150,0)); fast.Stats.SetModifier("r1-speed",new StatModifier(CombatStat.AttackSpeedMultiplier,Multiplier:2)); await Frames(110);
            Check(fast.GetNode<GroundChase>("GroundChase").Ranged.ShotsFired>=2,"Ranged ignored attack-speed stats"); fast.QueueFree(); ClearShots(); await Frames(3);
            var invalid=(RangedCombatProfile)GD.Load<RangedCombatProfile>("res://ENTITIES/Species/Undead/Skeletons/Archer/ArcherRangedProfile.tres").Duplicate(); invalid.WindupSeconds=float.NaN;
            bool rejected=false; try { invalid.Snapshot(_target.Stats.Values); } catch(ArgumentException) { rejected=true; }
            Check(rejected,"Invalid ranged values accepted"); invalid.Dispose();
            GD.Print($"R1 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // The actual authored variant reuses Entity.tscn; explicit MeleeEnabled=true proves ranged intent wins.
    // =========================================================
    private Entity Make(Vector2 position,bool bind=true)
    {
        var actor=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        var definition=(EntityDefinition)GD.Load<EntityDefinition>("res://ENTITIES/Species/Undead/Skeletons/Archer/SkeletonArcher.tres").Duplicate();
        definition.MaxHealth=10000; definition.MeleeEnabled=true; actor.Configure(definition); actor.Position=position; AddChild(actor);
        actor.ConfigureNavigation(_target,_navigation); if(bind) actor.ConfigureRanged(_spawner); return actor;
    }
    // =========================================================
    private void ClearShots() { foreach(Node child in _spawner.GetChildren()) if(child is Projectile shot) shot.Retire(); }
    // =========================================================
    private async Task Until(Func<bool> condition,int frames) { for(int i=0;i<frames && !condition();i++) await Frames(1); Check(condition(),"Timed out waiting for ranged phase"); }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
