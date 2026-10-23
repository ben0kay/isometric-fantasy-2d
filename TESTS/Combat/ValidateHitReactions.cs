// C3.2 disposable production-component checks: reactions, contact eligibility, collision and lifecycle.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidateHitReactions : Node2D
{
    private int _checks;
    // Validate deterministic state before exercising actual physics bodies and authoritative melee contacts.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            using var profile=new AttackProfile { KnockbackDistance=55,StaggerSeconds=0.3f,BaseDamage=5,HitReach=60 };
            var values=profile.Snapshot(1);
            var reaction=new Knockback();
            Check(reaction.Apply(values,Vector2.Right,false,0,0),"Reaction rejected");
            float distance=0;
            for(int i=0;i<30;i++) distance+=reaction.Advance(1f/60).X/60;
            Check(Mathf.Abs(distance-55)<0.01f,"Integrated distance");
            Check(!reaction.Active,"Reaction failed to expire");
            Check(!reaction.Apply(values,Vector2.Right,false,0,0),"Recovery grace missing");
            reaction.Advance(0.3f);
            Check(reaction.Apply(values,Vector2.Down,false,0.5f,1),"Resistance rejected push");
            Check(!reaction.Staggered,"Stagger immunity");
            Check(Mathf.Abs(reaction.Advance(1).Y-27.5f)<0.01f,"Large step/resistant distance");
            reaction.Clear();
            Check(!reaction.Apply(values,Vector2.Right,true,0,0),"Total immunity");
            Check(!reaction.Apply(values,Vector2.Zero,false,0,0),"Invalid direction");
            float longest=0, run=0;
            for(int i=0;i<120;i++) { reaction.Apply(values,Vector2.Right,false,0,0); reaction.Advance(1f/60); run=reaction.Staggered ? run+1f/60 : 0; longest=Mathf.Max(longest,run); }
            Check(longest<=0.61f,"Repeated hits allowed unbounded stagger");
            reaction.Clear(); Check(!reaction.NeedsUpdate,"Clear left transient work");
            profile.KnockbackDistance=999; profile.StaggerSeconds=999;
            Check(profile.Snapshot(1).KnockbackDistance==120 && profile.Snapshot(1).StaggerSeconds==0.6f,"Tuning caps");
            profile.KnockbackDistance=float.NaN;
            bool rejected=false; try { profile.Snapshot(1); } catch(ArgumentException) { rejected=true; }
            Check(rejected,"NaN accepted"); profile.KnockbackDistance=55; profile.StaggerSeconds=0.3f;
            var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>();
            AddChild(player); player.SetPhysicsProcess(false);
            var target=MakeEntity(new Vector2(35,0)); await Sync();
            using var query=new MeleeHitQuery(player);
            Check(query.Strike(player,values,Vector2.Right,player.Stats.Values,out var contact),"Contact missing");
            Check(contact.Damage?.AppliedDamage==5 && target.Health==95,"Damage changed");
            Check(target.Reaction.Staggered && target.Reaction.Advance(0.01f).X>0,"Forward aim not propagated"); target.Reaction.Clear();
            Check(query.Strike(player,values with { Damage=0 },Vector2.Right,player.Stats.Values,out _),"Zero damage contact");
            Check(!target.Reaction.Active,"Zero damage pushed");
            using(var execution=new AttackExecution(player))
            {
                Check(execution.TryStart(profile,Vector2.Right),"Attack start"); execution.Interrupt();
                execution.Advance(0.1); Check(execution.Busy && execution.QueryCount==0,"Interrupt refunded cooldown");
                execution.Advance(1); Check(!execution.Busy && execution.QueryCount==0,"Delayed interrupted contact");
            }
            target.Position=new Vector2(300,0); await Sync();
            var wall=new StaticBody2D { Position=new Vector2(350,0),CollisionLayer=1,CollisionMask=3 };
            wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(10,100) } }); AddChild(wall); await Sync();
            target.ReceiveReaction(values,Vector2.Right); await Frames(40);
            Check(target.Position.X>300 && target.Position.X<337,"Passive recoil crossed wall or did not wake");
            Check(!target.Reaction.NeedsUpdate && !target.GetNode<GroundChase>("GroundChase").IsPhysicsProcessing(),"Passive motor failed to sleep");
            player.ReceiveReaction(values,Vector2.Down);
            Check(player.Reaction.Active && !player.TryHop() && !player.TryGuard(Vector2.Right),"Player priority");
            player.ProcessMode=ProcessModeEnum.Disabled; Check(!player.Reaction.NeedsUpdate,"Player suspension"); player.ProcessMode=ProcessModeEnum.Inherit;
            target.Position=new Vector2(35,0); await Sync();
            player.Guard.Start(Vector2.Right);
            player.Guard.Profile.Reduction=1;
            using(var enemyQuery=new MeleeHitQuery(target))
            {
                Check(enemyQuery.Strike(target,values with { Targets=CombatFaction.Player },Vector2.Left,target.Stats.Values,out var blocked),"Blocked contact missing");
                Check(blocked.Damage?.AppliedDamage==0 && !player.Reaction.Active,"Zero-damage block pushed");
                player.StopGuard();
                Check(enemyQuery.Strike(target,values with { Targets=CombatFaction.Player },Vector2.Left,target.Stats.Values,out _),"Enemy contact");
                Check(player.Reaction.Active && player.Reaction.Advance(0.01f).X<0,"Enemy to player direction"); player.Reaction.Clear();
            }
            target.Config.KnockbackResistance=1; target.Config.StaggerResistance=1;
            target.ReceiveReaction(values,Vector2.Right); Check(!target.Reaction.Active,"Heavy definition immunity");
            target.Config.KnockbackResistance=0; target.Config.StaggerResistance=0;
            target.ReceiveReaction(values,Vector2.Right); target.ProcessMode=ProcessModeEnum.Disabled;
            Check(!target.Reaction.NeedsUpdate,"Entity suspension"); target.ProcessMode=ProcessModeEnum.Inherit;
            target.ReceiveReaction(values,Vector2.Right); target.TakeDamage(999);
            Check(!target.Reaction.NeedsUpdate,"Death retained reaction");
            var dying=MakeEntity(new Vector2(0,35)); await Sync();
            Check(query.Strike(player,values with { Damage=999 },Vector2.Down,player.Stats.Values,out var overkill),"Overkill contact");
            Check(overkill.Damage?.Killed==true && !dying.Reaction.NeedsUpdate,"Killing blow pushed corpse");
            reaction.Clear(); reaction.Apply(values,new Vector2(-1,1),false,0,0);
            Vector2 diagonal=reaction.Advance(0.01f);
            Check(diagonal.X<0 && diagonal.Y>0 && Mathf.Abs(diagonal.X+diagonal.Y)<0.001f,"Diagonal direction");
            reaction.Apply(values,Vector2.Up,false,0,0);
            Check(reaction.Advance(0.01f).X==0,"Concurrent hit accumulated directions");
            var chaser=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
            chaser.Configure(new EntityDefinition { MaxHealth=100,ChasePlayer=true,MeleeEnabled=false,DetectionRadius=600 });
            chaser.Position=new Vector2(-150,0); AddChild(chaser);
            var navigation=new StaticObstacleNavigation(); navigation.Build(this,new Rect2(-300,-200,600,400));
            chaser.ConfigureNavigation(player,navigation);
            chaser.ReceiveReaction(values,Vector2.Left); await Frames(12);
            float displaced=chaser.Position.X;
            Check(displaced<-180,"Pursuit overwrote recoil");
            await Frames(40); Check(chaser.Position.X>displaced,"Chase failed to resume");
            chaser.Config.MeleeEnabled=true; chaser.ConfigureNavigation(player,navigation);
            var enemyAttack=chaser.GetNode<GroundChase>("GroundChase").Attack;
            Check(enemyAttack.TryStart(profile,Vector2.Right),"Enemy attack start");
            chaser.ReceiveReaction(values with { InterruptAttack=true },Vector2.Left);
            Check(enemyAttack.Busy && !enemyAttack.IsSwinging,"Receiving interruption policy");
            await Frames(45); Check(enemyAttack.QueryCount==0,"Interrupted enemy emitted delayed contact");
            GD.Print($"C3.2 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // No production species is introduced for resistance, passive motion or immunity tests.
    // =========================================================
    private Entity MakeEntity(Vector2 position)
    {
        var entity=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        entity.Configure(new EntityDefinition { MaxHealth=100,ChasePlayer=false }); entity.Position=position; AddChild(entity); return entity;
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private Task Sync() => Frames(3);
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
