// Shared nearest eligible tool target check, once per swing; no input or species knowledge.
using Godot;
public interface IToolHitTarget
{
    Vector2 ClosestHitPoint(Vector2 origin);
    Vector2 HitAnchor { get; }
    void ReceiveHit(Vector2 direction);
}
public partial class ToolHitQuery : Node
{
    public int HitCount { get; private set; }
    // =========================================================
    public void Strike(Vector2 origin, Vector2 aim, float reach, float halfAngleDegrees)
    {
        float closest = Mathf.Max(0f, reach) * Mathf.Max(0f, reach);
        float minimumDot = Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(halfAngleDegrees, 1f, 89f)));
        IToolHitTarget selected = null;
        foreach (Node node in GetTree().GetNodesInGroup("world_tool_targets"))
        {
            if (node is not IToolHitTarget target) continue;
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