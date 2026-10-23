// Bounded stable approach selection. A tiny target-scoped ledger reserves points, never moves actors or ticks.
using Godot;
using System;
using System.Collections.Generic;
public sealed class EngagementPositioning : IDisposable
{
    private sealed class Claims
    {
        public readonly ulong[] Owners=new ulong[64];
        public readonly Vector2[] Points=new Vector2[64];
        public readonly float[] Radii=new float[64];
        public int Count;
    }
    private static readonly Dictionary<ulong,Claims> Targets=new();
    private Claims _claims;
    private ulong _targetId, _ownerId;
    private int _slot=-1;
    private float _elapsed=10, _held, _blocked, _retry;
    private Vector2 _targetOrigin, _goal;
    public bool HasPosition => _slot>=0;
    public Vector2 Goal => _goal;
    public int SelectionCount { get; private set; }
    public static int TargetCount => Targets.Count;
    // Only pursuing actors advance this state; attacks retain their claim without selection/query work.
    // =========================================================
    public void Advance(float delta,float progress,float desiredSpeed,EngagementProfile profile)
    {
        _elapsed+=delta; _held+=delta; _retry=Mathf.Max(0,_retry-delta);
        if (!HasPosition || profile?.BlockedFallbackEnabled==false) { _blocked=0; return; }
        _blocked=desiredSpeed>1 && progress<desiredSpeed*delta*0.15f ? _blocked+delta : 0;
        if (_blocked<Safe(profile?.BlockedTimeout ?? 1,1,0.3f,4)) return;
        Release(); _retry=1; _blocked=0;
    }
    // At most eight path candidates per reconsideration; reservations cap at64 per target, then direct fallback.
    // =========================================================
    public Vector2 Select(Entity actor,CharacterBody2D target,StaticObstacleNavigation navigation,EngagementProfile profile,float reach)
    {
        if (profile==null || profile.Mode==EngagementMode.Direct || navigation==null) { Clear(); return target.GlobalPosition; }
        ulong targetId=target.GetInstanceId();
        if (_targetId!=targetId) { Clear(); _targetId=targetId; _ownerId=actor.GetInstanceId(); }
        if (_retry>0) return target.GlobalPosition;
        float interval=Safe(profile.ReconsiderSeconds,0.5f,0.2f,2);
        bool moved=profile.MovementThresholdEnabled && _targetOrigin.DistanceTo(target.GlobalPosition)>=Safe(profile.TargetMovementThreshold,16,0,100);
        float persistence=profile.PersistenceEnabled ? Safe(profile.PersistenceSeconds,1.2f,0,5) : 0;
        if(HasPosition && _elapsed<interval) return _goal;
        if(HasPosition && !moved && _held<persistence) return _goal;
        _elapsed=0; SelectionCount++;
        if (_claims==null)
        {
            if(!Targets.TryGetValue(targetId,out _claims)) { _claims=new Claims(); Targets.Add(targetId,_claims); }
        }
        int available=_slot;
        if(available<0) for(int i=0;i<64;i++) if(_claims.Owners[i]==0) { available=i; break; }
        if(available<0) return target.GlobalPosition;
        float radius=Mathf.Min(Safe(profile.PreferredDistance,32,18,80),Mathf.Max(18,reach-3));
        float spacing=Safe(profile.Spacing,10,0,40);
        float baseAngle=(actor.GlobalPosition-target.GlobalPosition).Angle();
        float best=float.PositiveInfinity; Vector2 chosen=default; bool found=false;
        float directDistance=actor.GlobalPosition.DistanceTo(target.GlobalPosition);
        for(int i=0;i<8;i++)
        {
            // Nearest approach first, alternating sides; actors reserve distinct reachable points.
            int sector=i==0 ? 0 : (i+1)/2*(i%2==1 ? 1 : -1);
            Vector2 wanted=target.GlobalPosition+Vector2.FromAngle(baseAngle+sector*Mathf.Tau/8)*radius;
            if(!navigation.IsWalkable(wanted)) continue;
            if(target is PlayerMovement p && p.Streaming!=null && !p.Streaming.IsPositionLoaded(wanted)) continue;
            bool crowded=false;
            for(int j=0;j<64;j++)
            {
                if(j==_slot || _claims.Owners[j]==0) continue;
                float clearance=actor.FeetRadius+_claims.Radii[j]+spacing;
                if(wanted.DistanceSquaredTo(_claims.Points[j])<clearance*clearance) { crowded=true; break; }
            }
            if(crowded) continue;
            float stability=HasPosition ? wanted.DistanceTo(_goal)*0.3f : 0;
            // Straight distance is a lower bound: skip AStar when this candidate cannot beat the current route.
            if(actor.GlobalPosition.DistanceTo(wanted)+stability>=best) continue;
            var path=navigation.FindPath(actor.GlobalPosition,wanted);
            if(path.Length==0) continue;
            float length=actor.GlobalPosition.DistanceTo(path[0]);
            for(int j=1;j<path.Length;j++) length+=path[j-1].DistanceTo(path[j]);
            length+=path[^1].DistanceTo(wanted);
            if(profile.DetourLimitEnabled && length>directDistance+Safe(profile.MaximumDetour,70,0,200)) continue;
            float score=length+stability;
            if(score>=best) continue; best=score; chosen=wanted; found=true;
        }
        if(!found) { Release(); _retry=interval; return target.GlobalPosition; }
        if(_slot<0) { _slot=available; _claims.Count++; _claims.Owners[_slot]=_ownerId; }
        _claims.Points[_slot]=chosen; _claims.Radii[_slot]=actor.FeetRadius;
        _goal=chosen; _targetOrigin=target.GlobalPosition; _held=0;
        return _goal;
    }
    // Release on death, suspension, reaction, target change, master/profile Off and unload.
    // =========================================================
    private void Release()
    {
        if(_claims!=null)
        {
            if(_slot>=0) { _claims.Owners[_slot]=0; _claims.Count--; }
            if(_claims.Count==0) Targets.Remove(_targetId);
        }
        _slot=-1; _claims=null;
    }
    // =========================================================
    public void Clear() { Release(); _targetId=_ownerId=0; _elapsed=10; _held=_blocked=_retry=0; }
    // =========================================================
    public void Dispose() => Clear();
    // =========================================================
    private static float Safe(float value,float fallback,float min,float max) => SeparationProfile.Bounded(value,fallback,min,max);
}
