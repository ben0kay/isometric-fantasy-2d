// Movement and following-camera settings for the production player.
using Godot;

[GlobalClass]
public partial class PlayerMasterConfig : Resource
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "20,400,5")] public float WalkSpeed { get; set; } = 150f;
    [Export(PropertyHint.Range, "1,3,0.05")] public float SprintMultiplier { get; set; } = 1.55f;
    [ExportGroup("Dodge")]
    [Export] public bool DodgeEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.12,0.5,0.01")] public float DodgeDoubleTapSeconds { get; set; } = 0.28f;
    [Export(PropertyHint.Range, "100,900,10")] public float DodgeSpeed { get; set; } = 480f;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float DodgeSeconds { get; set; } = 0.16f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float DodgeCooldownSeconds { get; set; } = 0.6f;
    [ExportGroup("Combat foundation")]
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public CombatStatProfile CombatStats { get; set; }
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "1,5,0.25")] public float CameraZoom { get; set; } = 2f;
    [Export(PropertyHint.Range, "1,2,0.05")] public float CameraMaxViewMultiplier { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.01,0.25,0.01")] public float CameraZoomWheelStep { get; set; } = 0.05f;
}
