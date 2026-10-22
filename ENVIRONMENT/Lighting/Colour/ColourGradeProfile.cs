// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class ColourGradeProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Contrast { get; set; } = 1.10f;
    [Export] public float Saturation { get; set; } = 1.08f;
    [Export] public float Vignette { get; set; } = 0.08f;
}
