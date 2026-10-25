// PL1 contract fixture; no biome registration or production asset moves.
using Godot;
using System;

public partial class ValidateVegetation : Node
{
    // Test one shared template, independent types, seeded cosmetic selection, atlas roots and dormant nodes.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var template = GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Plants/Plant.tscn");
            var first = GD.Load<Texture2D>("res://WORLD/Content/Vegetation/Plants/Sprites/plant_oak_001.png");
            var last = GD.Load<Texture2D>("res://WORLD/Content/Vegetation/Plants/Sprites/plant_oak_008.png");
            var atlas = new AtlasTexture { Atlas = last, Region = new Rect2(8, 14, 93, 86) };
            var a = new VegetationDefinition { Id = "plant.001", Template = template };
            var b = new VegetationDefinition { Id = "plant.008", Template = template };
            a.VisualVariants.Add(new VegetationVisualVariant { Texture = first, SourceAnchor = new(45.5f, 91) });
            b.VisualVariants.Add(new VegetationVisualVariant { Texture = last, SourceAnchor = new(54.5f, 100) });
            Verify(a, 0, false); Verify(b, 0, true);
            a.VisualVariants.Add(new VegetationVisualVariant { Texture = atlas, SourceAnchor = new(46.5f, 86), GroundOffset = new(3, 2), SpriteScale = new(0.5f, 0.75f) });
            var rng = new RandomNumberGenerator(); bool zero = false, one = false;
            for (ulong key = 1; key <= 64; key++)
            {
                rng.Seed = key; float sample = (rng.Randi() >> 8) / 16777216f;
                int index = a.SelectVariant(sample);
                rng.Seed = key;
                Check(index == a.SelectVariant((rng.Randi() >> 8) / 16777216f), "seed repeat");
                zero |= index == 0; one |= index == 1;
                Verify(a, index, false); Verify(a, index, true);
            }
            Check(zero && one && a.Id == "plant.001", "both cosmetic choices retain one identity");
            bool rejected = false;
            try { a.SelectVariant(float.NaN); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "invalid sample rejected");
            GD.Print("PL1 vegetation contract PASS"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // Verify the selected source root maps to the ground point for both orientations and atlas sizes.
    // =========================================================
    private void Verify(VegetationDefinition definition, int index, bool mirrored)
    {
        var instance = definition.Template.Instantiate<VegetationInstance>();
        instance.Configure(definition, index, mirrored); AddChild(instance);
        var sprite = instance.GetNode<Sprite2D>("Sprite"); var art = definition.VisualVariants[index];
        Vector2 anchor = art.SourceAnchor, ground = art.GroundOffset;
        if (mirrored) { anchor.X = art.Texture.GetWidth() - anchor.X; ground.X = -ground.X; }
        Check((sprite.Position + anchor * sprite.Scale).IsEqualApprox(ground), "root anchor");
        Check(sprite.Texture == art.Texture && instance.Definition == definition && sprite.FlipH == mirrored, "selected art/type");
        Check(!instance.IsProcessing() && !instance.IsPhysicsProcessing(), "dormant callbacks");
        instance.Free();
    }

    // Fail the headless fixture with an actionable invariant.
    // =========================================================
    private static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException("PL1: " + label); }
}
