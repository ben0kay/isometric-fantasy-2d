// One authoritative HP value per actor; changes are explicit and have no idle callbacks.
using System;
public readonly record struct HealthChange(int Before, int After, bool Restored);
public sealed class ActorHealth
{
    public int Maximum { get; }
    public int Current { get; private set; }
    public bool IsAlive => Current > 0;
    public event Action<HealthChange> Changed;
    // Validate the fixed capacity once; current HP starts full.
    // =========================================================
    public ActorHealth(int maximum)
    {
        if (maximum < 1 || maximum > 1000000) throw new ArgumentOutOfRangeException(nameof(maximum));
        Maximum = Current = maximum;
    }
    // Damage cannot wrap below zero or damage an already dead actor.
    // =========================================================
    public int Damage(int amount)
    {
        if (amount <= 0 || !IsAlive) return 0;
        int before = Current; Current -= Math.Min(Current,amount);
        Changed?.Invoke(new(before,Current,false)); return before-Current;
    }
    // Healing does not revive; restoration is the explicit save/test initialization boundary.
    // =========================================================
    public int Heal(int amount)
    {
        if (amount <= 0 || !IsAlive) return 0;
        int before = Current; Current += Math.Min(Maximum-Current,amount);
        if (Current != before) Changed?.Invoke(new(before,Current,false));
        return Current-before;
    }
    // Preserve exact saved state; do not silently clamp corrupt checkpoints or replay damage effects.
    // =========================================================
    public void Restore(int health)
    {
        if (health < 0 || health > Maximum) throw new System.IO.InvalidDataException("Saved health is outside this actor's maximum.");
        int before = Current; Current = health;
        if (before != Current) Changed?.Invoke(new(before,Current,true));
    }
}
