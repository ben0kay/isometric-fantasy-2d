// One range/cone query per swing; receivers own their response, not the equipped weapon.
using Godot;

public interface IWeaponHitTarget
{
    Vector2 ClosestHitPoint(Vector2 origin);
    Vector2 HitAnchor { get; }
    void ReceiveHit(Vector2 direction);
}

public partial class WeaponHitQuery : Node
{
    public int HitCount { get; private set; }

    // Select the nearest trunk surface in the aimed sector; never hit every nearby tree.
    // =========================================================
    public void Strike(Vector2 origin, Vector2 aim, float reach, float halfAngleDegrees)
    {
        float closest = Mathf.Max(0f, reach) * Mathf.Max(0f, reach);
        float minimumDot = Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(halfAngleDegrees, 1f, 89f)));
        IWeaponHitTarget selected = null;
        foreach (Node node in GetTree().GetNodesInGroup("debug_weapon_hit_targets"))
        {
            if (node is not IWeaponHitTarget target) continue;
            Vector2 direction = target.HitAnchor - origin;
            if (direction.LengthSquared() > 0.01f && direction.Normalized().Dot(aim) < minimumDot) continue;
            float distance = origin.DistanceSquaredTo(target.ClosestHitPoint(origin));
            if (distance > closest) continue;
            closest = distance; selected = target;
        }
        if (selected == null) return;
        selected.ReceiveHit(aim); HitCount++;
    }
}
