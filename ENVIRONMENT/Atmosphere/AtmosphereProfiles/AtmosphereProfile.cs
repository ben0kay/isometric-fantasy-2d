// Editable production AtmosphereProfile settings extracted from the proven visual sandbox.
using Godot;

[GlobalClass]
public partial class AtmosphereProfile : Resource
{
    [ExportGroup("Switches")]
    [Export] public bool Haze { get => _Haze; set { if (_Haze == value) return; _Haze = value; EmitChanged(); } }
    private bool _Haze = true;
    [Export] public bool Pollen { get => _Pollen; set { if (_Pollen == value) return; _Pollen = value; EmitChanged(); } }
    private bool _Pollen = true;
    [Export] public bool Leaves { get => _Leaves; set { if (_Leaves == value) return; _Leaves = value; EmitChanged(); } }
    private bool _Leaves = true;
    [Export] public bool Smoke { get => _Smoke; set { if (_Smoke == value) return; _Smoke = value; EmitChanged(); } }
    private bool _Smoke = true;
    [Export] public bool Embers { get => _Embers; set { if (_Embers == value) return; _Embers = value; EmitChanged(); } }
    private bool _Embers = true;
    [ExportGroup("Haze")]
    [Export(PropertyHint.Range, "0,0.6,0.01")] public float HazeDensity { get => _HazeDensity; set { if (_HazeDensity == value) return; _HazeDensity = value; EmitChanged(); } }
    private float _HazeDensity = 0.22f;
    [Export(PropertyHint.Range, "60,500,5")] public float PatchSize { get => _PatchSize; set { if (_PatchSize == value) return; _PatchSize = value; EmitChanged(); } }
    private float _PatchSize = 190f;
    [Export] public Color HazeColour { get => _HazeColour; set { if (_HazeColour == value) return; _HazeColour = value; EmitChanged(); } }
    private Color _HazeColour = new(0.74f, 0.80f, 0.79f);
    [ExportGroup("Particle intensity")]
    [Export(PropertyHint.Range, "0,100,1")] public int PollenCount { get => _PollenCount; set { if (_PollenCount == value) return; _PollenCount = value; EmitChanged(); } }
    private int _PollenCount = 35;
    [Export(PropertyHint.Range, "0,40,1")] public int LeafCount { get => _LeafCount; set { if (_LeafCount == value) return; _LeafCount = value; EmitChanged(); } }
    private int _LeafCount = 10;
    [ExportGroup("Leaves / wind")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float LeafWindCountBoost { get => _LeafWindCountBoost; set { if (_LeafWindCountBoost == value) return; _LeafWindCountBoost = value; EmitChanged(); } }
    private float _LeafWindCountBoost = 1f;
    [Export(PropertyHint.Range, "0,6,0.25")] public float LeafWindDrift { get => _LeafWindDrift; set { if (_LeafWindDrift == value) return; _LeafWindDrift = value; EmitChanged(); } }
    private float _LeafWindDrift = 2f;
    [Export(PropertyHint.Range, "0,40,1")] public int SmokeCount { get => _SmokeCount; set { if (_SmokeCount == value) return; _SmokeCount = value; EmitChanged(); } }
    private int _SmokeCount = 14;
    [Export(PropertyHint.Range, "0,40,1")] public int EmberCount { get => _EmberCount; set { if (_EmberCount == value) return; _EmberCount = value; EmitChanged(); } }
    private int _EmberCount = 10;
}
