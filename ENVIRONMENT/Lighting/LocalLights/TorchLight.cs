// Local torch presentation; physical art/collision stay in WORLD content.
using Godot;
public partial class TorchLight : Node
{
    public TorchProfile Config { get; set; }
    public Node2D Torch { get; set; }
    private PointLight2D _light;
    private float _elapsed;
    // Cache the reusable content light once.
    // =========================================================
    public override void _Ready() => _light = Torch.GetNode<PointLight2D>("Light");
    // Retain the original small two-frequency energy variation; no fire gameplay is added.
    // =========================================================
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        Torch.Visible = Config.Enabled; _light.Enabled = Config.Enabled;
        float flicker = 1f + 0.07f * Mathf.Sin(_elapsed * 8.7f) + 0.035f * Mathf.Sin(_elapsed * 17.3f);
        _light.Energy = Config.Energy * flicker;
    }
    // A removed environmental component must not leave hidden invisible light/collision state.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Torch)) Torch.QueueFree();
    }
}
