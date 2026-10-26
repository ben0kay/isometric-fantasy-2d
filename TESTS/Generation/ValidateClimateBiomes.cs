// Focused BC2 eligibility/determinism/frozen-schema checks, deliberately independent of full gameplay scenes.
using Godot;
using System;
using System.Linq;
using System.Text.Json;
public partial class ValidateClimateBiomes : Node
{
    // The normal four definitions must obey hard limits, keep complete land coverage and restore frozen outcomes.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres").Duplicate(true);
            var pond=WorldLiquidFootprint.WorldBounds(GD.Load<WorldLiquidMasterConfig>("res://WORLD/Liquids/Definitions/Water/PondConfig.tres"));
            var result=WorldGenerationPipeline.Build(config,213876,pond); var map=result.Map;
            Check(map.GenerationVersion==MacroWorldMap.ClimateGeneratorVersion,"Climate generator identity missing");
            int tundra=0,desert=0;
            for(int y=0;y<map.Rows;y++) for(int x=0;x<map.Columns;x++)
            {
                int region=map.RegionIdAtCell(x,y); if(region<0) continue;
                var id=map.Biomes[map.Regions[region].BiomeIndex].Id;
                var definition=config.Biomes.Single(b=>b.Id==id);
                float left=x*map.CellSize,top=y*map.CellSize;
                var point=map.Bounds.Position+new Vector2(left+Mathf.Min(map.CellSize,map.Bounds.Size.X-left)*0.5f,top+Mathf.Min(map.CellSize,map.Bounds.Size.Y-top)*0.5f);
                var climate=result.Climate.Sample(point);
                Check((definition.PlacementClimate?.Preference(climate) ?? 1)>0,"Hard affinity violated");
                if(id=="biome.snowy_tundra") tundra++;
                if(id=="biome.desert") desert++;
            }
            Check(tundra>0 && desert>0,"Cold/hot content missing from representative full world");
            Check(map.Regions.Sum(r=>r.CellCount)+map.NonLandCells==map.Columns*map.Rows,"Land coverage lost");
            config.Biomes.Reverse();
            var repeated=WorldGenerationPipeline.Build(config,213876,pond);
            Check(repeated.Map.Hash==map.Hash && repeated.Climate.Hash==result.Climate.Hash,"Resource order changed geography");
            var blueprint=WorldBlueprintManifest.CreatePreview(map,result.Features) with {
                SchemaVersion=11,GeographyVersion=WorldGeographyPlan.Version,Water=result.Geography.Water.Snapshot(),Climate=result.Climate.Snapshot(),
                AuthorPolicy=new WorldAuthorPolicySnapshot(1,"Middle",config.Authoring.MajorPoiClearance,config.Authoring.ShoreClearance),
                LocalGeneratorVersion=ChunkSceneryData.GeneratorVersion,ChunkSize=512,CandidateCellSize=96,
                MacroRegions=map.Regions.Select(r=>new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray(),
                MacroBiomes=map.Biomes.Select(b=>new SavedMacroBiome(b.Id,b.Name,b.Colour.R,b.Colour.G,b.Colour.B,b.Colour.A)).ToArray()
            };
            var restored=JsonSerializer.Deserialize<WorldBlueprintManifest>(JsonSerializer.Serialize(blueprint));
            SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,restored);
            Check(WorldBlueprintRestore.Map(restored).Hash==map.Hash && WorldBlueprintRestore.Climate(restored).Hash==result.Climate.Hash,"Frozen schema-11 identity changed");
            config.Biomes=new Godot.Collections.Array<WorldBiomeDefinition> { config.Biomes.Single(b=>b.Id=="biome.oak_forest") };
            var one=WorldGenerationPipeline.Build(config,213876,pond);
            Check(one.Map.Biomes.Count==1 && one.Map.Regions.Sum(r=>r.CellCount)+one.Map.NonLandCells==one.Map.Columns*one.Map.Rows,"Single soft-affinity biome failed coverage");
            config.Biomes[0].PlacementClimate.RequireTemperatureRange=true;
            bool rejected=false;
            try { WorldGenerationPipeline.Build(config,213876,pond); } catch(ArgumentException) { rejected=true; }
            Check(rejected,"Impossible hard climate requirements silently relaxed");
            GD.Print("BC2 CLIMATE BIOMES PASS"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Report focused failures without continuing a partial validation pass.
    // =========================================================
    private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
}
