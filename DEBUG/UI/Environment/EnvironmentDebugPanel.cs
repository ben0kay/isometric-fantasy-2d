// Removable developer controls only; production modules never read or create this UI.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnvironmentDebugPanel : CanvasLayer
{
    [Export] public NodePath EnvironmentPath { get; set; } = "../Environment";
    private EnvironmentRuntime _environment;
    private PanelContainer _panel;
    private Label _status;
    private WorldDiagnosticsPanel _diagnostics;
    private readonly List<Action> _refresh = new();
    private bool _syncing;

    #region Setup and sections
    // Defer until WorldScene has initialized its environment after scenery generation.
    // =========================================================
    public override void _Ready() => CallDeferred(nameof(Build));
    // A scrollable collapsible panel stays outside grading and leaves movement keys unfocused.
    // =========================================================
    private void Build()
    {
        _environment = GetNodeOrNull<EnvironmentRuntime>(EnvironmentPath);
        if (_environment?.Initialized != true) { GD.PushWarning("Environment panel has no initialized production environment."); return; }
        Layer = 20;
        for (Node ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not WorldScene world) continue;
            _diagnostics = new WorldDiagnosticsPanel { Name = "WorldDiagnostics", World = world, Environment = _environment };
            AddChild(_diagnostics); break;
        }
        _panel = new PanelContainer { OffsetLeft = 16, OffsetTop = 16, OffsetRight = 366, OffsetBottom = 940 };
        AddChild(_panel);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(340, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _panel.AddChild(scroll);
        var controls = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(controls);
        controls.AddChild(new Label { Text = "Environment controls — F9 to hide" });
        _status = new Label(); controls.AddChild(_status);
        BiomeEnvironmentProfile s = _environment.Settings;
        var time = Section(controls, "Time", true);
        Slider(time, "Hour", 0, 23.999, 0.01, () => _environment.Clock.Hour, v => _environment.Clock.SetHour((float)v));
        Toggle(time, "Pause clock", () => _environment.Clock.Config.Paused, v => _environment.Clock.Config.Paused = v);
        Slider(time, "Seconds per day", 5, 3600, 1, () => _environment.Clock.Config.CycleSeconds, v => _environment.Clock.Config.CycleSeconds = (float)v);
        var wind = Section(controls, "Shared wind", true);
        Slider(wind, "Left / calm / right", -3, 3, 0.05, () => s.Wind.Strength, v => s.Wind.Strength = (float)v);
        var lighting = Section(controls, "Lighting and clouds", false);
        Toggle(lighting, "Colour grading", () => s.ColourGrade.Enabled, v => s.ColourGrade.Enabled = v);
        Toggle(lighting, "Soft sun shadows", () => s.Lighting.SoftShadows, v => s.Lighting.SoftShadows = v);
        Slider(lighting, "Sun shadow tolerance (px)", 0, 1, 0.05, () => s.Lighting.ShadowProjectionTolerancePixels, v => s.Lighting.ShadowProjectionTolerancePixels = (float)v);
        Toggle(lighting, "Moving cloud shade", () => s.Clouds.Enabled, v => s.Clouds.Enabled = v);
        Toggle(lighting, "Flickering torch", () => s.Torch.Enabled, v => s.Torch.Enabled = v);
        Slider(lighting, "Contrast", 0.8, 1.5, 0.01, () => s.ColourGrade.Contrast, v => s.ColourGrade.Contrast = (float)v);
        Slider(lighting, "Saturation", 0, 2, 0.01, () => s.ColourGrade.Saturation, v => s.ColourGrade.Saturation = (float)v);
        Slider(lighting, "Vignette", 0, 0.3, 0.01, () => s.ColourGrade.Vignette, v => s.ColourGrade.Vignette = (float)v);
        Slider(lighting, "Cloud opacity", 0, 0.4, 0.01, () => s.Clouds.Opacity, v => s.Clouds.Opacity = (float)v);
        Slider(lighting, "Torch energy", 0, 2, 0.05, () => s.Torch.Energy, v => s.Torch.Energy = (float)v);
        Toggle(lighting, "Local torch shadows", () => s.Torch.CastShadows, v => s.Torch.CastShadows = v);
        Slider(lighting, "Torch radius scale", 0.25, 3, 0.05, () => s.Torch.TextureScale, v => s.Torch.TextureScale = (float)v);
        Slider(lighting, "Local shadow softness", 0, 8, 0.1, () => s.Torch.ShadowSoftness, v => s.Torch.ShadowSoftness = (float)v);
        var atmosphere = Section(controls, "Atmosphere", false);
        Toggle(atmosphere, "Ground haze", () => s.Atmosphere.Haze, v => s.Atmosphere.Haze = v);
        Toggle(atmosphere, "Pollen / dust", () => s.Atmosphere.Pollen, v => s.Atmosphere.Pollen = v);
        Toggle(atmosphere, "Falling leaves", () => s.Atmosphere.Leaves, v => s.Atmosphere.Leaves = v);
        Toggle(atmosphere, "Torch smoke", () => s.Atmosphere.Smoke, v => s.Atmosphere.Smoke = v);
        Toggle(atmosphere, "Torch embers", () => s.Atmosphere.Embers, v => s.Atmosphere.Embers = v);
        Slider(atmosphere, "Haze density", 0, 0.6, 0.01, () => s.Atmosphere.HazeDensity, v => s.Atmosphere.HazeDensity = (float)v);
        var weather = Section(controls, "Rain and lightning", true);
        Slider(weather, "Rain", 0, 1, 0.01, () => s.Weather.RainIntensity, v => s.Weather.RainIntensity = (float)v);
        Toggle(weather, "Ground splashes", () => s.Weather.Splashes, v => s.Weather.Splashes = v);
        Toggle(weather, "Wet tree foliage", () => s.Weather.WetFoliage, v => s.Weather.WetFoliage = v);
        Slider(weather, "Leaf darkening", 0, 0.35, 0.01, () => s.Weather.WetFoliageDarkening,
            v => s.Weather.WetFoliageDarkening = (float)v);
        Toggle(weather, "Overcast lighting", () => s.Weather.Overcast, v => s.Weather.Overcast = v);
        Slider(weather, "Overcast shadow reduction", 0, 1, 0.01,
            () => s.Weather.OvercastShadowReduction,
            v => s.Weather.OvercastShadowReduction = (float)v);
        Toggle(weather, "Storm lightning", () => s.Weather.Lightning, v => s.Weather.Lightning = v);
        Button(weather, "Test nearby flash", () => _environment.Weather.Lightning.Trigger(false));
        Button(weather, "Test visible strike", () => _environment.Weather.Lightning.Trigger(true));
        var snow = Section(controls, "Snow", true);
        Toggle(snow, "Enable snow", () => s.Snow.Enabled, v => s.Snow.Enabled = v);
        Slider(snow, "Settled snow", 0, 1, 0.01, () => s.Snow.Coverage, v => _environment.Snow.SetCoverage((float)v));
        Toggle(snow, "Falling snow", () => s.Snow.Snowfall, v => s.Snow.Snowfall = v);
        Slider(snow, "Snowfall intensity", 0, 1, 0.01, () => s.Snow.SnowfallRate, v => s.Snow.SnowfallRate = (float)v);
        Toggle(snow, "Auto accumulate / melt", () => s.Snow.AutoAccumulate, v => s.Snow.AutoAccumulate = v);
    }

    // Sections collapse without pausing their underlying production effects.
    // =========================================================
    private static VBoxContainer Section(VBoxContainer parent, string title, bool open)
    {
        var toggle = new Button { Text = title, ToggleMode = true, ButtonPressed = open, FocusMode = Control.FocusModeEnum.None }; parent.AddChild(toggle);
        var body = new VBoxContainer { Visible = open }; parent.AddChild(body); toggle.Toggled += value => body.Visible = value; return body;
    }
    // Refresh widgets from runtime state while preventing feedback through ValueChanged.
    // =========================================================
    private void Slider(VBoxContainer parent, string title, double min, double max, double step, Func<double> get, Action<double> set)
    {
        var label = new Label(); parent.AddChild(label);
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step, Value = get(), FocusMode = Control.FocusModeEnum.None }; parent.AddChild(slider);
        slider.ValueChanged += value => { if (!_syncing) set(value); };
        _refresh.Add(() => { slider.Value = get(); label.Text = $"{title}: {get():0.00}"; });
    }
    // Checkboxes are commands against private runtime settings, never saved resources.
    // =========================================================
    private void Toggle(VBoxContainer parent, string title, Func<bool> get, Action<bool> set)
    {
        var toggle = new CheckButton { Text = title, ButtonPressed = get(), FocusMode = Control.FocusModeEnum.None }; parent.AddChild(toggle);
        toggle.Toggled += value => { if (!_syncing) set(value); }; _refresh.Add(() => toggle.ButtonPressed = get());
    }
    // Manual storm previews reuse the production API and its existing cooldown/master switch.
    // =========================================================
    private static void Button(VBoxContainer parent, string title, Action action)
    {
        var button = new Button { Text = title, FocusMode = Control.FocusModeEnum.None }; parent.AddChild(button); button.Pressed += action;
    }
    #endregion

    #region Live status
    // Keep Inspector/auto-accumulation changes visible without influencing the simulation.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("EnvironmentDebugPanel.Process");
        if (_panel == null || !GodotObject.IsInstanceValid(_environment)) return;
        _panel.OffsetBottom = Mathf.Max(200f, GetViewport().GetVisibleRect().Size.Y - 16f);
        if (!_panel.Visible) return;
        _syncing = true; foreach (Action refresh in _refresh) refresh(); _syncing = false;
        _status.Text = $"{_environment.Clock.Hour:00.00} h | Rain {_environment.Weather.Rain:P0}\nSettled snow {_environment.Snow.Coverage:P0}";
    }
    // Hiding a panel is a developer affordance; effects keep running with it absent entirely.
    // =========================================================
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F9 && _panel != null)
        { _panel.Visible = !_panel.Visible; if (_diagnostics != null) _diagnostics.Visible = _panel.Visible; GetViewport().SetInputAsHandled(); }
    }
    #endregion
}
