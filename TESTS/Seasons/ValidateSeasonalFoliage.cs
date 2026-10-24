// Local Godot Mono regression for shared palettes, mask fallback, material copies and cleanup.
using Godot;
using System;
using System.Linq;
using FantasyGame.WorldInteractions.Fire;
using Tree = FantasyGame.World.Trees.Tree;
using ProductionFireErosionAdapter = FantasyGame.WorldInteractions.Fire.FireErosionAdapter;

public partial class ValidateSeasonalFoliage : Node
{
    private int _checks;

    // Run a small isolated production-tree fixture without campaign or generator side effects.
    // =========================================================
    public override void _Ready()
    {
        try { Run(); GD.Print($"SE2 PASS: {_checks} checks"); GetTree().Quit(); }
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
        long idleUploads = adapter.PaletteUploads;
        seasons.SetYearPosition(seasons.YearPosition); seasons.SetColourStrength(seasons.ColourStrength);
        Check(adapter.PaletteUploads == idleUploads, "Unchanged commands uploaded palette data");
        seasons.SetYearPosition(0.2f); seasons.SetYearPosition(0.5f);
        Check(adapter.PaletteUploads == idleUploads, "Disabled scrubbing uploaded neutral atlas repeatedly");
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
        // Minimal weather signal owner, deliberately not started as a precipitation scene.
        var weather = new WorldWeather { Config = new WeatherProfile(), Wind = wind };
        var wetness = new VegetationWetness { Weather = weather }; AddChild(wetness); wetness.Attach(actors);
        wetness.StartAt(0.8f);
        Check(((ShaderMaterial)sprite.Material).GetShaderParameter("seasonal_state").AsGodotObject() == palette, "Wetness lost season atlas");
        var snow = new VegetationSnowAdapter();
        snow.Attach(actors, GD.Load<Texture2D>("res://ENVIRONMENT/Weather/Snow/SnowCoverageNoise.tres"), weather: weather);
        snow.Update(0.7f, Colors.White);
        var snowy = (ShaderMaterial)sprite.Material;
        Check(snowy.GetShaderParameter("seasonal_state").AsGodotObject() == palette, "Snow lost shared season texture");
        wetness.StartAt(0.45f);
        Check(Mathf.Abs(snowy.GetShaderParameter("wet_coverage").AsSingle() - 0.45f) < 0.001f, "Snow copy lost changing wetness");
        int fireCache = ProductionFireErosionAdapter.CachedShaderCount;
        var erosionA = new ProductionFireErosionAdapter(); var erosionB = new ProductionFireErosionAdapter();
        var noise = GD.Load<Texture2D>("res://WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres");
        Check(erosionA.Attach(sprite, noise) && erosionB.Attach(second.GetNode<Sprite2D>("Sprite"), noise), "Fire erosion did not compose");
        Check(ProductionFireErosionAdapter.CachedShaderCount == fireCache + 1, "Active fires did not share composed shader");
        seasons.SetYearPosition(0.76f);
        Check(((ShaderMaterial)sprite.Material).GetShaderParameter("seasonal_state").AsGodotObject() == palette, "Fire lost live season atlas");
        snow.Update(0f, Colors.White);
        Check(erosionA.Update(0.4f, 1f, Colors.Black) && erosionB.Update(0.4f, 1f, Colors.Black), "Snow-off during burning lost composition");
        Check(ProductionFireErosionAdapter.CachedShaderCount == fireCache + 1, "Snow swap retained old fire shader cache entries");
        erosionA.Restore(); erosionB.Restore();
        Check(ProductionFireErosionAdapter.CachedShaderCount == fireCache, "Fire completion retained composed shaders");
        snow.Update(0.7f, Colors.White);
        seasons.SetEnabled(false);
        Check(palette.GetImage().GetPixel(0, 0).A == 0, "Disable did not propagate through material copies");
        var receiver = first.GetNode<Burnable>("Burnable"); receiver.RestoreProgress(1f);
        Check(!sprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "Burnt tree retains healthy recolour mask");
        receiver.ResetBurn();
        Check(sprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "Reset did not restore healthy selection");
        snow.Restore(); wetness.Free(); weather.Free(); adapter.Detach(actors);
        Check(adapter.ChunkCount == 0 && adapter.PaletteCount == 0 && adapter.MaterialCount == 0 && adapter.MaskVariantCount == 0, "Unload retained chunk/palette references");
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

        // Future species can burn without charred art: completion still notifies/sleeps and reset recovers.
        var noArt = (TreeDefinition)oak.Duplicate();
        noArt.FireProfile = (TreeFireProfile)oak.FireProfile.Duplicate(); noArt.FireProfile.BurntTexture = null;
        actors = new Node2D(); AddChild(actors); Tree noArtTree = Spawn(actors, noArt);
        Sprite2D noArtSprite = noArtTree.GetNode<Sprite2D>("Sprite"); Texture2D healthy = noArtSprite.Texture;
        var appearance = noArtTree.GetNode<TreeFireAppearance>("FireAppearance"); int notifications = 0;
        appearance.AppearanceChanged += () => notifications++;
        noArtTree.GetNode<Burnable>("Burnable").RestoreProgress(1f);
        Check(noArtSprite.Texture == healthy && !noArtSprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "Missing charred art changed texture or kept seasonal foliage active");
        Check(!appearance.IsProcessing() && notifications == 1, "No-art completion skipped cleanup/notification");
        noArtTree.GetNode<Burnable>("Burnable").ResetBurn();
        Check(noArtSprite.Texture == healthy && noArtSprite.GetInstanceShaderParameter("foliage_healthy").AsBool(), "No-art reset lost original artwork");
        actors.Free();

        // Rebinding the same stable identity preserves timing and starts directly in current Autumn.
        seasons.SetEnabled(true); seasons.SetYearPosition(0.76f);
        for (int cycle = 0; cycle < 8; cycle++)
        {
            actors = new Node2D(); AddChild(actors); Tree rebound = Spawn(actors, oak);
            rebound.SetMeta("world_instance_id", "fixture/oak/a");
            windAdapter = new VegetationWind { Wind = wind }; AddChild(windAdapter); windAdapter.Attach(actors);
            adapter.Attach(actors);
            var reboundSprite = rebound.GetNode<Sprite2D>("Sprite");
            var reboundPalette = (Texture2D)((ShaderMaterial)reboundSprite.Material).GetShaderParameter("seasonal_state").AsGodotObject();
            int bucket = SeasonalFoliageAdapter.TimingBucket("fixture/oak/a");
            Check(reboundSprite.GetInstanceShaderParameter("seasonal_bucket").AsSingle() == bucket && reboundPalette.GetImage().GetPixel(bucket, 0).A > 0.9f, "Rebound tree changed timing or flashed neutral");
            adapter.Detach(actors); actors.Free(); windAdapter.Free();
            Check(adapter.PaletteCount == 0 && adapter.MaterialCount == 0 && adapter.MaskVariantCount == 0, "Repeated chunk release grew seasonal caches");
        }

        // Fingerprint-only contract: this detached dummy never enters IO or a real campaign session.
        Check(SeasonalSaveCompatibility.CanMigrate(), "Exact shipped seasonal bridge is not enabled");
        string[] fingerprints = CampaignSession.SeasonalContentFingerprints().Distinct().ToArray();
        Check(fingerprints.Length >= 3, "Missing historical seasonal fingerprints");
        foreach (string fingerprint in new[] { fingerprints[0], fingerprints[fingerprints.Length / 2], fingerprints[^1] })
            CampaignSession.RequireContent(new CampaignSnapshot(2, "fixture", "run", "world", fingerprint, "", null, null, null, null, Array.Empty<SavedChunk>()));
        bool rejected = false;
        try { CampaignSession.RequireContent(new CampaignSnapshot(2, "fixture", "run", "world", new string('0', 64), "", null, null, null, null, Array.Empty<SavedChunk>())); }
        catch (System.IO.InvalidDataException) { rejected = true; }
        Check(rejected, "Season bridge accepted an arbitrary incompatible fingerprint");

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
