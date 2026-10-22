// Reusable presentation data; runtime instances are cloned before developer changes.
using Godot;
[GlobalClass]
public partial class TorchProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Energy { get; set; } = 0.85f;
    [ExportGroup("Local illumination")]
    [Export] public float TextureScale { get; set; } = 1f;
    [Export] public Color Colour { get; set; } = new(1f, 0.60f, 0.25f);
    [Export] public bool CastShadows { get; set; }
    [Export] public Light2D.ShadowFilterEnum ShadowFilter { get; set; } = Light2D.ShadowFilterEnum.Pcf5;
    [Export(PropertyHint.Range, "0,8,0.1")] public float ShadowSoftness { get; set; } = 1f;
    [ExportGroup("Development preview placement; reload after changes")]
    [Export] public Vector2[] AdditionalOffsets { get; set; } = System.Array.Empty<Vector2>();
    [Export] public Vector2 OffsetFromSpawn { get; set; } = new(100, -30);
}
