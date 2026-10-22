// Coordinates optional debug presentation effects without changing gameplay.
using Godot;

public partial class DebugVisualPolish : Node2D
{
    [Export] public PolishMasterConfig Config { get; set; }
    private DebugDayNight _day;
    private DebugWeather _weather;
    private ColorRect _grade;
    private Polygon2D _cloud;
    private ShaderMaterial _gradeMaterial, _cloudMaterial;
    private Node2D _torch;
    private PointLight2D _torchLight;
    private float _elapsed;

    // Cache scene references and build four comparison switches.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new PolishMasterConfig();
        _day = GetNode<DebugDayNight>("../DayNight");
        _weather = GetNode<DebugWeather>("../Weather");
        _grade = GetNode<ColorRect>("Post/Grade");
        _cloud = GetNode<Polygon2D>("Clouds");
        _gradeMaterial = (ShaderMaterial)_grade.Material;
        _cloudMaterial = (ShaderMaterial)_cloud.Material;
        _torch = GetNode<Node2D>("../Actors/Torch");
        _torchLight = _torch.GetNode<PointLight2D>("Light");
        VBoxContainer controls = GetNode<VBoxContainer>("UI/Panel/Controls");
        AddSwitch(controls, "Colour grading", Config.ColourGrading, enabled => Config.ColourGrading = enabled);
        AddSwitch(controls, "Soft sun shadows", Config.SoftShadows, enabled => Config.SoftShadows = enabled);
        AddSwitch(controls, "Moving cloud shade", Config.CloudShade, enabled => Config.CloudShade = enabled);
        AddSwitch(controls, "Flickering torch", Config.Torch, enabled => Config.Torch = enabled);
    }

    // Keep individual effects independently reversible during visual comparisons.
    // =========================================================
    private static void AddSwitch(VBoxContainer parent, string text, bool enabled, System.Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = enabled };
        toggle.Toggled += value => changed(value);
        parent.AddChild(toggle);
    }

    // Update lightweight shader controls and one local light; never search actors each frame.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        _grade.Visible = Config.ColourGrading;
        _cloud.Visible = Config.CloudShade;
        _torch.Visible = Config.Torch;
        _torchLight.Enabled = Config.Torch;
        _gradeMaterial.SetShaderParameter("contrast", Config.Contrast);
        _gradeMaterial.SetShaderParameter("saturation", Config.Saturation);
        _gradeMaterial.SetShaderParameter("vignette", Config.Vignette);
        _cloudMaterial.SetShaderParameter("opacity", Config.CloudOpacity);
        _cloudMaterial.SetShaderParameter("drift", -_weather.Displacement);
        _cloudMaterial.SetShaderParameter("daylight", _day.Daylight);
        _day.SetShadowSoftness(Config.SoftShadows ? Config.ShadowSoftness : 0f);
        float flicker = 1f + 0.07f * Mathf.Sin(_elapsed * 8.7f) + 0.035f * Mathf.Sin(_elapsed * 17.3f);
        _torchLight.Energy = Config.TorchEnergy * flicker;
    }
}
