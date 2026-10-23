// Universal Resource-driven entity: sprite, hits, health, flash and death.
// Behaviour modules handle locomotion/AI; no DEBUG dependencies.
using Godot;

public partial class Entity : CharacterBody2D, IWorldNavigationAgent, IToolHitTarget, ICombatTarget, IReactionTarget, ICombatImpactTarget
{
    [Export] public EntityDefinition Config { get; set; }
    public ActorHealth HealthState { get; private set; }
    public ActorStats Stats { get; private set; }
    public Node2D CombatNode => this;
    public CombatFaction Faction => Config.Faction;
    private HitFeedback _hitFeedback;
    public HitFeedback ImpactVisual => _hitFeedback;
    // Authoritative confirmed contact only; generic damage/healing/restoration never infer an impact.
    // =========================================================
    public void ReceiveImpact(AttackContact contact) => _hitFeedback=HitFeedback.Emit(this,_hitFeedback,Config.ImpactFeedback,contact);
    public Knockback Reaction { get; } = new();
    private CombatMasterConfig _combatConfig;
    public int Health => HealthState.Current;

    private Sprite2D _sprite;
    private CollisionShape2D _collision;
    private GroundChase _chase;
    public Vector2 FeetCenter => _collision.GlobalPosition;
    public float FeetRadius => (_collision.Shape as CircleShape2D)?.Radius * Mathf.Max(_collision.GlobalScale.Abs().X,_collision.GlobalScale.Abs().Y) ?? 9f;
    private float _flashTimer, _deathTimer;


    #region Setup
    // Apply a new Resource before entering the scene tree; shared scene owns all common functionality.
    // =========================================================
    public void Configure(EntityDefinition definition)
    {
        if (IsInsideTree()) { GD.PushError("Configure an enemy before AddChild."); return; }
        Config = definition;
    }

    // Keep this actor inert if placed in an editor/test scene without world services.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new EntityDefinition();
        _combatConfig ??= GD.Load<CombatMasterConfig>("res://COMBAT/DefaultCombatConfig.tres");
        HealthState = new ActorHealth(Config.MaxHealth);
        Stats = new ActorStats(Config.CombatStats);
        HealthState.Changed += OnHealthChanged;
        _sprite = GetNode<Sprite2D>("Sprite");
        _collision = GetNode<CollisionShape2D>("Collision");
        if (Config.Sprite != null) { _sprite.Texture = Config.Sprite; _sprite.Offset = Config.SpriteOffset; _sprite.Scale = Config.SpriteScale; }
        if (_collision.Shape is CircleShape2D circle) { var shape = (CircleShape2D)circle.Duplicate(); shape.Radius = Mathf.Max(3f, Config.CollisionRadius); _collision.Shape = shape; }
        AddToGroup("world_tool_targets");
        _chase = GetNode<GroundChase>("GroundChase");
        _chase.Enabled = Config.ChasePlayer;
        MotionMode = MotionModeEnum.Floating;
        GetNode<SharedHealthBar>("HealthBar").Bind(HealthState, _sprite);
        var numbers = new DamageNumbers { Name = "DamageNumbers", ZIndex = 5 };
        AddChild(numbers);
        numbers.Bind(HealthState, _sprite);
        SetProcess(false);
    }

    // Inject the current target and the shared grid after seeded scenery generation.
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        _chase?.ConfigureNavigation(target, navigation);
    }
    // Explicit scene-local projectile ownership; navigation injection can happen before or after this.
    // =========================================================
    public void ConfigureRanged(ProjectileSpawner spawner) => _chase?.ConfigureRanged(spawner);
    #endregion

    // Restore validated living state without replaying damage effects or death transitions.
    // =========================================================
    public void RestoreHealth(int health)
    {
        if (health < 1 || health > Mathf.Max(1,Config.MaxHealth)) throw new System.IO.InvalidDataException("Invalid saved entity health.");
        HealthState.Restore(health);
    }

    #region Health and common tool hit contract
    public Vector2 HitAnchor => GlobalPosition;
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin)
    {
        return CombatTargeting.ClosestPoint(origin,GlobalPosition,Config.HitRadius);
    }
    // =========================================================
    public void ReceiveHit(Vector2 direction) => TakeDamage(1);

    // All valid tool strikes call ReceiveHit; damage/death is shared by every definition.
    // =========================================================
    public void TakeDamage(int damage)
    {
        if (damage > 0) ReceiveDamage(new DamageRequest(damage),EffectiveStats.Neutral);
    }
    // Shared resolution keeps defense, rounding and actual applied damage consistent across actors.
    // =========================================================
    public DamageResult ReceiveDamage(DamageRequest request, EffectiveStats attacker)
        => DamageResolver.Apply(HealthState,request,attacker,Stats.Values);

    // Confirmed living contacts activate the existing movement owner, including passive creatures.
    // =========================================================
    public void ReceiveReaction(AttackValues attack, Vector2 direction)
    {
        if (!CombatTargeting.Active(this) || !Reaction.Apply(attack with { KnockbackDistance=_combatConfig.KnockbackEnabled ? attack.KnockbackDistance : 0, StaggerSeconds=_combatConfig.StaggerEnabled ? attack.StaggerSeconds : 0 },direction,Config.ReactionImmune,Config.KnockbackResistance,Config.StaggerResistance)) return;
        if (attack.InterruptAttack && Reaction.Staggered) { _chase?.Attack?.Interrupt(); _chase?.Ranged?.Interrupt(); }
        _chase?.WakeReaction();
    }
    // Disabled ownership must never reactivate stale recoil.
    // =========================================================
    public override void _Notification(int what) { if (what==NotificationDisabled) Reaction.Clear(); }
    // Wake feedback only after actual damage; restored state never replays hit effects.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (change.Restored || change.After >= change.Before) return;
        _flashTimer = Mathf.Max(0.05f, Config.HitFlashSeconds);
        SetProcess(true);
        if (HealthState.IsAlive) return;
        RemoveFromGroup("world_tool_targets");
        Reaction.Clear();
        Velocity = Vector2.Zero;
        _chase?.Stop();
        _collision.SetDeferred("disabled", true);
    }
    #endregion

    #region Visual response
    // Red hit flash and a bounded death fade; remove once fully transparent.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("Entity.Process");
        float dt = (float)delta;
        _flashTimer = Mathf.Max(0f, _flashTimer - dt);
        float red = Mathf.Clamp(_flashTimer / Mathf.Max(0.05f, Config.HitFlashSeconds), 0f, 1f);
        float alpha = 1f;
        if (!HealthState.IsAlive)
        {
            _deathTimer += dt;
            alpha = 1f - Mathf.Clamp(_deathTimer / Mathf.Max(0.1f, Config.DeathFadeSeconds), 0f, 1f);
        }
        _sprite.Modulate = new Color(1f, 1f - red * 0.7f, 1f - red * 0.7f, alpha);
        if (!HealthState.IsAlive && alpha <= 0f) { QueueFree(); return; }
        if (HealthState.IsAlive && _flashTimer <= 0f) SetProcess(false);
    }
    #endregion
    // Release Resource subscriptions when the actor unloads.
    // =========================================================
    public override void _ExitTree()
    { if (HealthState != null) HealthState.Changed -= OnHealthChanged; Stats?.Dispose(); }
}
