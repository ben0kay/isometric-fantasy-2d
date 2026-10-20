// Shared fixed-camera framing and follow settings.
using Godot;

[GlobalClass]
public partial class CameraMasterConfig : Resource
{
    [ExportGroup("Framing")]
    [Export] public Camera3D.ProjectionType Projection { get; set; } = Camera3D.ProjectionType.Orthogonal;
    [Export(PropertyHint.Range, "4,40,0.5")] public float Size { get; set; } = 18f;
    [Export] public float FieldOfView { get; set; } = 55f;
    [Export] public float YawDegrees { get; set; } = 45f;
    [Export] public float PitchDegrees { get; set; } = -35.264f;
    [Export] public float Distance { get; set; } = 24f;
    [ExportGroup("Following")]
    [Export] public float FollowSharpness { get; set; } = 9f;
    [Export] public Vector3 TargetOffset { get; set; } = new(0, 0.8f, 0);
}
