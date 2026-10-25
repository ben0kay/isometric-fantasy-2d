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
        var fixture = new Node2D(); var body = new Node2D();
        body.AddChild(new Sprite2D { Name = "Sprite", Texture = new GradientTexture2D() }); fixture.AddChild(body);
        var initialShadows = new SunShadowRenderer(); initialShadows.Attach(fixture);
        Check(initialShadows.GetChildren().OfType<Sprite2D>().All(s => !s.Visible),
            "New sun shadows are visible before their first valid projection");
        initialShadows.Free(); fixture.Free();
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
        AddChild(world);
        var initialActors = world.ActorsRoot;
        Check(initialActors.FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>()
            .Any(t => t.GlobalPosition.DistanceTo(world.Config.PlayerSpawn) < 700),"Spawn clearing hides all nearby trees");
        var stream = world.Stream; var player = world.Player;
        Check(world.TestActorsRoot.GetChildren().OfType<IWorldNavigationAgent>().Count() == 8,"Shared enemies did not load/spawn");
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
        var seasonEnvironment = world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        seasonEnvironment.Seasons.SetEnabled(true); seasonEnvironment.Seasons.SetYearPosition(0.76f);
        var seasonalAdapter = seasonEnvironment.GetNode<SeasonalFoliageAdapter>("SeasonalFoliage");
        var homeTiming = world.ActorsRoot.FindChildren("*", "", true, false).OfType<FantasyGame.World.Trees.Tree>()
            .Where(t => t.Definition.SeasonalFoliage != null)
            .ToDictionary(t => t.GetMeta("world_instance_id").AsString(), t => t.GetNode<Sprite2D>("Sprite").GetInstanceShaderParameter("seasonal_bucket").AsSingle());
        var home = Capture(world); Check(home.Count > 0,"No initial scenery");
        var tree = world.ActorsRoot.FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>().First();
        string burntId = tree.GetMeta("world_instance_id").AsString(); tree.GetNode<Burnable>("Burnable").RestoreProgress(1);
        var removed = world.ActorsRoot.FindChildren("*","",true,false).OfType<Node2D>()
            .First(n => n.HasMeta("world_instance_id") && n.GetMeta("world_instance_id").AsString() != burntId);
        string removedId = removed.GetMeta("world_instance_id").AsString(); home.Remove(removedId); removed.QueueFree();
        var env = world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment"); env.Snow.Config.Enabled = true; env.Snow.SetCoverage(0.6f);
        foreach (Vector2 destination in new[] { new Vector2(9000,5000),new Vector2(-9000,-5000),new Vector2(5000,-9000),Vector2.Zero })
        {
            player.GlobalPosition = destination; player.ResetPhysicsInterpolation();
            for (int i = 0; i < 250; i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach (Node2D prop in world.ActorsRoot.FindChildren("*","",true,false).OfType<Node2D>().Where(n => n.HasMeta("world_biome_id")))
            {
                string biome = prop.GetMeta("world_biome_id").AsString(); seenBiomes.Add(biome);
                Check(world.MacroMap.BiomeAt(prop.GlobalPosition)?.Id == biome,"Scenery does not follow its macro biome");
            }
            var shadows = env.DayNight.Shadows.FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>().ToArray();
            Check(shadows.Length > 0 && shadows.All(s => s.PhysicsInterpolationMode == Node.PhysicsInterpolationModeEnum.Off),
                "Process-driven shadows interpolate through uninitialized full-size transforms");
            var foliageShaders = world.ActorsRoot.FindChildren("*","",true,false)
                .OfType<FantasyGame.World.Trees.Tree>().Where(t => t.GetNode<Burnable>("Burnable").State == BurnState.Healthy)
                .Select(t => ((ShaderMaterial)t.GetNode<Sprite2D>("Sprite").Material).Shader.GetInstanceId()).Distinct().Count();
            Check(foliageShaders <= 8,"Chunks compile separate copies of the same foliage shaders");
            var healthy = world.ActorsRoot.FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>()
                .First(t => t.GetNode<Burnable>("Burnable").State == BurnState.Healthy);
            var material = (ShaderMaterial)healthy.GetNode<Sprite2D>("Sprite").Material;
            Check(material.Shader.Code.Contains("wet_foliage_colour"),"New chunk foliage lost rain wetness");
            Check(Mathf.Abs(material.GetShaderParameter("wind_multiplier").AsSingle()-env.Wind.Strength) < 0.001f,"Wet/snow material stopped following shared wind");
            foreach (var oak in world.ActorsRoot.FindChildren("*", "", true, false).OfType<FantasyGame.World.Trees.Tree>()
                .Where(t => t.Definition.SeasonalFoliage != null).Take(8))
            {
                var sprite = oak.GetNode<Sprite2D>("Sprite"); string id = oak.GetMeta("world_instance_id").AsString();
                int bucket = SeasonalFoliageAdapter.TimingBucket(id);
                Check(sprite.GetInstanceShaderParameter("seasonal_bucket").AsSingle() == bucket, "Streamed seasonal timing changed stable identity");
                var atlas = (Texture2D)((ShaderMaterial)sprite.Material).GetShaderParameter("seasonal_state").AsGodotObject();
                Check(atlas != null && atlas.GetImage().GetPixel(bucket, 0).A > 0.9f, "New Autumn chunk has missing/neutral seasonal atlas");
            }
            int liveProfiles = world.ActorsRoot.FindChildren("*", "", true, false).OfType<FantasyGame.World.Trees.Tree>()
                .Select(t => t.Definition.SeasonalFoliage).Where(p => p != null).Distinct().Count();
            Check(!seasonalAdapter.IsProcessing() && seasonalAdapter.PaletteCount <= liveProfiles + 1
                && seasonalAdapter.ChunkCount <= stream.LoadedCount + 2, "Seasonal streaming cache/callback bound failed");
            Check(stream.LoadedCount <= 81,"Loaded chunks grew beyond the unload square");
            Check(stream.CanMoveTo(destination),"Active chunks did not finish loading");
            Check(GetTree().GetNodesInGroup("world_trees").Count < 1500,"Unloaded tree nodes leaked");
            GD.Print($"P3 traverse {destination}: {stream.LoadedCount} loaded, nodes {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}");
        }
        Check(seenBiomes.Count == 2,"Traversal did not cover both configured biomes");
        var returned = Capture(world);
        foreach (var pair in home) Check(returned.TryGetValue(pair.Key,out Vector2 point) && point == pair.Value,"Home scenery changed after unload/reload");
        var restored = world.ActorsRoot.FindChildren("*","",true,false).OfType<Node2D>().First(n => n.HasMeta("world_instance_id") && n.GetMeta("world_instance_id").AsString() == burntId);
        Check(restored.GetNode<Burnable>("Burnable").State == BurnState.Burnt,"Burnt state reset on chunk return");
        foreach (var oak in world.ActorsRoot.FindChildren("*", "", true, false).OfType<FantasyGame.World.Trees.Tree>())
            if (homeTiming.TryGetValue(oak.GetMeta("world_instance_id").AsString(), out float bucket))
                Check(oak.GetNode<Sprite2D>("Sprite").GetInstanceShaderParameter("seasonal_bucket").AsSingle() == bucket, "Returning tree changed seasonal timing");
        Check(!returned.ContainsKey(removedId),"Removed runtime prop resurrected on return");
        Check(world.Blueprint.LocalGeneratorVersion == ChunkSceneryData.GeneratorVersion && world.Blueprint.ChunkSize == 512,"Missing local generation manifest metadata");
        Check(world.Blueprint.WorldId == stream.WorldId,"Chunk state lost world identity");
        world.QueueFree(); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(GetTree().GetNodesInGroup("world_trees").Count == 0,"World exit leaked tree registrations");
    }
    // Snapshot stable per-instance IDs rather than scene-tree child order or runtime node names.
    // =========================================================
    private static Dictionary<string,Vector2> Capture(WorldScene world) => world.ActorsRoot.FindChildren("*","",true,false).OfType<Node2D>()
        .Where(n => n.HasMeta("world_instance_id")).ToDictionary(n => n.GetMeta("world_instance_id").AsString(),n => n.GlobalPosition);
    private void Check(bool condition,string message) { _checks++; if (!condition) throw new Exception(message); }
}
