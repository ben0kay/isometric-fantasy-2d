// Shared bounded freshwater geometry: new worlds plan before biomes; the explicit legacy path retains old eligibility.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class WaterBodyPlanner
{
    // Legacy biome-first entry point retained for frozen baseline tests; production creation uses BuildGeography.
    // =========================================================
    public static WaterBodyPlan Build(WorldGenerationMasterConfig config, MacroWorldMap map, WorldFeaturePlan features, Rect2? authoredPond = null)
    {
        if (config == null || map == null || features == null || map.Seed != features.Seed || map.Bounds != features.Reservations.Bounds)
            throw new ArgumentException("Water requires matching macro/feature ownership.");
        return BuildCore(config,map.Seed,map.Bounds,features,authoredPond,map);
    }
    // P5 major geography needs only seed, bounds and required reservations; no biome map or biome Resources are read.
    // =========================================================
    public static WaterBodyPlan BuildGeography(WorldGenerationMasterConfig config,int seed,WorldFeaturePlan features,Rect2? authoredPond=null)
    {
        if (config==null || features==null || features.Seed!=seed || features.Reservations.Bounds!=config.Bounds)
            throw new ArgumentException("Water geography requires matching world/feature ownership.");
        return BuildCore(config,seed,config.Bounds,features,authoredPond,null);
    }
    // Share shape/placement code with the legacy biome-first validation path; only eligibility inputs differ.
    // =========================================================
    private static WaterBodyPlan BuildCore(WorldGenerationMasterConfig config,int seed,Rect2 bounds,WorldFeaturePlan features,Rect2? authoredPond,MacroWorldMap map)
    {
        var settings=config.Water; var bodies=new List<PlannedWaterBody>(); var warnings=new List<string>();
        if (settings==null || !settings.Enabled) return new(seed,bounds,bodies,warnings);
        Validate(settings);
        var profiles=new Dictionary<string,BiomeHydrologyProfile>(StringComparer.Ordinal);

        if (map!=null) foreach (var biome in config.Biomes)
        {
            if (biome == null || string.IsNullOrWhiteSpace(biome.Id) || !profiles.TryAdd(biome.Id,biome.Hydrology))
                throw new ArgumentException("Water requires unique biome IDs.");
            var h = biome.Hydrology;
            if (h != null && (!float.IsFinite(h.LakeSuitability) || h.LakeSuitability < 0 || h.LakeSuitability > 1
                || !float.IsFinite(h.RiverSuitability) || h.RiverSuitability < 0 || h.RiverSuitability > 1))
                throw new ArgumentException("Hydrology suitability must be finite within 0–1.");
        }
        if (map!=null) foreach (var biome in map.Biomes) if (!profiles.ContainsKey(biome.Id)) throw new ArgumentException("Missing macro biome hydrology.");
        for (int slot = 0; slot < settings.LakeCount; slot++)
        {
            using var rng = new RandomNumberGenerator { Seed = SlotSeed(seed,slot) };
            bool placed = false;
            for (int attempt = 0; attempt < settings.AttemptsPerLake; attempt++)
            {
                float radius = rng.RandfRange(settings.MinimumDiameter,settings.MaximumDiameter)*0.5f;
                float outer = radius*(1f+settings.Irregularity);
                var centre = bounds.Position + new Vector2(rng.Randf(),rng.Randf())*bounds.Size;
                // Conservative envelope protects required features, shore clearance and the fixed pond, including its bank.
                if (!features.Reservations.AllowsDisk(centre,outer+settings.Separation,WorldPlacementKind.Water)) continue;
                var envelope = new Rect2(centre-Vector2.One*outer,Vector2.One*outer*2);
                if (authoredPond.HasValue && authoredPond.Value.Grow(settings.Separation).Intersects(envelope,true)) continue;
                float phase = rng.Randf()*Mathf.Tau; var polygon = new Vector2[32];
                for (int i = 0; i < polygon.Length; i++)
                {
                    float a = Mathf.Tau*i/polygon.Length;
                    float scale = 1f+settings.Irregularity*(0.65f*Mathf.Sin(3*a+phase)+0.35f*Mathf.Cos(5*a-phase));
                    polygon[i] = centre+Vector2.FromAngle(a)*radius*scale;
                }
                var body = new PlannedWaterBody($"lake.{slot:D3}",settings.LiquidId,polygon);
                if (bodies.Any(b => FeatureGeometry.Overlaps(polygon,body.Bounds,settings.Separation,b.CopyPolygon(),b.Bounds,0))) continue;
                float suitability = map==null ? 1f : Suitability(polygon,body.Bounds,map,profiles);
                if (suitability <= 0 || rng.Randf() >= suitability) continue;
                bodies.Add(body); placed = true; break;
            }
            if (!placed) warnings.Add($"Water lake.{slot:D3}: no eligible placement after {settings.AttemptsPerLake} attempts; skipped.");
        }
        RiverPlanner.Append(settings,seed,bounds,map,features,profiles,bodies,warnings,authoredPond);
        return new(seed,bounds,bodies,warnings);
    }
    // Every coarse biome cell touched by the polygon must allow water, including interior islands.
    // =========================================================
    internal static float Suitability(Vector2[] polygon, Rect2 bounds, MacroWorldMap map, Dictionary<string,BiomeHydrologyProfile> profiles,bool river = false)
    {
        int left = Math.Max(0,Mathf.FloorToInt((bounds.Position.X-map.Bounds.Position.X)/map.CellSize));
        int top = Math.Max(0,Mathf.FloorToInt((bounds.Position.Y-map.Bounds.Position.Y)/map.CellSize));
        int right = Math.Min(map.Columns-1,Mathf.FloorToInt((bounds.End.X-map.Bounds.Position.X)/map.CellSize));
        int bottom = Math.Min(map.Rows-1,Mathf.FloorToInt((bounds.End.Y-map.Bounds.Position.Y)/map.CellSize));
        // Bound even pathological diameter/cell-size authoring; no unbounded whole-map footprint scan.
        if ((long)(right-left+1)*(bottom-top+1) > 4096) throw new ArgumentException("Water footprint exceeds 4096 macro cells; reduce diameter or increase macro cell size.");
        float sum = 0; int count = 0;
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
        {
            var p = map.Bounds.Position+new Vector2(x,y)*map.CellSize;
            var cell = new Rect2(p,Vector2.One*map.CellSize).Intersection(map.Bounds);
            var corners = new[] { cell.Position,new Vector2(cell.End.X,cell.Position.Y),cell.End,new Vector2(cell.Position.X,cell.End.Y) };
            if (!FeatureGeometry.Overlaps(polygon,bounds,0,corners,cell,0)) continue;
            var profile = profiles[map.Biomes[map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex].Id];
            if (profile != null && !(river ? profile.AllowFreshwaterRivers : profile.AllowFreshwaterLakes)) return river ? -1 : 0;
            sum += profile == null ? 1f : river ? profile.RiverSuitability : profile.LakeSuitability; count++;
        }
        return count == 0 ? 0 : sum/count;
    }
    // Explicit integer seed mixing is independent of Resource order, runtime string hashes and other planners.
    // =========================================================
    private static ulong SlotSeed(int seed, int slot)
    {
        unchecked
        {
            ulong value = ((ulong)(uint)seed<<32) ^ (uint)slot ^ 0x5747314C414B4531UL;
            value = (value^(value>>30))*0xBF58476D1CE4E5B9UL;
            value = (value^(value>>27))*0x94D049BB133111EBUL;
            return value^(value>>31);
        }
    }
    // Reject malformed settings rather than silently clamp or allocate excessive startup geometry.
    // =========================================================
    private static void Validate(WaterGenerationMasterConfig s)
    {
        if (s.LakeCount < 0 || s.LakeCount > 32 || s.AttemptsPerLake < 1 || s.AttemptsPerLake > 128
            || string.IsNullOrWhiteSpace(s.LiquidId) || s.LiquidId.Length > 128 || !float.IsFinite(s.MinimumDiameter) || s.MinimumDiameter < 64
            || !float.IsFinite(s.MaximumDiameter) || s.MaximumDiameter < s.MinimumDiameter || s.MaximumDiameter > 8192
            || !float.IsFinite(s.Separation) || s.Separation < 0 || s.Separation > 4096
            || !float.IsFinite(s.Irregularity) || s.Irregularity < 0 || s.Irregularity > 0.3f)
            throw new ArgumentException("Invalid bounded water generation settings.");
    }
}
