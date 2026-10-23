// Contact-window physics broad phase, directional hurt geometry and obstruction test; dormant between attacks.
using Godot;
using System;
public readonly record struct AttackContact(Node2D Target, DamageResult? Damage, Node2D Source=null, Vector2 Position=default, Vector2 Direction=default);
public interface ICombatImpactTarget { void ReceiveImpact(AttackContact contact); }
public sealed class MeleeHitQuery : IDisposable
{
    private const int CandidateLimit=256;
    private readonly CircleShape2D _circle=new();
    private readonly PhysicsShapeQueryParameters2D _overlap=new() { CollideWithAreas=false,CollideWithBodies=true };
    private readonly PhysicsRayQueryParameters2D _ray=new() { CollideWithAreas=false,CollideWithBodies=true,HitFromInside=true };
    private float _radius;
    private uint _mask;
    private Vector2 _origin;
    public int QueryCount { get; private set; }
    public int OverflowCount { get; private set; }

    // Allocate native query objects once; source body exclusion applies to overlap and visibility rays.
    // =========================================================
    public MeleeHitQuery(CollisionObject2D source)
    {
        _overlap.Shape=_circle;
        var exclude=new Godot.Collections.Array<Rid> { source.GetRid() };
        _overlap.Exclude=exclude; _ray.Exclude=exclude;
    }
    // Select at most one nearest eligible contact. A saturated broad phase fails closed rather than choosing an arbitrary subset.
    // =========================================================
    public bool Strike(ICombatTarget source, AttackValues attack, Vector2 aim, EffectiveStats stats, out AttackContact contact)
    {
        contact=default;
        if (!CombatTargeting.Active(source)) return false;
        Vector2 origin=source.CombatNode.GlobalPosition;
        float radius=attack.Reach+64; // All actor hurt radii clamp to 60, including offset feet colliders.
        if (_radius!=radius) { _circle.Radius=radius; _radius=radius; }
        if (_origin!=origin) { _overlap.Transform=new Transform2D(0,origin); _origin=origin; }
        if (_mask!=attack.CollisionMask) { _overlap.CollisionMask=attack.CollisionMask; _ray.CollisionMask=attack.CollisionMask; _mask=attack.CollisionMask; }
        _ray.From=origin;
        var space=source.CombatNode.GetWorld2D().DirectSpaceState;
        var candidates=space.IntersectShape(_overlap,CandidateLimit); QueryCount++;
        if (candidates.Count==CandidateLimit) { OverflowCount++; return false; }
        Node2D selected=null; ICombatTarget actor=null; IToolHitTarget tool=null;
        Vector2 selectedPoint=Vector2.Zero;
        float best=attack.Reach*attack.Reach; ulong bestId=ulong.MaxValue;
        foreach (var entry in candidates)
        {
            if (entry["collider"].AsGodotObject() is not CollisionObject2D body || !body.IsInsideTree() || body.IsQueuedForDeletion() || !body.CanProcess()) continue;
            ICombatTarget candidateActor=body as ICombatTarget;
            IToolHitTarget candidateTool=null; Node2D receiver=body;
            Vector2 anchor, point;
            if (candidateActor!=null)
            {
                if (!CombatTargeting.Eligible(source,candidateActor,attack.Targets)) continue;
                anchor=candidateActor.HitAnchor; point=candidateActor.ClosestHitPoint(origin);
            }
            else
            {
                if (!attack.HitTools) continue;
                // Prop receivers are attached helpers; inspect only this nearby body's immediate children.
                if (body is IToolHitTarget direct && body.IsInGroup("world_tool_targets")) candidateTool=direct;
                for (int i=0;candidateTool==null && i<body.GetChildCount();i++)
                    if (body.GetChild(i) is Node2D child && child is IToolHitTarget helper && child.IsInGroup("world_tool_targets")
                        && !child.IsQueuedForDeletion() && child.CanProcess()) { candidateTool=helper; receiver=child; }
                if (candidateTool==null) continue;
                anchor=candidateTool.HitAnchor; point=candidateTool.ClosestHitPoint(origin);
            }
            Vector2 direction=anchor-origin; float distance=origin.DistanceSquaredTo(point); ulong id=body.GetInstanceId();
            if (!point.IsFinite() || distance>best || (distance==best && id>=bestId)
                || (direction.LengthSquared()>0.01f && direction.Normalized().Dot(aim)<attack.MinimumDot)) continue;
            // Rocks/walls/other bodies block melee rather than allowing a hit through terrain.
            _ray.To=point;
            var obstruction=space.IntersectRay(_ray);
            if (obstruction.Count!=0 && obstruction["collider"].AsGodotObject()!=body) continue;
            best=distance; bestId=id; selectedPoint=point; selected=receiver; actor=candidateActor; tool=candidateTool;
        }
        if (selected==null || selected.IsQueuedForDeletion() || !selected.IsInsideTree()) return false;
        if (actor!=null)
        {
            if (!CombatTargeting.Eligible(source,actor,attack.Targets)) return false;
            var result=actor.ReceiveDamage(new DamageRequest(attack.Damage,origin),stats);
            contact=new(selected,result,source.CombatNode,selectedPoint,aim);
            // Includes lethal contacts before deferred retirement, but excludes zero damage/immune blocks.
            if (result.AppliedDamage>0 && actor is ICombatImpactTarget impactTarget) impactTarget.ReceiveImpact(contact);
            if (result.AppliedDamage>0 && !result.Killed && CombatTargeting.Active(actor) && actor is IReactionTarget reactionTarget)
                reactionTarget.ReceiveReaction(attack,aim);
        }
        else { tool.ReceiveHit(aim); contact=new(selected,null,source.CombatNode,selectedPoint,aim); }
        return true;
    }
    // Native shapes/parameters are owned by this query, never shared with actor movement colliders.
    // =========================================================
    public void Dispose() { _overlap.Dispose(); _ray.Dispose(); _circle.Dispose(); }
}
