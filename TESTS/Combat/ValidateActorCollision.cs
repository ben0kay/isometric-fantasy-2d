// AC1 disposable production-body checks; no campaigns, extra movement controllers or species-specific scenes.
using Godot;
using System;
using System.Threading.Tasks;

public partial class ValidateActorCollision : Node2D
{
    private int _checks;

    // Exercise actual scene masks and swept movement against actors and a world-solid wall.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var player=GD.Load<PackedScene>("res://ENTITIES/Player/Player.tscn").Instantiate<PlayerMovement>();
            AddChild(player); player.SetPhysicsProcess(false);
            Entity first=MakeEntity(new Vector2(60,0)), second=MakeEntity(new Vector2(120,0));
            var wall=new StaticBody2D { Position=new Vector2(200,0),CollisionLayer=1,CollisionMask=1 };
            wall.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(10,100) } }); AddChild(wall);
            await Sync();
            Check(player.CollisionLayer==2 && player.CollisionMask==3,"Player classification");
            Check(first.CollisionLayer==2 && first.CollisionMask==3,"Shared entity classification");
            using var profile=new AttackProfile();
            Check(profile.CollisionMask==3,"Default melee mask must include actors and scenery");
            var collision=player.MoveAndCollide(new Vector2(100,0));
            Check(collision!=null && collision.GetCollider()==first,"Player must stop at entity");
            Check(player.Position.X<first.Position.X-10,"Player crossed entity");
            collision=first.MoveAndCollide(new Vector2(100,0));
            Check(collision!=null && collision.GetCollider()==second,"Entity must stop at entity");
            collision=second.MoveAndCollide(new Vector2(200,0));
            Check(collision!=null && collision.GetCollider()==wall,"Entity must stop at world solid");
            player.Position=new Vector2(160,0); await Sync();
            collision=player.MoveAndCollide(new Vector2(100,0));
            Check(collision!=null && collision.GetCollider()==second,"Player must detect actor on layer 2");
            second.QueueFree(); await Sync();
            collision=player.MoveAndCollide(new Vector2(100,0));
            Check(collision!=null && collision.GetCollider()==wall,"Player must detect world solid on layer 1");
            // Same mask/source exclusion used by campaign arrival: detect actors as well as scenery.
            var feet=first.GetNode<CollisionShape2D>("Collision");
            using var query=new PhysicsShapeQueryParameters2D { Shape=feet.Shape,Transform=feet.GlobalTransform,
                CollisionMask=player.CollisionMask,CollideWithAreas=false,CollideWithBodies=true,
                Exclude=new Godot.Collections.Array<Rid> { player.GetRid() } };
            Check(GetWorld2D().DirectSpaceState.IntersectShape(query,8).Count>0,"Arrival-style query missed actor");
            feet=(CollisionShape2D)wall.GetChild(0); query.Shape=feet.Shape; query.Transform=feet.GlobalTransform;
            Check(GetWorld2D().DirectSpaceState.IntersectShape(query,8).Count>0,"Arrival-style query missed scenery");
            GD.Print($"AC1 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }

    // Reuse the canonical shared Entity scene with pursuit disabled.
    // =========================================================
    private Entity MakeEntity(Vector2 position)
    {
        var entity=GD.Load<PackedScene>("res://ENTITIES/Shared/Entity.tscn").Instantiate<Entity>();
        entity.Configure(new EntityDefinition { ChasePlayer=false }); entity.Position=position; AddChild(entity); return entity;
    }

    // Allow physics registration/removal to settle between isolated scenarios.
    // =========================================================
    private async Task Sync()
    { for(int i=0;i<3;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }

    // Fail with the exact violated collision contract.
    // =========================================================
    private void Check(bool condition,string message)
    { _checks++; if(!condition) throw new Exception(message); }
}
