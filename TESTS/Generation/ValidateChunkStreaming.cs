// P3 integration: deterministic cross-chunk placement, bounded traversal, cache teardown and runtime deltas.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FantasyGame.WorldInteractions.Fire;
public partial class ValidateChunkStreaming : Node
{
    private int _checks;
    // Exercise actual production startup, snow/fire adapters, distant teleports and return trips.
    // =========================================================
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"P3 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private async Task Run()
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
        AddChild(world);
        var stream = world.Stream; var player = world.GetNode<PlayerMovement>("Actors/Player");
        Check(world.GetNode<Node2D>("Actors").GetChildren().OfType<IWorldNavigationAgent>().Count() == 1,"Shared enemy did not load/spawn");
        Check(stream != null && stream.LoadedCount == 25,"Initial active square missing");
        Check(ChunkCoord.At(new Vector2(-0.1f,-512.1f),512) == new ChunkCoord(-1,-2),"Negative coordinate floor failed");
        var data = new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing);
        foreach (var edge in new[] { ChunkCoord.At(world.MacroMap.Bounds.Position,512),ChunkCoord.At(world.MacroMap.Bounds.End-Vector2.One,512) })
            foreach (var item in data.Build(edge)) Check(world.MacroMap.Bounds.HasPoint(item.Position),"Edge chunk escaped finite bounds");
        var seenBiomes = new HashSet<string>();
        var coords = new[] { new ChunkCoord(6,4),new ChunkCoord(7,4),new ChunkCoord(6,5),new ChunkCoord(7,5) };
        var original = coords.SelectMany(data.Build).ToDictionary(i => i.Id);
        var reversed = coords.Reverse().SelectMany(data.Build).ToDictionary(i => i.Id);
        Check(original.Count > 0 && original.Count == reversed.Count,"Empty or changed neighbour data");
        foreach (var pair in original) Check(pair.Value == reversed[pair.Key],"Load order changed instance/geometry");
        var items = original.Values.ToArray();
        for (int i = 0; i < items.Length; i++) for (int j = i+1; j < items.Length; j++)
        { float distance = items[i].Radius+items[j].Radius+world.Config.ExtraSpacing; Check(items[i].Position.DistanceSquaredTo(items[j].Position) >= distance*distance,"Overlapping chunk seam scenery"); }
        var home = Capture(world); Check(home.Count > 0,"No initial scenery");
        var tree = world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>().First();
        string burntId = tree.GetMeta("world_instance_id").AsString(); tree.GetNode<Burnable>("Burnable").RestoreProgress(1);
        var removed = world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<Node2D>()
            .First(n => n.HasMeta("world_instance_id") && n.GetMeta("world_instance_id").AsString() != burntId);
        string removedId = removed.GetMeta("world_instance_id").AsString(); home.Remove(removedId); removed.QueueFree();
        var env = world.GetNode<EnvironmentRuntime>("Environment"); env.Snow.Config.Enabled = true; env.Snow.SetCoverage(0.6f);
        foreach (Vector2 destination in new[] { new Vector2(9000,5000),new Vector2(-9000,-5000),new Vector2(5000,-9000),Vector2.Zero })
        {
            player.GlobalPosition = destination; player.ResetPhysicsInterpolation();
            for (int i = 0; i < 250; i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach (Node2D prop in world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<Node2D>().Where(n => n.HasMeta("world_biome_id")))
            {
                string biome = prop.GetMeta("world_biome_id").AsString(); seenBiomes.Add(biome);
                Check(world.MacroMap.BiomeAt(prop.GlobalPosition)?.Id == biome,"Scenery does not follow its macro biome");
            }
            var healthy = world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>()
                .First(t => t.GetNode<Burnable>("Burnable").State == BurnState.Healthy);
            var material = (ShaderMaterial)healthy.GetNode<Sprite2D>("Sprite").Material;
            Check(material.Shader.Code.Contains("wet_foliage_colour"),"New chunk foliage lost rain wetness");
            Check(Mathf.Abs(material.GetShaderParameter("wind_multiplier").AsSingle()-env.Wind.Strength) < 0.001f,"Wet/snow material stopped following shared wind");
            Check(stream.LoadedCount <= 81,"Loaded chunks grew beyond the unload square");
            Check(stream.CanMoveTo(destination),"Active chunks did not finish loading");
            Check(GetTree().GetNodesInGroup("world_trees").Count < 1500,"Unloaded tree nodes leaked");
            GD.Print($"P3 traverse {destination}: {stream.LoadedCount} loaded, nodes {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}");
        }
        Check(seenBiomes.Count == 2,"Traversal did not cover both configured biomes");
        var returned = Capture(world);
        foreach (var pair in home) Check(returned.TryGetValue(pair.Key,out Vector2 point) && point == pair.Value,"Home scenery changed after unload/reload");
        var restored = world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<Node2D>().First(n => n.HasMeta("world_instance_id") && n.GetMeta("world_instance_id").AsString() == burntId);
        Check(restored.GetNode<Burnable>("Burnable").State == BurnState.Burnt,"Burnt state reset on chunk return");
        Check(!returned.ContainsKey(removedId),"Removed runtime prop resurrected on return");
        Check(world.Blueprint.LocalGeneratorVersion == 1 && world.Blueprint.ChunkSize == 512,"Missing local generation manifest metadata");
        Check(world.Blueprint.WorldId == stream.WorldId,"Chunk state lost world identity");
        world.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(GetTree().GetNodesInGroup("world_trees").Count == 0,"World exit leaked tree registrations");
    }
    // Snapshot stable per-instance IDs rather than scene-tree child order or runtime node names.
    // =========================================================
    private static Dictionary<string,Vector2> Capture(WorldScene world) => world.GetNode<Node2D>("Actors").FindChildren("*","",true,false).OfType<Node2D>()
        .Where(n => n.HasMeta("world_instance_id")).ToDictionary(n => n.GetMeta("world_instance_id").AsString(),n => n.GlobalPosition);
    private void Check(bool condition,string message) { _checks++; if (!condition) throw new Exception(message); }
}
