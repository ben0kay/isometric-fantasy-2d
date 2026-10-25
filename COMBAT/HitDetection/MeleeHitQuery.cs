// Contact-window broad phase for bounded multi-actor cleaves and a single nearest tool hit.
using Godot;
using System;
public readonly record struct AttackContact(Node2D Target, DamageResult? Damage, Node2D Source=null, Vector2 Position=default, Vector2 Direction=default);
public interface ICombatImpactTarget { void ReceiveImpact(AttackContact contact); }
public sealed class MeleeHitQuery : IDisposable
{
    public const int MaxTargetCount=16;
    private const int CandidateLimit=256;
    private const uint ActorLayer=2u; // AC: actors on physics layer 2; scenery/world solids on layer 1.
    private readonly CircleShape2D _circle=new();
    private readonly PhysicsShapeQueryParameters2D _overlap=new() { CollideWithAreas=false,CollideWithBodies=true };
    private readonly PhysicsRayQueryParameters2D _ray=new() { CollideWithAreas=false,CollideWithBodies=true,HitFromInside=true };
    private readonly Selection[] _selected=new Selection[MaxTargetCount];
    private readonly AttackContact[] _single=new AttackContact[1];
    private float _radius;
    private uint _mask;
    private Vector2 _origin;
    public int QueryCount { get; private set; }
    public int OverflowCount { get; private set; }

    private struct Selection
    {
        public Node2D Receiver;
        public ICombatTarget Actor;
        public IToolHitTarget Tool;
        public Vector2 Point;
        public float Distance;
        public ulong Id;
    }

    #region Setup
    // Keep native query objects and candidate storage per executor; no idle physics queries.
    // =========================================================
    public MeleeHitQuery(CollisionObject2D source)
    {
        _overlap.Shape=_circle;
        var exclude=new Godot.Collections.Array<Rid> { source.GetRid() };
        _overlap.Exclude=exclude; _ray.Exclude=exclude;
    }
    #endregion

    #region Contact
    // Preserve the original single-contact method for existing direct callers and fixtures.
    // =========================================================
    public bool Strike(ICombatTarget source, AttackValues attack, Vector2 aim, EffectiveStats stats, out AttackContact contact)
    {
        int count=StrikeMany(source,attack with { MaxTargets=1 },aim,stats,_single);
        contact=count>0 ? _single[0] : default;
        return count>0;
    }

