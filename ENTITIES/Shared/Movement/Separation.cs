// Bounded local steering state, owned by the existing motor; never moves or ticks independently.
using Godot;
using System;
public sealed class Separation : IDisposable
{
    private CircleShape2D _shape;
    private PhysicsShapeQueryParameters2D _query;
    private Vector2[] _offsets;
    private float[] _distances, _radii;
    private ulong[] _ids;
    private Vector2 _steering, _smoothed;
    private float _timer, _sideTimer, _side;
    private bool _started;
    public int QueryCount { get; private set; }
    public int SaturatedSamples { get; private set; }
    public int NeighborCount { get; private set; }
    public Vector2 Steering => _steering;
    // The master is checked only by already-active motors; disabled mechanics issue no queries.
    // =========================================================
    public Vector2 Blend(Entity actor,CharacterBody2D target,Vector2 desired,float delta,bool enabled,float attackFade)
    {
        var profile=actor.Config.Separation;
        float strength=SeparationProfile.Bounded(profile?.Strength ?? 0.35f,0.35f,0,0.5f);
        if (strength<=0 || !enabled || profile?.Enabled==false || desired.LengthSquared()<0.001f || attackFade<=0) { Clear(); return desired; }
        if (!_started) { _started=true; _timer=(actor.GetInstanceId()%200)/1000f; }
        _timer-=delta; _sideTimer-=delta;
        if (_timer<=0)
        {
            _timer=0.2f;
            Sample(actor,target,desired.Normalized(),profile);
        }
        float speed=desired.Length();
        _smoothed=_smoothed.Lerp(_steering,1-Mathf.Exp(-delta/0.1f));
        return (desired+_smoothed*speed*strength*Mathf.Clamp(attackFade,0,1)).LimitLength(speed);
    }
    // Sort a bounded set by distance and instance ID; saturated broad-phase subsets are rejected.
    // =========================================================
    private void Sample(Entity actor,CharacterBody2D target,Vector2 forward,SeparationProfile profile)
    {
        if (_shape==null)
        {
            _shape=new CircleShape2D { Radius=64 };
            _offsets=new Vector2[8]; _distances=new float[8]; _radii=new float[8]; _ids=new ulong[8];
        }
        _query ??= new PhysicsShapeQueryParameters2D { Shape=_shape,CollisionMask=2,CollideWithBodies=true,CollideWithAreas=false };
        _query.Transform=new Transform2D(0,actor.FeetCenter);
        _query.Exclude=new Godot.Collections.Array<Rid> { actor.GetRid(),target.GetRid() };
        var hits=actor.GetWorld2D().DirectSpaceState.IntersectShape(_query,33); QueryCount++;
        NeighborCount=0;
        if (hits.Count>=33) { SaturatedSamples++; _steering=Vector2.Zero; return; }
        foreach(var hit in hits)
        {
            if (hit["collider"].AsGodotObject() is not Entity other || other==actor || other==target
                || !other.IsInsideTree() || other.IsQueuedForDeletion() || !other.CanProcess() || other.HealthState?.IsAlive!=true) continue;
            Vector2 offset=actor.FeetCenter-other.FeetCenter;
            float distance=offset.LengthSquared(); ulong id=other.GetInstanceId();
            int slot=0;
            while(slot<NeighborCount && (_distances[slot]<distance || (_distances[slot]==distance && _ids[slot]<id))) slot++;
            if(slot>=8) continue;
            for(int j=Math.Min(NeighborCount,7);j>slot;j--) { _distances[j]=_distances[j-1]; _offsets[j]=_offsets[j-1]; _radii[j]=_radii[j-1]; _ids[j]=_ids[j-1]; }
            _distances[slot]=distance; _offsets[slot]=offset; _radii[slot]=other.FeetRadius; _ids[slot]=id;
            NeighborCount=Math.Min(8,NeighborCount+1);
        }
        Vector2 force=Vector2.Zero, lateral=new(-forward.Y,forward.X);
        float clearance=SeparationProfile.Bounded(profile?.Clearance ?? 10,10,0,40);
        for(int i=0;i<NeighborCount;i++)
        {
            float distance=Mathf.Sqrt(_distances[i]), spacing=actor.FeetRadius+_radii[i]+clearance;
            if(distance>=spacing) continue;
            Vector2 away=distance>0.01f ? _offsets[i]/distance : lateral*(actor.GetInstanceId()<_ids[i] ? -1 : 1);
            // Remove backwards braking; persistent lateral escape handles collinear queues.
            float sideways=away.Dot(lateral);
            if(Mathf.Abs(sideways)<0.15f)
            {
                if(_sideTimer<=0) { _side=(actor.GetInstanceId()&1)==0 ? 1 : -1; _sideTimer=0.4f; }
                away=lateral*_side;
            }
            else away-=forward*Mathf.Min(0,away.Dot(forward));
            force+=away*(1-distance/Mathf.Max(1,spacing));
        }
        _steering=force.LimitLength(1);
    }
    // All stop/attack/reaction/lifecycle gates discard cached steering; queries stay reusable.
    // =========================================================
    public void Clear() { _steering=Vector2.Zero; _smoothed=Vector2.Zero; _timer=0; _sideTimer=0; _started=false; NeighborCount=0; }
    // Release native query resources on unload.
    // =========================================================
    public void Dispose() { Clear(); _query?.Dispose(); _shape?.Dispose(); }
}
