// Shared deterministic damage contract; no input, collision queries, scene paths or feedback dependencies.
using System;
public readonly record struct DamageRequest(float BaseDamage);
public readonly record struct DamageResult(float RequestedDamage, int ResolvedDamage, int AppliedDamage, int HealthBefore, int HealthAfter, bool Killed);
public interface ICombatant
{
    ActorHealth HealthState { get; }
    ActorStats Stats { get; }
    DamageResult ReceiveDamage(DamageRequest request, EffectiveStats attacker);
}
public static class DamageResolver
{
    // Flat defense follows attacker scaling; round once away from zero, with no implicit minimum damage.
    // =========================================================
    public static int Resolve(DamageRequest request, EffectiveStats attacker, EffectiveStats defender)
    {
        if (!float.IsFinite(request.BaseDamage) || request.BaseDamage < 0) throw new ArgumentOutOfRangeException(nameof(request));
        EffectiveStats.Validate(attacker); EffectiveStats.Validate(defender);
        double damage=Math.Max(0,(double)request.BaseDamage*attacker.DamageMultiplier-defender.Defense);
        return (int)Math.Min(1000000,Math.Round(damage,MidpointRounding.AwayFromZero));
    }
    // Resolve once and mutate exactly one HP source; the returned value describes actual overkill/clamping.
    // =========================================================
    public static DamageResult Apply(ActorHealth health, DamageRequest request, EffectiveStats attacker, EffectiveStats defender)
    {
        int resolved=Resolve(request,attacker,defender), before=health.Current;
        int applied=health.Damage(resolved);
        return new(request.BaseDamage,resolved,applied,before,health.Current,before>0 && health.Current==0);
    }
}
