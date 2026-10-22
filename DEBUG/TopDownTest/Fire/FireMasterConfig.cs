// Oak artwork anchors and real-time FIRE1 lifecycle settings; no particle or lightning rules.
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
    [ExportGroup("Manual test")]
    [Export(PropertyHint.Range, "50,1000,10")] public float NearbyRadius { get; set; } = 320f;
}