    // One broad-phase query selects up to the attack's target cap; each actor can be hit only once.
    // =========================================================
    public int StrikeMany(ICombatTarget source, AttackValues attack, Vector2 aim, EffectiveStats stats, AttackContact[] contacts, ToolWork toolWork=default)
    {
        if (contacts==null || contacts.Length==0 || !CombatTargeting.Active(source)) return 0;
        int limit=Math.Min(Math.Clamp(attack.MaxTargets,1,MaxTargetCount),contacts.Length);
        Vector2 origin=source.CombatNode.GlobalPosition;
        float radius=attack.Reach+64; // Actor hurt radii clamp to 60, independent of feet colliders.
        if (_radius!=radius) { _circle.Radius=radius; _radius=radius; }
        if (_origin!=origin) { _overlap.Transform=new Transform2D(0,origin); _origin=origin; }
        if (_mask!=attack.CollisionMask) { _overlap.CollisionMask=attack.CollisionMask; _mask=attack.CollisionMask; }
        _ray.From=origin;
        var space=source.CombatNode.GetWorld2D().DirectSpaceState;
        var candidates=space.IntersectShape(_overlap,CandidateLimit); QueryCount++;
        if (candidates.Count==CandidateLimit) { OverflowCount++; return 0; }
        int selected=0;
        Selection nearestTool=default;
        float toolDistance=float.PositiveInfinity;
        float maxDistance=attack.Reach*attack.Reach;
        foreach (var entry in candidates)
        {
            if (entry["collider"].AsGodotObject() is not CollisionObject2D body || !body.IsInsideTree()
                || body.IsQueuedForDeletion() || !body.CanProcess()) continue;
            ICombatTarget actor=body as ICombatTarget;
            IToolHitTarget tool=null;
            Node2D receiver=body;
            Vector2 anchor, point;
            if (actor!=null)
            {
                if (!CombatTargeting.Eligible(source,actor,attack.Targets)) continue;
                anchor=actor.HitAnchor; point=actor.ClosestHitPoint(origin);
            }
            else
            {
                if (!attack.HitTools) continue;
                if (body is IToolHitTarget direct && body.IsInGroup("world_tool_targets")) tool=direct;
                for (int i=0;tool==null && i<body.GetChildCount();i++)
                    if (body.GetChild(i) is Node2D child && child is IToolHitTarget helper && child.IsInGroup("world_tool_targets")
                        && !child.IsQueuedForDeletion() && child.CanProcess()) { tool=helper; receiver=child; }
                if (tool==null) continue;
                anchor=tool.HitAnchor; point=tool.ClosestHitPoint(origin);
            }
            if (!anchor.IsFinite() || !point.IsFinite()) continue;
            float distance=origin.DistanceSquaredTo(point);
            Vector2 direction=anchor-origin;
            if (distance>maxDistance || (direction.LengthSquared()>0.01f && direction.Normalized().Dot(aim)<attack.MinimumDot)) continue;
            ulong id=body.GetInstanceId();
            if (actor!=null)
            {
                bool duplicate=false;
                for (int i=0;i<selected;i++) if (_selected[i].Id==id) { duplicate=true; break; }
                if (duplicate) continue;
            }
            // In a cleave, intervening actors don't shield each other; scenery still blocks.
            // For one-target attacks preserve original obstruction behavior, including actors.
            uint rayMask=actor!=null && limit>1 ? attack.CollisionMask & ~ActorLayer : attack.CollisionMask;
            if (rayMask!=0)
            {
                if (_ray.CollisionMask!=rayMask) _ray.CollisionMask=rayMask;
                _ray.To=point;
                var obstruction=space.IntersectRay(_ray);
                if (obstruction.Count!=0 && obstruction["collider"].AsGodotObject()!=body) continue;
            }
            Selection found=new() { Receiver=receiver,Actor=actor,Tool=tool,Point=point,Distance=distance,Id=id };
            if (tool!=null)
            {
                if (distance<toolDistance || (distance==toolDistance && id<nearestTool.Id))
                { nearestTool=found; toolDistance=distance; }
                continue;
            }
            int insert=0;
            while (insert<selected && (distance>_selected[insert].Distance
                || (distance==_selected[insert].Distance && id>=_selected[insert].Id))) insert++;
            if (insert>=limit) continue;
            for(int k=Math.Min(selected,limit-1);k>insert;k--) _selected[k]=_selected[k-1];
            _selected[insert]=found;
            if(selected<limit) selected++;
        }

        // Tools retain one hit only. The nearest tool wins only when closer than the closest actor.
        if (nearestTool.Tool!=null && (selected==0 || toolDistance<_selected[0].Distance
            || (toolDistance==_selected[0].Distance && nearestTool.Id<_selected[0].Id)))
        {
            if (nearestTool.Receiver.IsQueuedForDeletion() || !nearestTool.Receiver.IsInsideTree()) return 0;
            if (nearestTool.Tool is IHarvestHitTarget harvestTarget)
                harvestTarget.ReceiveHarvestHit(new(source.CombatNode,toolWork,aim,nearestTool.Point));
            else nearestTool.Tool.ReceiveHit(aim);
            contacts[0]=new(nearestTool.Receiver,null,source.CombatNode,nearestTool.Point,aim);
            return 1;
        }

        int hitCount=0;
        for(int i=0;i<selected;i++)
        {
            Selection choice=_selected[i];
            if (!GodotObject.IsInstanceValid(choice.Receiver) || choice.Receiver.IsQueuedForDeletion()
                || !choice.Receiver.IsInsideTree() || !CombatTargeting.Eligible(source,choice.Actor,attack.Targets)) continue;
            var result=choice.Actor.ReceiveDamage(new DamageRequest(attack.Damage,origin),stats);
            var contact=new AttackContact(choice.Receiver,result,source.CombatNode,choice.Point,aim);
            contacts[hitCount++]=contact;
            if (result.AppliedDamage>0 && choice.Actor is ICombatImpactTarget impactTarget) impactTarget.ReceiveImpact(contact);
            if (result.AppliedDamage>0 && !result.Killed && CombatTargeting.Active(choice.Actor)
                && choice.Actor is IReactionTarget reactionTarget) reactionTarget.ReceiveReaction(attack,aim);
        }
        return hitCount;
    }
    #endregion

    // Dispose only the native shapes/parameters; movement colliders belong to the actors.
    // =========================================================
    public void Dispose() { _overlap.Dispose(); _ray.Dispose(); _circle.Dispose(); }
}
