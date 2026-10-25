// PL3 regression against actual World composition, candidate planning and preview placement.
using Godot;
using System;
using System.Linq;

public partial class ValidatePlantWorld : Node
{
    // Verify production definitions, runtime publication and traversal-independent candidate choices.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var world = GD.Load<PackedScene>("res://SCENES/World/World.tscn").Instantiate<WorldScene>();
            world.Config = (WorldMasterConfig)world.Config.Duplicate(); world.Config.RandomSeedOnLoad = false;
            AddChild(world);
            bool oldLayoutRejected = false;
            try { SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema, world.Blueprint with { LocalGeneratorVersion = 1 }); }
            catch (Exception) { oldLayoutRejected = true; }
            Check(oldLayoutRejected, "old local layout rejected safely");
            var oak = GD.Load<WorldBiomeDefinition>("res://WORLD/Biomes/Definitions/OakForest/Biome.tres");
            var pink = GD.Load<WorldBiomeDefinition>("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres");
            var definitions = oak.Populations.Select(p => p.Content).OfType<VegetationDefinition>().ToArray();
            Check(definitions.Length == 8 && definitions.Select(d => d.Id).Distinct().Count() == 8, "eight separate types");
            Check(!pink.Populations.Any(p => p.Content is VegetationDefinition), "Pink eligibility");
            foreach (var definition in definitions)
            {
                definition.Validate(); Check(definition.VisualVariants.Count == 1, "initial one-image types");
                var plant = definition.Template.Instantiate<VegetationInstance>();
                plant.Configure(definition, 0, true); plant.Free();
            }
            var published = world.ActorsRoot.FindChildren("*", "", true, false).OfType<VegetationInstance>().ToArray();
            Check(published.Length > 0, "normal World publishes plants");
            foreach (var plant in published)
            {
                Check(plant.GetMeta("world_content_id").AsString() == plant.Definition.Id, "canonical identity");
                Check(plant.GetMeta("world_biome_id").AsString() == oak.Id, "Oak-only publication");
                Check(!plant.IsProcessing() && !plant.IsPhysicsProcessing(), "dormant instance");
                Check(plant.GetNode<Sprite2D>("Sprite").Material is ShaderMaterial, "weather-compatible material");
            }
            var data = new ChunkSceneryData(world.MacroMap,world.FeaturePlan,world.Config.Streaming,world.Config.Generation.Biomes,world.Config.ExtraSpacing);
            var coordinates = published.Select(p => ChunkCoord.At(p.GlobalPosition, world.Config.Streaming.ChunkSize)).Distinct().ToArray();
            var first = coordinates.SelectMany(data.Build).ToDictionary(i => i.Id);
            var reverse = coordinates.Reverse().SelectMany(data.Build).ToDictionary(i => i.Id);
            Check(first.Count == reverse.Count && first.All(p => reverse[p.Key] == p.Value), "revisit/load order");
            // Exercise two real artworks under ONE temporary type through the actual candidate planner.
            var config = (WorldMasterConfig)world.Config.Duplicate(true);
            var temporary = config.Generation.Biomes.SelectMany(b => b.Populations).Select(p => p.Content).OfType<VegetationDefinition>().First();
            var extra = (VegetationVisualVariant)temporary.VisualVariants[0].Duplicate();
            extra.Texture = definitions[7].VisualVariants[0].Texture; extra.SourceAnchor = new(54.5f,100);
            extra.VisualMinimum = new(-55,-100); extra.VisualMaximum = new(56,0); temporary.VisualVariants.Add(extra);
            var multi = new ChunkSceneryData(world.MacroMap,world.FeaturePlan,config.Streaming,config.Generation.Biomes,config.ExtraSpacing);
            bool zero = false, one = false;
            var home = coordinates[0];
            for (int y = -10; y <= 10; y++) for (int x = -10; x <= 10; x++)
                foreach (var item in multi.Build(new ChunkCoord(home.X+x,home.Y+y)))
                    if (item.Content.Id == temporary.Id) { zero |= item.Variant == 0; one |= item.Variant == 1; }
            Check(zero && one, "candidate planner selects both artworks under one type");
            var preview = new WorldSceneryPlacement(); var destination = new Node2D();
            AddChild(preview); AddChild(destination); preview.Generate(world.Config, 1234, destination);
            Check(destination.GetChildren().OfType<VegetationInstance>().Any(), "compatibility preview plants");
            world.Free(); preview.Free(); destination.Free();
            GD.Print("PL3 plant World PASS"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // Surface a concrete invariant to the local test runner.
    // =========================================================
    private static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException("PL3: " + label); }
}
