// Shared regional presentation tuning; no random weather scheduler or survival mechanics.
using Godot;
using System;
[GlobalClass]
public partial class RegionalClimateMasterConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float SeasonalAmplitudeCelsius { get; set; } = 12;
    [Export] public float SnowTransitionCelsius { get; set; } = 2;
    [Export] public float AccumulationSeconds { get; set; } = 45;
    [Export] public float MeltSeconds { get; set; } = 90;
    [Export] public float UpdateSeconds { get; set; } = 0.25f;
    [Export] public int FieldResolution { get; set; } = 48;
    [Export] public float ViewMargin { get; set; } = 900;
    // Bound view sampling and reject non-finite Inspector data at initialization.
    // =========================================================
    public void Validate()
    {
        if(!float.IsFinite(SeasonalAmplitudeCelsius) || SeasonalAmplitudeCelsius<0 || SeasonalAmplitudeCelsius>60
            || !float.IsFinite(SnowTransitionCelsius) || SnowTransitionCelsius<0.1f || SnowTransitionCelsius>10
            || !float.IsFinite(AccumulationSeconds) || AccumulationSeconds<1 || AccumulationSeconds>3600
            || !float.IsFinite(MeltSeconds) || MeltSeconds<1 || MeltSeconds>3600
            || !float.IsFinite(UpdateSeconds) || UpdateSeconds<0.1f || UpdateSeconds>5
            || FieldResolution<16 || FieldResolution>64 || !float.IsFinite(ViewMargin) || ViewMargin<256 || ViewMargin>2400)
            throw new ArgumentException("Invalid regional climate tuning.");
    }
}
