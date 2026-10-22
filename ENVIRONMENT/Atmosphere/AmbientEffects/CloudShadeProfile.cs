// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class CloudShadeProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Opacity { get; set; } = 0.12f;
}
