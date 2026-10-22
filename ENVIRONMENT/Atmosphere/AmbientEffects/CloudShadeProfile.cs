// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class CloudShadeProfile : Resource
{
    [Export] public bool Enabled { get => _enabled; set { if (_enabled == value) return; _enabled = value; EmitChanged(); } }
    private bool _enabled = true;
    [Export] public float Opacity { get => _opacity; set { if (_opacity == value) return; _opacity = value; EmitChanged(); } }
    private float _opacity = 0.12f;
}
