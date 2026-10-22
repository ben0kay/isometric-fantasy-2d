// Temporary equipped axe presentation, driven solely by PlayerInput events.
using Godot;
public partial class EquippedTool : Node2D
{
    [Export] public AxeConfig Config { get; set; }
    public bool IsAttacking { get; private set; }
    private Sprite2D _playerSprite, _toolSprite;
    private float _angle, _attackStart, _elapsed;
    private bool _hitChecked;
    private ToolHitQuery _hits;
    private PlayerInput _input;
    #region Input contract
    // Subscribe to centralized input and keep all mouse events out of tool code.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AxeConfig();
        _playerSprite = GetParent().GetNode<Sprite2D>("Sprite");
        _toolSprite = GetNode<Sprite2D>("ToolSprite");
        _hits = GetNode<ToolHitQuery>("HitQuery");
        _input = GetParent().GetNode<PlayerInput>("Input");
        _input.ToolUseRequested += BeginSwing;
        ProcessPriority = 5;
    }
    // =========================================================
    private void BeginSwing(Vector2 requestedAim)
    {
        if (!Config.Enabled || IsAttacking) return;
        IsAttacking = true; _elapsed = 0f; _hitChecked = false;
        _attackStart = requestedAim.LengthSquared() > 4f ? requestedAim.Angle() : _angle;
    }
    #endregion
    #region Visual and tool contact
    // Only uses mouse WORLD POSITION for continuous aiming, not mouse/button input.
    // =========================================================
    public override void _Process(double delta)
    {
        Visible = Config.Enabled;
        if (!Visible) { IsAttacking = false; return; }
        Position = Config.HandAnchor + _playerSprite.Position;
        float dt = (float)delta;
        Vector2 direction = GetGlobalMousePosition() - GlobalPosition;
        if (IsAttacking)
        {
            _elapsed += dt;
            float progress = Mathf.Clamp(_elapsed / Mathf.Max(0.1f, Config.AttackSeconds), 0f, 1f);
            if (!_hitChecked && progress >= Mathf.Clamp(Config.ContactProgress, 0.05f, 0.95f))
            {
                _hitChecked = true;
                _hits.Strike(((Node2D)GetParent()).GlobalPosition, Vector2.FromAngle(_attackStart),
                    Config.HitReach, Config.HitHalfAngleDegrees);
            }
            float sweep = progress * progress * (3f - 2f * progress);
            _angle = _attackStart + sweep * Mathf.Tau;
            if (progress >= 1f) { IsAttacking = false; _angle = Mathf.Wrap(_angle, -Mathf.Pi, Mathf.Pi); }
        }
        else if (direction.LengthSquared() > 4f)
            _angle = Mathf.LerpAngle(_angle, direction.Angle(),
                1f - Mathf.Exp(-Mathf.Max(1f, Config.AimSpeed) * dt));
        Rotation = _angle;
        _toolSprite.Position = new Vector2(Mathf.Max(4f, Config.OrbitRadius), 0f);
        ZIndex = Mathf.Sin(_angle) < -0.15f ? -1 : 1;
    }
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.ToolUseRequested -= BeginSwing;
    }
    #endregion
}