// Manual Northern Hemisphere year preview; commands publish changes without calendar/frame simulation.
using Godot;
using System;
using System.Globalization;

public partial class SeasonController : Node
{
    public bool Enabled { get; private set; }
    public float YearPosition { get; private set; } = 0.5f;
    public float ColourStrength { get; private set; } = 1f;
    public float TimingVariationDays { get; private set; } = 14f;
    public int PreviewDay => Mathf.Clamp((int)Mathf.Round(YearPosition * 365f), 0, 365);
    public DateTime PreviewDate => new DateTime(2001, 1, 1).AddDays(PreviewDay);
    public string SeasonName => PreviewDate.Month switch
    { 3 or 4 or 5 => "Spring", 6 or 7 or 8 => "Summer", 9 or 10 or 11 => "Autumn", _ => "Winter" };
    public string DateLabel => $"{PreviewDate.ToString("MMMM d", CultureInfo.InvariantCulture)} — {SeasonName}";
    public event Action Changed;

    // Copy defaults into world-owned scalars; authored Resources remain untouched.
    // =========================================================
    public void Initialize(SeasonMasterConfig defaults)
    {
        Enabled = defaults?.Enabled ?? false;
        YearPosition = FiniteClamp(defaults?.YearPosition ?? 0.5f, 0f, 1f, 0.5f);
        ColourStrength = FiniteClamp(defaults?.ColourStrength ?? 1f, 0f, 1f, 1f);
        TimingVariationDays = FiniteClamp(defaults?.TimingVariationDays ?? 14f, 0f, 45f, 14f);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // Commands work without F9 and publish only effective changes.
    // =========================================================
    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled; Changed?.Invoke();
    }

    // January is zero; both endpoints show January 1 for a seamless yearly wrap.
    // =========================================================
    public void SetYearPosition(float position)
    {
        float value = FiniteClamp(position, 0f, 1f, 0.5f);
        if (YearPosition == value) return;
        YearPosition = value; Changed?.Invoke();
    }

    // Strength scales the whole seasonal appearance, independently of rain and snow.
    // =========================================================
    public void SetColourStrength(float strength)
    {
        float value = FiniteClamp(strength, 0f, 1f, 1f);
        if (ColourStrength == value) return;
        ColourStrength = value; Changed?.Invoke();
    }

    // Symmetric early/late range; zero synchronizes trees without reassigning their stable groups.
    // =========================================================
    public void SetTimingVariationDays(float days)
    {
        float value = FiniteClamp(days, 0f, 45f, 14f);
        if (TimingVariationDays == value) return;
        TimingVariationDays = value; Changed?.Invoke();
    }

    // Reject non-finite external inputs before clamping preview controls.
    // =========================================================
    private static float FiniteClamp(float value, float min, float max, float fallback)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
