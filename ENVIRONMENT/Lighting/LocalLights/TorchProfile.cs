// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class TorchProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Energy { get; set; } = 0.85f;
    [Export] public Vector2 OffsetFromSpawn { get; set; } = new(100, -30);
}
