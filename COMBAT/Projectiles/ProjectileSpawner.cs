// Scene-local shot budget and lifetime boundary. No idle/active callbacks, global manager or world scans.
using Godot;
using System;
public partial class ProjectileSpawner : Node2D
{
    [Export(PropertyHint.Range,"1,256,1")] public int MaximumActive { get; set; } = 64;
    [Export] public Rect2 Bounds { get; set; }
    public Func<Vector2,bool> IsPositionLoaded { get; set; }
    public int ActiveCount { get; private set; }
    // Match the world's Y-sort chain; no sensors, physics bodies or timers are added.
    // =========================================================
    public override void _Ready() { YSortEnabled=true; SetProcess(false); SetPhysicsProcess(false); }
    // Validate all input first; scene ownership is explicit and shots never force-load chunks.
    // =========================================================
    public bool TryFire(ICombatTarget source,ProjectileProfile profile,Vector2 muzzle,Vector2 direction,out Projectile shot)
    {
        shot=null;
        if(!IsInsideTree() || !CanProcess() || IsQueuedForDeletion() || ActiveCount>=Mathf.Clamp(MaximumActive,1,256)
            || source?.CombatNode is not CollisionObject2D || !CombatTargeting.Active(source) || source.Stats==null || source.CombatNode.GetWorld2D()!=GetWorld2D() || !GodotObject.IsInstanceValid(profile)
            || !muzzle.IsFinite() || !direction.IsFinite() || !float.IsFinite(direction.LengthSquared()) || direction.LengthSquared()<0.001f || !Allows(muzzle)) return false;
        ProjectileValues values;
        try { values=profile.Snapshot(source.Stats.Values); } catch(ArgumentException) { return false; }
        shot=new Projectile(); shot.Configure(this,source,values,direction.Normalized());
        ActiveCount++; AddChild(shot); shot.GlobalTransform=new Transform2D(direction.Angle(),muzzle); return true;
    }
    // Sample long movement against loaded-area membership; pathological >1024px steps fail closed.
    // =========================================================
    internal bool Allows(Vector2 point) => Bounds.Position.IsFinite() && Bounds.Size.IsFinite() && Bounds.Size.X>0 && Bounds.Size.Y>0 && Bounds.HasPoint(point) && (IsPositionLoaded?.Invoke(point) ?? true);
    // =========================================================
    internal bool AllowsSegment(Vector2 from,Vector2 to)
    {
        if(!from.IsFinite() || !to.IsFinite()) return false;
        int steps=Mathf.Max(1,Mathf.CeilToInt(from.DistanceTo(to)/16));
        if(steps>64) return false;
        for(int i=0;i<=steps;i++) if(!Allows(from.Lerp(to,(float)i/steps))) return false;
        return true;
    }
    // Each shot releases its slot once, including disable/remove/unload paths.
    // =========================================================
    internal void Release() => ActiveCount=Mathf.Max(0,ActiveCount-1);
}
