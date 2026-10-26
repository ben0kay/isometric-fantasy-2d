// Reconcile an authored ocean with the existing protected-start contract; the campaign region selector is BC5.
using Godot;
using System;
using System.Linq;
public static class WorldAuthorStartPlanner
{
    // Bounded land search after water/major reservations, before local scenery; no distant chunk loads.
    // =========================================================
    public static WorldFeaturePlan Complete(WorldGenerationMasterConfig config,int seed,WorldFeaturePlan features,WaterBodyPlan water,MacroWorldMap map,Rect2? pond)
    {
        var p=config.Authoring;
        var request=config.Layout?.Features.SingleOrDefault(f=>f.Enabled && f.IsStartingArea);
        if(request==null) throw new ArgumentException("World Author requires one protected-start directive.");
        using var rng=new RandomNumberGenerator { Seed=unchecked((ulong)(uint)WorldClimatePlan.DeriveSeed(seed,"start.middle")) };
        var zone=new Rect2(config.Bounds.Position+p.StartZone.Position*config.Bounds.Size,p.StartZone.Size*config.Bounds.Size);
        var requestZone=new Rect2(config.Bounds.Position+request.NormalizedZone.Position*config.Bounds.Size,request.NormalizedZone.Size*config.Bounds.Size);
        // A conservative radius includes the entire authored polygon at its largest permitted scale.
        float radius=request.Size.Length()*request.MaximumScale*0.5f+request.Clearance;
        for(int attempt=0;attempt<p.StartAttempts;attempt++)
        {
            var anchor=zone.Position+new Vector2(rng.Randf(),rng.Randf())*zone.Size;
            if(map.BiomeAt(anchor)==null || !requestZone.Encloses(new Rect2(anchor-Vector2.One*radius,Vector2.One*radius*2))
                || !features.Reservations.AllowsDisk(anchor,radius,request.Kind)) continue;
            if(pond.HasValue && pond.Value.Grow(radius+p.ShoreClearance).HasPoint(anchor)) continue;
            bool blocked=false;
            foreach(var body in water.Bodies)
                if(body.Bounds.Grow(radius+p.ShoreClearance).HasPoint(anchor) && body.SignedDistance(anchor)>=-(radius+p.ShoreClearance)) { blocked=true; break; }
            if(blocked) continue;
            foreach(var feature in features.Reservations.Features)
                if(feature.OverlapsDisk(anchor,radius+((feature.Kind&WorldPlacementKind.PointOfInterest)!=0 ? p.MajorPoiClearance : 0))) { blocked=true; break; }
            if(blocked) continue;
            var polygon=request.NormalizedPolygon.Select(v=>anchor+v*request.Size*request.MaximumScale).ToArray();
            var start=new WorldFeatureReservation(request,anchor,polygon);
            if(WorldReservationMap.Conflict(start,features.Reservations.Features)!=null) continue;
            return new(seed,config.Bounds,features.Reservations.Features.Append(start),features.Warnings);
        }
        throw new ArgumentException($"World Author middle start: no safe land within requested zone and POI/shore clearances after {p.StartAttempts} attempts. No constraints were relaxed.");
    }
}
