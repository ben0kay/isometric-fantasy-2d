// Fixed-angle camera. Movement uses this camera's basis regardless of projection.
using Godot;

public partial class IsometricCamera : Camera3D
{
    [ExportGroup("References")]
    [Export] public CameraMasterConfig Config { get; set; }
    [Export] public Node3D Target { get; set; }
    private Vector3 _focus;
    private Vector3 _cameraOffset;

    #region Camera lifecycle
    // Set the angle once and snap to the initial target before the first frame.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new CameraMasterConfig();
        RotationDegrees = new Vector3(Config.PitchDegrees, Config.YawDegrees, 0);
        Projection = Config.Projection;
        Size = Config.Size;
        Fov = Config.FieldOfView;
        _cameraOffset = Basis.Z * Config.Distance;
        _focus = (Target?.GlobalPosition ?? Vector3.Zero) + Config.TargetOffset;
        GlobalPosition = _focus + _cameraOffset;
        Current = true;
    }

    // Smooth all target coordinates, including height on ramps; never rotate.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Target)) return;
        float weight = 1f - Mathf.Exp(-Mathf.Max(0, Config.FollowSharpness) * (float)delta);
        _focus = _focus.Lerp(Target.GetGlobalTransformInterpolated().Origin + Config.TargetOffset, weight);
        GlobalPosition = _focus + _cameraOffset;
    }
    #endregion
}
