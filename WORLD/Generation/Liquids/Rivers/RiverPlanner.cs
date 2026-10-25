// Seeded bounded macro routing and organic river corridors. Output reuses the existing liquid geometry/runtime contract.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class RiverPlanner
{
    private sealed class RouteGrid
    {
        public int Columns,Rows,Step;
        public Rect2 Bounds;
        public float[] Costs,Suitability;
        public readonly List<int> Eligible=new();
        // Route coordinates are independent of chunk size and remain stable at negative world origins.
        // =========================================================
        public Vector2 Point(int id) => Bounds.Position+new Vector2(id%Columns+0.5f,id/Columns+0.5f)*Step;
    }
    #region Macro planning
    // Rivers get independent slot RNG streams after lakes; disabling them preserves lake hashes and old outcomes.
    // =========================================================
    internal static void Append(WaterGenerationMasterConfig settings,int seed,Rect2 bounds,MacroWorldMap map,WorldFeaturePlan features,
        Dictionary<string,BiomeHydrologyProfile> profiles,List<PlannedWaterBody> bodies,List<string> warnings,Rect2? authoredPond)
    {
        if (settings.RiverCount==0) return;
        Validate(settings);
        for (int slot=0;slot<settings.RiverCount;slot++)
        {
            var grid=BuildGrid(settings,bounds,map,features,profiles,bodies,authoredPond);
            using var rng=new RandomNumberGenerator { Seed=Seed(seed,slot) };
            bool placed=false;
            for (int attempt=0;attempt<settings.AttemptsPerRiver && grid.Eligible.Count>1;attempt++)
            {
                int start=grid.Eligible[(int)(rng.Randi()%(uint)grid.Eligible.Count)],end=-1;
                if (grid.Suitability[start]<=0 || rng.Randf()>=grid.Suitability[start]) continue;
                for (int candidate=0;candidate<64;candidate++)
                {
                    int id=grid.Eligible[(int)(rng.Randi()%(uint)grid.Eligible.Count)];
                    float span=grid.Point(start).DistanceTo(grid.Point(id));
                    if (span<settings.MinimumRiverSpan || span>settings.MaximumRiverSpan || grid.Suitability[id]<=0 || rng.Randf()>=grid.Suitability[id]) continue;
                    if (map!=null && settings.RequireMultipleRiverBiomes && map.Biomes.Count>1 && map.BiomeAt(grid.Point(start)).Id==map.BiomeAt(grid.Point(id)).Id) continue;
                    end=id; break;
                }
                if (end<0) continue;
                var route=FindRoute(grid,start,end,settings.MaximumRiverSearchNodes);
                if (route==null) continue;
                var path=ShapePath(route,settings,rng);
                if (path==null) continue;
                PlannedWaterBody body;
                try { body=new PlannedWaterBody($"river.{slot:D3}",settings.LiquidId,Outline(path,settings.RiverBankIrregularity),path); }
                catch (ArgumentException) { continue; } // Invalid bends are rejected as a whole, never drawn as disconnected pieces.
                if (!ValidFootprint(body,settings,bounds,map,features,profiles,bodies,authoredPond)) continue;
                bodies.Add(body); placed=true; break;
            }
            if (!placed) warnings.Add($"Water river.{slot:D3}: no valid source-to-basin corridor after bounded attempts/search; skipped (biomes, reservations, width or route limits).");
        }
    }
    // Cache a modest cost field once per river slot. Conservative clearance protects transitions between grid centers.
    // =========================================================
    private static RouteGrid BuildGrid(WaterGenerationMasterConfig settings,Rect2 bounds,MacroWorldMap map,WorldFeaturePlan features,
        Dictionary<string,BiomeHydrologyProfile> profiles,List<PlannedWaterBody> bodies,Rect2? pond)
    {
        var grid=new RouteGrid { Bounds=bounds,Step=settings.RiverRouteCellSize };
        grid.Columns=Mathf.CeilToInt(bounds.Size.X/grid.Step); grid.Rows=Mathf.CeilToInt(bounds.Size.Y/grid.Step);
        if ((long)grid.Columns*grid.Rows>65536) throw new ArgumentException("River route grid exceeds 65536 cells; increase RiverRouteCellSize.");
        grid.Costs=new float[grid.Columns*grid.Rows]; grid.Suitability=new float[grid.Costs.Length];
        float radius=settings.MaximumRiverWidth*1.4f+settings.RiverBendStrength+settings.Separation+grid.Step*0.75f;
        for (int id=0;id<grid.Costs.Length;id++)
        {
            Vector2 point=grid.Point(id);
            if (!features.Reservations.AllowsDisk(point,radius,WorldPlacementKind.Water) || (pond.HasValue && pond.Value.Grow(radius).HasPoint(point))) continue;
            bool blocked=false;
            foreach (var body in bodies) if (body.Bounds.Grow(radius).HasPoint(point)) { blocked=true; break; }
            if (blocked) continue;
            float suitability=map==null ? 1f : DiskSuitability(point,radius,map,profiles);
            if (suitability<0) continue;
            var endpointProfile=map==null ? null : profiles[map.BiomeAt(point).Id];
            grid.Suitability[id]=endpointProfile?.RiverSuitability ?? 1f;
            grid.Costs[id]=1f+(1f-suitability)*4f; grid.Eligible.Add(id);
        }
        return grid;
    }
    // Inspect every biome cell touched by the conservative disk; hard prohibitions are never a center-only check.
    // =========================================================
    private static float DiskSuitability(Vector2 point,float radius,MacroWorldMap map,Dictionary<string,BiomeHydrologyProfile> profiles)
    {
        int left=Math.Max(0,Mathf.FloorToInt((point.X-radius-map.Bounds.Position.X)/map.CellSize));
        int top=Math.Max(0,Mathf.FloorToInt((point.Y-radius-map.Bounds.Position.Y)/map.CellSize));
        int right=Math.Min(map.Columns-1,Mathf.FloorToInt((point.X+radius-map.Bounds.Position.X)/map.CellSize));
        int bottom=Math.Min(map.Rows-1,Mathf.FloorToInt((point.Y+radius-map.Bounds.Position.Y)/map.CellSize));
        if ((long)(right-left+1)*(bottom-top+1)>4096) throw new ArgumentException("River eligibility disk exceeds 4096 macro cells.");
        float sum=0; int count=0;
        for (int y=top;y<=bottom;y++) for (int x=left;x<=right;x++)
        {
            Vector2 corner=map.Bounds.Position+new Vector2(x,y)*map.CellSize;
            Vector2 nearest=new(Mathf.Clamp(point.X,corner.X,corner.X+map.CellSize),Mathf.Clamp(point.Y,corner.Y,corner.Y+map.CellSize));
            if (point.DistanceSquaredTo(nearest)>radius*radius) continue;
            var profile=profiles[map.Biomes[map.Regions[map.RegionIdAtCell(x,y)].BiomeIndex].Id];
            if (profile!=null && !profile.AllowFreshwaterRivers) return -1;
            sum+=profile?.RiverSuitability ?? 1f; count++;
        }
        return count==0 ? -1 : sum/count;
    }
    #endregion

    #region Route and organic outline
    // Deterministic weighted A* with explicit tie IDs, bounded expansions and no diagonal corner cutting.
    // =========================================================
    private static Vector2[] FindRoute(RouteGrid grid,int start,int end,int limit)
    {
        var distance=new float[grid.Costs.Length]; Array.Fill(distance,float.PositiveInfinity);
        var parent=new int[distance.Length]; Array.Fill(parent,-1); var closed=new bool[distance.Length];
        var open=new PriorityQueue<int,(float,int)>(); distance[start]=0; open.Enqueue(start,(0,start));
        int expanded=0;
        while (open.Count>0 && expanded<limit)
        {
            int id=open.Dequeue(); if (closed[id]) continue;
            closed[id]=true; expanded++;
            if (id==end)
            {
                var result=new List<Vector2>();
                for (int node=end;node!=-1;node=parent[node]) result.Add(grid.Point(node));
                result.Reverse(); return result.ToArray();
            }
            int x=id%grid.Columns,y=id/grid.Columns;
            for (int dy=-1;dy<=1;dy++) for (int dx=-1;dx<=1;dx++)
            {
                int nx=x+dx,ny=y+dy;
                if ((dx==0 && dy==0) || nx<0 || ny<0 || nx>=grid.Columns || ny>=grid.Rows) continue;
                int next=ny*grid.Columns+nx;
                if (closed[next] || grid.Costs[next]<=0 || (dx!=0 && dy!=0 && (grid.Costs[y*grid.Columns+nx]<=0 || grid.Costs[ny*grid.Columns+x]<=0))) continue;
                float step=dx!=0 && dy!=0 ? 1.41421356f : 1f;
                float score=distance[id]+step*(grid.Costs[id]+grid.Costs[next])*0.5f;
                if (score>=distance[next]) continue;
                distance[next]=score; parent[next]=id;
                float heuristic=grid.Point(next).DistanceTo(grid.Point(end))/grid.Step;
                open.Enqueue(next,(score+heuristic,next));
            }
        }
        return null;
    }
    // Smooth the macro route with cubic curves, then sample by world distance for continuous meanders and banks.
    // =========================================================
    private static PlannedRiverPath ShapePath(Vector2[] route,WaterGenerationMasterConfig settings,RandomNumberGenerator rng)
    {
        var lengths=new float[route.Length];
        for (int i=1;i<route.Length;i++) lengths[i]=lengths[i-1]+route[i].DistanceTo(route[i-1]);
        float total=lengths[^1];
        if (total<settings.MinimumRiverSpan || total>settings.MaximumRiverSpan*2f) return null;
        // Sparse routing anchors describe obstacle avoidance only; dense curve samples determine the actual shoreline.
        int anchorsCount=Math.Clamp(Mathf.CeilToInt(total/1024f)+1,4,32);
        var anchors=new Vector2[anchorsCount];
        for (int i=0,segment=1;i<anchorsCount;i++)
        {
            float along=total*i/(anchorsCount-1);
            while (segment<route.Length-1 && lengths[segment]<along) segment++;
            anchors[i]=route[segment-1].Lerp(route[segment],(along-lengths[segment-1])/(lengths[segment]-lengths[segment-1]));
        }
        // Average interior routing corners before interpolation; final footprint checks still protect all exclusions.
        var original=(Vector2[])anchors.Clone();
        for (int i=1;i<anchorsCount-1;i++) anchors[i]=(original[i-1]+original[i]*2+original[i+1])*0.25f;
        int count=Math.Clamp(Mathf.CeilToInt(total/32f)+1,4,1000);
        var points=new Vector2[count]; var widths=new float[count]; float phase=rng.Randf()*Mathf.Tau;
        for (int i=0;i<count;i++)
        {
            float t=i/(float)(count-1),u=t*(anchorsCount-1); int k=Math.Min((int)u,anchorsCount-2); float f=u-k;
            Vector2 p0=k==0 ? anchors[0]*2-anchors[1] : anchors[k-1],p1=anchors[k],p2=anchors[k+1];
            Vector2 p3=k+2>=anchorsCount ? p2*2-p1 : anchors[k+2];
            // Uniform Catmull-Rom has matching tangents across anchors; extrapolated end controls avoid flat endpoints.
            points[i]=0.5f*((2*p1)+(-p0+p2)*f+(2*p0-5*p1+4*p2-p3)*f*f+(-p0+3*p1-3*p2+p3)*f*f*f);
            Vector2 tangent=(p2-p1).Normalized(),normal=new(-tangent.Y,tangent.X);
            // Smoothly interpolate normals too, avoiding a displacement jump at a routing anchor.
            Vector2 before=(p2-p0).Normalized(),after=(p3-p1).Normalized();
            tangent=before.Lerp(after,f).Normalized(); normal=new(-tangent.Y,tangent.X);
            float envelope=Mathf.Pow(Mathf.Sin(Mathf.Pi*t),2);
            float wave=Mathf.Sin(total*t*Mathf.Tau/settings.RiverMeanderLength+phase);
            points[i]+=normal*(settings.RiverBendStrength*envelope*wave);
            float widthWave=0.5f+0.5f*Mathf.Sin(t*Mathf.Tau*1.3f+phase);
            float pools=1f+0.7f*Mathf.Pow(1-t,8)+1.2f*Mathf.Pow(t,8);
            widths[i]=Mathf.Lerp(settings.MinimumRiverWidth,settings.MaximumRiverWidth,widthWave)*pools;
        }
        try { return new PlannedRiverPath(points,widths); } catch (ArgumentException) { return null; }
    }
    // One simple polygon includes positive-width spring/terminal pools, not a ribbon fading away at biome edges.
    // =========================================================
    internal static Vector2[] Outline(PlannedRiverPath path,float irregularity=0f)
    {
        int count=path.Points.Count; float along=0; var left=new Vector2[count]; var right=new Vector2[count];
        for (int i=0;i<count;i++)
        {
            var before=(path.Points[i]-path.Points[Math.Max(0,i-1)]).Normalized();
            var after=(path.Points[Math.Min(count-1,i+1)]-path.Points[i]).Normalized();
            if (i==0) before=after; if (i==count-1) after=before;
            if (before.Dot(after)<-0.2f) throw new ArgumentException("River has an acute reversal.");
            var n1=new Vector2(-before.Y,before.X); var n2=new Vector2(-after.Y,after.X); var normal=(n1+n2).Normalized();
            float half=path.Widths[i]*0.5f/Mathf.Max(0.65f,normal.Dot(n1));
            // Independent broad bank waves preserve terminal caps and the saved centerline; no runtime noise sampling.
            if (i>0) along+=path.Points[i].DistanceTo(path.Points[i-1]);
            float t=i/(float)(count-1),envelope=i==0 || i==count-1 ? 0 : Mathf.Min(1f,Mathf.Sin(Mathf.Pi*t)*8f);
            float phase=path.Points[0].X*0.013f+path.Points[0].Y*0.017f;
            float leftWave=0.65f*Mathf.Sin(along*Mathf.Tau/380f+phase)+0.35f*Mathf.Sin(along*Mathf.Tau/170f-phase);
            float rightWave=0.65f*Mathf.Sin(along*Mathf.Tau/430f-phase)+0.35f*Mathf.Sin(along*Mathf.Tau/190f+phase);
            left[i]=path.Points[i]+normal*(half*(1f+irregularity*envelope*leftWave));
            right[i]=path.Points[i]-normal*(half*(1f+irregularity*envelope*rightWave));
        }
        var polygon=new List<Vector2>(count*2+10); polygon.AddRange(left);
        void Cap(int point,Vector2 direction)
        {
            float angle=direction.Angle()+Mathf.Pi*0.5f;
            for (int j=1;j<6;j++) polygon.Add(path.Points[point]+Vector2.FromAngle(angle-Mathf.Pi*j/6f)*(path.Widths[point]*0.5f));
        }
        Cap(count-1,path.Points[^1]-path.Points[^2]);
        for (int i=count-1;i>=0;i--) polygon.Add(right[i]);
        Cap(0,path.Points[0]-path.Points[1]);
        return polygon.ToArray();
    }
    #endregion

    #region Final contract validation
    // Validate the entire final noisy corridor, not only the coarse route or its endpoints.
    // =========================================================
    private static bool ValidFootprint(PlannedWaterBody body,WaterGenerationMasterConfig settings,Rect2 bounds,MacroWorldMap map,WorldFeaturePlan features,
        Dictionary<string,BiomeHydrologyProfile> profiles,List<PlannedWaterBody> placed,Rect2? pond)
    {
        if (!bounds.Encloses(body.Bounds)) return false;
        var polygon=body.CopyPolygon(); float margin=settings.Separation;
        foreach (var feature in features.Reservations.Features)
            if ((feature.AllowedPlacements&WorldPlacementKind.Water)==0 && FeatureGeometry.Overlaps(polygon,body.Bounds,margin,feature.CopyPolygon(),feature.Bounds,feature.Clearance)) return false;
        foreach (var other in placed)
            if (FeatureGeometry.Overlaps(polygon,body.Bounds,margin,other.CopyPolygon(),other.Bounds,0)) return false;
        if (pond is { } rect)
        {
            var corners=new[] { rect.Position,new Vector2(rect.End.X,rect.Position.Y),rect.End,new Vector2(rect.Position.X,rect.End.Y) };
            if (FeatureGeometry.Overlaps(polygon,body.Bounds,margin,corners,rect,0)) return false;
        }
        if (map==null) return true;
        if ((long)Mathf.CeilToInt(body.Bounds.Size.X/map.CellSize+2)*Mathf.CeilToInt(body.Bounds.Size.Y/map.CellSize+2)>4096) return false;
        return WaterBodyPlanner.Suitability(polygon,body.Bounds,map,profiles,true)>=0;
    }
    // Independent explicit integer streams avoid consuming lake/P1/P2 RNG or depending on authoring order.
    // =========================================================
    private static ulong Seed(int seed,int slot)
    {
        unchecked
        {
            ulong value=((ulong)(uint)seed<<32)^(uint)slot^0x5747335249564552UL;
            value=(value^(value>>30))*0xBF58476D1CE4E5B9UL; value=(value^(value>>27))*0x94D049BB133111EBUL;
            return value^(value>>31);
        }
    }
    // River settings are bounded independently and only validated when rivers are requested.
    // =========================================================
    private static void Validate(WaterGenerationMasterConfig s)
    {
        if (s.RiverCount<0 || s.RiverCount>4 || s.RiverCount+s.LakeCount>32 || s.AttemptsPerRiver<1 || s.AttemptsPerRiver>32
            || s.MaximumRiverSearchNodes<32 || s.MaximumRiverSearchNodes>65536 || s.RiverRouteCellSize<256 || s.RiverRouteCellSize>1024
            || !float.IsFinite(s.MinimumRiverSpan) || s.MinimumRiverSpan<1500 || !float.IsFinite(s.MaximumRiverSpan) || s.MaximumRiverSpan<s.MinimumRiverSpan || s.MaximumRiverSpan>12000
            || !float.IsFinite(s.MinimumRiverWidth) || s.MinimumRiverWidth<64 || !float.IsFinite(s.MaximumRiverWidth) || s.MaximumRiverWidth<s.MinimumRiverWidth || s.MaximumRiverWidth>384
            || !float.IsFinite(s.RiverBendStrength) || s.RiverBendStrength<0 || s.RiverBendStrength>512
            || !float.IsFinite(s.RiverMeanderLength) || s.RiverMeanderLength<1200 || s.RiverMeanderLength>6000
            || !float.IsFinite(s.RiverBankIrregularity) || s.RiverBankIrregularity<0 || s.RiverBankIrregularity>0.3f)
            throw new ArgumentException("Invalid bounded river settings.");
    }
    #endregion
}
