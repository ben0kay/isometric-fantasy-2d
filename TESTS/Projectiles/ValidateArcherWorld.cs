// R1 integration: playable practice reset plus real streamed World service ownership/segment guards/unload.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateArcherWorld : Node
{
    private int _checks;
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            var practice=GD.Load<PackedScene>("res://TESTS/Projectiles/ArcherPractice.tscn").Instantiate<ArcherPractice>(); AddChild(practice);
            await Frames(340);
            Check(practice.Archer.GetNode<GroundChase>("GroundChase").Ranged?.ShotsFired>0,"Practice archer never fired");
            Check(practice.Player.HealthState.Current<practice.Player.HealthState.Maximum,"Practice arrows missed stationary player");
            var reset=practice.FindChildren("*","Button",true,false).OfType<Button>().Single(); reset.EmitSignal(BaseButton.SignalName.Pressed);
            Check(practice.Archer.Health==50 && practice.Player.HealthState.Current==practice.Player.HealthState.Maximum && practice.Projectiles.ActiveCount==0,"Practice reset");
            practice.QueueFree(); await Frames(4);
            var world=GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
            world.Config=(WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad=false;
            // Default F6 World mixes the authored Basic practice group with three Archer markers.
            AddChild(world); await Frames(120);
            Check(world.StartupError==null && world.Stream!=null,"Streamed archer World failed");
            var arrows=world.ActorsRoot.GetNodeOrNull<ProjectileSpawner>("Projectiles");
            Check(arrows!=null && arrows==world.GetProjectileSpawner() && arrows.GetParent()==world.ActorsRoot,"Missing/duplicate/wrong projectile owner");
            Check(!arrows.IsPhysicsProcessing() && !arrows.IsProcessing(),"World projectile owner ticks");
            var actors=world.TestActorsRoot.GetChildren().OfType<Entity>().ToArray();
            Check(actors.Length==8 && actors.Count(e=>e.Config.Ranged==null)==5
                && actors.Count(e=>e.Config.Ranged!=null)==3
                && actors.Where(e=>e.Config.Ranged!=null).All(e=>e.GetNode<GroundChase>("GroundChase").Ranged!=null),
                "Mixed practice skeletons or ranged injection failed");
            Vector2 point=world.Player.GlobalPosition;
            Check(world.Stream.IsSegmentLoaded(point+new Vector2(-64,-64),point+new Vector2(64,64)),"Loaded diagonal failed");
            Check(world.Stream.IsSegmentLoaded(point,point),"Loaded zero-length segment");
            Check(!world.Stream.IsSegmentLoaded(point,new Vector2(-1000000,-1000000)),"Outside-world segment accepted");
            int size=world.Config.Streaming.ChunkSize;
            var chunk=world.Stream.PlayerChunk;
            Vector2 edge=new((chunk.X+1)*size,point.Y);
            Check(world.Stream.IsSegmentLoaded(edge,edge+new Vector2(0,32)),"Loaded edge segment");
            var original=arrows.IsSegmentLoaded; arrows.IsSegmentLoaded=(_,_)=>false;
            await Frames(4); Check(arrows.ActiveCount==0,"Segment guard did not retire live arrows"); arrows.IsSegmentLoaded=original;
            var root=(DebugActorRoot)world.TestActorsRoot; root.Enabled=false; await Frames(3);
            Check(actors.All(e=>!e.GetNode<GroundChase>("GroundChase").Ranged.Drawing) && arrows.ActiveCount==0,"Debug suspension retained shot intent");
            root.Enabled=true; await Frames(60);
            world.QueueFree(); await Frames(5);
            Check(!GodotObject.IsInstanceValid(arrows),"World exit retained projectile owner");
            GD.Print($"R1 WORLD PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame); }
    // =========================================================
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new Exception(message); }
}
