// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class ColourGradeProfile : Resource
{
    [Export] public bool Enabled { get => _Enabled; set { if (_Enabled == value) return; _Enabled = value; EmitChanged(); } }
    private bool _Enabled = true;
    [Export] public float Contrast { get => _Contrast; set { if (_Contrast == value) return; _Contrast = value; EmitChanged(); } }
    private float _Contrast = 1.10f;
    [Export] public float Saturation { get => _Saturation; set { if (_Saturation == value) return; _Saturation = value; EmitChanged(); } }
    private float _Saturation = 1.08f;
    [Export] public float Vignette { get => _Vignette; set { if (_Vignette == value) return; _Vignette = value; EmitChanged(); } }
    private float _Vignette = 0.08f;
}
