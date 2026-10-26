// New-world planning order: required reservations, biome-independent geography, then compatible biome regions.
using Godot;
using System;
public sealed record WorldGenerationResult(WorldFeaturePlan Features,WorldGeographyPlan Geography,MacroWorldMap Map,WorldClimatePlan Climate = null);
public static class WorldGenerationPipeline
{
    // No local scenery/Nodes or saves here. Continue restores frozen outcomes through the existing persistence owner.
    // =========================================================
    public static WorldGenerationResult Build(WorldGenerationMasterConfig config,int seed,Rect2? authoredPond=null)
    {
        if (config==null) throw new ArgumentException("New-world generation requires a config.");
        config.Authoring?.Validate();
        var features=WorldFeaturePlanner.Build(config,seed,config.Authoring!=null);
        var water=WaterBodyPlanner.BuildGeography(config,seed,features,authoredPond);
        var geography=new WorldGeographyPlan(seed,config.Bounds,water);
        var climate=config.Authoring==null ? null : WorldClimatePlan.FromPreset(config.Authoring,seed,config.Bounds);
        var map=MacroWorldBuilder.Build(config,seed,geography,climate);
        if(config.Authoring!=null) features=WorldAuthorStartPlanner.Complete(config,seed,features,water,map,authoredPond);
        return new(features,geography,map,climate);
    }
}
