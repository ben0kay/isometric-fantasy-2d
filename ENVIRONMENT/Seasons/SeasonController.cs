// Shared Northern Hemisphere calendar, driven by the existing clock with event-driven daily palette updates.
using Godot;
using System;
using System.Globalization;

public partial class SeasonController : Node
{
    private GameClock _clock;
    public bool Automatic { get; private set; }
    public bool ManualOverride { get; private set; }
    public int YearLengthDays { get; private set; } = 120;
    public double AbsoluteDay { get; private set; } = 60;
    public int Year => (int)(AbsoluteDay/YearLengthDays)+1;
    public bool Enabled { get; private set; }
    public float YearPosition { get; private set; } = 0.5f;
    public float ColourStrength { get; private set; } = 1f;
    public float TimingVariationDays { get; private set; } = 14f;
    public int PreviewDay => Mathf.Clamp((int)Mathf.Round(YearPosition * 365f), 0, 365);
    public DateTime PreviewDate => new DateTime(2001, 1, 1).AddDays(PreviewDay);
    public string SeasonName => PreviewDate.Month switch
    { 3 or 4 or 5 => "Spring", 6 or 7 or 8 => "Summer", 9 or 10 or 11 => "Autumn", _ => "Winter" };
    public string DateLabel => $"Year {Year} / {PreviewDate.ToString("MMMM d", CultureInfo.InvariantCulture)} — {SeasonName}";
    public event Action Changed;

    // Copy defaults into world-owned scalars; authored Resources remain untouched.
    // =========================================================
    public void Initialize(SeasonMasterConfig defaults)
    {
        Automatic=defaults?.Automatic ?? false;
        YearLengthDays=Math.Clamp(defaults?.YearLengthDays ?? 120,30,730);
        AbsoluteDay=double.IsFinite(defaults?.StartingDay ?? 60) ? Math.Clamp(defaults?.StartingDay ?? 60,0,100000000) : 60;
        Enabled = defaults?.Enabled ?? false;
        YearPosition = FiniteClamp(defaults?.YearPosition ?? 0.5f, 0f, 1f, 0.5f);
        ColourStrength = FiniteClamp(defaults?.ColourStrength ?? 1f, 0f, 1f, 1f);
        TimingVariationDays = FiniteClamp(defaults?.TimingVariationDays ?? 14f, 0f, 45f, 14f);
        if(Automatic) YearPosition=(float)(AbsoluteDay%YearLengthDays/YearLengthDays);
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
        ManualOverride=true;
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

    // Subscribe to the existing clock; calendar has no separate per-frame callback.
    // =========================================================
    public void BindClock(GameClock clock)
    {
        if(_clock!=null) _clock.DaysAdvanced-=Advance;
        _clock=clock; if(_clock!=null) _clock.DaysAdvanced+=Advance;
    }
    // Advance fractional days but publish foliage/climate changes only on simulation-day boundaries.
    // =========================================================
    public void Advance(double days)
    {
        if(!Automatic || !double.IsFinite(days) || days<=0) return;
        long previous=(long)AbsoluteDay; AbsoluteDay=Math.Min(100000000,AbsoluteDay+days);
        if(ManualOverride || (long)AbsoluteDay==previous) return;
        YearPosition=(float)(AbsoluteDay%YearLengthDays/YearLengthDays); Changed?.Invoke();
    }
    // Automatic mode and explicit date controls are world-owned commands, never Resource edits.
    // =========================================================
    public void SetAutomatic(bool value) { Automatic=value; if(value) ClearOverride(); else Changed?.Invoke(); }
    public void SetCalendarDay(double day)
    {
        if(!double.IsFinite(day)) return;
        AbsoluteDay=Math.Clamp(day,0,100000000); ClearOverride();
    }
    public void SetYearLength(int days)
    {
        int next=Math.Clamp(days,30,730);
        double years=AbsoluteDay/YearLengthDays; YearLengthDays=next; AbsoluteDay=years*next; ClearOverride();
    }
    public void ClearOverride()
    {
        ManualOverride=false; YearPosition=(float)(AbsoluteDay%YearLengthDays/YearLengthDays); Changed?.Invoke();
    }
    // Save authored calendar state and active debugging overrides explicitly; restore before consumers resume.
    // =========================================================
    public SavedCalendar Capture() => new(AbsoluteDay,YearLengthDays,Automatic,false,(float)(AbsoluteDay%YearLengthDays/YearLengthDays),Enabled,ColourStrength,TimingVariationDays);
    public void Restore(SavedCalendar state)
    {
        if(state==null) return; state.Validate();
        AbsoluteDay=state.Day; YearLengthDays=state.YearLength; Automatic=state.Automatic; ManualOverride=state.ManualOverride;
        YearPosition=state.Position; Enabled=state.FoliageEnabled; ColourStrength=state.Strength; TimingVariationDays=state.Variation;
        Changed?.Invoke();
    }
    // Detach the clock when leaving a World; no stale calendar subscriber survives Continue.
    // =========================================================
    public override void _ExitTree() { if(_clock!=null) _clock.DaysAdvanced-=Advance; }

    // Reject non-finite external inputs before clamping preview controls.
    // =========================================================
    private static float FiniteClamp(float value, float min, float max, float fallback)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
