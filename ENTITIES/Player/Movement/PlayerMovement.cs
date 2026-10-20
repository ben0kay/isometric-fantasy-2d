// Owns the 3D body velocity, camera-relative ground movement and gravity.
using Godot;

public partial class PlayerMovement : CharacterBody3D
{
    [ExportGroup("References")]
    [Export] public PlayerMasterConfig Config { get; set; }
    private PlayerInput _input;
    private Camera3D _camera;

    #region Movement lifecycle
    // Cache input; camera is resolved lazily because it may enter after the player.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new PlayerMasterConfig();
        _input = GetNode<PlayerInput>("Input");
        FloorSnapLength = Config.FloorSnap;
        FloorMaxAngle = Mathf.DegToRad(Config.MaxSlopeDegrees);
        FloorConstantSpeed = true;
    }

    // Project camera axes onto the ground and keep diagonals at walking speed.
    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(_camera)) _camera = GetViewport().GetCamera3D();
        Vector2 intent = _input.ReadMovement();
        Vector3 right = _camera != null ? _camera.GlobalBasis.X : Vector3.Right;
        Vector3 back = _camera != null ? _camera.GlobalBasis.Z : Vector3.Back;
        right.Y = 0;
        back.Y = 0;
        Vector3 direction = right.Normalized() * intent.X + back.Normalized() * intent.Y;
        direction = direction.LimitLength(1f);
        Vector3 velocity = Velocity;
        velocity.X = direction.X * Config.WalkSpeed;
        velocity.Z = direction.Z * Config.WalkSpeed;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - Config.Gravity * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
    }
    #endregion
}
