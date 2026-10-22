// Editable production DayNightProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class DayNightProfile : Resource
{
    [ExportGroup("Ambient colours")]
    [Export] public Color NoonColour { get => _NoonColour; set { if (_NoonColour == value) return; _NoonColour = value; EmitChanged(); } }
    private Color _NoonColour = new(1f, 0.98f, 0.91f);
    [Export] public Color HorizonColour { get => _HorizonColour; set { if (_HorizonColour == value) return; _HorizonColour = value; EmitChanged(); } }
    private Color _HorizonColour = new(0.83f, 0.56f, 0.38f);
    [Export] public Color NightColour { get => _NightColour; set { if (_NightColour == value) return; _NightColour = value; EmitChanged(); } }
    private Color _NightColour = new(0.18f, 0.23f, 0.36f);
    [Export] public bool SoftShadows { get => _SoftShadows; set { if (_SoftShadows == value) return; _SoftShadows = value; EmitChanged(); } }
    private bool _SoftShadows = true;
    [Export] public float ShadowSoftness { get => _ShadowSoftness; set { if (_ShadowSoftness == value) return; _ShadowSoftness = value; EmitChanged(); } }
    private float _ShadowSoftness = 0.008f;
    [ExportGroup("Sun shadows")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ShadowOpacity { get => _ShadowOpacity; set { if (_ShadowOpacity == value) return; _ShadowOpacity = value; EmitChanged(); } }
    private float _ShadowOpacity = 0.36f;
    [Export] public float MinimumLength { get => _MinimumLength; set { if (_MinimumLength == value) return; _MinimumLength = value; EmitChanged(); } }
    private float _MinimumLength = 0.18f;
    [Export] public float MaximumLength { get => _MaximumLength; set { if (_MaximumLength == value) return; _MaximumLength = value; EmitChanged(); } }
    private float _MaximumLength = 1.65f;
    [Export] public float SunDirectionOffsetDegrees { get => _SunDirectionOffsetDegrees; set { if (_SunDirectionOffsetDegrees == value) return; _SunDirectionOffsetDegrees = value; EmitChanged(); } }
    private float _SunDirectionOffsetDegrees = 0f;
    [ExportGroup("Shadow projection scheduling")]
    [Export(PropertyHint.Range,"0,1,0.05")] public float ShadowProjectionTolerancePixels { get => _tolerance; set { if(_tolerance==value) return; _tolerance=value; EmitChanged(); } }
    private float _tolerance=0.25f;
}
