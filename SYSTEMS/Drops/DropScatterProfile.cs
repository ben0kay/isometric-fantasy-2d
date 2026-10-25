// Shared arcade drop presentation and bounded population budgets; no per-item configuration file is required.
using Godot;
using System;
public readonly record struct DropScatterValues(float Radius,float HopHeight,float FlightSeconds,float Rotation,float Spin,int MaxNodes,int MaxRecords,int MaxBurst,float CollectionRadius);
[GlobalClass]
public partial class DropScatterProfile : Resource
{
    [Export] public float ScatterRadius { get; set; } = 58;
    [Export] public float HopHeight { get; set; } = 24;
    [Export] public float FlightSeconds { get; set; } = 0.55f;
    [Export] public float RotationDegrees { get; set; } = 25;
    [Export] public float SpinDegrees { get; set; } = 120;
    [Export] public float CollectionRadius { get; set; } = 60;
    [Export] public int MaximumLivePickups { get; set; } = 128;
    [Export] public int MaximumSessionRecords { get; set; } = 512;
    [Export] public int MaximumBurstPickups { get; set; } = 16;
    // Reject invalid authoring instead of allowing unbounded bursts or non-finite motion.
    // =========================================================
    public DropScatterValues Snapshot()
    {
        if (!float.IsFinite(ScatterRadius) || ScatterRadius<16 || ScatterRadius>128 || !float.IsFinite(HopHeight) || HopHeight<0 || HopHeight>96
            || !float.IsFinite(FlightSeconds) || FlightSeconds<0.1f || FlightSeconds>2 || !float.IsFinite(RotationDegrees) || RotationDegrees<0 || RotationDegrees>180
            || !float.IsFinite(SpinDegrees) || SpinDegrees<0 || SpinDegrees>720 || MaximumLivePickups<1 || MaximumLivePickups>512
            || !float.IsFinite(CollectionRadius) || CollectionRadius<8 || CollectionRadius>128
            || MaximumSessionRecords<MaximumLivePickups || MaximumSessionRecords>4096 || MaximumBurstPickups<1 || MaximumBurstPickups>32)
            throw new InvalidOperationException("Invalid shared drop tuning.");
        return new(ScatterRadius,HopHeight,FlightSeconds,Mathf.DegToRad(RotationDegrees),Mathf.DegToRad(SpinDegrees),MaximumLivePickups,MaximumSessionRecords,MaximumBurstPickups,CollectionRadius);
    }
}
