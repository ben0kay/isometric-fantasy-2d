// Universal Resource-driven entity: sprite, hits, health, flash and death.
// Behaviour modules handle locomotion/AI; no DEBUG dependencies.
using Godot;

public partial class Entity : CharacterBody2D, IWorldNavigationAgent, IToolHitTarget
{
    [Export] public EntityDefinition Config { get; set; }
    public int Health { get; private set; }

    private Sprite2D _sprite;
    private CollisionShape2D _collision;
    private GroundChase _chase;
    private float _flashTimer, _deathTimer;
    private bool _dead;

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
        Health = Mathf.Max(1, Config.MaxHealth);
        _sprite = GetNode<Sprite2D>("Sprite");
        _collision = GetNode<CollisionShape2D>("Collision");
        if (Config.Sprite != null) { _sprite.Texture = Config.Sprite; _sprite.Offset = Config.SpriteOffset; _sprite.Scale = Config.SpriteScale; }
        if (_collision.Shape is CircleShape2D circle) { var shape = (CircleShape2D)circle.Duplicate(); shape.Radius = Mathf.Max(3f, Config.CollisionRadius); _collision.Shape = shape; }
        AddToGroup("world_tool_targets");
        _chase = GetNode<GroundChase>("GroundChase");
        _chase.Enabled = Config.ChasePlayer;
        MotionMode = MotionModeEnum.Floating;
        SetProcess(false);
    }

    // Inject the current target and the shared grid after seeded scenery generation.
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        _chase?.ConfigureNavigation(target, navigation);
    }
    #endregion

    // Restore validated living state without replaying damage effects or death transitions.
    // =========================================================
    public void RestoreHealth(int health)
    {
        if (health < 1 || health > Mathf.Max(1,Config.MaxHealth)) throw new System.IO.InvalidDataException("Invalid saved entity health.");
        Health = health;
    }

    #region Health and common tool hit contract
    public Vector2 HitAnchor => GlobalPosition;
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin)
    {
        Vector2 direction = origin - GlobalPosition;
        float radius = Mathf.Max(3f, Config.HitRadius);
        return direction.LengthSquared() <= radius * radius ? origin : GlobalPosition + direction.Normalized() * radius;
    }
    // =========================================================
    public void ReceiveHit(Vector2 direction) => TakeDamage(1);

    // All valid tool strikes call ReceiveHit; damage/death is shared by every definition.
    // =========================================================
    public void TakeDamage(int damage)
    {
        if (_dead || damage <= 0) return;
        Health = Mathf.Max(0, Health - damage);
        _flashTimer = Mathf.Max(0.05f, Config.HitFlashSeconds);
        SetProcess(true);
        if (Health > 0) return;
        _dead = true;
        RemoveFromGroup("world_tool_targets");
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
        if (_dead)
        {
            _deathTimer += dt;
            alpha = 1f - Mathf.Clamp(_deathTimer / Mathf.Max(0.1f, Config.DeathFadeSeconds), 0f, 1f);
        }
        _sprite.Modulate = new Color(1f, 1f - red * 0.7f, 1f - red * 0.7f, alpha);
        if (_dead && alpha <= 0f) { QueueFree(); return; }
        if (!_dead && _flashTimer <= 0f) SetProcess(false);
    }
    #endregion
}
