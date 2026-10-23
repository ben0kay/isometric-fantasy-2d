// A solid tool receiver proves arrows stop on props without invoking wood-chopping contracts.
using Godot;
public partial class ProjectileObstacleProbe : StaticBody2D, IToolHitTarget
{
    public int Hits { get; private set; }
    public Vector2 HitAnchor => GlobalPosition;
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin) => GlobalPosition;
    // =========================================================
    public void ReceiveHit(Vector2 direction) => Hits++;
}
