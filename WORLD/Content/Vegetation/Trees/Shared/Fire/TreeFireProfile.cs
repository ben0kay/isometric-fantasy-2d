// Tree artwork and canopy settings; lifecycle rules are inherited from the shared fire profile.
using Godot;
namespace FantasyGame.WorldInteractions.Fire;
[GlobalClass]
public partial class TreeFireProfile : FireProfile
{
    [Export] public Texture2D HealthyTexture { get; set; }
    [Export] public Texture2D BurntTexture { get; set; }
    [ExportGroup("Source-pixel ground anchors")]
    [Export] public Vector2 HealthyAnchor { get; set; } = new(548f, 1290f);
    [Export] public Vector2 BurntAnchor { get; set; } = new(565f, 1235f);
    [ExportGroup("FIRE2 - canopy emission")]
    [Export] public bool UseCanopyEmissionMask { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float CanopyTopFraction { get; set; } = 0.07f;
    [Export(PropertyHint.Range, "0.4,0.95,0.01")] public float CanopyBottomFraction { get; set; } = 0.71f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float CanopyAlphaThreshold { get; set; } = 0.55f;

}
