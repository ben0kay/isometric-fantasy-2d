// Disposable ES1 checks using actual actor broad-phase membership and the production helper/motor.
using Godot;
using System;
using System.Threading.Tasks;
public partial class ValidateSeparation : Node2D
{
    private int _checks;
    // Exercise disabled, bounded queries, passive neighbors, lifecycle and motor priority.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            Entity actor=Make(Vector2.Zero), neighbor=Make(new Vector2(20,0)), target=Make(new Vector2(180,0));
            using var state=new Separation();
            await Frames(3);
            Vector2 desired=new(65,0);
            Check(state.Blend(actor,target,desired,0.21f,false,1)==desired && state.QueryCount==0,"Disabled query");
            Vector2 blended=state.Blend(actor,target,desired,0.21f,true,1);
            Check(state.NeighborCount==1 && state.QueryCount==1,"Passive neighbor/target exclusion");
            Check(blended.Y!=0 && blended.X>0 && blended.Length()<=65.001f,"Steering/progress/speed cap");
            for(int i=0;i<10;i++) state.Blend(actor,target,desired,0.01f,true,1);
            Check(state.QueryCount==1,"Per-frame querying");
            Check(state.Blend(actor,target,desired,0.01f,false,1)==desired && state.Steering==Vector2.Zero,"Master off retained cache");
            actor.Config.Separation=new SeparationProfile { Enabled=false };
            state.Blend(actor,target,desired,1,true,1); Check(state.QueryCount==1,"Profile opt-out");
            actor.Config.Separation.Enabled=true;
            state.Blend(actor,target,desired,1,true,1); Check(state.QueryCount==2,"Reactivation");
            neighbor.ProcessMode=ProcessModeEnum.Disabled; await Frames(3);
            state.Blend(actor,target,desired,1,true,1); Check(state.NeighborCount==0,"Disabled neighbor");
            neighbor.ProcessMode=ProcessModeEnum.Inherit; neighbor.Position=Vector2.Zero; await Frames(3);
            blended=state.Blend(actor,target,desired,1,true,1); Check(blended.IsFinite() && blended.Y!=0,"Coincident fallback");
            neighbor.TakeDamage(999); await Frames(3);
            state.Blend(actor,target,desired,1,true,1); Check(state.NeighborCount==0,"Dead neighbor");
            for(int i=0;i<34;i++) Make(new Vector2(10+(i%6)*2,10+(i/6)*2));
            await Frames(3); state.Blend(actor,target,desired,1,true,1);
            Check(state.SaturatedSamples==1 && state.Steering==Vector2.Zero,"Saturation sentinel");
            foreach(Node node in GetChildren()) if(node is Entity e && e!=actor && e!=target) e.QueueFree();
            await Frames(3); state.Clear();
            state.Blend(actor,target,desired,1,true,1); Check(state.NeighborCount==0,"Unload retained neighbor");
            var navigation=new StaticObstacleNavigation(); navigation.Build(this,new Rect2(-300,-200,600,400));
            actor.GetNode<GroundChase>("GroundChase").Enabled=true; actor.ConfigureNavigation(target,navigation);
            var master=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
            master.Enabled=false;
            await Frames(12); Check(actor.Position.X>0,"Pursuit failed");
            Check(actor.GetNode<GroundChase>("GroundChase").SeparationState.QueryCount==0,"Master did not stop motor queries");
            master.Enabled=true; await Frames(15);
            Check(actor.GetNode<GroundChase>("GroundChase").SeparationState.QueryCount>0,"Master did not reactivate motor queries");
            var motor=actor.GetNode<GroundChase>("GroundChase");
            using var attack=new AttackProfile { KnockbackDistance=55,StaggerSeconds=0.3f };
            actor.ReceiveReaction(attack.Snapshot(1),Vector2.Left); float before=actor.Position.X;
            await Frames(12); Check(actor.Position.X<before && motor.SeparationState.Steering==Vector2.Zero,"Reaction priority");
            await Frames(40); before=actor.Position.X; await Frames(12); Check(actor.Position.X>before,"Resume chase");
            actor.ProcessMode=ProcessModeEnum.Disabled; Check(motor.SeparationState.Steering==Vector2.Zero,"Suspension cache");
            actor.ProcessMode=ProcessModeEnum.Inherit; actor.TakeDamage(999);
            Check(!motor.IsPhysicsProcessing() && motor.SeparationState.Steering==Vector2.Zero,"Death sleep");
            Check(!target.GetNode<GroundChase>("GroundChase").IsPhysicsProcessing(),"Passive actor woke");
            actor.QueueFree(); target.QueueFree(); await Frames(3);
            foreach(int count in new[] { 1,2,5,10 }) await Crowd(count);
            GD.Print($"ES1 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Exercise real pursuit in a narrow lane, with a passive blocker and shared collision walls.
    // =========================================================
    private async Task Crowd(int count)
    {
        var root=new Node2D(); AddChild(root);
        var target=Make(new Vector2(200,0),10000); target.Config.Faction=CombatFaction.Player;
        var movers=new Entity[count];
        for(int i=0;i<count;i++)
        {
            movers[i]=Make(new Vector2(-180-(i/3)*24,(i%3-1)*24));
            movers[i].GetNode<GroundChase>("GroundChase").Enabled=true;
            movers[i].Config.MeleeEnabled=true; movers[i].Config.DetectionRadius=600;
        }
        foreach(int side in new[] {-1,1})
        {
            var wall=new StaticBody2D { Position=new Vector2(0,side*70),CollisionLayer=1,CollisionMask=3 };
            wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(600,10) } }); root.AddChild(wall);
        }
        var blocker=Make(new Vector2(-20,0));
        await Frames(3);
        var navigation=new StaticObstacleNavigation(); navigation.Build(this,new Rect2(-320,-180,640,360));
        foreach(var mover in movers) mover.ConfigureNavigation(target,navigation);
        await Frames(360);
        int contacts=0;
        foreach(var mover in movers)
        {
            Check(mover.Position.X>-150 && Mathf.Abs(mover.Position.Y)<57,"Lane progress/wall collision");
            Check(mover.GetNode<GroundChase>("GroundChase").SeparationState.NeighborCount<=8,"Neighbor cap");
            contacts+=mover.GetNode<GroundChase>("GroundChase").Attack.QueryCount;
        }
        Check(contacts>0,"Crowd attack starvation");
        Check(!blocker.GetNode<GroundChase>("GroundChase").IsPhysicsProcessing(),"Passive blocker woke");
        foreach(var mover in movers) mover.QueueFree(); blocker.QueueFree(); target.QueueFree(); root.QueueFree(); await Frames(3);
    }
    // All entities reuse the shared scene; no species-specific behavior or authored changes.
    // =========================================================
    private Entity Make(Vector2 position,int health=100)
    {
        var entity=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        entity.Configure(new EntityDefinition { MaxHealth=health,MeleeEnabled=false }); entity.Position=position; AddChild(entity); return entity;
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
