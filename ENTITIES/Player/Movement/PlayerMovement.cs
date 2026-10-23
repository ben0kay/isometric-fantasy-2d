// Production CharacterBody2D movement: shared stats plus sprint and held-direction dodge physics.
using Godot;

public partial class PlayerMovement : CharacterBody2D, ICombatTarget, IReactionTarget
{
    [ExportGroup("References")]
    [Export] public PlayerMasterConfig Config { get; set; }
    public float SurfaceSpeedMultiplier { get; set; } = 1f;
    public WorldChunkStream Streaming { get; set; }
    public Node2D CombatNode => this;
    public CombatFaction Faction => CombatFaction.Player;
    public Vector2 HitAnchor => GlobalPosition;
    // Explicit receiving geometry stays independent of the movement rectangle.
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin) => CombatTargeting.ClosestPoint(origin,GlobalPosition,12);
    public ActorHealth HealthState { get; private set; }
    public ActorStats Stats { get; private set; }
    public PlayerActions Actions { get; private set; }
    public DirectionalGuard Guard { get; private set; }
    public Knockback Reaction { get; } = new();
    public bool IsDodging => _dodgeRemaining>0;
    public bool IsHopping => _hopRemaining>0;
    public float HopHeight { get; private set; }
    private Sprite2D _art;
    private Vector2 _artOffset;
    private float _hopRemaining, _hopCooldown, _hopDuration;
    private PlayerInput _input;
    private Vector2 _dodgeDirection;
    private float _dodgeRemaining, _dodgeCooldown;

    #region Lifecycle
    // Initialize before child equipment subscribes; health and stats have no ticking nodes.
    // =========================================================
    public override void _EnterTree()
    {
        Config ??= new PlayerMasterConfig();
        Actions=new PlayerActions(); Guard=new DirectionalGuard(Config.ShieldGuard ?? GD.Load<GuardProfile>("res://COMBAT/Defense/DefaultGuard.tres"));
        HealthState = new ActorHealth(Config.MaxHealth); Stats = new ActorStats(Config.CombatStats);
        HealthState.Changed += OnHealthChanged;
    }
    // Shared damage entry point; only eligible directional guard changes C1 incoming damage.
    // =========================================================
    public DamageResult ReceiveDamage(DamageRequest request, EffectiveStats attacker)
        => DamageResolver.Apply(HealthState,request,attacker,Stats.Values,Guard.ReductionFor(request,GlobalPosition));
    // Death stops movement/input and cancels any current dodge.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (!HealthState.IsAlive)
        {
            ResetActions();
        }
        GetNodeOrNull<CollisionShape2D>("Collision")?.SetDeferred("disabled", !HealthState.IsAlive);
        SetPhysicsProcess(HealthState.IsAlive);
        _input?.SetProcessUnhandledInput(HealthState.IsAlive);
    }
    // Release input event subscriptions and shared stat listeners on unload.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.DodgeRequested -= RequestDodge;
        HealthState.Changed -= OnHealthChanged; Stats.Dispose();
    }
    // Cache input; camera child owns framing and wheel zoom.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new PlayerMasterConfig();
        _art=GetNode<Sprite2D>("Sprite"); _artOffset=_art.Offset;
        _input = GetNode<PlayerInput>("Input");
        _input.DodgeDoubleTapSeconds = Config.DodgeDoubleTapSeconds;
        _input.DodgeRequested += RequestDodge;
        MotionMode = MotionModeEnum.Floating;
        GetNode<SharedHealthBar>("HealthBar").Bind(HealthState, GetNode<Sprite2D>("Sprite"));
        var numbers = new DamageNumbers { Name = "DamageNumbers", ZIndex = 5 };
        AddChild(numbers);
        numbers.Bind(HealthState, _art);
    }
    #endregion

    // Damaging reaction wins over dodge/guard; visual hops finish normally; attack interruption is explicitly authored.
    // =========================================================
    public void ReceiveReaction(AttackValues attack, Vector2 direction)
    {
        if (!CombatTargeting.Active(this) || !Reaction.Apply(attack,direction,Config.ReactionImmune,Config.KnockbackResistance,Config.StaggerResistance)) return;
        StopGuard(); _dodgeRemaining=0; Actions.End(PlayerAction.Dodge);
        if (attack.InterruptAttack && Reaction.Staggered) GetNodeOrNull<EquippedTool>("EquippedAxe")?.Attack?.Interrupt();
    }
    // Shield guarding is a shared defensive state, gated by this actor's single action owner.
    // =========================================================
    public bool TryGuard(Vector2 facing)
    { if (!HealthState.IsAlive || Reaction.Active || !Actions.Begin(PlayerAction.Block)) return false; Guard.Start(facing); return true; }
    // Releases and successful dodges stop mitigation immediately.
    // =========================================================
    public void StopGuard() { Guard.Stop(); Actions.End(PlayerAction.Block); }
    // Visual hop changes texture offset only; surface burial owns Sprite.Position independently.
    // =========================================================
    public bool TryHop()
    {
        if (!HealthState.IsAlive || Reaction.Active || _hopCooldown>0 || Config.HopHeight<=0 || Config.HopSeconds<=0 || !float.IsFinite(Config.HopSeconds) || !float.IsFinite(Config.HopHeight)
            || !float.IsFinite(Config.HopCooldownSeconds) || !Actions.Begin(PlayerAction.Hop)) return false;
        _hopDuration=Mathf.Clamp(Config.HopSeconds,0.1f,1); _hopRemaining=_hopDuration;
        _hopCooldown=_hopDuration+Mathf.Clamp(Config.HopCooldownSeconds,0,5); return true;
    }
    // Suspension never resumes a stale dodge, guard or hop. The equipment adapter cancels its own attack.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationDisabled) ResetActions(); }
    // Restore visual offsets and discard transient actions without changing saved HP/equipment definitions.
    // =========================================================
    private void ResetActions()
    {
        Reaction.Clear(); Actions?.Reset(); Guard?.Stop(); Velocity=Vector2.Zero; _dodgeRemaining=0; _hopRemaining=0; HopHeight=0;
        if (GodotObject.IsInstanceValid(_art)) _art.Offset=_artOffset;
        _input?.ResetMovementActions();
    }
    #region Dodge and movement
    // Event-driven activation; direction is captured from held movement at the second Shift press.
    // =========================================================
    private void RequestDodge(Vector2 direction)
    {
        if (!HealthState.IsAlive || Reaction.Active || !Config.DodgeEnabled || _dodgeCooldown > 0f || _dodgeRemaining > 0f
            || direction.LengthSquared() < 0.5f) return;
        StopGuard();
        if (!Actions.Begin(PlayerAction.Dodge)) return;
        _dodgeDirection = direction.Normalized();
        _dodgeRemaining = Mathf.Max(0.01f, Config.DodgeSeconds);
        _dodgeCooldown = Mathf.Max(_dodgeRemaining, Config.DodgeCooldownSeconds);
    }

    // Resolve walking/sprint/dodge through one physics motor; streaming and collisions remain authoritative.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("PlayerMovement.PhysicsProcess");
        if (!HealthState.IsAlive) { Velocity = Vector2.Zero; return; }
        float dt = (float)delta;
        _dodgeCooldown = Mathf.Max(0f, _dodgeCooldown - dt);
        _hopCooldown=Mathf.Max(0,_hopCooldown-dt);
        if (_hopRemaining>0)
        {
            _hopRemaining=Mathf.Max(0,_hopRemaining-dt);
            HopHeight=Mathf.Sin(Mathf.Pi*(1-_hopRemaining/_hopDuration))*Mathf.Clamp(Config.HopHeight,0,60);
            if (_hopRemaining==0) { HopHeight=0; Actions.End(PlayerAction.Hop); }
            _art.Offset=_artOffset+new Vector2(0,-HopHeight/Mathf.Max(0.001f,Mathf.Abs(_art.Scale.Y)));
        }
        float speedMultiplier = Stats.Values.MovementSpeedMultiplier * Mathf.Clamp(SurfaceSpeedMultiplier, 0.1f, 1f);
        if (_dodgeRemaining > 0f)
        {
            Velocity = _dodgeDirection * Config.DodgeSpeed * speedMultiplier;
            _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - dt);
            if (_dodgeRemaining==0) Actions.End(PlayerAction.Dodge);
        }
        else
        {
            float sprint = Guard.Active ? Guard.MovementMultiplier : _input.SprintHeld ? Mathf.Max(1f, Config.SprintMultiplier) : 1f;
            Velocity = _input.ReadMovement() * Config.WalkSpeed * sprint * speedMultiplier;
        }
        if (Reaction.NeedsUpdate)
        {
            bool staggered=Reaction.Staggered;
            Vector2 recoil=Reaction.Advance(dt);
            Velocity=(staggered || recoil!=Vector2.Zero) ? recoil : Velocity;
        }
        if (Streaming != null && !Streaming.CanMoveTo(GlobalPosition + Velocity * dt)) Velocity = Vector2.Zero;
        MoveAndSlide();
    }
    #endregion
}
