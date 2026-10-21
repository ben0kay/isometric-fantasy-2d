// Disposable visual-test settings; no production gameplay depends on this resource.
using Godot;

[GlobalClass]
public partial class TopDownTestConfig : Resource
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "20,400,5")] public float WalkSpeed { get; set; } = 150f;
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "1,5,0.25")] public float CameraZoom { get; set; } = 2f;
}
