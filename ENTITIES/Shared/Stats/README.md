# Shared / Stats

`CombatStatProfile` optionally authors damage multiplier (1), flat defense (0), attack-speed multiplier (1) and movement-speed multiplier (1). PlayerMasterConfig and EntityDefinition expose it; null uses neutral values without allocating a Resource per actor.

`ActorStats` caches all four effective values per actor. `SetModifier(id, value)` replaces a stable ID; `RemoveModifier` removes it. Inputs invalidate only on actual changes or profile `Changed`; the next read rebuilds once. No ticking node or repeated arithmetic on unchanged reads. Dispose removes the shared Resource listener when actors exit.

Formula: `(base + sum(flat)) * max(0, 1 + sum(percent)) * product(multiplier)`, then clamp. Percent 0.25 means +25%. IDs sort ordinally so insertion order cannot change results; logarithmic multiplication avoids intermediate product overflow. Limits: damage 0–10000, defense 0–1000000, attack speed 0.05–20, movement 0–10. Invalid external base/modifier inputs reject. Profiles are authored data; runtime effects must use instance modifiers. Equipment/buff ownership and persistence are future work.

Movement multiplies existing WalkSpeed/MoveSpeed. Equipped axe snapshots attack-speed timing at swing start; its existing contact query and one-damage actor adapter remain until C2 supplies attacker-aware shared attack execution.
