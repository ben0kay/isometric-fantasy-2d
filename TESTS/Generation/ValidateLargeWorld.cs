// BC4 focused coarse-plan/frozen-query/shoreline checks; no detailed world chunks or profiling claims.
using Godot;
using System;
using System.Linq;
using System.Text.Json;
public partial class ValidateLargeWorld : Node
{
    // Cover current default area/caps, arbitrary query order, frozen restoration and indexed water parity.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/DefaultWorldGeneration.tres").Duplicate(true);
            double ratio=(double)config.Bounds.Size.X*config.Bounds.Size.Y/(142304d*106728);
            Check(Math.Abs(ratio-50)<0.001 && config.CellSize==2048 && config.ScalableMacro,"Wrong 50x area preset");
            var pond=WorldLiquidFootprint.WorldBounds(GD.Load<WorldLiquidMasterConfig>("res://WORLD/Liquids/Definitions/Water/PondConfig.tres"));
            var result=WorldGenerationPipeline.Build(config,213876,pond); var map=result.Map;
            Check(map.Columns==492 && map.Rows==369 && result.Metrics.Cells==181548,"Coarse budget changed");
            Check(map.GenerationVersion==5 && map.Regions.Count<=1024 && result.Metrics.PeakGrowthQueue<=map.Columns*map.Rows*4,"Macro region/frontier budget exceeded");
            var points=new[] { new Vector2(-490000,-350000),new Vector2(490000,350000),new Vector2(-450000,300000),new Vector2(450000,-300000),result.Features.StartingArea.Anchor };
            var expected=points.Select(p=>map.RegionAt(p)?.Id ?? -1).ToArray();
            for(int i=points.Length-1;i>=0;i--) Check((map.RegionAt(points[i])?.Id ?? -1)==expected[i],"Query order changed region ownership");
            var manifest=WorldBlueprintManifest.CreatePreview(map,result.Features) with {
                SchemaVersion=12,Climate=result.Climate.Snapshot(),AuthorPolicy=new WorldAuthorPolicySnapshot(1,"Middle",2000,256),
                GeographyVersion=2,Water=result.Geography.Water.Snapshot(),LocalGeneratorVersion=ChunkSceneryData.GeneratorVersion,ChunkSize=512,CandidateCellSize=96,
                MacroRegions=map.Regions.Select(r=>new SavedMacroRegion(r.Id,r.BiomeIndex,r.Centre.X,r.Centre.Y,r.TargetDiameter,r.CellCount)).ToArray(),
                MacroBiomes=map.Biomes.Select(b=>new SavedMacroBiome(b.Id,b.Name,b.Colour.R,b.Colour.G,b.Colour.B,b.Colour.A)).ToArray()
            };
            string json=JsonSerializer.Serialize(manifest); Check(System.Text.Encoding.UTF8.GetByteCount(json)<32*1024*1024,"Blueprint exceeded unchanged file-size limit");
            var saved=JsonSerializer.Deserialize<WorldBlueprintManifest>(json);
            SaveCompatibility.RequireSupported(CampaignSnapshot.CurrentSchema,saved);
            var restored=WorldBlueprintRestore.Map(saved);
            Check(restored.Hash==map.Hash && restored.RefinementStrength==map.RefinementStrength,"Frozen refinement identity changed");
            for(int i=0;i<points.Length;i++) Check((restored.RegionAt(points[i])?.Id ?? -1)==expected[i],"Continue changed far-away region");
            var ocean=result.Geography.Water.Bodies.Single(b=>b.IsOcean);
            foreach(var point in points.Concat(new[] { ocean.Bounds.GetCenter(),ocean.CopyPolygon()[0] }))
                Check(Mathf.Abs(ocean.SignedDistance(point)-BruteDistance(ocean.CopyPolygon(),point))<0.25f,"Indexed shoreline differs from brute-force geometry");
            var originalBounds=config.Bounds; config.Bounds=new Rect2(-2000000,-2000000,4000000,4000000);
            bool rejected=false; try { WorldMacroBudget.Validate(config); } catch(ArgumentException) { rejected=true; }
            Check(rejected,"Unbudgeted coordinate magnitude accepted"); config.Bounds=originalBounds;
            GD.Print($"BC4 LARGE WORLD PASS | cells {result.Metrics.Cells} | regions {map.Regions.Count} | planning {result.Metrics.TotalMilliseconds:F1}ms | blueprint {json.Length} chars");
            GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Independent exhaustive shoreline reference for accelerated nearest/ray queries.
    // =========================================================
    private static float BruteDistance(Vector2[] polygon,Vector2 point)
    {
        float minimum=float.MaxValue; bool inside=false;
        for(int i=0;i<polygon.Length;i++)
        {
            var a=polygon[i]; var b=polygon[(i+1)%polygon.Length];
            minimum=Mathf.Min(minimum,point.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(point,a,b)));
            if((a.Y>point.Y)!=(b.Y>point.Y) && point.X<(double)a.X+((double)b.X-a.X)*(point.Y-a.Y)/((double)b.Y-a.Y)) inside=!inside;
        }
        return Mathf.Sqrt(minimum)*(inside ? 1 : -1);
    }
    // Do not continue with partial acceptance after a failed invariant.
    // =========================================================
    private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
}
