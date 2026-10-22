// Local torch presentation; physical art/collision stay in WORLD content.
using Godot;
public partial class TorchLight : Node
{
    public TorchProfile Config { get; set; }
    public Node2D Torch { get; set; }
    private PointLight2D _light;
    private float _elapsed;
    public int StaticWrites { get; private set; }
    public int EnergyWrites { get; private set; }
    // Cache the reusable content light once.
    // =========================================================
    public override void _Ready() { _light = Torch.GetNode<PointLight2D>("Light"); Config.Changed += ApplySettings; ApplySettings(); }
    // Retain the original small two-frequency energy variation; no fire gameplay is added.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("TorchLight.Process");
        if (!Config.Enabled || Config.Energy <= 0) return;
        _elapsed += (float)delta;
        float flicker = 1f + 0.07f * Mathf.Sin(_elapsed * 8.7f) + 0.035f * Mathf.Sin(_elapsed * 17.3f);
        SetEnergy(Mathf.Max(0,Config.Energy) * flicker);
    }
    // Brightness and texture reach remain independent; native Godot handles shadow updates.
    // =========================================================
    private void ApplySettings()
    {
        bool lit = Config.Enabled && Config.Energy > 0;
        if (Torch.Visible != Config.Enabled) { Torch.Visible = Config.Enabled; StaticWrites++; }
        if (_light.Enabled != lit) { _light.Enabled = lit; StaticWrites++; }
        SetProcess(lit);
        if (!Config.Enabled) { SetEnergy(0); return; }
        float scale = Mathf.Max(0.01f,Config.TextureScale), softness = Mathf.Clamp(Config.ShadowSoftness,0,8);
        if (_light.TextureScale != scale) { _light.TextureScale = scale; StaticWrites++; }
        if (_light.Color != Config.Colour) { _light.Color = Config.Colour; StaticWrites++; }
        if (_light.ShadowEnabled != Config.CastShadows) { _light.ShadowEnabled = Config.CastShadows; StaticWrites++; }
        if (_light.ShadowFilter != Config.ShadowFilter) { _light.ShadowFilter = Config.ShadowFilter; StaticWrites++; }
        if (_light.ShadowFilterSmooth != softness) { _light.ShadowFilterSmooth = softness; StaticWrites++; }
        float flicker = 1f + 0.07f * Mathf.Sin(_elapsed * 8.7f) + 0.035f * Mathf.Sin(_elapsed * 17.3f);
        SetEnergy(Mathf.Max(0,Config.Energy) * flicker);
    }
    private void SetEnergy(float value)
    {
        if (_light.Energy == value) return;
        _light.Energy = value; EnergyWrites++;
    }

    // A removed environmental component must not leave hidden invisible light/collision state.
    // =========================================================
    public override void _ExitTree()
    {
        Config.Changed -= ApplySettings;
        if (GodotObject.IsInstanceValid(Torch)) Torch.QueueFree();
    }
}
