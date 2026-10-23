// Optional reusable ground-pursuit behaviour; never required by neutral or passive entities.
using Godot;
using System;
public partial class GroundChase : Node
{
    public bool Enabled { get; set; }
    public AttackExecution Attack { get; private set; }
    private AttackProfile _defaultAttack;
    private MovementMasterConfig _movement;
    public Separation SeparationState { get; } = new();
    private float _blockedSeconds;
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
        _movement=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
        SetPhysicsProcess(Enabled);
    }
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        SeparationState.Clear(); _blockedSeconds=0;
        _target = target;
        _navigation = navigation;
        _repathTimer = 0f;
        Attack?.Cancel();
        if (Enabled && _actor.Config.MeleeEnabled) EnsureAttack();
        SetPhysicsProcess((Enabled && target != null) || _actor.Reaction.NeedsUpdate);
    }
    // Optional melee allocates one executor only when enabled, including later config reactivation.
    // =========================================================
    private void EnsureAttack()
    {
        if (Attack!=null) return;
        Attack=new AttackExecution(_actor); _defaultAttack=GD.Load<AttackProfile>("res://COMBAT/Weapons/DefaultEnemyMelee.tres");
    }
    // Reuse this motor for event-driven passive recoil; sleep again when it expires.
    // =========================================================
    public void WakeReaction() => SetPhysicsProcess(true);
    // Stop pursuit and pending contact together on death.
    // =========================================================
    public void Stop()
    {
        SeparationState.Clear(); _blockedSeconds=0; Attack?.Cancel(); _actor?.Reaction.Clear();
        SetPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity = Vector2.Zero;
    }
    // Process-mode suspension (streaming/scene ownership) invalidates any pending attack.
    // =========================================================
    public override void _Notification(int what)
    {
        if (what!=NotificationDisabled) return;
        SeparationState.Clear(); _blockedSeconds=0; _actor?.Reaction.Clear(); Attack?.Cancel(); _path=Array.Empty<Vector2>(); _index=0; _repathTimer=0;
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero;
    }
    // Dispose contact-query resources on death cleanup/unload.
    // =========================================================
    public override void _ExitTree() { Attack?.Dispose(); SeparationState.Dispose(); }
    #endregion
    #region Pursuit
    // Cached static-obstacle route, equivalent to the original Basic Skeleton chasing.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundChase.PhysicsProcess");
        if (_actor.Reaction.NeedsUpdate && _actor.HealthState.IsAlive)
        {
            bool staggered=_actor.Reaction.Staggered;
            Vector2 recoil=_actor.Reaction.Advance((float)delta);
            if (staggered || recoil!=Vector2.Zero)
            {
                SeparationState.Clear(); _blockedSeconds=0;
                if (_actor.Config.MeleeEnabled) Attack?.Advance(delta); else Attack?.Cancel();
                Vector2 reactionNext=_actor.GlobalPosition+recoil*(float)delta;
                if ((_navigation==null || _navigation.Contains(reactionNext)) && !(_target is PlayerMovement p && p.Streaming!=null && !p.Streaming.IsPositionLoaded(reactionNext)))
                { _actor.Velocity=recoil; _actor.MoveAndSlide(); }
                else _actor.Velocity=Vector2.Zero;
                _repathTimer=0;
                if (!_actor.Reaction.NeedsUpdate && !Enabled) SetPhysicsProcess(false);
                return;
            }
        }
        if (!Enabled && !_actor.Reaction.NeedsUpdate) SetPhysicsProcess(false);
        if (!Enabled || !GodotObject.IsInstanceValid(_actor) || !_actor.HealthState.IsAlive
            || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || _target.IsQueuedForDeletion()
            || (_target is ICombatTarget dead && !CombatTargeting.Active(dead)))
        { SeparationState.Clear(); Attack?.Cancel(); if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero; return; }
        if (!_actor.Config.MeleeEnabled) Attack?.Cancel();
        else { EnsureAttack(); Attack.Advance(delta); }
        EntityDefinition config = _actor.Config;
        float distanceSquared = _actor.GlobalPosition.DistanceSquaredTo(_target.GlobalPosition);
        if (Attack?.IsSwinging==true) { SeparationState.Clear(); _actor.Velocity=Vector2.Zero; return; }
        // Pursuit supplies intent only; the same executor as the player owns contacts and cooldown.
        var profile=config.MeleeAttack ?? _defaultAttack;
        if (config.MeleeEnabled && Attack!=null && !Attack.Busy && profile!=null && _target is ICombatTarget target
            && distanceSquared<=config.DetectionRadius*config.DetectionRadius
            && CombatTargeting.Eligible(_actor,target,profile.TargetFactions)
            && _actor.GlobalPosition.DistanceSquaredTo(target.ClosestHitPoint(_actor.GlobalPosition))<=profile.HitReach*profile.HitReach
            && Attack.TryStart(profile,_target.GlobalPosition-_actor.GlobalPosition))
        { SeparationState.Clear(); _actor.Velocity=Vector2.Zero; return; }
        if (_navigation==null || distanceSquared > config.DetectionRadius * config.DetectionRadius ||
            distanceSquared <= config.StopDistance * config.StopDistance)
        {
            SeparationState.Clear();
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
        if (_path.Length == 0) { SeparationState.Clear(); _actor.Velocity = Vector2.Zero; return; }
        const float waypointRadius = 12f;
        while (_index < _path.Length - 1 &&
               _actor.GlobalPosition.DistanceSquaredTo(_path[_index]) <= waypointRadius * waypointRadius)
            _index++;
        Vector2 goal = _path[_index];
        if (_index == _path.Length - 1 && goal.DistanceSquaredTo(_target.GlobalPosition) < 30f * 30f)
            goal = _target.GlobalPosition;
        Vector2 heading = goal - _actor.GlobalPosition;
        _actor.Velocity = heading.LengthSquared() <= 1f ? Vector2.Zero : heading.Normalized() * config.MoveSpeed * _actor.Stats.Values.MovementSpeedMultiplier;
        Vector2 intent=_actor.Velocity;
        float fade=Mathf.Clamp((Mathf.Sqrt(distanceSquared)-Mathf.Max(config.StopDistance,config.MeleeEnabled ? profile?.HitReach ?? 0 : 0))/24f,0,1);
        _actor.Velocity=SeparationState.Blend(_actor,_target,intent,(float)delta,_movement.Enabled && _blockedSeconds<0.5f,fade);
        Vector2 before=_actor.GlobalPosition;
        Vector2 next=_actor.GlobalPosition+_actor.Velocity*(float)delta;
        if (!_navigation.Contains(next) || (_target is PlayerMovement player && player.Streaming!=null && !player.Streaming.IsPositionLoaded(next)))
            _actor.Velocity=Vector2.Zero;
        _actor.MoveAndSlide();
        // Retry the static route without steering after sustained poor progress against scenery.
        float progress=(_actor.GlobalPosition-before).Dot(intent.Normalized());
        _blockedSeconds=progress<intent.Length()*(float)delta*0.2f ? _blockedSeconds+(float)delta : 0;
        if(_blockedSeconds>0.9f) _blockedSeconds=0;
    }
    #endregion
}
