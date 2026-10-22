// Equipped mouse-aimed sword; one full orbit per click, with no combat/damage dependency.
using Godot;

public partial class DebugSword : Node2D
{
    [Export] public WeaponMasterConfig Config { get; set; }
    public bool IsAttacking { get; private set; }
    private Sprite2D _playerSprite, _sword;
    private float _angle, _attackStart, _elapsed;

    #region Setup and input
    // Keep the weapon on the player's existing actor root so scenery sorting still works.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new WeaponMasterConfig();
        _playerSprite = GetParent().GetNode<Sprite2D>("Sprite");
        _sword = GetNode<Sprite2D>("Sword");
        ProcessPriority = 5; // After the shared snow/water visual response.
    }

    // Unhandled clicks respect the comparison UI; holding the button does not auto-repeat.
    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (!Config.Enabled || IsAttacking) return;
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        {
            IsAttacking = true; _elapsed = 0f; _attackStart = _angle;
            GetViewport().SetInputAsHandled();
        }
    }
    #endregion

    #region Presentation
    // Aim in world space through the active camera; animate the same pivot during an attack.
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
            float sweep = progress * progress * (3f - 2f * progress);
            _angle = _attackStart + sweep * Mathf.Tau;
            if (progress >= 1f) { IsAttacking = false; _angle = Mathf.Wrap(_angle, -Mathf.Pi, Mathf.Pi); }
        }
        else if (direction.LengthSquared() > 4f)
            _angle = Mathf.LerpAngle(_angle, direction.Angle(), 1f - Mathf.Exp(-Mathf.Max(1f, Config.AimSpeed) * dt));
        Rotation = _angle;
        _sword.Position = new Vector2(Mathf.Max(4f, Config.OrbitRadius), 0f);
        ZIndex = Mathf.Sin(_angle) < -0.15f ? -1 : 1;
    }
    #endregion
}
