// Event-controlled directional mitigation. No HP, callbacks, polling, faction or player dependencies.
using Godot;
public sealed class DirectionalGuard
{
    public GuardProfile Profile { get; }
    public bool Active { get; private set; }
    public float MovementMultiplier => float.IsFinite(Profile.MovementMultiplier) ? Mathf.Clamp(Profile.MovementMultiplier,0.1f,1) : 1;
    public Vector2 Facing { get; private set; } = Vector2.Right;
    // Shared authored settings are read without mutating the Resource.
    // =========================================================
    public DirectionalGuard(GuardProfile profile) { Profile=profile; }
    // An actor/action adapter explicitly enables the equipped guard.
    // =========================================================
    public void Start(Vector2 facing) { Face(facing); Active=true; }
    // Aim can change while guarding without allocating or recalculating actor stats.
    // =========================================================
    public void Face(Vector2 facing) { if (facing.IsFinite() && facing.LengthSquared()>0.001f) Facing=facing.Normalized(); }
    // Release, dodge and actor teardown stop mitigation immediately.
    // =========================================================
    public void Stop() => Active=false;
    // Unknown-origin, rear and coincident contacts receive no directional mitigation.
    // =========================================================
    public float ReductionFor(DamageRequest request, Vector2 position)
    {
        if (!Active || !float.IsFinite(Profile.HalfAngleDegrees) || request.Origin is not Vector2 origin || !origin.IsFinite()) return 0;
        Vector2 incoming=origin-position;
        if (incoming.LengthSquared()<0.001f || incoming.Normalized().Dot(Facing)<Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(Profile.HalfAngleDegrees,1,89)))) return 0;
        return float.IsFinite(Profile.Reduction) ? Mathf.Clamp(Profile.Reduction,0,1) : 0;
    }
}
