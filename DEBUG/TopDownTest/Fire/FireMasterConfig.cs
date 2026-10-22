// Data-driven oak burn states, GPU presentation settings, and bounded shared-wind response.
using Godot;

[GlobalClass]
public partial class FireMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Texture2D HealthyTexture { get; set; }
    [Export] public Texture2D BurntTexture { get; set; }
    [ExportGroup("Source-pixel ground anchors")]
    [Export] public Vector2 HealthyAnchor { get; set; } = new(548f, 1290f);
    [Export] public Vector2 BurntAnchor { get; set; } = new(565f, 1235f);
    [ExportGroup("Real-time lifecycle")]
    [Export(PropertyHint.Range, "0,20,0.1")] public float IgnitionDelaySeconds { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.1,20,0.1")] public float TransitionSeconds { get; set; } = 5f;
    [Export] public bool MayReignite { get; set; } = false;

    [ExportGroup("FIRE2 - visual effects")]
    [Export] public bool EnableVisualEffects { get; set; } = true;
    [Export(PropertyHint.Range, "1,16,1")] public int MaxActiveEffects { get; set; } = 10;
    [Export(PropertyHint.Range, "1,180,1")] public int FlameParticles { get; set; } = 120;
    [Export(PropertyHint.Range, "1,130,1")] public int SmokeParticles { get; set; } = 48;
    [Export(PropertyHint.Range, "1,120,1")] public int EmberParticles { get; set; } = 32;
    [Export(PropertyHint.Range, "0,1,0.05")] public float FlameIntensity { get; set; } = 0.95f;
    [Export(PropertyHint.Range, "0.85,1.35,0.05")] public float FlamePuffiness { get; set; } = 1.10f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float SmokeIntensity { get; set; } = 0.70f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float EmberIntensity { get; set; } = 0.60f;
    [Export(PropertyHint.Range, "0,6,0.1")] public float SmokeTailSeconds { get; set; } = 1.8f;
    [Export] public Color CharTint { get; set; } = new(0.18f, 0.105f, 0.075f);

    [ExportGroup("FIRE2 - canopy emission")]
    [Export] public bool UseCanopyEmissionMask { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float CanopyTopFraction { get; set; } = 0.07f;
    [Export(PropertyHint.Range, "0.4,0.95,0.01")] public float CanopyBottomFraction { get; set; } = 0.71f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float CanopyAlphaThreshold { get; set; } = 0.55f;

    [ExportGroup("FIRE2 - shared wind response")]
    [Export(PropertyHint.Range, "0,45,1")] public float FlameWindResponse { get; set; } = 9f;
    [Export(PropertyHint.Range, "0,90,1")] public float SmokeWindResponse { get; set; } = 26f;
    [Export(PropertyHint.Range, "0,90,1")] public float EmberWindResponse { get; set; } = 32f;

    [ExportGroup("FIRE2 - firelight")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float FireLightEnergy { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0.1,4,0.05")] public float FireLightRadius { get; set; } = 1.25f;
    [Export] public Color FireLightColour { get; set; } = new(1f, 0.56f, 0.23f);

    [ExportGroup("Manual test")]
    [Export(PropertyHint.Range, "50,1000,10")] public float NearbyRadius { get; set; } = 320f;
}
