// Optional reusable ground-pursuit behaviour; never required by neutral or passive entities.
using Godot;
using System;
public partial class GroundChase : Node
{
    public bool Enabled { get; set; }
    public AttackExecution Attack { get; private set; }
    private AttackProfile _defaultAttack;
    private Entity _actor;
    private CharacterBody2D _target;
    private StaticObstacleNavigation _navigation;
    private Vector2[] _path = Array.Empty<Vector2>();
    private int _index;
    private float _repathTimer;
    #region Setup
    // Passive entities sleep; navigation injection explicitly activates pursuit/nearby attack intent.
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
        Attack?.Cancel();
        if (Enabled && _actor.Config.MeleeEnabled) EnsureAttack();
        SetPhysicsProcess(Enabled && target != null);
    }
    // Optional melee allocates one executor only when enabled, including later config reactivation.
    // =========================================================
    private void EnsureAttack()
    {
        if (Attack!=null) return;
        Attack=new AttackExecution(_actor); _defaultAttack=GD.Load<AttackProfile>("res://COMBAT/Weapons/DefaultEnemyMelee.tres");
    }
    // Stop pursuit and pending contact together on death.
    // =========================================================
    public void Stop()
    {
        Attack?.Cancel();
        SetPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity = Vector2.Zero;
    }
    // Process-mode suspension (streaming/scene ownership) invalidates any pending attack.
    // =========================================================
    public override void _Notification(int what)
    {
        if (what!=NotificationDisabled) return;
        Attack?.Cancel(); _path=Array.Empty<Vector2>(); _index=0; _repathTimer=0;
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero;
    }
    // Dispose contact-query resources on death cleanup/unload.
    // =========================================================
    public override void _ExitTree() => Attack?.Dispose();
    #endregion
    #region Pursuit
    // Cached static-obstacle route, equivalent to the original Basic Skeleton chasing.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundChase.PhysicsProcess");
        if (!Enabled || !GodotObject.IsInstanceValid(_actor) || !_actor.HealthState.IsAlive
            || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || _target.IsQueuedForDeletion()
            || (_target is ICombatTarget dead && !CombatTargeting.Active(dead)))
        { Attack?.Cancel(); if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero; return; }
        if (!_actor.Config.MeleeEnabled) Attack?.Cancel();
        else { EnsureAttack(); Attack.Advance(delta); }
        EntityDefinition config = _actor.Config;
        float distanceSquared = _actor.GlobalPosition.DistanceSquaredTo(_target.GlobalPosition);
        if (Attack?.IsSwinging==true) { _actor.Velocity=Vector2.Zero; return; }
        // Pursuit supplies intent only; the same executor as the player owns contacts and cooldown.
        var profile=config.MeleeAttack ?? _defaultAttack;
        if (config.MeleeEnabled && Attack!=null && !Attack.Busy && profile!=null && _target is ICombatTarget target
            && distanceSquared<=config.DetectionRadius*config.DetectionRadius
            && CombatTargeting.Eligible(_actor,target,profile.TargetFactions)
            && _actor.GlobalPosition.DistanceSquaredTo(target.ClosestHitPoint(_actor.GlobalPosition))<=profile.HitReach*profile.HitReach
            && Attack.TryStart(profile,_target.GlobalPosition-_actor.GlobalPosition))
        { _actor.Velocity=Vector2.Zero; return; }
        if (_navigation==null || distanceSquared > config.DetectionRadius * config.DetectionRadius ||
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
        Vector2 next=_actor.GlobalPosition+_actor.Velocity*(float)delta;
        if (!_navigation.Contains(next) || (_target is PlayerMovement player && player.Streaming!=null && !player.Streaming.IsPositionLoaded(next)))
            _actor.Velocity=Vector2.Zero;
        _actor.MoveAndSlide();
    }
    #endregion
}
