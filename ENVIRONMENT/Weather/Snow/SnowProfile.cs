// Editable production SnowProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class SnowProfile : Resource
{
    [Export] public bool Enabled { get => _Enabled; set { if (_Enabled == value) return; _Enabled = value; EmitChanged(); } }
    private bool _Enabled = false;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Coverage { get => _Coverage; set { if (_Coverage == value) return; _Coverage = value; EmitChanged(); } }
    private float _Coverage = 0.6f;
    [Export] public bool Snowfall { get => _Snowfall; set { if (_Snowfall == value) return; _Snowfall = value; EmitChanged(); } }
    private bool _Snowfall = true;
    // 0–1 keeps ordinary snowfall; 1–3 additionally drives the presentation-only storm state.
    [Export(PropertyHint.Range, "0,3,0.01")] public float SnowfallRate { get => _SnowfallRate; set { value = float.IsFinite(value) ? Mathf.Clamp(value, 0f, 3f) : 0f; if (_SnowfallRate == value) return; _SnowfallRate = value; EmitChanged(); } }
    private float _SnowfallRate = 0.65f;
    [Export] public bool AutoAccumulate { get => _AutoAccumulate; set { if (_AutoAccumulate == value) return; _AutoAccumulate = value; EmitChanged(); } }
    private bool _AutoAccumulate = false;
    [Export(PropertyHint.Range, "5,120,1")] public float AccumulationSeconds { get => _AccumulationSeconds; set { if (_AccumulationSeconds == value) return; _AccumulationSeconds = value; EmitChanged(); } }
    private float _AccumulationSeconds = 30f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float TransitionSeconds { get => _TransitionSeconds; set { if (_TransitionSeconds == value) return; _TransitionSeconds = value; EmitChanged(); } }
    private float _TransitionSeconds = 3f;
    [ExportGroup("Shallow snow depth")]
    [Export] public bool PlayerSinks { get => _PlayerSinks; set { if (_PlayerSinks == value) return; _PlayerSinks = value; EmitChanged(); } }
    private bool _PlayerSinks = true;
    [Export(PropertyHint.Range, "0,8,0.25")] public float MaximumSinkPixels { get => _MaximumSinkPixels; set { if (_MaximumSinkPixels == value) return; _MaximumSinkPixels = value; EmitChanged(); } }
    private float _MaximumSinkPixels = 4f;
    [Export(PropertyHint.Range, "60,500,10")] public float DriftScale { get => _DriftScale; set { if (_DriftScale == value) return; _DriftScale = value; EmitChanged(); } }
    private float _DriftScale = 220f;
    [Export(PropertyHint.Range, "0,0.15,0.005")] public float DriftContrast { get => _DriftContrast; set { if (_DriftContrast == value) return; _DriftContrast = value; EmitChanged(); } }
    private float _DriftContrast = 0.065f;
    [Export] public Color SnowColour { get => _SnowColour; set { if (_SnowColour == value) return; _SnowColour = value; EmitChanged(); } }
    private Color _SnowColour = new(0.87f, 0.91f, 0.94f);
    [ExportGroup("Rock snow appearance")]
    [Export(PropertyHint.Range, "0.25,0.85,0.01")] public float RockCapDepth { get => _RockCapDepth; set { if (_RockCapDepth == value) return; _RockCapDepth = value; EmitChanged(); } }
    private float _RockCapDepth = 0.58f;
}
