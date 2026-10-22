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
    private PlayerMovement _owner;
    private float _attackSeconds;
    #region Input contract
    // Subscribe to centralized input and keep all mouse events out of tool code.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new AxeConfig();
        _owner = GetParent<PlayerMovement>();
        _owner.HealthState.Changed += OnHealthChanged;
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
        if (!_owner.HealthState.IsAlive || !Config.Enabled || IsAttacking) return;
        _attackSeconds = Mathf.Max(0.1f,Config.AttackSeconds) / _owner.Stats.Values.AttackSpeedMultiplier;
        IsAttacking = true; _elapsed = 0f; _hitChecked = false;
        _attackStart = requestedAim.LengthSquared() > 4f ? requestedAim.Angle() : _angle;
    }
    #endregion
    #region Visual and tool contact
    // Continuous facing reads centralized mouse aim, not independent tool input.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("EquippedTool.Process");
        Visible = Config.Enabled;
        if (!Visible) { IsAttacking = false; return; }
        Position = Config.HandAnchor + _playerSprite.Position;
        float dt = (float)delta;
        Vector2 direction = _input.ReadToolAim(GlobalPosition);
        if (IsAttacking)
        {
            _elapsed += dt;
            float progress = Mathf.Clamp(_elapsed / _attackSeconds, 0f, 1f);
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
    // Suspend equipment immediately on death; explicit restoration wakes existing presentation.
    // =========================================================
    private void OnHealthChanged(HealthChange change)
    {
        if (!_owner.HealthState.IsAlive) { IsAttacking = false; Hide(); }
        SetProcess(_owner.HealthState.IsAlive);
    }
    // Release actor and input listeners on unload.
    // =========================================================
    public override void _ExitTree()
    {
        _owner.HealthState.Changed -= OnHealthChanged;
        if (GodotObject.IsInstanceValid(_input)) _input.ToolUseRequested -= BeginSwing;
    }
    #endregion
}