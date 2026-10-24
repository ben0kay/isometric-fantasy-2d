// One event-driven season preview per world; no calendar, persistence or per-frame simulation.
using Godot;
using System;

public partial class SeasonController : Node
{
    public bool Enabled { get; private set; }
    public float YearPosition { get; private set; } = 0.25f;
    public string SeasonName => ((int)Mathf.Floor(YearPosition * 4f + 0.5f) % 4) switch
    { 0 => "Spring", 1 => "Summer", 2 => "Autumn", _ => "Winter" };
    public event Action Changed;

    // Copy authored defaults into private world state before consumers attach.
    // =========================================================
    public void Initialize(SeasonMasterConfig defaults)
    {
        Enabled = defaults?.Enabled ?? false;
        YearPosition = Normalize(defaults?.YearPosition ?? 0.25f);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // Commands work without developer UI, and notify only when the effective state changes.
    // =========================================================
    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled; Changed?.Invoke();
    }

    // Keep both slider endpoints selectable; 1 and 0 represent the same Spring palette.
    // =========================================================
    public void SetYearPosition(float position)
    {
        float value = Normalize(position);
        if (YearPosition == value) return;
        YearPosition = value; Changed?.Invoke();
    }

    // Reject non-finite external inputs before clamping the manual preview range.
    // =========================================================
    private static float Normalize(float value) => float.IsFinite(value) ? Mathf.Clamp(value, 0f, 1f) : 0.25f;
}
