// Focused BC1 plan/freeze regression; optional local run, no full World scene or performance claims.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
public partial class ValidateWorldAuthor : Node
{
    // Check determinism, saved field isolation, spawn clearance and the explicit null-preset path.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var config=(WorldGenerationMasterConfig)GD.Load<WorldGenerationMasterConfig>("res://WORLD/Generation/World/StandardWorldGeneration.tres").Duplicate(true);
            var pond=new Rect2(650,500,860,600);
            var world=WorldGenerationPipeline.Build(config,41,pond);
            var again=WorldGenerationPipeline.Build(config,41,pond);
            Require(world.Climate.Hash==again.Climate.Hash && world.Map.Hash==again.Map.Hash && world.Features.Hash==again.Features.Hash
                && world.Geography.Water.Hash==again.Geography.Water.Hash,"Repeated seed changed outcomes");
            var bounds=config.Bounds;
            var north=bounds.Position+bounds.Size*new Vector2(0.4f,0.02f);
            var south=bounds.Position+bounds.Size*new Vector2(0.4f,0.98f);
            Require(world.Climate.Sample(north).Zone==WorldClimateZone.Cold && world.Climate.Sample(south).Zone==WorldClimateZone.Hot,"Latitude provinces missing");
            var original=world.Climate.Sample(north);
            world.Climate.Sample(south);
            Require(world.Climate.Sample(north)==original,"Query order changed field");
            var snapshot=JsonSerializer.Deserialize<WorldClimateSnapshot>(JsonSerializer.Serialize(world.Climate.Snapshot()));
            config.Authoring.NorthTemperatureCelsius=-70;
            var restored=WorldClimatePlan.Restore(snapshot,41,bounds);
            Require(restored.Hash==world.Climate.Hash && restored.Sample(north)==original,"Frozen field used edited preset");
            bool rejected=false;
            try { WorldClimatePlan.Restore(snapshot with { Warp=0.01f },41,bounds); } catch(InvalidDataException) { rejected=true; }
            Require(rejected,"Tampered climate accepted");
            Require(WorldClimatePlan.FromPreset(config.Authoring,42,bounds).Hash!=world.Climate.Hash,"Seed did not vary climate");
            var start=world.Features.StartingArea;
            Require(start!=null && world.Map.BiomeAt(start.Anchor)!=null,"Missing safe land start");
            float radius=config.Layout.Features.Single(f=>f.IsStartingArea).Size.Length()*0.5f+start.Clearance;
            foreach(var body in world.Geography.Water.Bodies)
                Require(body.SignedDistance(start.Anchor)<-(radius+config.Authoring.ShoreClearance),"Start violates shoreline buffer");
            foreach(var feature in world.Features.Reservations.Features.Where(f=>!f.IsStartingArea && (f.Kind&WorldPlacementKind.PointOfInterest)!=0))
                Require(!feature.OverlapsDisk(start.Anchor,radius+config.Authoring.MajorPoiClearance),"Start violates POI footprint buffer");
            config.Authoring=null;
            var legacy=WorldGenerationPipeline.Build(config,41,pond);
            Require(legacy.Climate==null && legacy.Features.Hash==WorldFeaturePlanner.Build(config,41).Hash,"Null authoring changed legacy feature path");
            GD.Print("BC1 WORLD AUTHOR PASS"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
    // Fail with a useful local diagnostic rather than continuing a partial acceptance run.
    // =========================================================
    private static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
}
