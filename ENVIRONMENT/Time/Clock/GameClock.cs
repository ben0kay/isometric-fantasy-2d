// Authoritative hour-of-day; weather transitions use real elapsed seconds independently.
using Godot;
public partial class GameClock : Node
{
    public ClockMasterConfig Config { get; set; }
    public float Hour { get; private set; }
    // Initialize before consumers; scrubbing is also available while paused.
    // =========================================================
    public override void _Ready() { ProcessPriority = -30; SetHour(Config.StartHour); }
    // Advance one shared 24-hour loop with no frame-rate-dependent increments.
    // =========================================================
    public override void _Process(double delta)
    {
        if (!Config.Paused) SetHour(Hour + (float)delta * 24f / Mathf.Max(1f, Config.CycleSeconds));
    }
    // Public command for developer tools and future time-setting callers.
    // =========================================================
    public void SetHour(float hour) => Hour = Mathf.PosMod(hour, 24f);
}
