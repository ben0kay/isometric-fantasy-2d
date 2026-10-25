// Runtime-cloned presentation tuning; the existing snowfall rate supplies severity, not another slider.
using Godot;

[GlobalClass]
public partial class BlizzardProfile : Resource
{
    private float _transitionSeconds = 3f;
    [ExportGroup("Storm transition (real seconds)")]
    [Export(PropertyHint.Range, "0,10,0.1")]
    public float TransitionSeconds
    {
        get => _transitionSeconds;
        set
        {
            value = float.IsFinite(value) ? Mathf.Clamp(value, 0f, 10f) : 3f;
            if (_transitionSeconds == value) return;
            _transitionSeconds = value; EmitChanged();
        }
    }
}
