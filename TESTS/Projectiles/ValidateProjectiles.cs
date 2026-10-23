// R0 production-component tests: swept collision, canonical guard/damage, budgets and transient lifecycle.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidateProjectiles : Node2D
{
    private int _checks;
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var source=Make(new Vector2(-120,0),CombatFaction.Hostile);
            var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>(); AddChild(player); player.SetPhysicsProcess(false); player.GetNode<EquippedTool>("EquippedAxe").ProcessMode=ProcessModeEnum.Disabled;
            var spawner=new ProjectileSpawner { Bounds=new Rect2(-500,-300,1000,600),MaximumActive=2 }; AddChild(spawner);
            using var attack=new AttackProfile { BaseDamage=10,TargetFactions=CombatFaction.Player };
            using var profile=new ProjectileProfile { Impact=attack,Speed=2000 };
            await Frames(3);
            Check(!spawner.IsPhysicsProcessing() && spawner.ActiveCount==0,"Never-active idle callbacks");
            Check(!spawner.TryFire(null,profile,Vector2.Zero,Vector2.Right,out _),"Null source");
            Check(!spawner.TryFire(source,null,Vector2.Zero,Vector2.Right,out _),"Null profile");
            Check(!spawner.TryFire(source,profile,Vector2.Zero,Vector2.Zero,out _),"Zero direction");
            Check(!spawner.TryFire(source,profile,Vector2.Zero,new Vector2(float.MaxValue,float.MaxValue),out _),"Overflow direction");
            profile.Speed=float.NaN; Check(!spawner.TryFire(source,profile,Vector2.Zero,Vector2.Right,out _),"NaN accepted"); profile.Speed=2000;
            profile.Lifetime=0; Check(!spawner.TryFire(source,profile,Vector2.Zero,Vector2.Right,out _),"Zero lifetime"); profile.Lifetime=4;
            attack.CollisionMask=0; Check(!spawner.TryFire(source,profile,Vector2.Zero,Vector2.Right,out _),"Invalid collision mask"); attack.CollisionMask=3;
            Check(!spawner.TryFire(source,profile,new Vector2(-900,0),Vector2.Right,out _),"Outside spawn bounds");
            int before=player.HealthState.Current, finished=0;
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out var shot),"Valid fire");
            shot.Finished+=(reason,contact)=> { finished++; Check(reason==ProjectileEnd.Impact && contact?.Damage?.AppliedDamage==10,"Impact provenance"); };
            attack.BaseDamage=99; await Frames(12); attack.BaseDamage=10;
            Check(player.HealthState.Current==before-10 && finished==1 && spawner.ActiveCount==0,"Snapshot/once-only damage");
            await Frames(10); Check(player.HealthState.Current==before-10,"Repeated contact");
            // Frontal guard must use incoming direction even if the shooter moves behind the defender.
            player.Guard.Profile.Reduction=1; player.Guard.Start(Vector2.Left); before=player.HealthState.Current;
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out _),"Guard shot"); source.Position=new Vector2(120,0);
            await Frames(12); Check(player.HealthState.Current==before,"Frontal guard ignored incoming bearing");
            player.Guard.Start(Vector2.Right); Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out _),"Rear shot");
            await Frames(12); Check(player.HealthState.Current==before-10,"Rear guard protected wrong side"); player.StopGuard();
            source.Position=new Vector2(-120,0);
            var friend=Make(new Vector2(-60,0),CombatFaction.Hostile); await Frames(3); before=player.HealthState.Current;
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out _),"Friendly obstruction shot"); await Frames(12);
            Check(friend.Health==1000 && player.HealthState.Current==before,"Friendly damaged or arrow passed through"); friend.QueueFree(); await Frames(3);
            var wall=new ProjectileObstacleProbe { Position=new Vector2(-40,0),CollisionLayer=1 };
            wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(1,100) } }); AddChild(wall); await Frames(3);
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out _),"Thin wall shot"); await Frames(12);
            Check(player.HealthState.Current==before && spawner.ActiveCount==0,"Fast arrow tunnelled wall"); Check(wall.Hits==0,"Arrow triggered tool damage"); wall.QueueFree(); await Frames(3);
            // Opposite and vertical approaches hit the same canonical receiver.
            foreach(var origin in new[] { new Vector2(120,-5),new Vector2(0,-120),new Vector2(0,120) })
            {
                before=player.HealthState.Current;
                Check(spawner.TryFire(source,profile,origin,(new Vector2(0,-5)-origin).Normalized(),out _),"Directional shot"); await Frames(12);
                Check(player.HealthState.Current==before-10,"Directional hit missing");
            }
            profile.Speed=100; profile.MaximumDistance=10;
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _),"Range expiry fire"); await Frames(12);
            Check(spawner.ActiveCount==0,"Range expiry retained shot"); profile.MaximumDistance=700; profile.Lifetime=0.05f;
            spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _); await Frames(10); Check(spawner.ActiveCount==0,"Life expiry retained shot"); profile.Lifetime=4;
            spawner.IsPositionLoaded=point=>point.X < -80 || point.X > -40;
            before=player.HealthState.Current; profile.Speed=2000;
            spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Right,out _); await Frames(12);
            Check(player.HealthState.Current==before && spawner.ActiveCount==0,"Arrow crossed unloaded gap"); spawner.IsPositionLoaded=null;
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out var first),"Cap first");
            Check(spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out var second),"Cap second");
            Check(!spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _),"Shot cap exceeded");
            first.Retire(); first.Retire(); second.Retire(); Check(spawner.ActiveCount==0,"Double retirement released twice"); await Frames(3);
            spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _); spawner.ProcessMode=ProcessModeEnum.Disabled;
            Check(spawner.ActiveCount==0,"Disabled owner retained shot"); spawner.ProcessMode=ProcessModeEnum.Inherit; await Frames(3);
            foreach(int count in new[] {1,5,20})
            {
                spawner.MaximumActive=count; profile.Lifetime=0.05f; profile.Speed=100;
                for(int i=0;i<count;i++) Check(spawner.TryFire(source,profile,new Vector2(-120,150),Vector2.Left,out _),"Burst cap accepted fewer shots");
                Check(spawner.ActiveCount==count,"Burst count"); await Frames(10);
                Check(spawner.ActiveCount==0 && !spawner.IsPhysicsProcessing(),"Burst expiry/idle cost");
            }
            profile.Lifetime=4; profile.Speed=2000;
            spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _); source.ProcessMode=ProcessModeEnum.Disabled;
            await Frames(3); Check(spawner.ActiveCount==0,"Suspended shooter retained shot"); source.ProcessMode=ProcessModeEnum.Inherit; await Frames(3);
            spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _); source.TakeDamage(9999); await Frames(3);
            Check(spawner.ActiveCount==0,"Dead shooter retained arrow");
            Check(!spawner.TryFire(source,profile,new Vector2(-120,-5),Vector2.Left,out _),"Dead source fired");
            var live=Make(new Vector2(-120,100),CombatFaction.Hostile); await Frames(3);
            Check(spawner.TryFire(live,profile,new Vector2(-120,95),Vector2.Left,out _),"Reactivation"); live.QueueFree(); await Frames(3);
            Check(spawner.ActiveCount==0,"Freed shooter retained arrow");
            live=Make(new Vector2(-120,100),CombatFaction.Hostile); await Frames(3);
            spawner.TryFire(live,profile,new Vector2(-120,95),Vector2.Left,out var removed); int late=0; removed.Finished+=(_,_)=>late++;
            RemoveChild(spawner); Check(spawner.ActiveCount==0 && late==0,"Scene unload callback/budget leak"); spawner.Free();
            GD.Print($"R0 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    private Entity Make(Vector2 position,CombatFaction faction)
    {
        var actor=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        actor.Configure(new EntityDefinition { MaxHealth=1000,Faction=faction }); actor.Position=position; AddChild(actor); return actor;
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
