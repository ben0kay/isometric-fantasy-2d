// Fullscreen colour polish only; developer controls and cloud shade are separate owners.
using Godot;
public partial class WorldColourGrade : Node2D
{
    public ColourGradeProfile Config { get; set; }
    private ColorRect _grade;
    private ShaderMaterial _material;
    // Each world owns one private grading material.
    // =========================================================
    public override void _Ready()
    {
        _grade = GetNode<ColorRect>("Post/Grade");
        _material = (ShaderMaterial)_grade.Material.Duplicate(); _grade.Material = _material;
    }
    // Preserve tested contrast, saturation and vignette, without reading screen state on the CPU.
    // =========================================================
    public override void _Process(double delta)
    {
        _grade.Visible = Config.Enabled;
        _material.SetShaderParameter("contrast", Config.Contrast);
        _material.SetShaderParameter("saturation", Config.Saturation);
        _material.SetShaderParameter("vignette", Config.Vignette);
    }
}
