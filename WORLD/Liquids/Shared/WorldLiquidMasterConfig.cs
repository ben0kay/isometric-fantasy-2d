// Disposable liquid preview settings; geometry is built once at startup, appearance stays live.
using Godot;

[GlobalClass]
public partial class WorldLiquidMasterConfig : Resource
{
    [Export] public bool Enabled { get => _enabled; set { if (_enabled == value) return; _enabled=value; EmitChanged(); } }
    private bool _enabled = true;
    [Export] public bool Animate { get => _animate; set { if (_animate == value) return; _animate=value; EmitChanged(); } }
    private bool _animate = true;
    [Export] public bool Shoreline { get => _shoreline; set { if (_shoreline == value) return; _shoreline=value; EmitChanged(); } }
    private bool _shoreline = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RippleStrength { get => _rippleStrength; set { if (_rippleStrength == value) return; _rippleStrength=value; EmitChanged(); } }
    private float _rippleStrength = 0.35f;
    [Export(PropertyHint.Range, "0,28,1")] public float ShoreWidth { get => _shoreWidth; set { if (_shoreWidth == value) return; _shoreWidth=value; EmitChanged(); } }
    private float _shoreWidth = 18f;
    [ExportGroup("Rain surface splashes")]
    [Export] public bool RainSurfaceSplashes { get => _rainSurfaceSplashes; set { if (_rainSurfaceSplashes == value) return; _rainSurfaceSplashes=value; EmitChanged(); } }
    private bool _rainSurfaceSplashes = true;
    [Export(PropertyHint.Range, "0,180,5")] public float RainSplashRate { get => _rainSplashRate; set { if (_rainSplashRate == value) return; _rainSplashRate=value; EmitChanged(); } }
    private float _rainSplashRate = 72f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float RainSplashOpacity { get => _rainSplashOpacity; set { if (_rainSplashOpacity == value) return; _rainSplashOpacity=value; EmitChanged(); } }
    private float _rainSplashOpacity = 0.55f;
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float RainSplashSize { get => _rainSplashSize; set { if (_rainSplashSize == value) return; _rainSplashSize=value; EmitChanged(); } }
    private float _rainSplashSize = 1f;
    [Export(PropertyHint.Range, "0.2,1.2,0.05")] public float RainSplashLifetime { get => _rainSplashLifetime; set { if (_rainSplashLifetime == value) return; _rainSplashLifetime=value; EmitChanged(); } }
    private float _rainSplashLifetime = 0.60f;
    [ExportGroup("Startup shape")]
    [Export] public Vector2 Centre { get; set; } = new(1080f, 800f);
    [Export] public Vector2 Radius { get; set; } = new(270f, 170f);
    [Export] public int ShapeSeed { get; set; } = 6319;
}
