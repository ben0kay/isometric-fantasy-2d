// Local Godot Mono regression for shared palettes, mask fallback, material copies and cleanup.
using Godot;
using System;
using FantasyGame.WorldInteractions.Fire;
using Tree = FantasyGame.World.Trees.Tree;

public partial class ValidateSeasonalFoliage : Node
{
    private int _checks;

    // Run a small isolated production-tree fixture without campaign or generator side effects.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"SE1 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // Exercise actual Resource references and cloned material texture identity, not just RGB formulas.
    // =========================================================
    private void Run()
    {
        var seasons = new SeasonController(); seasons.Initialize(new SeasonMasterConfig()); AddChild(seasons);
        var wind = new WindController { Config = new WindMasterConfig() }; AddChild(wind);
        var actors = new Node2D(); AddChild(actors);
        var oak = GD.Load<TreeDefinition>("res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres");
        Tree first = Spawn(actors, oak), second = Spawn(actors, oak);
        first.SetMeta("world_instance_id", "fixture/oak/a"); second.SetMeta("world_instance_id", "fixture/oak/b");
        var windAdapter = new VegetationWind { Wind = wind }; AddChild(windAdapter); windAdapter.Attach(actors);
        var adapter = new SeasonalFoliageAdapter { Seasons = seasons, Wind = wind }; AddChild(adapter); adapter.Attach(actors);
        var sprite = first.GetNode<Sprite2D>("Sprite"); var material = (ShaderMaterial)sprite.Material;
        Check(material == second.GetNode<Sprite2D>("Sprite").Material, "Identical trees should share a material");
        Check(material.GetShaderParameter("foliage_has_mask").AsBool(), "Oak mask was not bound");
        Check(!adapter.IsProcessing() && !seasons.IsProcessing(), "Idle seasonal callbacks are active");
        var palette = (Texture2D)material.GetShaderParameter("seasonal_state").AsGodotObject();
        Check(palette.GetImage().GetPixel(0, 0).A == 0, "Disabled preview recolours leaves");
        seasons.SetEnabled(true);
        Check(palette.GetImage().GetPixel(0, 0).A == 0, "Summer must retain original artwork");
        seasons.SetTimingVariationDays(0f); seasons.SetYearPosition(0.76f);
        Check(palette.GetImage().GetPixel(0, 0).A > 0.99f, "Autumn palette did not update");
        Check(sprite.Material == material, "Slider rebuilt material identity");
        Check(palette.GetWidth() == 16 && palette.GetHeight() == 2, "Expected bounded 16x2 palette atlas");
        Check(sprite.GetInstanceShaderParameter("seasonal_bucket").AsSingle() == SeasonalFoliageAdapter.TimingBucket("fixture/oak/a"), "Tree timing group differs from stable ID");
        Check(SeasonalFoliageAdapter.TimingBucket("fixture/oak/a") != SeasonalFoliageAdapter.TimingBucket("fixture/oak/b"), "Fixture IDs should exercise different groups");
        seasons.SetColourStrength(0.5f);
        Check(Mathf.Abs(palette.GetImage().GetPixel(0, 0).A - 0.5f) < 0.001f, "Strength did not scale influence");
        seasons.SetColourStrength(0f);
        Check(palette.GetImage().GetPixel(0, 0).A == 0f, "Zero strength is not neutral");
        seasons.SetColourStrength(1f); seasons.SetTimingVariationDays(14f);
        var atlas = palette.GetImage();
        Check(atlas.GetPixel(0, 0) != atlas.GetPixel(15, 0), "Early and late trees are identical in Autumn");
        seasons.SetYearPosition(0f);
        Check(seasons.DateLabel == "January 1 — Winter", "Zero is not January/Winter");
        Color january = palette.GetImage().GetPixel(0, 0);
        Check(palette.GetImage().GetPixel(0, 1).G > 0f, "Winter has no brightness lift");
        seasons.SetYearPosition(1f);
        Check(seasons.DateLabel == "January 1 — Winter" && palette.GetImage().GetPixel(0, 0).IsEqualApprox(january), "Year boundary does not wrap");
        var snow = new VegetationSnowAdapter();
        snow.Attach(actors, GD.Load<Texture2D>("res://ENVIRONMENT/Weather/Snow/SnowCoverageNoise.tres"));
        snow.Update(0.7f, Colors.White);
        var snowy = (ShaderMaterial)sprite.Material;
        Check(snowy.GetShaderParameter("seasonal_state").AsGodotObject() == palette, "Snow lost shared season texture");
        seasons.SetEnabled(false);
        Check(palette.GetImage().GetPixel(0, 0).A == 0, "Disable did not propagate through material copies");
        var receiver = first.GetNode<Burnable>("Burnable"); receiver.RestoreProgress(1f);
        Check(!sprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "Burnt tree retains healthy recolour mask");
        receiver.ResetBurn();
        Check(sprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "Reset did not restore healthy selection");
        snow.Restore(); adapter.Detach(actors);
        Check(adapter.ChunkCount == 0 && adapter.PaletteCount == 0, "Unload retained chunk/palette references");
        actors.Free(); windAdapter.Free();

        // Missing and invalid masks both retain automatic detection, without mutating authored Oak.
        foreach (bool invalid in new[] { false, true })
        {
            var definition = (TreeDefinition)oak.Duplicate();
            var variant = (TreeVisualVariant)oak.VisualVariants[0].Duplicate();
            variant.FoliageMask = invalid ? ImageTexture.CreateFromImage(Image.CreateEmpty(1, 1, false, Image.Format.Rgba8)) : null;
            definition.VisualVariants = new Godot.Collections.Array<TreeVisualVariant> { variant };
            actors = new Node2D(); AddChild(actors); Tree fallback = Spawn(actors, definition);
            windAdapter = new VegetationWind { Wind = wind }; AddChild(windAdapter); windAdapter.Attach(actors);
            adapter.Attach(actors);
            Check(!((ShaderMaterial)fallback.GetNode<Sprite2D>("Sprite").Material).GetShaderParameter("foliage_has_mask").AsBool(), "Missing/invalid mask failed fallback");
            adapter.Detach(actors); actors.Free(); windAdapter.Free();
        }
        Check(oak.VisualVariants[0].FoliageMask != null, "Fixture mutated authored mask");
    }

    // Production template owns artwork/anchor/collider configuration.
    // =========================================================
    private static Tree Spawn(Node2D actors, TreeDefinition definition)
    {
        var tree = GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn").Instantiate<Tree>();
        tree.Configure(definition, 0, false); actors.AddChild(tree); return tree;
    }

    // Fail immediately with a useful local-console message.
    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new Exception(message); }
}
