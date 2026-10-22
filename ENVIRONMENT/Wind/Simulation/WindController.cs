// Integrates shared wind displacement so changing direction never teleports clouds or haze.
using Godot;
public partial class WindController : Node
{
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
    public override void _Process(double delta) => Displacement += Velocity * (float)delta;
}
