// Shared approach policy; real attack eligibility always takes priority over positioning.
using Godot;
public enum EngagementMode { Direct, Spread }
[GlobalClass]
public partial class EngagementProfile : Resource
{
    [Export] public EngagementMode Mode { get; set; } = EngagementMode.Spread;
    [Export(PropertyHint.Range,"18,80,1")] public float PreferredDistance { get; set; } = 32;
    [Export(PropertyHint.Range,"0,40,1")] public float Spacing { get; set; } = 10;
    [Export] public bool PersistenceEnabled { get; set; } = true;
    [Export(PropertyHint.Range,"0,5,0.1")] public float PersistenceSeconds { get; set; } = 1.2f;
    [Export] public bool MovementThresholdEnabled { get; set; } = true;
    [Export(PropertyHint.Range,"0,100,1")] public float TargetMovementThreshold { get; set; } = 16;
    [Export(PropertyHint.Range,"0.2,2,0.05")] public float ReconsiderSeconds { get; set; } = 0.5f;
    [Export] public bool BlockedFallbackEnabled { get; set; } = true;
    [Export(PropertyHint.Range,"0.3,4,0.1")] public float BlockedTimeout { get; set; } = 1;
    [Export] public bool DetourLimitEnabled { get; set; } = true;
    [Export(PropertyHint.Range,"0,200,5")] public float MaximumDetour { get; set; } = 70;
}
