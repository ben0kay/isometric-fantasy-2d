// AC2 disposable lifecycle checks using real player/shared Entity bodies, without campaign data.
using Godot;
using System;
using System.Threading.Tasks;

public partial class ValidateActorLifecycle : Node2D
{
    private int _checks;

    // Disabled ownership removes physics bodies; reactivation and health restoration restore valid blocking.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>();
            AddChild(player); player.SetPhysicsProcess(false);
            var root=new DebugActorRoot { Name="Practice" }; AddChild(root);
            var actor=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
            actor.Configure(new EntityDefinition { ChasePlayer=false,DeathFadeSeconds=10 }); actor.Position=new Vector2(60,0); root.AddChild(actor);
            await Frames();
            Check(player.TestMove(player.GlobalTransform,new Vector2(100,0)),"Living actor did not block");
            root.Enabled=false; await Frames();
            Check(!actor.CanProcess(),"Disabled parent left actor processing");
            Check(!player.TestMove(player.GlobalTransform,new Vector2(100,0)),"Disabled parent left ghost collider");
            root.Enabled=true; await Frames();
            Check(actor.CanProcess() && player.TestMove(player.GlobalTransform,new Vector2(100,0)),"Reenabled actor did not block");
            actor.Velocity=new Vector2(100,0); root.Enabled=false; await Frames();
            Check(actor.Velocity==Vector2.Zero,"Suspension retained stale enemy velocity");
            root.Enabled=true; actor.TakeDamage(actor.Health); await Frames();
            Check(actor.GetNode<CollisionShape2D>("Collision").Disabled,"Dead entity collider remained enabled");
            Check(!player.TestMove(player.GlobalTransform,new Vector2(100,0)),"Dead entity remained solid");
            player.HealthState.Damage(player.HealthState.Current); await Frames();
            Check(player.GetNode<CollisionShape2D>("Collision").Disabled && !player.IsPhysicsProcessing(),"Dead player retained blocking/movement");
            player.HealthState.Restore(player.HealthState.Maximum); await Frames(); player.SetPhysicsProcess(false);
            Check(!player.GetNode<CollisionShape2D>("Collision").Disabled,"Player restoration lost collider");
            root.QueueFree(); await Frames();
            Check(!player.TestMove(player.GlobalTransform,new Vector2(100,0)),"Retired actor left ghost body");
            // Hop changes only art; existing C2.5 fixture covers action priorities and damage.
            Check(player.TryHop(),"Hop request rejected");
            Check(!player.GetNode<CollisionShape2D>("Collision").Disabled,"Hop disabled physical blocking");
            player.ProcessMode=ProcessModeEnum.Disabled; await Frames();
            Check(!player.IsHopping && player.Velocity==Vector2.Zero,"Suspension retained player action/motion");
            player.ProcessMode=ProcessModeEnum.Inherit; await Frames();
            Check(!player.GetNode<CollisionShape2D>("Collision").Disabled,"Living player did not reactivate collider");
            await MovementAgainstWall(player);
            GD.Print($"AC2 LIFECYCLE PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }

    // Exercise real walk/sprint/dodge input through the existing player MoveAndSlide motor.
    // =========================================================
    private async Task MovementAgainstWall(PlayerMovement player)
    {
        var wall=new StaticBody2D { Position=new Vector2(50,0),CollisionLayer=1,CollisionMask=1 };
        wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(10,100) } }); AddChild(wall);
        var input=player.GetNode<PlayerInput>("Input");
        for(int mode=0;mode<3;mode++)
        {
            player.SetPhysicsProcess(false); input.ResetMovementActions(); player.Position=Vector2.Zero;
            Input.ActionPress("player_right"); await Frames();
            if (mode==1) input._UnhandledInput(new InputEventKey { Keycode=Key.Shift,Location=KeyLocation.Right,Pressed=true });
            if (mode==2) input.EmitSignal(PlayerInput.SignalName.DodgeRequested,Vector2.Right);
            player.SetPhysicsProcess(true);
            for(int i=0;i<40;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Input.ActionRelease("player_right"); input.ResetMovementActions(); player.SetPhysicsProcess(false);
            Check(player.Position.X>0 && player.Position.X<39,"Walk/sprint/dodge crossed wall, mode "+mode);
        }
    }

    // Synchronize deferred collider changes and physics-server lifetime transitions.
    // =========================================================
    private async Task Frames()
    { for(int i=0;i<3;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }

    // Report the violated contract without mutating user saves.
    // =========================================================
    private void Check(bool condition,string message)
    { _checks++; if(!condition) throw new Exception(message); }
}
