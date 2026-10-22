// Production Skeleton Basic: static sprite, detection, obstacle-aware chase and damage/death.
// Does not attack or deal player damage. No references to the DEBUG prototype.
using Godot;
using System;

public partial class SkeletonBasic : CharacterBody2D, IWorldNavigationAgent
{
    [Export] public SkeletonBasicConfig Config { get; set; }
    public int Health { get; private set; }

    private CharacterBody2D _target;
    private StaticObstacleNavigation _navigation;
    private Sprite2D _sprite;
    private CollisionShape2D _collision;
    private Vector2[] _path = Array.Empty<Vector2>();
    private int _index;
    private float _repathTimer, _flashTimer, _deathTimer;
    private bool _dead;

    #region Setup
    // Keep this actor inert if placed in an editor/test scene without world services.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new SkeletonBasicConfig();
        Health = Mathf.Max(1, Config.MaxHealth);
        _sprite = GetNode<Sprite2D>("Sprite");
        _collision = GetNode<CollisionShape2D>("Collision");
        MotionMode = MotionModeEnum.Floating;
        SetProcess(false);
    }

    // Inject the current target and the shared grid after seeded scenery generation.
    // =========================================================
    public void ConfigureNavigation(CharacterBody2D target, StaticObstacleNavigation navigation)
    {
        _target = target;
        _navigation = navigation;
        _repathTimer = 0f;
    }
    #endregion

    #region Health
    // Later COMBAT wiring can invoke this; production player has no axe yet.
    // =========================================================
    public void TakeDamage(int damage)
    {
        if (_dead || damage <= 0) return;
        Health = Mathf.Max(0, Health - damage);
        _flashTimer = Mathf.Max(0.05f, Config.HitFlashSeconds);
        SetProcess(true);
        if (Health > 0) return;
        _dead = true;
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        _collision.SetDeferred("disabled", true);
    }
    #endregion

    #region Navigation / pursuit
    // Periodic path queries avoid running A* every physics frame.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        if (_dead || _target == null || !GodotObject.IsInstanceValid(_target) || _navigation == null) return;

        float distanceSquared = GlobalPosition.DistanceSquaredTo(_target.GlobalPosition);
        if (distanceSquared > Config.DetectionRadius * Config.DetectionRadius ||
            distanceSquared <= Config.StopDistance * Config.StopDistance)
        {
            Velocity = Vector2.Zero;
            _repathTimer = 0f;
            return;
        }

        _repathTimer -= (float)delta;
        if (_repathTimer <= 0f)
        {
            _path = _navigation.FindPath(GlobalPosition, _target.GlobalPosition);
            _index = 0;
            _repathTimer = Mathf.Max(0.15f, Config.RepathSeconds);
        }
        if (_path.Length == 0) { Velocity = Vector2.Zero; return; }

        const float waypointRadius = 12f;
        while (_index < _path.Length - 1 &&
               GlobalPosition.DistanceSquaredTo(_path[_index]) <= waypointRadius * waypointRadius)
            _index++;

        Vector2 goal = _path[_index];
        // Approach the player's exact feet once the last route point is nearby.
        if (_index == _path.Length - 1 && goal.DistanceSquaredTo(_target.GlobalPosition) < 30f * 30f)
            goal = _target.GlobalPosition;

        Vector2 heading = goal - GlobalPosition;
        Velocity = heading.LengthSquared() <= 1f ? Vector2.Zero : heading.Normalized() * Config.MoveSpeed;
        MoveAndSlide();
    }
    #endregion

    #region Visual response
    // Red hit flash and a bounded death fade; remove once fully transparent.
    // =========================================================
    public override void _Process(double delta)
    {
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
