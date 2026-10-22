// Disposable CharacterBody2D movement. The static sprite is an independent scene child.
using Godot;

public partial class TestPlayerMovement : CharacterBody2D
{
    [ExportGroup("References")]
    [Export] public TopDownTestConfig Config { get; set; }
    public float SurfaceSpeedMultiplier { get; set; } = 1f;
    private TestPlayerInput _input;

    #region Lifecycle
    // Cache the input component and apply framing once; no animation or rotation.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new TopDownTestConfig();
        _input = GetNode<TestPlayerInput>("Input");
        GetNode<Camera2D>("Camera").Zoom = Vector2.One * Config.CameraZoom;
        MotionMode = MotionModeEnum.Floating;
    }

    // Resolve screen-aligned 2D movement against world and prop colliders.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        Velocity = _input.ReadMovement() * Config.WalkSpeed * Mathf.Clamp(SurfaceSpeedMultiplier, 0.1f, 1f);
        MoveAndSlide();
    }
    #endregion
}
