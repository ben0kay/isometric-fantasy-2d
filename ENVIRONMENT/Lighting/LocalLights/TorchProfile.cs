// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class TorchProfile : Resource
{
    [Export] public bool Enabled { get => _Enabled; set { if (_Enabled == value) return; _Enabled = value; EmitChanged(); } }
    private bool _Enabled = true;
    [Export] public float Energy { get => _Energy; set { if (_Energy == value) return; _Energy = value; EmitChanged(); } }
    private float _Energy = 0.85f;
    [ExportGroup("Local illumination")]
    [Export] public float TextureScale { get => _TextureScale; set { if (_TextureScale == value) return; _TextureScale = value; EmitChanged(); } }
    private float _TextureScale = 1f;
    [Export] public Color Colour { get => _Colour; set { if (_Colour == value) return; _Colour = value; EmitChanged(); } }
    private Color _Colour = new(1f, 0.60f, 0.25f);
    [Export] public bool CastShadows { get => _CastShadows; set { if (_CastShadows == value) return; _CastShadows = value; EmitChanged(); } }
    private bool _CastShadows = false;
    [Export] public Light2D.ShadowFilterEnum ShadowFilter { get => _ShadowFilter; set { if (_ShadowFilter == value) return; _ShadowFilter = value; EmitChanged(); } }
    private Light2D.ShadowFilterEnum _ShadowFilter = Light2D.ShadowFilterEnum.Pcf5;
    [Export(PropertyHint.Range, "0,8,0.1")] public float ShadowSoftness { get => _ShadowSoftness; set { if (_ShadowSoftness == value) return; _ShadowSoftness = value; EmitChanged(); } }
    private float _ShadowSoftness = 1f;
    [ExportGroup("Development preview placement; reload after changes")]
    [Export] public Vector2[] AdditionalOffsets { get; set; } = System.Array.Empty<Vector2>();
    [Export] public Vector2 OffsetFromSpawn { get; set; } = new(100, -30);
}
