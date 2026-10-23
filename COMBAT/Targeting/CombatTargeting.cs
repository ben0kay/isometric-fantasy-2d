// Minimal receiving geometry and faction policy; no species branches, world paths or global registry.
using Godot;
using System;
[Flags]
public enum CombatFaction { Player=1, Hostile=2, Neutral=4 }
public interface ICombatTarget : ICombatant
{
    Node2D CombatNode { get; }
    CombatFaction Faction { get; }
    Vector2 HitAnchor { get; }
    Vector2 ClosestHitPoint(Vector2 origin);
}
public static class CombatTargeting
{
    // Queued/unloaded/dead/disabled actors never start attacks or receive new contacts.
    // =========================================================
    public static bool Active(ICombatTarget actor)
    {
        var node=actor?.CombatNode;
        return GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion()
            && node.CanProcess() && node.GlobalPosition.IsFinite() && actor.HealthState?.IsAlive==true
            && actor.Faction is CombatFaction.Player or CombatFaction.Hostile or CombatFaction.Neutral;
    }
    // Same-faction and self hits are always excluded; profiles opt into receiving factions.
    // =========================================================
    public static bool Eligible(ICombatTarget source, ICombatTarget target, CombatFaction mask)
        => Active(source) && Active(target) && source.CombatNode!=target.CombatNode && source.Faction!=target.Faction
            && (target.Faction & mask)!=0 && source.CombatNode.GetWorld2D()==target.CombatNode.GetWorld2D();
    // Actor hurt geometry is independent from movement collision; authored radii stay bounded for broad-phase padding.
    // =========================================================
    public static Vector2 ClosestPoint(Vector2 origin, Vector2 anchor, float radius)
    {
        Vector2 offset=origin-anchor; radius=Mathf.Clamp(radius,3,60);
        return offset.LengthSquared()<=radius*radius ? origin : anchor+offset.Normalized()*radius;
    }
}
