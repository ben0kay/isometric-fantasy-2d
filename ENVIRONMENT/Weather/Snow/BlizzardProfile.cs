// Runtime-cloned presentation tuning; the existing snowfall rate supplies severity, not another slider.
using Godot;

[GlobalClass]
public partial class BlizzardProfile : Resource
{
    private float _transitionSeconds = 3f;
    [ExportGroup("Storm transition (real seconds)")]
    [Export(PropertyHint.Range, "0,10,0.1")]
    public float TransitionSeconds
    {
        get => _transitionSeconds;
        set
        {
            value = float.IsFinite(value) ? Mathf.Clamp(value, 0f, 10f) : 3f;
            if (_transitionSeconds == value) return;
            _transitionSeconds = value; EmitChanged();
        }
    }
    private bool _layeredSnow = true, _spindrift = true;
    private int _extraFlakes = 1300, _driftParticles = 64;
    private float _gustStrength = 0.45f, _gustSeconds = 5f;
    [ExportGroup("B1 bounded GPU layers")]
    [Export] public bool LayeredSnow { get => _layeredSnow; set { if (_layeredSnow == value) return; _layeredSnow = value; EmitChanged(); } }
    [Export] public bool Spindrift { get => _spindrift; set { if (_spindrift == value) return; _spindrift = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0,1300,50")] public int ExtraFlakes { get => _extraFlakes; set { value = Mathf.Clamp(value,0,1300); if (_extraFlakes == value) return; _extraFlakes = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0,128,1")] public int DriftParticles { get => _driftParticles; set { value = Mathf.Clamp(value,0,128); if (_driftParticles == value) return; _driftParticles = value; EmitChanged(); } }
    [ExportGroup("B1 visual gusts (never change global wind)")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float GustStrength { get => _gustStrength; set { value = float.IsFinite(value) ? Mathf.Clamp(value,0,1) : 0.45f; if (_gustStrength == value) return; _gustStrength = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "1,15,0.5")] public float GustSeconds { get => _gustSeconds; set { value = float.IsFinite(value) ? Mathf.Clamp(value,1,15) : 5f; if (_gustSeconds == value) return; _gustSeconds = value; EmitChanged(); } }
}
