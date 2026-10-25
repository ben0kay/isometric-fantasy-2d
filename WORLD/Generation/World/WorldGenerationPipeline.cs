// New-world planning order: required reservations, biome-independent geography, then compatible biome regions.
using Godot;
using System;
public sealed record WorldGenerationResult(WorldFeaturePlan Features,WorldGeographyPlan Geography,MacroWorldMap Map);
public static class WorldGenerationPipeline
{
    // No local scenery/Nodes or saves here. Continue restores frozen outcomes through the existing persistence owner.
    // =========================================================
    public static WorldGenerationResult Build(WorldGenerationMasterConfig config,int seed,Rect2? authoredPond=null)
    {
        if (config==null) throw new ArgumentException("New-world generation requires a config.");
        var features=WorldFeaturePlanner.Build(config,seed);
        var water=WaterBodyPlanner.BuildGeography(config,seed,features,authoredPond);
        var geography=new WorldGeographyPlan(seed,config.Bounds,water);
        var map=MacroWorldBuilder.Build(config,seed,geography);
        return new(features,geography,map);
    }
}
