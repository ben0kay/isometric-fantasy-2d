// Reusable asset-driven occlusion presentation settings for world props.
using Godot;

[GlobalClass]
public partial class OcclusionProfile : Resource
{
    [Export] public bool Enabled { get => _Enabled; set { if(_Enabled==value) return; _Enabled=value; EmitChanged(); } }
    private bool _Enabled=true;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MinimumAlpha { get => _MinimumAlpha; set { if(_MinimumAlpha==value) return; _MinimumAlpha=value; EmitChanged(); } }
    private float _MinimumAlpha=0.35f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float FadeOutSeconds { get => _FadeOutSeconds; set { if(_FadeOutSeconds==value) return; _FadeOutSeconds=value; EmitChanged(); } }
    private float _FadeOutSeconds=0.18f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float FadeInSeconds { get => _FadeInSeconds; set { if(_FadeInSeconds==value) return; _FadeInSeconds=value; EmitChanged(); } }
    private float _FadeInSeconds=0.25f;
    [Export(PropertyHint.Range, "0,80,1")] public float VisualPadding { get => _VisualPadding; set { if(_VisualPadding==value) return; _VisualPadding=value; EmitChanged(); } }
    private float _VisualPadding=8f;
    [Export(PropertyHint.Range, "0,80,1")] public float BehindAnchorTolerance { get => _BehindAnchorTolerance; set { if(_BehindAnchorTolerance==value) return; _BehindAnchorTolerance=value; EmitChanged(); } }
    private float _BehindAnchorTolerance=5f;
}
