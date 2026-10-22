// Fullscreen colour polish only; developer controls and cloud shade are separate owners.
using Godot;
public partial class WorldColourGrade : Node2D
{
    public ColourGradeProfile Config { get; set; }
    private ColorRect _grade;
    private ShaderMaterial _material;
    private float _contrast = float.NaN, _saturation = float.NaN, _vignette = float.NaN;
    public int MaterialWrites { get; private set; }
    // Each world owns one private grading material.
    // =========================================================
    public override void _Ready()
    {
        _grade = GetNode<ColorRect>("Post/Grade");
        _material = (ShaderMaterial)_grade.Material.Duplicate(); _grade.Material = _material;
        SetProcess(false); Config.Changed += ApplySettings; ApplySettings();
    }
    // Apply only profile changes; disabled grading defers uploads until explicitly re-enabled.
    // =========================================================
    private void ApplySettings()
    {
        if (_grade.Visible != Config.Enabled) _grade.Visible = Config.Enabled;
        if (!Config.Enabled) return;
        if (_contrast != Config.Contrast) { _contrast=Config.Contrast; _material.SetShaderParameter("contrast",_contrast); MaterialWrites++; }
        if (_saturation != Config.Saturation) { _saturation=Config.Saturation; _material.SetShaderParameter("saturation",_saturation); MaterialWrites++; }
        if (_vignette != Config.Vignette) { _vignette=Config.Vignette; _material.SetShaderParameter("vignette",_vignette); MaterialWrites++; }
    }
    // A removed world does not retain a live Resource subscription.
    // =========================================================
    public override void _ExitTree() => Config.Changed -= ApplySettings;
}
