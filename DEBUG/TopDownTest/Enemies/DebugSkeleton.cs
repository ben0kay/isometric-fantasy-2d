// Disposable passive skeleton: distance sensing, grid waypoint pursuit and three axe hits to die.
// Implements the same hit target used by tree feedback; never attacks or damages the player.
using Godot;

public partial class DebugSkeleton : CharacterBody2D, IWeaponHitTarget
{
    [Export] public EnemyMasterConfig Config { get; set; }
    public CharacterBody2D Player { get; set; }
    public DebugEnemyNavigation Navigation { get; set; }

    public Vector2 HitAnchor => GlobalPosition;
    public int Health { get; private set; }

    private Sprite2D _sprite;
    private CollisionShape2D _collider;
    private Vector2[] _route = System.Array.Empty<Vector2>();
    private int _waypoint;
    private float _repathRemaining, _flashRemaining, _deathElapsed;
    private bool _dying;

    #region Setup and hits
    // Register as a hit target without touching the player's existing axe implementation.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new EnemyMasterConfig();
        _sprite = GetNode<Sprite2D>("Sprite");
        _collider = GetNode<CollisionShape2D>("Collision");
        Health = Mathf.Max(1, Config.MaxHealth);
        MotionMode = MotionModeEnum.Floating;
        AddToGroup("debug_weapon_hit_targets");
        SetProcess(false);
    }

    // Use the feet/collision centre, not the taller sprite silhouette, for accurate range tests.
    // =========================================================
    public Vector2 ClosestHitPoint(Vector2 origin)
    {
        Vector2 towardsAttacker = origin - GlobalPosition;
        float radius = Mathf.Max(3f, Config.HitRadius);
        return towardsAttacker.LengthSquared() <= radius * radius
            ? origin : GlobalPosition + towardsAttacker.Normalized() * radius;
    }

    // One hit per swing, shared target query handles aim/range and nearest selection.
    // =========================================================
    public void ReceiveHit(Vector2 direction)
    {
        if (_dying) return;
        Health--;
        _flashRemaining = Mathf.Max(0.05f, Config.DamageFlashSeconds);
        SetProcess(true);
        if (Health > 0) return;
        _dying = true;
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        RemoveFromGroup("debug_weapon_hit_targets");
        _collider.SetDeferred("disabled", true);
    }
    #endregion

    #region Chase
    // Recalculate a grid path only at a configurable interval; physics still prevents overlap.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        if (_dying || Player == null || !GodotObject.IsInstanceValid(Player) || Navigation == null)
            return;

        Vector2 relative = Player.GlobalPosition - GlobalPosition;
        float distanceSquared = relative.LengthSquared();
        if (distanceSquared > Config.DetectionRadius * Config.DetectionRadius ||
            distanceSquared <= Config.StopDistance * Config.StopDistance)
        {
            Velocity = Vector2.Zero;
            _repathRemaining = 0f;
            return;
        }

        _repathRemaining -= (float)delta;
        if (_repathRemaining <= 0f)
        {
            _route = Navigation.FindPath(GlobalPosition, Player.GlobalPosition);
            _waypoint = _route.Length > 1 ? 1 : 0;
            _repathRemaining = Mathf.Max(0.15f, Config.RepathSeconds);
        }

        if (_route.Length == 0) { Velocity = Vector2.Zero; return; }

        const float waypointRadius = 11f;
        while (_waypoint < _route.Length - 1 &&
               GlobalPosition.DistanceSquaredTo(_route[_waypoint]) < waypointRadius * waypointRadius)
            _waypoint++;

        // Once in the destination grid cell, close the remaining gap naturally.
        Vector2 goal = _route[_waypoint];
        if (_waypoint == _route.Length - 1 && goal.DistanceSquaredTo(Player.GlobalPosition) <= 36f * 36f)
            goal = Player.GlobalPosition;

        Vector2 travel = goal - GlobalPosition;
        Velocity = travel.LengthSquared() < 4f ? Vector2.Zero : travel.Normalized() * Config.MoveSpeed;
        MoveAndSlide();
    }
    #endregion

    #region Visual response
    // Brief red flash for hits; fade and free the enemy only after the final hit.
    // =========================================================
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _flashRemaining = Mathf.Max(0f, _flashRemaining - dt);
        float flash = Mathf.Clamp(_flashRemaining / Mathf.Max(0.05f, Config.DamageFlashSeconds), 0f, 1f);
        float alpha = 1f;
        if (_dying)
        {
            _deathElapsed += dt;
            alpha = 1f - Mathf.Clamp(_deathElapsed / Mathf.Max(0.1f, Config.DeathFadeSeconds), 0f, 1f);
        }
        _sprite.Modulate = new Color(1f, 1f - flash * 0.7f, 1f - flash * 0.7f, alpha);
        if (_dying && alpha <= 0f) { QueueFree(); return; }
        if (!_dying && _flashRemaining <= 0f) SetProcess(false);
    }
    #endregion
}
