// Native SE3 contract checks; uses production types explicitly and never edits authored resources.
using Godot;
using System;
using Tree = FantasyGame.World.Trees.Tree;

public partial class ValidateFoliageMasks : Node
{
    private int _checks;

    // Small isolated fixture; rendering/strong-wind acceptance remains a separate graphical check.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"SE3 PASS: {_checks} checks"); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // Validate optional/full-canvas inputs and confirm binding never changes source anchors or transforms.
    // =========================================================
    private void Run()
    {
        var oak = GD.Load<TreeDefinition>("res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres");
        var original = oak.VisualVariants[0];
        Texture2D mask = original.FoliageMask; Vector2 originalScale = original.SpriteScale;
        Check(FoliageMaskValidation.Resolve(original.Texture, mask, out string message) == mask && message == null, "Real Oak mask rejected");
        Check(FoliageMaskValidation.Resolve(original.Texture, null, out message) == null && message == null, "Absent mask must be silent fallback");
        Check(FoliageMaskValidation.Resolve(null, mask, out message) == null && message != null, "Missing artwork was not diagnosed");
        var wrong = ImageTexture.CreateFromImage(Image.CreateEmpty(1, 1, false, Image.Format.Rgba8));
        Check(FoliageMaskValidation.Resolve(original.Texture, wrong, out message) == null && message.Contains(original.Texture.GetWidth().ToString()), "Wrong-size diagnostic lacks source dimensions");
        var atlas = new AtlasTexture { Atlas = mask, Region = new Rect2(Vector2.Zero, mask.GetSize()) };
        Check(FoliageMaskValidation.Resolve(original.Texture, atlas, out message) == null && message != null, "Atlas-region mask must not silently bind full-texture UVs");

        var seasons = new SeasonController(); seasons.Initialize(new SeasonMasterConfig()); AddChild(seasons);
        var wind = new WindController { Config = new WindMasterConfig() }; AddChild(wind);
        var actors = new Node2D(); AddChild(actors);
        var scene = GD.Load<PackedScene>("res://WORLD/Content/Vegetation/Trees/Shared/Tree.tscn");
        int count = 0;
        foreach (float scale in new[] { 0.3f, 0.575f, 1.2f })
            foreach (bool mirrored in new[] { false, true })
            {
                var definition = (TreeDefinition)oak.Duplicate();
                var variant = (TreeVisualVariant)original.Duplicate();
                variant.SpriteScale = Vector2.One * scale; variant.SpriteOffset = new Vector2(7, -5);
                variant.Centered = mirrored; variant.GroundOffset = new Vector2(3, 2);
                definition.VisualVariants = new Godot.Collections.Array<TreeVisualVariant> { variant };
                var tree = scene.Instantiate<Tree>(); tree.Position = new Vector2(count++ * 200, 0);
                tree.Configure(definition, 0, mirrored); actors.AddChild(tree);
            }
        var windAdapter = new VegetationWind { Wind = wind }; AddChild(windAdapter); windAdapter.Attach(actors);
        var adapter = new SeasonalFoliageAdapter { Seasons = seasons, Wind = wind }; AddChild(adapter); adapter.Attach(actors);
        Check(adapter.MaskValidationChecks == count, "Expected one validation per unique loaded artwork variant");
        long checks = adapter.MaskValidationChecks;
        adapter.Attach(actors); seasons.SetEnabled(true); seasons.SetYearPosition(0.76f);
        Check(adapter.MaskValidationChecks == checks, "Repeated attach/scrubbing revalidates masks");
        foreach (Tree tree in actors.GetChildren())
        {
            Sprite2D sprite = tree.GetNode<Sprite2D>("Sprite"); var variant = tree.SelectedVariant;
            Vector2 anchor = variant.SourceAnchor;
            if (tree.Mirrored) anchor.X = sprite.Texture.GetWidth() - anchor.X;
            Vector2 origin = sprite.Centered ? sprite.Texture.GetSize() * -0.5f : Vector2.Zero;
            Check(sprite.ToGlobal(anchor + origin + sprite.Offset).DistanceTo(tree.ToGlobal(variant.GroundOffset)) < 0.001f, "Mask binding moved the ground anchor");
            var material = (ShaderMaterial)sprite.Material;
            Check(material.GetShaderParameter("foliage_mask").AsGodotObject() == mask && material.GetShaderParameter("foliage_has_mask").AsBool(), "Transform changed the mask reference");
            Check(sprite.Scale == variant.SpriteScale && sprite.Offset == variant.SpriteOffset && sprite.FlipH == tree.Mirrored, "Mask binding altered sprite transforms");
        }
        adapter.Detach(actors);
        Check(adapter.MaskVariantCount == 0 && adapter.MaterialCount == 0 && adapter.PaletteCount == 0, "SE3 validation cache survived detach");
        windAdapter.Free(); actors.Free();
        Check(original.FoliageMask == mask && original.SpriteScale == originalScale, "Fixture changed authored Oak");
    }

    // Keep failures precise for a local Mono run.
    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new Exception(message); }
}
