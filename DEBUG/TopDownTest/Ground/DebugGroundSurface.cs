// Applies grass-only settings and comparison controls; never coordinates weather or sunlight.
using Godot;

public partial class DebugGroundSurface : Node2D
{
    [Export] public GroundMasterConfig Config { get; set; }
    [Export] public BiomeMasterConfig Biomes { get; set; }
    private ShaderMaterial _grass;
    private HSlider _variation;
    private Label _label;
    private bool _syncing;

    #region Setup
    // Keep the material private to this test instance and put controls under the polish panel.
    // =========================================================
    public override void _Ready()
    {
        Config ??= new GroundMasterConfig();
        Sprite2D grass = GetNode<Sprite2D>("Grass");
        _grass = (ShaderMaterial)grass.Material.Duplicate();
        grass.Material = _grass;
        var ui = new CanvasLayer { Name = "GroundUI", Layer = 20 };
        AddChild(ui);
        var panel = new PanelContainer { OffsetLeft = 16, OffsetTop = 365, OffsetRight = 336, OffsetBottom = 585 };
        ui.AddChild(panel);
        var controls = new VBoxContainer(); panel.AddChild(controls);
        controls.AddChild(new Label { Text = "Grass surface comparison" });
        AddSwitch(controls, "Natural grass", Config.NaturalGrass, value => Config.NaturalGrass = value);
        AddSwitch(controls, "Organic colour patches", Config.ColourPatches, value => Config.ColourPatches = value);
        AddSwitch(controls, "Fine grass detail", Config.FineDetail, value => Config.FineDetail = value);
        _label = new Label(); controls.AddChild(_label);
        _variation = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = Config.Variation, FocusMode = Control.FocusModeEnum.None };
        _variation.ValueChanged += value => { if (!_syncing) Config.Variation = (float)value; };
        controls.AddChild(_variation);
    }

    // Each layer is independently comparable; keyboard movement keeps its arrow keys.
    // =========================================================
    private static void AddSwitch(VBoxContainer parent, string text, bool value, System.Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = value, FocusMode = Control.FocusModeEnum.None };
        toggle.Toggled += enabled => changed(enabled); parent.AddChild(toggle);
    }
    #endregion

    #region Material settings
    // Update only simple uniforms; the seamless noise resource is shared and generated once.
    // =========================================================
    public override void _Process(double delta)
    {
        _grass.SetShaderParameter("natural_grass", Config.NaturalGrass);
        _grass.SetShaderParameter("colour_patches", Config.ColourPatches);
        _grass.SetShaderParameter("fine_detail", Config.FineDetail);
        _grass.SetShaderParameter("variation", Mathf.Clamp(Config.Variation, 0f, 1f));
        _grass.SetShaderParameter("patch_size", Mathf.Max(80f, Config.PatchSize));
        _grass.SetShaderParameter("shade_colour", Biomes?.West?.ShadeColour ?? Config.ShadeColour);
        _grass.SetShaderParameter("fresh_colour", Biomes?.West?.FreshColour ?? Config.FreshColour);
        _grass.SetShaderParameter("dry_colour", Biomes?.West?.DryColour ?? Config.DryColour);
        _grass.SetShaderParameter("detail_strength", Config.DetailStrength);
        if (Biomes?.West != null && Biomes.East != null)
        {
            _grass.SetShaderParameter("biome_boundary_x", Biomes.BoundaryX);
            _grass.SetShaderParameter("biome_curve_amplitude", Biomes.CurveAmplitude);
            _grass.SetShaderParameter("biome_curve_scale", Biomes.CurveScale);
            _grass.SetShaderParameter("biome_blend_width", Biomes.BlendWidth);
            _grass.SetShaderParameter("west_base", Biomes.West.BaseColour);
            _grass.SetShaderParameter("east_base", Biomes.East.BaseColour);
            _grass.SetShaderParameter("east_shade", Biomes.East.ShadeColour);
            _grass.SetShaderParameter("east_fresh", Biomes.East.FreshColour);
            _grass.SetShaderParameter("east_dry", Biomes.East.DryColour);
        }
        _syncing = true; _variation.Value = Config.Variation; _syncing = false;
        _label.Text = $"Colour variation: {Config.Variation:P0}";
    }
    #endregion
}
