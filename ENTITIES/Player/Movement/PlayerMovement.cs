// Production CharacterBody2D movement. The static sprite is an independent scene child.
using Godot;

public partial class PlayerMovement : CharacterBody2D
{
    [ExportGroup("References")]
    [Export] public PlayerMasterConfig Config { get; set; }
    public float SurfaceSpeedMultiplier { get; set; } = 1f;
    public WorldChunkStream Streaming { get; set; }
    private PlayerInput _input;

    #region Lifecycle
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
        Velocity = _input.ReadMovement() * Config.WalkSpeed * Mathf.Clamp(SurfaceSpeedMultiplier, 0.1f, 1f);
        if (Streaming != null && !Streaming.CanMoveTo(GlobalPosition + Velocity * (float)delta)) Velocity = Vector2.Zero;
        MoveAndSlide();
    }
    #endregion
}
