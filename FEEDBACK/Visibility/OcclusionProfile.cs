// Reusable asset-driven occlusion presentation settings for world props.
using Godot;

[GlobalClass]
public partial class OcclusionProfile : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MinimumAlpha { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float FadeOutSeconds { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float FadeInSeconds { get; set; } = 0.25f;
    [Export(PropertyHint.Range, "0,80,1")] public float VisualPadding { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,80,1")] public float BehindAnchorTolerance { get; set; } = 5f;
}
