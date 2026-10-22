// One lazy cache for all actor stats; modifier/profile events invalidate it, with no per-frame recomputation.
using System;
using System.Collections.Generic;
public enum CombatStat { DamageMultiplier, Defense, AttackSpeedMultiplier, MovementSpeedMultiplier }
public readonly record struct EffectiveStats(float DamageMultiplier, float Defense, float AttackSpeedMultiplier, float MovementSpeedMultiplier)
{
    public static EffectiveStats Neutral => new(1,0,1,1);
    // Validate external inputs at the boundary; modifier results are clamped before reaching this contract.
    // =========================================================
    public static EffectiveStats Validate(EffectiveStats v)
    {
        if (!float.IsFinite(v.DamageMultiplier) || v.DamageMultiplier < 0 || v.DamageMultiplier > 10000
            || !float.IsFinite(v.Defense) || v.Defense < 0 || v.Defense > 1000000
            || !float.IsFinite(v.AttackSpeedMultiplier) || v.AttackSpeedMultiplier < 0.05f || v.AttackSpeedMultiplier > 20
            || !float.IsFinite(v.MovementSpeedMultiplier) || v.MovementSpeedMultiplier < 0 || v.MovementSpeedMultiplier > 10)
            throw new ArgumentOutOfRangeException(nameof(v),"Invalid effective combat stats.");
        return v;
    }
}
public readonly record struct StatModifier(CombatStat Stat, float Flat = 0, float Percent = 0, float Multiplier = 1);
public sealed class ActorStats : IDisposable
{
    private readonly SortedDictionary<string,StatModifier> _modifiers = new(StringComparer.Ordinal);
    private readonly CombatStatProfile _profile;
    private EffectiveStats _cached;
    private bool _dirty = true, _disposed;
    public int RebuildCount { get; private set; }
    public int ModifierCount => _modifiers.Count;
    // Observe optional shared authoring data while keeping runtime modifiers local.
    // =========================================================
    public ActorStats(CombatStatProfile profile = null)
    { _profile=profile; if (profile != null) profile.Changed += Invalidate; }
    // Lazy reads are constant-time until a real authored/modifier input changes.
    // =========================================================
    public EffectiveStats Values
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if (!_dirty) return _cached;
            var b = _profile?.Values ?? EffectiveStats.Neutral;
            _cached = new(Evaluate(CombatStat.DamageMultiplier,b.DamageMultiplier,0,10000),
                Evaluate(CombatStat.Defense,b.Defense,0,1000000),Evaluate(CombatStat.AttackSpeedMultiplier,b.AttackSpeedMultiplier,0.05f,20),
                Evaluate(CombatStat.MovementSpeedMultiplier,b.MovementSpeedMultiplier,0,10));
            _dirty=false; RebuildCount++; return _cached;
        }
    }
    // Replace by stable ID rather than stack duplicate effects; no-op updates preserve the cache.
    // =========================================================
    public void SetModifier(string id, StatModifier modifier)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if (string.IsNullOrWhiteSpace(id) || !Enum.IsDefined(modifier.Stat) || !float.IsFinite(modifier.Flat) || !float.IsFinite(modifier.Percent)
            || !float.IsFinite(modifier.Multiplier) || modifier.Multiplier < 0 || Math.Abs(modifier.Flat)>1000000 || Math.Abs(modifier.Percent)>10000 || modifier.Multiplier>10000)
            throw new ArgumentException("Invalid combat stat modifier.");
        if (_modifiers.TryGetValue(id,out var current) && current==modifier) return;
        _modifiers[id]=modifier; Invalidate();
    }
    // Remove an effect by stable identity; missing IDs do not invalidate the cache.
    // =========================================================
    public bool RemoveModifier(string id)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if (id == null || !_modifiers.Remove(id)) return false;
        Invalidate(); return true;
    }
    // Defined order: additive flat, additive percentage, multiplicative factors, then the stat's bounds.
    // =========================================================
    private float Evaluate(CombatStat stat, float basis, float minimum, float maximum)
    {
        double flat=0, percent=0, logMultiplier=0; bool zero=false;
        foreach (var entry in _modifiers.Values)
        {
            if (entry.Stat!=stat) continue;
            flat+=entry.Flat; percent+=entry.Percent;
            if (entry.Multiplier==0) zero=true; else logMultiplier+=Math.Log(entry.Multiplier);
        }
        double value=(basis+flat)*Math.Max(0,1+percent);
        value=value<=0 || zero ? 0 : Math.Exp(Math.Log(value)+logMultiplier);
        return (float)Math.Clamp(value,minimum,maximum);
    }
    // Resource signals and explicit modifier edits are the only invalidation paths.
    // =========================================================
    private void Invalidate() => _dirty=true;
    // Remove shared Resource listeners on scene exit; no persistent actor references survive disposal.
    // =========================================================
    public void Dispose()
    { if (_disposed) return; if (_profile != null) _profile.Changed-=Invalidate; _modifiers.Clear(); _disposed=true; }
}
