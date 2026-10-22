// Production CharacterBody2D movement. The static sprite is an independent scene child.
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
        => DamageResolver.Apply(HealthState,request,attacker,Stats.Values);
    // Death stops movement/input; explicit restore can revive without replacing the actor.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (!HealthState.IsAlive) Velocity = Vector2.Zero;
        SetPhysicsProcess(HealthState.IsAlive);
        _input?.SetProcessUnhandledInput(HealthState.IsAlive);
    }
    // Release shared Resource listeners on unload.
    // =========================================================
    public override void _ExitTree()
    { HealthState.Changed -= OnHealthChanged; Stats.Dispose(); }
    // Cache movement input only; the Camera2D child owns its framing and wheel zoom.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new PlayerMasterConfig();
        _input = GetNode<PlayerInput>("Input");
        MotionMode = MotionModeEnum.Floating;
    }

    // Resolve screen-aligned 2D movement against world and prop colliders.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("PlayerMovement.PhysicsProcess");
        if (!HealthState.IsAlive) { Velocity = Vector2.Zero; return; }
        Velocity = _input.ReadMovement() * Config.WalkSpeed * Stats.Values.MovementSpeedMultiplier * Mathf.Clamp(SurfaceSpeedMultiplier, 0.1f, 1f);
        if (Streaming != null && !Streaming.CanMoveTo(GlobalPosition + Velocity * (float)delta)) Velocity = Vector2.Zero;
        MoveAndSlide();
    }
    #endregion
}
