// Optional reusable ground-pursuit behaviour; never required by neutral or passive entities.
using Godot;
using System;
public partial class GroundChase : Node
{
    public bool Enabled { get; set; }
    public AttackExecution Attack { get; private set; }
    public int ConfirmedMeleeContacts { get; private set; }
    private AttackProfile _defaultAttack;
    public RangedCombatAI Ranged { get; private set; }
    private ProjectileSpawner _projectiles;
    private RangedCombatProfile _rangedProfile;
    private bool _rangedBound;
    private MovementMasterConfig _movement;
    public EngagementPositioning EngagementState { get; } = new();
    private CombatMasterConfig _combat;
    private EngagementProfile _engagementProfile;
    private bool _engagementEnabled;
    private Vector2 _destination;
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
        _combat=GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres");
        _movement=GD.Load<MovementMasterConfig>("res://ENTITIES/Shared/Movement/DefaultMovementConfig.tres");
        SetPhysicsProcess(Enabled);
    }
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        SeparationState.Clear(); EngagementState.Clear(); _blockedSeconds=0;
        bool targetChanged=_target!=target;
        _target = target;
        _navigation = navigation;
        _repathTimer = 0f;
        Attack?.Cancel(); if(targetChanged) ResetRanged(); else Ranged?.Cancel();
        if (_actor.Config.Ranged==null && Enabled && (_actor.Config.MeleeEnabled && _combat.EnemyMeleeEnabled)) EnsureAttack();
        SetPhysicsProcess((Enabled && target != null && (_actor.Config.Ranged==null || GodotObject.IsInstanceValid(_projectiles))) || _actor.Reaction.NeedsUpdate);
    }
    // No optional module is allocated until an enabled ranged actor has explicit services.
    // =========================================================
    public void ConfigureRanged(ProjectileSpawner spawner)
    { _projectiles=spawner; ResetRanged(); _repathTimer=0; _actor.Velocity=Vector2.Zero; SetPhysicsProcess((Enabled && _target!=null && (_actor.Config.Ranged==null || GodotObject.IsInstanceValid(spawner))) || _actor.Reaction.NeedsUpdate); }
    // =========================================================
    private void ResetRanged() { Ranged?.Dispose(); Ranged=null; _rangedBound=false; }
    // =========================================================
    private void EnsureRanged()
    {
        if(_actor.Config.Ranged!=_rangedProfile) { ResetRanged(); _rangedProfile=_actor.Config.Ranged; }
        if(_rangedBound || !GodotObject.IsInstanceValid(_projectiles)) return;
        _rangedBound=true;
        try { Ranged=new RangedCombatAI(_actor,_target,_projectiles,_rangedProfile); }
        catch(ArgumentException error) { GD.PushWarning(error.Message); }
    }
    // Optional melee allocates one executor only when enabled, including later config reactivation.
    // =========================================================
    private void EnsureAttack()
    {
        if (Attack!=null) return;
        Attack=new AttackExecution(_actor); Attack.Contact+=_=>ConfirmedMeleeContacts++; _defaultAttack=GD.Load<AttackProfile>("res://COMBAT/Weapons/DefaultEnemyMelee.tres");
    }
    // Reuse this motor for event-driven passive recoil; sleep again when it expires.
    // =========================================================
    public void WakeReaction() => SetPhysicsProcess(true);
    // Stop pursuit and pending contact together on death.
    // =========================================================
    public void Stop()
    {
        SeparationState.Clear(); EngagementState.Clear(); _blockedSeconds=0; Attack?.Cancel(); Ranged?.Cancel(true); _actor?.Reaction.Clear();
        SetPhysicsProcess(false);
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity = Vector2.Zero;
    }
    // Process-mode suspension (streaming/scene ownership) invalidates any pending attack.
    // =========================================================
    public override void _Notification(int what)
    {
        if (what!=NotificationDisabled) return;
        SeparationState.Clear(); EngagementState.Clear(); _blockedSeconds=0; _actor?.Reaction.Clear(); Ranged?.Cancel(true); Attack?.Cancel(); _path=Array.Empty<Vector2>(); _index=0; _repathTimer=0;
        if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero;
    }
    // Dispose contact-query resources on death cleanup/unload.
    // =========================================================
    public override void _ExitTree() { Attack?.Dispose(); Ranged?.Dispose(); SeparationState.Dispose(); EngagementState.Dispose(); }
    #endregion
    #region Pursuit
    // Cached static-obstacle route, equivalent to the original Basic Skeleton chasing.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GroundChase.PhysicsProcess");
        bool pursuitReady=Enabled && (_actor.Config.Ranged==null || (GodotObject.IsInstanceValid(_projectiles) && _projectiles.IsInsideTree()));
        var engagement=_actor.Config.Engagement ?? _movement.EngagementDefaults;
        bool useEngagement=_movement.EngagementEnabled && engagement?.Mode!=EngagementMode.Direct;
        if(useEngagement!=_engagementEnabled || engagement!=_engagementProfile)
        { EngagementState.Clear(); _repathTimer=0; _engagementEnabled=useEngagement; _engagementProfile=engagement; }
        if(!useEngagement) EngagementState.Clear();
        _actor.Reaction.Suppress(!_combat.KnockbackEnabled,!_combat.StaggerEnabled);
        if (_actor.Reaction.NeedsUpdate && _actor.HealthState.IsAlive)
        {
            bool staggered=_actor.Reaction.Staggered;
            Vector2 recoil=_actor.Reaction.Advance((float)delta);
            if (staggered || recoil!=Vector2.Zero)
            {
                SeparationState.Clear(); EngagementState.Clear(); _blockedSeconds=0;
                Ranged?.Pause((float)delta);
                if (_actor.Config.Ranged==null && (_actor.Config.MeleeEnabled && _combat.EnemyMeleeEnabled)) Attack?.Advance(delta); else Attack?.Cancel();
                Vector2 reactionNext=_actor.GlobalPosition+recoil*(float)delta;
                if ((_navigation==null || _navigation.Contains(reactionNext)) && !(_target is PlayerMovement p && p.Streaming!=null && !p.Streaming.IsPositionLoaded(reactionNext)))
                { _actor.Velocity=recoil; _actor.MoveAndSlide(); }
                else _actor.Velocity=Vector2.Zero;
                _repathTimer=0;
                if (!_actor.Reaction.NeedsUpdate && !pursuitReady) SetPhysicsProcess(false);
                return;
            }
        }
        if (!pursuitReady && !_actor.Reaction.NeedsUpdate) SetPhysicsProcess(false);
        if (!pursuitReady || !GodotObject.IsInstanceValid(_actor) || !_actor.HealthState.IsAlive
            || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || _target.IsQueuedForDeletion()
            || (_target is ICombatTarget dead && !CombatTargeting.Active(dead)))
        { Ranged?.Cancel(); EngagementState.Clear(); SeparationState.Clear(); Attack?.Cancel(); if (GodotObject.IsInstanceValid(_actor)) _actor.Velocity=Vector2.Zero; return; }
        EntityDefinition config=_actor.Config;
        float distanceSquared=_actor.GlobalPosition.DistanceSquaredTo(_target.GlobalPosition);
        bool ranged=config.Ranged!=null;
        AttackProfile profile=null;
        if(ranged)
        {
            Attack?.Cancel(); EngagementState.Clear();
            if(distanceSquared>config.DetectionRadius*config.DetectionRadius) { Ranged?.Cancel(); _actor.Velocity=Vector2.Zero; return; }
            EnsureRanged();
            if(Ranged==null || Ranged.Advance((float)delta,_navigation,config.Ranged.Enabled && _combat.EnemyRangedEnabled))
            { SeparationState.Clear(); _actor.Velocity=Vector2.Zero; _repathTimer=0; return; }
        }
        else
        {
            if(Ranged!=null) ResetRanged();
            if (!(_actor.Config.MeleeEnabled && _combat.EnemyMeleeEnabled)) Attack?.Cancel();
            else { EnsureAttack(); Attack.Advance(delta); }
            if (Attack?.IsSwinging==true) { SeparationState.Clear(); _actor.Velocity=Vector2.Zero; return; }
            // Pursuit supplies intent only; the same executor as the player owns contacts and cooldown.
            profile=config.MeleeAttack ?? _defaultAttack;
            if ((config.MeleeEnabled && _combat.EnemyMeleeEnabled) && Attack!=null && !Attack.Busy && profile!=null && _target is ICombatTarget target
                && distanceSquared<=config.DetectionRadius*config.DetectionRadius
                && CombatTargeting.Eligible(_actor,target,profile.TargetFactions)
                && _actor.GlobalPosition.DistanceSquaredTo(target.ClosestHitPoint(_actor.GlobalPosition))<=profile.HitReach*profile.HitReach
                && Attack.TryStart(profile,_target.GlobalPosition-_actor.GlobalPosition))
            { SeparationState.Clear(); _actor.Velocity=Vector2.Zero; return; }
        }
        if (_navigation==null || distanceSquared > config.DetectionRadius * config.DetectionRadius ||
            (!ranged && distanceSquared <= config.StopDistance * config.StopDistance))
        {
            if(_navigation==null || distanceSquared>config.DetectionRadius*config.DetectionRadius) EngagementState.Clear();
            SeparationState.Clear();
            _actor.Velocity = Vector2.Zero;
            _repathTimer = 0f;
            return;
        }
        _repathTimer -= (float)delta;
        if (_repathTimer <= 0f)
        {
            _destination=ranged ? Ranged.Destination : useEngagement ? EngagementState.Select(_actor,_target,_navigation,engagement,profile?.HitReach ?? config.StopDistance) : _target.GlobalPosition;
            _path = _navigation.FindPath(_actor.GlobalPosition, _destination);
            _index = 0;
            _repathTimer = Mathf.Max(0.15f, config.RepathSeconds);
        }
        if (_path.Length == 0) { SeparationState.Clear(); _actor.Velocity = Vector2.Zero; return; }
        const float waypointRadius = 12f;
        while (_index < _path.Length - 1 &&
               _actor.GlobalPosition.DistanceSquaredTo(_path[_index]) <= waypointRadius * waypointRadius)
            _index++;
        Vector2 goal = _path[_index];
        if (_index == _path.Length - 1 && goal.DistanceSquaredTo(_destination) < 30f * 30f)
            goal = _destination;
        Vector2 heading = goal - _actor.GlobalPosition;
        _actor.Velocity = heading.LengthSquared() <= 1f ? Vector2.Zero : heading.Normalized() * config.MoveSpeed * _actor.Stats.Values.MovementSpeedMultiplier;
        Vector2 intent=_actor.Velocity;
        float fade=ranged ? 1 : Mathf.Clamp((Mathf.Sqrt(distanceSquared)-Mathf.Max(config.StopDistance,(config.MeleeEnabled && _combat.EnemyMeleeEnabled) ? profile?.HitReach ?? 0 : 0))/24f,0,1);
        _actor.Velocity=SeparationState.Blend(_actor,_target,intent,(float)delta,_movement.Enabled && _blockedSeconds<0.5f,fade);
        Vector2 before=_actor.GlobalPosition;
        Vector2 next=_actor.GlobalPosition+_actor.Velocity*(float)delta;
        if (!_navigation.Contains(next) || (_target is PlayerMovement player && player.Streaming!=null && !player.Streaming.IsPositionLoaded(next)))
            _actor.Velocity=Vector2.Zero;
        _actor.MoveAndSlide();
        // Retry the static route without steering after sustained poor progress against scenery.
        float progress=(_actor.GlobalPosition-before).Dot(intent.Normalized());
        EngagementState.Advance((float)delta,progress,intent.Length(),engagement);
        _blockedSeconds=progress<intent.Length()*(float)delta*0.2f ? _blockedSeconds+(float)delta : 0;
        if(_blockedSeconds>0.9f) _blockedSeconds=0;
    }
    #endregion
}
