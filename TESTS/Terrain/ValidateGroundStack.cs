// GS1 authored-resource, fallback, precedence and rejection checks; no production mutation.
using Godot;
using System;
public partial class ValidateGroundStack : Node
{
    private int _checks;
    // Execute via the companion scene; failures return a nonzero exit status.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            foreach (string name in new[] { "OakForest", "PinkMeadow" })
            {
                var biome = GD.Load<WorldBiomeDefinition>($"res://WORLD/Biomes/Definitions/{name}/Biome.tres");
                string profile = name == "OakForest" ? "ForestGrass" : "PinkMeadowGrass";
                Check(biome.Surface == GD.Load<GroundSurfaceProfile>($"res://WORLD/Terrain/Ground/Surfaces/Profiles/{profile}.tres"),"Original profile identity");
                Check(biome.GroundStack.OverlayLayers.Count == (name=="OakForest" ? 2 : 0),"Oak preview layers / untouched Pink");
                foreach(var extra in biome.GroundStack.OverlayLayers) Check(!extra.Enabled && extra.AllowSessionPreview,"Oak extras must default off but be previewable");
            }
            var surface = GD.Load<GroundSurfaceProfile>("res://WORLD/Terrain/Ground/Surfaces/Profiles/ForestGrass.tres");
            using var third = new WorldBiomeDefinition { Id="arbitrary.third", Surface=surface };
            Check(third.Surface == surface,"Legacy fallback");
            using var layer = new BiomeGroundLayer { Id="base",Label="Third base",Surface=surface };
            Check(layer.Tint == Colors.White,"Neutral default tint");
            using var stack = new BiomeGroundStack { DefiningBaseGround=layer };
            using var patches=new GroundPatchLayer { Label="Third accompanying layer" };
            stack.AccompanyingPatches=patches;
            third.GroundStack=stack; third.Surface=null;
            Check(third.Surface == surface,"Stack takes precedence over legacy setter");
            var originalBase = surface.BaseColour;
            layer.Tint=new Color(1f,0.65f,0.8f,1f);
            Check(third.GroundTint==layer.Tint && surface.BaseColour==originalBase,"Biome tint leaves shared palette unchanged");
            layer.Tint=new Color(float.NaN,1,1,1); Reject(stack);
            layer.Tint=new Color(1,1,1,0.5f); Reject(stack);
            layer.Tint=new Color(1f,0.65f,0.8f,1f);
            using var overlay = new BiomeGroundLayer { Id="extra",Label="Optional",Surface=surface };
            stack.OverlayLayers.Add(overlay); Check(third.Surface == surface,"Disabled overlay preserves base");
            overlay.Enabled=true; Check(third.Surface==surface,"Enabled overlay validates without replacing base"); overlay.Enabled=false;
            overlay.Coverage=float.NaN; Reject(stack); overlay.Coverage=0.25f;
            overlay.PatchSize=0; Reject(stack); overlay.PatchSize=320;
            overlay.Id="base"; Reject(stack); overlay.Id="extra";
            stack.OverlayLayers.Add(null); Reject(stack); stack.OverlayLayers.RemoveAt(1);
            stack.OverlayLayers.Add(overlay); stack.OverlayLayers.Add(overlay); Reject(stack); stack.OverlayLayers.Clear();
            stack.DefiningBaseGround=null; Reject(stack); stack.DefiningBaseGround=layer;
            const string temporary="user://gs1-ground-stack-test.tres";
            Check(ResourceSaver.Save(third,temporary)==Error.Ok,"Save authored stack");
            var loaded=ResourceLoader.Load<WorldBiomeDefinition>(temporary,"",ResourceLoader.CacheMode.Ignore);
            Check(loaded.GroundTint==layer.Tint && loaded.GroundStack.DefiningBaseGround.Id=="base" && loaded.Surface.ResourcePath==surface.ResourcePath,"Serialized stack round trip");
            Check(loaded.GroundStack.AccompanyingPatches.Label==patches.Label
                && loaded.GroundStack.AccompanyingPatches.Strength==patches.Strength,"Accompanying layer serialized separately");
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(temporary));
            Check(CampaignSession.CanMigrateGround(),"Exact shipped conversion migration gate");
            var config=GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres");
            var map=MacroWorldBuilder.Build(config,213876);
            using var neutral=StreamedGroundPalette.Build(map,config.Biomes);
            Check(!neutral.GetShaderParameter("has_ground_tint").AsBool(),"Neutral path skips tint sampling");
            using var tintedBiome=new WorldBiomeDefinition { Id="biome.oak_forest",GroundStack=stack };
            var pink=GD.Load<WorldBiomeDefinition>("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres");
            using var tinted=StreamedGroundPalette.Build(map,new[] { tintedBiome,pink });
            Check(tinted.GetShaderParameter("has_ground_tint").AsBool(),"Tinted world enables map");
            var tintTexture=tinted.GetShaderParameter("map_tint").AsGodotObject() as Texture2D;
            Check(tintTexture != null && tintTexture.GetWidth()==map.Columns,"Shared coarse tint map");
            Check(CampaignSession.CanMigratePatches(),"Exact accompanying extraction migration gate");
            Check(CampaignSession.CanMigrateOakExtras(),"Exact optional Oak layer migration gate");
            CampaignSession.RequireContent(new CampaignSnapshot(2,"","","",CampaignSession.LegacyOakExtrasFingerprint(),"",null,null,null,null,null));
            Check(CampaignSession.CanMigrateRecipes(),"Exact procedural recipe migration gate");
            CampaignSession.RequireContent(new CampaignSnapshot(2,"","","",CampaignSession.LegacyRecipeFingerprint(),"",null,null,null,null,null));
            string patchPrevious=CampaignSession.LegacyPatchLayerFingerprint();
            var patchSnapshot=new CampaignSnapshot(2,"","","",patchPrevious,"",null,null,null,null,null);
            CampaignSession.RequireContent(patchSnapshot);
            Check(patchPrevious!=CampaignSession.ContentFingerprint(),"Previous stack layout deliberately migrated");
            string previous = CampaignSession.LegacyGS1ContentFingerprint();
            Check(previous != CampaignSession.ContentFingerprint(),"Migration actually changes fingerprint");
            var snapshot = new CampaignSnapshot(2,"","","",previous,"",null,null,null,null,null);
            CampaignSession.RequireContent(snapshot); Check(true,"Previous content accepted");
            bool rejected=false;
            try { CampaignSession.RequireContent(snapshot with { ContentFingerprint=new string('0',64) }); }
            catch(System.IO.InvalidDataException) { rejected=true; }
            Check(rejected,"Unrelated fingerprint rejected");
            GD.Print($"GS1 PASS: {_checks} checks"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Invalid authoring must fail instead of silently choosing another surface.
    // =========================================================
    private void Reject(BiomeGroundStack stack)
    {
        try { stack.ResolveBaseSurface(); } catch(ArgumentException) { _checks++; return; }
        throw new Exception("Invalid ground stack accepted");
    }
    // Keep failures meaningful in headless output.
    // =========================================================
    private void Check(bool value,string label) { if(!value) throw new Exception(label); _checks++; }
}
