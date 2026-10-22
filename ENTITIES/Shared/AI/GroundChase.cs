// Optional reusable ground-pursuit behaviour; never required by neutral or passive entities.
using Godot;
using System;
public partial class GroundChase : Node
{
    public bool Enabled { get; set; }
    private Entity _actor;
    private CharacterBody2D _target;
    private StaticObstacleNavigation _navigation;
    private Vector2[] _path = Array.Empty<Vector2>();
    private int _index;
    private float _repathTimer;
    #region Setup
    // Do not tick passive entities or entities without navigation.
    // =========================================================
    public override void _Ready()
    {
        _actor = GetParent<Entity>();
        SetPhysicsProcess(Enabled);
    }
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        _target = target;
        _navigation = navigation;
        _repathTimer = 0f;
        SetPhysicsProcess(Enabled && target != null && navigation != null);
    }
    // =========================================================
    public void Stop()
    {
        SetPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity = Vector2.Zero;
    }
    #endregion
    #region Pursuit
    // Cached static-obstacle route, equivalent to the original Basic Skeleton chasing.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundChase.PhysicsProcess");
        if (!Enabled || !GodotObject.IsInstanceValid(_actor) || !GodotObject.IsInstanceValid(_target) || _navigation == null) return;
        EntityDefinition config = _actor.Config;
        float distanceSquared = _actor.GlobalPosition.DistanceSquaredTo(_target.GlobalPosition);
        if (distanceSquared > config.DetectionRadius * config.DetectionRadius ||
            distanceSquared <= config.StopDistance * config.StopDistance)
        {
            _actor.Velocity = Vector2.Zero;
            _repathTimer = 0f;
            return;
        }
        _repathTimer -= (float)delta;
        if (_repathTimer <= 0f)
        {
            _path = _navigation.FindPath(_actor.GlobalPosition, _target.GlobalPosition);
            _index = 0;
            _repathTimer = Mathf.Max(0.15f, config.RepathSeconds);
        }
        if (_path.Length == 0) { _actor.Velocity = Vector2.Zero; return; }
        const float waypointRadius = 12f;
        while (_index < _path.Length - 1 &&
               _actor.GlobalPosition.DistanceSquaredTo(_path[_index]) <= waypointRadius * waypointRadius)
            _index++;
        Vector2 goal = _path[_index];
        if (_index == _path.Length - 1 && goal.DistanceSquaredTo(_target.GlobalPosition) < 30f * 30f)
            goal = _target.GlobalPosition;
        Vector2 heading = goal - _actor.GlobalPosition;
        _actor.Velocity = heading.LengthSquared() <= 1f ? Vector2.Zero : heading.Normalized() * config.MoveSpeed * _actor.Stats.Values.MovementSpeedMultiplier;
        _actor.MoveAndSlide();
    }
    #endregion
}
