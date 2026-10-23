// Production CharacterBody2D movement: shared stats plus sprint and cursor-directed dodge physics.
using Godot;

public partial class PlayerMovement : CharacterBody2D, ICombatant
{
    [ExportGroup("References")]
    [Export] public PlayerMasterConfig Config { get; set; }
    public float SurfaceSpeedMultiplier { get; set; } = 1f;
    public WorldChunkStream Streaming { get; set; }
    public ActorHealth HealthState { get; private set; }
    public ActorStats Stats { get; private set; }
    private PlayerInput _input;
    private Vector2 _dodgeDirection;
    private float _dodgeRemaining, _dodgeCooldown;

    #region Lifecycle
    // Initialize before child equipment subscribes; health and stats have no ticking nodes.
    // =========================================================
    public override void _EnterTree()
    {
        Config ??= new PlayerMasterConfig();
        HealthState = new ActorHealth(Config.MaxHealth); Stats = new ActorStats(Config.CombatStats);
        HealthState.Changed += OnHealthChanged;
    }
    // Shared damage entry point for future attacks.
    // =========================================================
    public DamageResult ReceiveDamage(DamageRequest request, EffectiveStats attacker)
        => DamageResolver.Apply(HealthState, request, attacker, Stats.Values);
    // Death stops movement/input and cancels any current dodge.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (!HealthState.IsAlive)
        {
            Velocity = Vector2.Zero; _dodgeRemaining = 0f;
            _input?.ResetMovementActions();
        }
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
        _input = GetNode<PlayerInput>("Input");
        _input.DodgeDoubleTapSeconds = Config.DodgeDoubleTapSeconds;
        _input.DodgeRequested += RequestDodge;
        MotionMode = MotionModeEnum.Floating;
    }
    #endregion

    #region Dodge and movement
    // Event-driven activation; direction is captured from mouse position at the second Shift press.
    // =========================================================
    private void RequestDodge(Vector2 direction)
    {
        if (!HealthState.IsAlive || !Config.DodgeEnabled || _dodgeCooldown > 0f || _dodgeRemaining > 0f
            || direction.LengthSquared() < 0.5f) return;
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
        float speedMultiplier = Stats.Values.MovementSpeedMultiplier * Mathf.Clamp(SurfaceSpeedMultiplier, 0.1f, 1f);
        if (_dodgeRemaining > 0f)
        {
            Velocity = _dodgeDirection * Config.DodgeSpeed * speedMultiplier;
            _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - dt);
        }
        else
        {
            float sprint = _input.SprintHeld ? Mathf.Max(1f, Config.SprintMultiplier) : 1f;
            Velocity = _input.ReadMovement() * Config.WalkSpeed * sprint * speedMultiplier;
        }
        if (Streaming != null && !Streaming.CanMoveTo(GlobalPosition + Velocity * dt)) Velocity = Vector2.Zero;
        MoveAndSlide();
    }
    #endregion
}
