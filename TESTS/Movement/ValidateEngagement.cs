// Disposable ES3 regression checks: stable claims, route fallback, master switches and attack priority.
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class ValidateEngagement : Node2D
{
    private int _checks;
    // Real navigation and shared actors keep this fixture independent of production species data.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var target=Make(new Vector2(180,0)); target.Config.Faction=CombatFaction.Player;
            var navigation=new StaticObstacleNavigation(); navigation.Build(this,new Rect2(-600,-400,1200,800));
            using var profile=new EngagementProfile();
            var actors=new List<Entity>(); var states=new List<EngagementPositioning>(); var goals=new List<Vector2>();
            for(int i=0;i<5;i++)
            {
                var actor=Make(new Vector2(-180,i*25-50)); actors.Add(actor);
                var state=new EngagementPositioning(); states.Add(state);
                Vector2 goal=state.Select(actor,target,navigation,profile,36); goals.Add(goal);
                Check(state.HasPosition && goal.DistanceTo(target.Position)<=33,"Reachable claim missing");
                for(int j=0;j<i;j++) Check(goal.DistanceTo(goals[j])>=27.9f,"Duplicate/crowded claims");
                state.Advance(0.6f,40,65,profile);
                Check(state.Select(actor,target,navigation,profile,36)==goal,"Position persistence");
            }
            Check(EngagementPositioning.TargetCount==1,"Target ledger scope");
            target.Position+=new Vector2(80,0); states[0].Advance(0.6f,40,65,profile);
            Check(states[0].Select(actors[0],target,navigation,profile,36)!=goals[0],"Moving target never reconsidered");
            states[0].Advance(1.1f,0,65,profile); Check(!states[0].HasPosition,"Blocked claim not released");
            Check(states[0].Select(actors[0],target,navigation,profile,36)==target.Position,"Blocked direct fallback");
            foreach(var state in states) state.Dispose();
            Check(EngagementPositioning.TargetCount==0,"Ledger retained released target");
            using(var direct=new EngagementPositioning())
            {
                profile.Mode=EngagementMode.Direct;
                Check(direct.Select(actors[0],target,navigation,profile,36)==target.Position && !direct.HasPosition,"Direct mode reserved point");
                profile.Mode=EngagementMode.Spread;
                profile.MaximumDetour=0;
                Check(direct.Select(actors[0],target,navigation,profile,36).IsFinite(),"Zero detour malformed");
                direct.Clear(); profile.MaximumDetour=70;
                var wall=new StaticBody2D { Position=target.Position,CollisionLayer=1 };
                wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(200,200) } }); AddChild(wall);
                navigation.Build(this,new Rect2(-600,-400,1200,800));
                Check(direct.Select(actors[0],target,navigation,profile,36)==target.Position && !direct.HasPosition,"Blocked ring fallback");
                Check(EngagementPositioning.TargetCount==0,"Failed selection leaked ledger");
                wall.QueueFree(); await Frames(3); navigation.Build(this,new Rect2(-600,-400,1200,800));
            }
            foreach(var actor in actors) actor.QueueFree(); await Frames(3);
            var mover=Make(new Vector2(-180,0)); var motor=mover.GetNode<GroundChase>("GroundChase"); motor.Enabled=true;
            mover.Config.MeleeEnabled=true; mover.Config.Engagement=profile; mover.ConfigureNavigation(target,navigation);
            await Frames(20); Check(motor.EngagementState.HasPosition,"Motor did not reserve destination");
            var master=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
            master.EngagementEnabled=false; await Frames(2); Check(!motor.EngagementState.HasPosition,"Master Off retained engagement");
            master.EngagementEnabled=true; await Frames(30); Check(motor.EngagementState.HasPosition,"Master On did not resume");
            mover.ProcessMode=ProcessModeEnum.Disabled; Check(!motor.EngagementState.HasPosition,"Suspension retained claim");
            mover.ProcessMode=ProcessModeEnum.Inherit; mover.ConfigureNavigation(target,navigation);
            await Frames(10); mover.TakeDamage(99999); Check(!motor.EngagementState.HasPosition,"Death retained claim");
            await Frames(30); Check(EngagementPositioning.TargetCount==0,"Death leaked target ledger");
            var attacker=Make(target.Position+new Vector2(-30,0)); attacker.Config.MeleeEnabled=true;
            var attackMotor=attacker.GetNode<GroundChase>("GroundChase"); attackMotor.Enabled=true; attacker.ConfigureNavigation(target,navigation);
            await Frames(45); Check(attackMotor.Attack.QueryCount>0 && attackMotor.EngagementState.SelectionCount==0,"Positioning delayed eligible attack");
            var combat=GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres");
            combat.EnemyMeleeEnabled=false; await Frames(2); int queries=attackMotor.Attack.QueryCount;
            await Frames(45); Check(attackMotor.Attack.QueryCount==queries,"Enemy melee Off still contacted"); combat.EnemyMeleeEnabled=true;
            using var hit=new AttackProfile { KnockbackDistance=40,StaggerSeconds=0.3f };
            combat.KnockbackEnabled=false; attacker.ReceiveReaction(hit.Snapshot(1),Vector2.Right);
            Check(attacker.Reaction.Staggered && attacker.Reaction.Advance(0.01f)==Vector2.Zero,"Independent knockback Off"); attacker.Reaction.Clear();
            combat.KnockbackEnabled=true; combat.StaggerEnabled=false; attacker.ReceiveReaction(hit.Snapshot(1),Vector2.Right);
            Check(!attacker.Reaction.Staggered && attacker.Reaction.Advance(0.01f).X>0,"Independent stagger Off"); attacker.Reaction.Clear(); combat.StaggerEnabled=true;
            attacker.Config.ImpactFeedback=GD.Load<HitFeedbackProfile>("res://FEEDBACK/HitFeedback/Profiles/Bone.tres");
            var contact=new AttackContact(attacker,new DamageResult(1,1,1,10000,9999,false),target,attacker.GlobalPosition,Vector2.Left);
            combat.ImpactFeedbackEnabled=false; attacker.ReceiveImpact(contact);
            Check(attacker.ImpactVisual==null,"Impact master Off allocated particles");
            combat.ImpactFeedbackEnabled=true; attacker.ReceiveImpact(contact);
            Check(attacker.ImpactVisual?.EmittedBursts==1,"Impact master On did not resume");
            attacker.QueueFree(); target.QueueFree(); await Frames(3);
            Check(EngagementPositioning.TargetCount==0,"Unload leaked claims");
            GD.Print($"ES3 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    private Entity Make(Vector2 position)
    {
        var actor=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        actor.Configure(new EntityDefinition { MaxHealth=10000,MeleeEnabled=false,DetectionRadius=600 }); actor.Position=position; AddChild(actor); return actor;
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
