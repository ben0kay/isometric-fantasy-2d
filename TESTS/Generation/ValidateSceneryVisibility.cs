// Real-world regression for shared cosmetic scheduling, chunk cancellation, SO1 paths and reversible A/B counters.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
public partial class ValidateSceneryVisibility : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print($"SCENERY STABILITY PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Fixed seed and explicit switches isolate visual scheduling from generation identity.
    // =========================================================
    private async Task Run()
    {
        var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
        world.Config.Streaming = (WorldStreamingConfig)world.Config.Streaming.Duplicate();
        AddChild(world);
        var env = world.GetNode<EnvironmentRuntime>("SystemsRoot/Environment");
        var player = world.GetNode<PlayerMovement>("Actors/Player"); var stream = world.Stream;
        Check(world.GetNode("TerrainRoot/StreamedGroundRoot").GetChildCount() == 25,"Ground ownership changed");
        Check(world.GetNodeOrNull("DebugUIRoot/EnvironmentDebugPanel") != null,"F9 owner missing");
        Check(world.GetNodeOrNull("DebugUIRoot/FireDebugPanel") != null,"Fire panel missing");
        Check(world.GetNodeOrNull("DebugUIRoot/MacroMapDebugPanel") != null,"F7 owner missing");
        foreach (Node section in world.GetChildren())
            GD.Print($"SO0 runtime /World/{section.Name}: {section.GetChildCount()} direct children");
        await Settle(220);
        Check(stream.PendingCount == 0,"Prefetch did not settle");
        var rain = env.Weather.GetNode<GroundRainSplashes>("GroundSplashes");
        var atmosphere = env.GetNode<WorldAtmosphere>("Atmosphere");
        int rainRegistrations = rain.RegistrationCount, treeRegistrations = atmosphere.RegistrationCount;
        await Settle(30);
        Check(rain.RegistrationCount == rainRegistrations && atmosphere.RegistrationCount == treeRegistrations,"Stationary scenery rebuilds source caches");
        int baselineCount = stream.LoadedCount;
        env.Clock.Config.Paused = true; env.Clock.SetHour(12); // Identical sun projection in both A/B samples.
        env.Config.CullOffscreenSceneryEffects = false; await Settle(3);
        Check(env.CosmeticChunkCount == env.RegisteredChunkCount,"A/B disabled state still suspends chunks");
        int allCasters = env.DayNight.Shadows.UpdatedCasterCount;
        double uncullMs = await MeasureShadows(env,60);
        env.Config.CullOffscreenSceneryEffects = true; await Settle(3);
        Check(env.CosmeticChunkCount < env.RegisteredChunkCount,"Off-screen chunk cosmetics remain active");
        Check(env.DayNight.Shadows.UpdatedCasterCount < allCasters,"Off-screen sun work was not reduced");
        Check(stream.LoadedCount == baselineCount,"Culling changed world ownership");
        double culledMs = await MeasureShadows(env,60);
        GD.Print($"SCENERY A/B: sun average {uncullMs:F3}ms -> {culledMs:F3}ms; casters {allCasters} -> {env.DayNight.Shadows.UpdatedCasterCount}; cosmetic chunks {env.CosmeticChunkCount}/{env.RegisteredChunkCount}. Headless CPU diagnostic only.");
        env.Snow.Config.Enabled = true; env.Snow.SetCoverage(0.7f);
        env.Wind.Config.Strength = -2;
        // Force cancellation after the first adapter really exists, while publication is still blocked.
        world.Config.Streaming.NodesPerFrame = 1;
        player.GlobalPosition = new Vector2(9000,5000); player.ResetPhysicsInterpolation();
        bool partialBinding = false;
        for (int i = 0; i < 80 && !partialBinding; i++)
        {
            await Settle(1);
            partialBinding = world.GetNode("Actors").GetChildren().OfType<Node2D>()
                .Any(n => n.HasMeta("world_chunk_id") && !n.Visible && n.HasMeta("chunk_wind"));
        }
        Check(partialBinding,"Cancellation fixture never reached partial adapter registration");
        player.GlobalPosition = new Vector2(-9000,5000); player.ResetPhysicsInterpolation(); await Settle(2);
        world.Config.Streaming.NodesPerFrame = 12;
        // Repeated quick travel also exercises cancellation during ordinary scene application.
        for (int i = 0; i < 8; i++)
        {
            player.GlobalPosition = new Vector2(i % 2 == 0 ? 9000 : -9000,5000);
            player.ResetPhysicsInterpolation(); await Settle(2);
        }
        player.GlobalPosition = world.Config.PlayerSpawn; player.ResetPhysicsInterpolation(); await Settle(260);
        Check(stream.PendingCount == 0 && stream.CanMoveTo(player.GlobalPosition),"Cancellation left a frontier stuck");
        Check(env.RegisteredChunkCount == stream.LoadedCount,"Cancelled binding leaked environmental adapters");
        var registeredProps = world.GetNode("Actors").FindChildren("*","",true,false).OfType<Node2D>().Where(n => n.HasMeta("world_instance_id")).ToArray();
        int torchCount = world.GetNode("Actors").GetChildren().OfType<Node2D>().Count(n => n.Name.ToString().Contains("Torch"));
        Check(rain.SourceCount == registeredProps.Length+torchCount,"Rain cache leaked cancelled/unloaded sources");
        Check(atmosphere.TreeSourceCount == registeredProps.OfType<FantasyGame.World.Trees.Tree>().Count(t => t.Definition.HasFoliage),"Leaf cache leaked cancelled/unloaded trees");
        Check(env.GetChildren().OfType<VegetationWind>().Count() == stream.LoadedCount+1,"Chunk wind adapters leaked");
        foreach (var chunk in world.GetNode("Actors").GetChildren().OfType<Node2D>().Where(n => n.HasMeta("world_chunk_id")))
            Check(chunk.PhysicsInterpolationMode == PhysicsInterpolationModeEnum.Off,"Static chunk interpolates from identity");
        foreach (var visual in world.GetNode("TerrainRoot/StreamedGroundRoot").GetChildren().OfType<Node2D>())
            Check(visual.PhysicsInterpolationMode == PhysicsInterpolationModeEnum.Off,"Ground interpolates from identity");
        Rect2 view = SceneryVisualBounds.View(env);
        var visibleTrees = world.GetNode("Actors").FindChildren("*","",true,false).OfType<FantasyGame.World.Trees.Tree>()
            .Select(t => t.GetNode<Sprite2D>("Sprite")).Where(s => SceneryVisualBounds.Sprite(s).Intersects(view)).ToArray();
        Check(visibleTrees.Length > 0,"No visible tree fixture");
        foreach (var sprite in visibleTrees)
        {
            var material = (ShaderMaterial)sprite.Material;
            Check(Mathf.Abs(material.GetShaderParameter("wind_multiplier").AsSingle()-env.Wind.Strength)<0.001f,"Visible foliage restored stale wind");
            Check(material.GetShaderParameter("snow_coverage").AsSingle()>0.6f,"Visible foliage restored stale snow");
        }
        world.QueueFree(); await Settle(3);
        Check(GetTree().GetNodesInGroup("world_trees").Count == 0,"World exit leaked registrations");
        // A negative-coordinate prop crossing spatial cells blocks rain until explicitly removed.
        var rainActors = new Node2D(); AddChild(rainActors);
        var testWeather = new WorldWeather { Config = new WeatherProfile { Splashes=false } };
        var isolatedRain = new GroundRainSplashes { Weather = testWeather, Actors = rainActors, WorldBounds = new Rect2(-2048,-2048,4096,4096) };
        AddChild(isolatedRain); isolatedRain.SetProcess(false);
        var blocker = new Node2D { Position = new Vector2(-256,-256) }; rainActors.AddChild(blocker);
        blocker.AddChild(new Sprite2D { Name = "Sprite", Texture = new GradientTexture2D { Width=64,Height=64 } });
        isolatedRain.RegisterSource(blocker); isolatedRain.RegisterSource(blocker);
        Check(isolatedRain.SourceCount == 1 && isolatedRain.SpatialCellCount >= 4,"Spatial registration duplicates or loses negative border cells");
        var random = new RandomNumberGenerator { Seed = 40 };
        Check(!isolatedRain.TryGroundPoint(new Rect2(-260,-260,8,8),random,out _),"Rain ignores registered artwork");
        isolatedRain.UnregisterSource(blocker);
        Check(isolatedRain.SourceCount == 0 && isolatedRain.SpatialCellCount == 0,"Removal leaves ghost rain exclusions");
        Check(isolatedRain.TryGroundPoint(new Rect2(-260,-260,8,8),random,out _),"Removed scenery still blocks rain");
        isolatedRain.QueueFree(); rainActors.QueueFree(); await Settle(3); testWeather.Free();
        // A synthetic caster outside the view must stay active when its long shadow can reach the view.
        var actors = new Node2D(); AddChild(actors);
        var body = new StaticBody2D { Position = new Vector2(-100,100) }; actors.AddChild(body);
        var sprite2 = new Sprite2D { Name = "Sprite", Texture = new GradientTexture2D { Width = 20, Height = 300 }, Position = new Vector2(0,-150) }; body.AddChild(sprite2);
        var wind = new WindController { Config = new WindMasterConfig() }; AddChild(wind);
        var shadows = new SunShadowRenderer { Wind = wind, PrepareMargin = 0 }; AddChild(shadows); shadows.Attach(actors);
        shadows.UpdateProjection(0,3,0.5f,0);
        Check(shadows.UpdatedCasterCount == 1,"Off-screen caster with an on-screen shadow was culled");
        actors.QueueFree(); shadows.QueueFree(); wind.QueueFree(); await Settle(3);
        var preview = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
        preview.Config = (WorldMasterConfig)preview.Config.Duplicate(); preview.Config.Streaming = null; preview.Config.RandomSeedOnLoad = false;
        AddChild(preview); await Settle(5);
        Check(preview.Stream == null && preview.GetNode<Node2D>("TerrainRoot/Ground").Visible,"SO1 broke preview mode");
        Check(preview.GetNode("TerrainRoot/StreamedGroundRoot").GetChildCount()==0,"Preview spawned streamed ground");
        preview.QueueFree(); await Settle(3);
    }
    private async Task<double> MeasureShadows(EnvironmentRuntime env,int frames)
    {
        double total = 0;
        for (int i = 0; i < frames; i++) { await Settle(1); total += env.DayNight.Shadows.LastProjectionMilliseconds; }
        return total/frames;
    }
    private async Task Settle(int frames) { for (int i = 0; i < frames; i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private void Check(bool condition,string message) { _checks++; if (!condition) throw new Exception(message); }
}
