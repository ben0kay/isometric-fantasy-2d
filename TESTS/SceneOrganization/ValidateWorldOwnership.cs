// SO2 structural integration: ownership, initial source discovery and clean streamed/preview teardown.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using WorldTree = FantasyGame.World.Trees.Tree;
public partial class ValidateWorldOwnership : Node
{
    private int _checks;
    // Behaviour/visual/performance acceptance is separate; this fixture checks actual instantiated nodes.
    // =========================================================
    public override async void _Ready()
    {
        try
        {
            foreach (bool streamed in new[] { true,false }) await CheckWorld(streamed);
            GD.Print($"SO2 OWNERSHIP PASS: {_checks} checks"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private async Task CheckWorld(bool streamed)
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
        world.Config.Streaming = (WorldStreamingConfig)world.Config.Streaming.Duplicate(); world.Config.Streaming.Enabled = streamed;
        AddChild(world);
        Check(world.StartupError == null,"World startup failed: " + world.StartupError);
        Check(world.GetNodeOrNull("Actors") == null,"Legacy Actors alias concealed a dependency");
        Check(world.SceneryPlacement.GetParent() == world.GetNode("SystemsRoot") && world.GetNode("SystemsRoot/SceneryPlacement") is not Node2D,"Generator still owns live scenery");
        Check(world.SceneryPlacement.GetChildCount()==0,"Generator has live actor children");
        Check(world.Player.GetParent()==world.EntitiesRoot,"Player belongs to a disposable root");
        Check(world.TestActorsRoot.GetChildren().OfType<Entity>().Count()==5,"Authored entity destination incorrect");
        Check(world.PropsRoot.GetNodeOrNull("Torch")!=null,"Original torch missing from persistent props");
        Check(world.PropsRoot.GetChildCount()==3,"Comparison torches missing or duplicated");
        foreach (var root in new[] { world.ActorsRoot,world.EntitiesRoot,world.PropsRoot,world.ChunkActorsRoot,world.PreviewSceneryRoot })
        {
            Check(root.YSortEnabled,"Nested ownership container breaks shared Y sorting");
            Check(root.Transform.IsEqualApprox(Transform2D.Identity),"Ownership root has hidden transform");
        }
        var sources = WorldActorSources.Initial(world.ActorsRoot).ToArray();
        Check(sources.Contains(world.Player),"Initial source discovery lost nested player");
        Check(!sources.Any(n=>n.HasMeta("world_chunk_id") || n.HasMeta("world_actor_container")),"Initial adapters received containers/chunks");
        var atmosphere = world.Environment.GetNode<WorldAtmosphere>("Atmosphere");
        var trees = world.ActorsRoot.FindChildren("*","",true,false).OfType<WorldTree>().ToArray();
        Check(trees.Length>0,"No scenery to validate");
        Check(atmosphere.TreeSourceCount==trees.Count(t=>t.Definition.HasFoliage),"Nested preview/stream leaf sources were missed or doubled");
        Check(world.Environment.GroundQueries.SourceCount>3,"Nested scenery missing from artwork query index");
        Check(world.Environment.Snow.Player==world.Player,"Snow did not receive explicit player reference");
        Check(world.Environment.DayNight.Shadows.GetChildren().OfType<Sprite2D>().Any(n=>n.Name.ToString().StartsWith("Player")),"Nested player lost its sun shadow");
        if (streamed)
        {
            Check(world.Stream.LoadedCount==25 && world.ChunkActorsRoot.GetChildCount()==25,"Stream actor destination changed initial ring");
            Check(world.StreamedGroundRoot.GetChildCount()==25,"Stream ground destination incorrect");
            Check(world.PreviewSceneryRoot.GetChildCount()==0,"Preview duplicated streamed scenery");
            Check(sources.All(n=>!n.HasMeta("world_instance_id")),"Initial adapters scanned streamed props twice");
        }
        else
        {
            Check(world.Stream==null && world.ChunkActorsRoot.GetChildCount()==0 && world.StreamedGroundRoot.GetChildCount()==0,"Preview created a second stream");
            Check(world.PreviewSceneryRoot.GetChildCount()>0,"Preview output missing");
            Check(trees.All(t=>t.GetParent()==world.PreviewSceneryRoot),"Preview scenery still belongs to a controller");
            Check(sources.OfType<WorldTree>().Count()==trees.Length,"Preview trees excluded from initial adapters");
        }
        world.QueueFree(); await Frames(3);
        Check(GetTree().GetNodesInGroup("world_trees").Count==0,"World exit leaked live tree references");
    }
    private async Task Frames(int count) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool condition,string message) { _checks++; if(!condition) throw new InvalidOperationException(message); }
}
