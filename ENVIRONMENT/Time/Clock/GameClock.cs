// Authoritative hour-of-day; weather transitions use real elapsed seconds independently.
using Godot;
public partial class GameClock : Node
{
    public ClockMasterConfig Config { get; set; }
    public float Hour { get; private set; }
    public event System.Action<double> DaysAdvanced;
    public ulong ManualRevision { get; private set; }
    // Initialize before consumers; scrubbing is also available while paused.
    // =========================================================
    public override void _Ready() { ProcessPriority = -30; SetHour(Config.StartHour); }
    // Advance one shared 24-hour loop with no frame-rate-dependent increments.
    // =========================================================
    public override void _Process(double delta)
    {
        using var frameTrace = RuntimeFrameTrace.Measure("GameClock.Process");
        if (Config.Paused || !double.IsFinite(delta) || delta<=0) return;
        double days=delta/Mathf.Max(1f,Config.CycleSeconds);
        Hour=(float)((Hour+days*24d)%24d);
        DaysAdvanced?.Invoke(days);
    }
    // Public command for developer tools and future time-setting callers.
    // =========================================================
    public void SetHour(float hour) { Hour = Mathf.PosMod(hour,24f); ManualRevision++; }
}
