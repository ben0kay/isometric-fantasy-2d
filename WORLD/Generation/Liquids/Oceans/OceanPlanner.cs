// Bounded seeded major ocean footprints, before lakes/rivers and biomes. Reuses canonical liquid geometry/runtime.
using Godot;
using System;
using System.Collections.Generic;
public static class OceanPlanner
{
    // Ocean requests may be required; impossible size/reservation constraints fail explicitly rather than becoming land.
    // =========================================================
    internal static void Append(WaterGenerationMasterConfig config,int seed,Rect2 bounds,WorldFeaturePlan features,
        List<PlannedWaterBody> bodies,List<string> warnings,Rect2? pond)
    {
        if (config.OceanCount<0 || config.OceanCount>2 || config.OceanCount+config.LakeCount+config.RiverCount>32)
            throw new ArgumentException("Ocean requests exceed bounded water slots.");
        if (config.OceanCount==0) return;
        if (!float.IsFinite(config.MinimumOceanDiameter) || config.MinimumOceanDiameter<1000 || !float.IsFinite(config.MaximumOceanDiameter)
            || config.MaximumOceanDiameter<config.MinimumOceanDiameter || config.MaximumOceanDiameter>200000
            || config.AttemptsPerOcean<1 || config.AttemptsPerOcean>128) throw new ArgumentException("Invalid bounded ocean settings.");
        for (int slot=0;slot<config.OceanCount;slot++)
        {
            using var rng=new RandomNumberGenerator { Seed=unchecked((ulong)(uint)seed*0x9E3779B97F4A7C15UL^(uint)slot^0x4F4345414EUL) };
            bool placed=false;
            for (int attempt=0;attempt<config.AttemptsPerOcean;attempt++)
            {
                float diameter=rng.RandfRange(config.MinimumOceanDiameter,config.MaximumOceanDiameter),radius=diameter*0.5f;
                float outer=radius*1.25f*1.22f;
                float clearance=outer+config.Separation;
                if (bounds.Size.X<=clearance*2 || bounds.Size.Y<=clearance*2) continue;
                Rect2 zone=bounds.Grow(-clearance);
                Vector2 centre=zone.Position+new Vector2(rng.Randf(),rng.Randf())*zone.Size;
                if (!features.Reservations.AllowsDisk(centre,outer+config.Separation,WorldPlacementKind.Water)
                    || (pond.HasValue && pond.Value.Grow(outer+config.Separation).HasPoint(centre))) continue;
                float phase=rng.Randf()*Mathf.Tau;
                int count=Math.Clamp(Mathf.CeilToInt(Mathf.Pi*diameter/128f),128,1024);
                var polygon=new Vector2[count];
                for (int i=0;i<count;i++)
                {
                    float a=i*Mathf.Tau/count;
                    float scale=1+0.12f*Mathf.Sin(a*3+phase)+0.06f*Mathf.Sin(a*5-phase)+0.025f*Mathf.Sin(a*11+phase);
                    polygon[i]=centre+Vector2.FromAngle(a)*new Vector2(radius*1.25f,radius*0.8f)*scale;
                }
                var body=new PlannedWaterBody($"ocean.{slot:D3}",config.LiquidId,polygon,isOcean:true);
                bool blocked=false;
                foreach (var other in bodies)
                    if (FeatureGeometry.Overlaps(polygon,body.Bounds,config.Separation,other.CopyPolygon(),other.Bounds,0)) { blocked=true; break; }
                if (blocked) continue;
                bodies.Add(body); placed=true; break;
            }
            if (!placed)
            {
                string error=$"Ocean ocean.{slot:D3}: footprint/clearance cannot be placed after {config.AttemptsPerOcean} bounded attempts; check world size and reservations.";
                if (config.OceansRequired) throw new ArgumentException(error);
                warnings.Add(error+" Optional request skipped.");
            }
        }
    }
}
