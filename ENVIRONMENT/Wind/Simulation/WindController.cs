// Integrates shared wind displacement so changing direction never teleports clouds or haze.
using Godot;
public partial class WindController : Node
{
    private readonly System.Collections.Generic.HashSet<ShaderMaterial> _followers = new();
    private float _lastStrength = float.NaN;
    public WindMasterConfig Config { get; set; }
    public float Strength => Mathf.Clamp(Config.Strength, -3f, 3f);
    public Vector2 Velocity => Config.DriftPerStrength * Strength;
    public Vector2 Displacement { get; private set; }
    public WindState State => new(Strength, Velocity, Displacement);
    // Wind updates ahead of precipitation and all visual consumers.
    // =========================================================
    public override void _Ready() => ProcessPriority = -20;
    // Integrate velocity instead of deriving position from current strength and elapsed time.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("WindController.Process");
        Displacement += Velocity * (float)delta;
        if (Mathf.IsEqualApprox(_lastStrength,Strength)) return;
        _lastStrength = Strength;
        foreach (var material in _followers) material.SetShaderParameter("wind_multiplier",Strength);
    }
    // Composite foliage shaders follow shared wind even when wetness simulation is asleep.
    // =========================================================
    public void RegisterMaterial(ShaderMaterial material) { _followers.Add(material); material.SetShaderParameter("wind_multiplier",Strength); }
    public void UnregisterMaterial(ShaderMaterial material) => _followers.Remove(material);
    public override void _ExitTree() => _followers.Clear();
}
