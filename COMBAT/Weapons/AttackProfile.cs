// Shared melee timing and contact data; AxeConfig inherits these fields to preserve existing resource values.
using Godot;
using System;
public enum AttackPresentation { Sweep, Thrust }
[GlobalClass]
public partial class AttackProfile : Resource
{
    [ExportGroup("Presentation")]
    [Export] public AttackPresentation Presentation { get; set; }
    [Export(PropertyHint.Range,"10,160,1")] public float SweepDegrees { get; set; } = 110;
    [Export(PropertyHint.Range,"0,0.3,0.01")] public float SweepWindupFraction { get; set; } = 0.16f;
    [Export(PropertyHint.Range,"0,40,1")] public float ThrustPixels { get; set; } = 14;
    [ExportGroup("Hit reaction")]
    [Export(PropertyHint.Range,"0,120,1")] public float KnockbackDistance { get; set; }
    [Export(PropertyHint.Range,"0.05,0.5,0.01")] public float KnockbackSeconds { get; set; } = 0.18f;
    [Export(PropertyHint.Range,"0,0.6,0.01")] public float StaggerSeconds { get; set; }
    [Export] public bool InterruptAttack { get; set; }
    [ExportGroup("Timing")]
    [Export(PropertyHint.Range,"0.05,3,0.01")] public float AttackSeconds { get; set; } = 0.38f;
    [Export(PropertyHint.Range,"0,1,0.01")] public float ContactProgress { get; set; } = 0.85f;
    [Export(PropertyHint.Range,"0,1,0.01")] public float ContactWindow { get; set; } = 0f;
    [Export(PropertyHint.Range,"0,3,0.01")] public float CooldownSeconds { get; set; }
    [ExportGroup("Contact")]
    [Export] public float BaseDamage { get; set; } = 1;
    [Export(PropertyHint.Range,"1,200,1")] public float HitReach { get; set; } = 52f;
    [Export(PropertyHint.Range,"1,89,1")] public float HitHalfAngleDegrees { get; set; } = 65f;
    [Export] public CombatFaction TargetFactions { get; set; } = CombatFaction.Hostile | CombatFaction.Neutral;
    [Export] public bool HitTools { get; set; }
    [Export(PropertyHint.Layers2DPhysics)] public uint CollisionMask { get; set; } = 3; // World solids + actors, including obstruction rays.

    // Snapshot once per accepted request; edits cannot change a swing already in flight.
    // =========================================================
    public AttackValues Snapshot(float speed)
    {
        if (!Enum.IsDefined(Presentation) || !float.IsFinite(SweepDegrees) || SweepDegrees<10 || SweepDegrees>160
            || !float.IsFinite(SweepWindupFraction) || SweepWindupFraction<0 || SweepWindupFraction>0.3f
            || !float.IsFinite(ThrustPixels) || ThrustPixels<0 || ThrustPixels>40 || !float.IsFinite(speed) || speed < 0.05f || speed > 20 || !float.IsFinite(AttackSeconds) || AttackSeconds < 0.05f || AttackSeconds > 10
            || !float.IsFinite(ContactProgress) || ContactProgress < 0 || ContactProgress > 1
            || !float.IsFinite(ContactWindow) || ContactWindow < 0 || ContactProgress+ContactWindow > 1
            || !float.IsFinite(CooldownSeconds) || CooldownSeconds < 0 || CooldownSeconds > 10
            || !float.IsFinite(BaseDamage) || BaseDamage < 0 || !float.IsFinite(HitReach) || HitReach < 1 || HitReach > 1000
            || !float.IsFinite(HitHalfAngleDegrees) || HitHalfAngleDegrees < 1 || HitHalfAngleDegrees > 89
            || (TargetFactions & ~(CombatFaction.Player|CombatFaction.Hostile|CombatFaction.Neutral)) != 0 || CollisionMask == 0)
            throw new ArgumentException("Invalid melee attack profile.");
        return new(AttackSeconds/speed,CooldownSeconds/speed,ContactProgress,ContactProgress+ContactWindow,
            BaseDamage,HitReach,Mathf.Cos(Mathf.DegToRad(HitHalfAngleDegrees)),TargetFactions,HitTools,CollisionMask,Bound(KnockbackDistance,0,120),Bound(KnockbackSeconds,0.05f,0.5f),Bound(StaggerSeconds,0,0.6f),InterruptAttack);
    }
    // Reject non-finite authored values and bound extreme reaction tuning at snapshot time.
    // =========================================================
    private static float Bound(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value,min,max) : throw new ArgumentException("Invalid reaction value.");
}
public readonly record struct AttackValues(float Duration, float Cooldown, float ContactStart, float ContactEnd,
    float Damage, float Reach, float MinimumDot, CombatFaction Targets, bool HitTools, uint CollisionMask, float KnockbackDistance=0, float KnockbackSeconds=0.18f, float StaggerSeconds=0, bool InterruptAttack=false);
