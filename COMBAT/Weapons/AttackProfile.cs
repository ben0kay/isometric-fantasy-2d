// Shared melee timing and contact data; AxeConfig inherits these fields to preserve existing resource values.
using Godot;
using System;
[GlobalClass]
public partial class AttackProfile : Resource
{
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
    [Export(PropertyHint.Layers2DPhysics)] public uint CollisionMask { get; set; } = 1;

    // Snapshot once per accepted request; edits cannot change a swing already in flight.
    // =========================================================
    public AttackValues Snapshot(float speed)
    {
        if (!float.IsFinite(speed) || speed < 0.05f || speed > 20 || !float.IsFinite(AttackSeconds) || AttackSeconds < 0.05f || AttackSeconds > 10
            || !float.IsFinite(ContactProgress) || ContactProgress < 0 || ContactProgress > 1
            || !float.IsFinite(ContactWindow) || ContactWindow < 0 || ContactProgress+ContactWindow > 1
            || !float.IsFinite(CooldownSeconds) || CooldownSeconds < 0 || CooldownSeconds > 10
            || !float.IsFinite(BaseDamage) || BaseDamage < 0 || !float.IsFinite(HitReach) || HitReach < 1 || HitReach > 1000
            || !float.IsFinite(HitHalfAngleDegrees) || HitHalfAngleDegrees < 1 || HitHalfAngleDegrees > 89
            || (TargetFactions & ~(CombatFaction.Player|CombatFaction.Hostile|CombatFaction.Neutral)) != 0 || CollisionMask == 0)
            throw new ArgumentException("Invalid melee attack profile.");
        return new(AttackSeconds/speed,CooldownSeconds/speed,ContactProgress,ContactProgress+ContactWindow,
            BaseDamage,HitReach,Mathf.Cos(Mathf.DegToRad(HitHalfAngleDegrees)),TargetFactions,HitTools,CollisionMask);
    }
}
public readonly record struct AttackValues(float Duration, float Cooldown, float ContactStart, float ContactEnd,
    float Damage, float Reach, float MinimumDot, CombatFaction Targets, bool HitTools, uint CollisionMask);
