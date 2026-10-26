// Canonical new-world planning; only cheap coarse semantics are global, gameplay detail remains streamed.
using Godot;
using System;
using System.Diagnostics;
public sealed record WorldGenerationResult(WorldFeaturePlan Features,WorldGeographyPlan Geography,MacroWorldMap Map,WorldClimatePlan Climate = null,WorldPlanningMetrics Metrics = null);
public static class WorldGenerationPipeline
{
    // Measured stage times are creation-only diagnostics; no preview raster, local scenery or disk work is done here.
    // =========================================================
    public static WorldGenerationResult Build(WorldGenerationMasterConfig config,int seed,Rect2? authoredPond=null)
    {
        if(config==null) throw new ArgumentException("New-world generation requires a config.");
        WorldMacroBudget.Validate(config); config.Authoring?.Validate();
        var timer=Stopwatch.StartNew();
        var features=WorldFeaturePlanner.Build(config,seed,config.Authoring!=null);
        double featureMs=timer.Elapsed.TotalMilliseconds; timer.Restart();
        var water=WaterBodyPlanner.BuildGeography(config,seed,features,authoredPond);
        var geography=new WorldGeographyPlan(seed,config.Bounds,water);
        double waterMs=timer.Elapsed.TotalMilliseconds; timer.Restart();
        var climate=config.Authoring==null ? null : WorldClimatePlan.FromPreset(config.Authoring,seed,config.Bounds);
        double climateMs=timer.Elapsed.TotalMilliseconds; timer.Restart();
        var map=MacroWorldBuilder.Build(config,seed,geography,climate);
        double biomeMs=timer.Elapsed.TotalMilliseconds; timer.Restart();
        if(config.Authoring!=null) features=WorldAuthorStartPlanner.Complete(config,seed,features,water,map,authoredPond);
        double startMs=timer.Elapsed.TotalMilliseconds;
        long cells=(long)map.Columns*map.Rows;
        var metrics=new WorldPlanningMetrics(featureMs,waterMs,climateMs,biomeMs,startMs,(int)cells,map.Regions.Count,
            cells*4,cells*7*16,map.BuildCounters?.PeakQueue ?? 0,map.BuildCounters?.Enqueues ?? 0);
        return new(features,geography,map,climate,metrics);
    }
}
