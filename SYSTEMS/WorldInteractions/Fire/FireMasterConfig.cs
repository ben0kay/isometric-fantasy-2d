// Data-driven oak burn states, GPU presentation settings, and bounded shared-wind response.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;

[GlobalClass]
public partial class FireMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
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

    [ExportGroup("FIRE2 - shared wind response")]
    [Export(PropertyHint.Range, "0,45,1")] public float FlameWindResponse { get; set; } = 9f;
    [Export(PropertyHint.Range, "0,90,1")] public float SmokeWindResponse { get; set; } = 26f;
    [Export(PropertyHint.Range, "0,90,1")] public float EmberWindResponse { get; set; } = 32f;

    [ExportGroup("FIRE2 - firelight")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float FireLightEnergy { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0.1,4,0.05")] public float FireLightRadius { get; set; } = 1.25f;
    [Export] public Color FireLightColour { get; set; } = new(1f, 0.56f, 0.23f);

}
