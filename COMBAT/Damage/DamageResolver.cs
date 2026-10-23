// Shared deterministic damage contract; no input, collision queries, scene paths or feedback dependencies.
using System;
using Godot;
public readonly record struct DamageRequest(float BaseDamage, Vector2? Origin = null);
public readonly record struct DamageResult(float RequestedDamage, int ResolvedDamage, int AppliedDamage, int HealthBefore, int HealthAfter, bool Killed, bool Blocked = false);
public interface ICombatant
{
    ActorHealth HealthState { get; }
    ActorStats Stats { get; }
    DamageResult ReceiveDamage(DamageRequest request, EffectiveStats attacker);
}
public static class DamageResolver
{
    // Scale, subtract flat defense, apply directional reduction, then round once; no implicit minimum damage.
    // =========================================================
    public static int Resolve(DamageRequest request, EffectiveStats attacker, EffectiveStats defender, float reduction = 0)
    {
        if (!float.IsFinite(request.BaseDamage) || request.BaseDamage < 0) throw new ArgumentOutOfRangeException(nameof(request));
        if (!float.IsFinite(reduction) || reduction<0 || reduction>1) throw new ArgumentOutOfRangeException(nameof(reduction));
        EffectiveStats.Validate(attacker); EffectiveStats.Validate(defender);
        double damage=Math.Max(0,(double)request.BaseDamage*attacker.DamageMultiplier-defender.Defense);
        return (int)Math.Min(1000000,Math.Round(damage*(1-reduction),MidpointRounding.AwayFromZero));
    }
    // Resolve once and mutate exactly one HP source; the returned value describes actual overkill/clamping.
    // =========================================================
    public static DamageResult Apply(ActorHealth health, DamageRequest request, EffectiveStats attacker, EffectiveStats defender, float reduction = 0)
    {
        int resolved=Resolve(request,attacker,defender,reduction), before=health.Current;
        int applied=health.Damage(resolved);
        bool blocked=before>0 && reduction>0 && resolved<Resolve(request,attacker,defender);
        return new(request.BaseDamage,resolved,applied,before,health.Current,before>0 && health.Current==0,blocked);
    }
}
