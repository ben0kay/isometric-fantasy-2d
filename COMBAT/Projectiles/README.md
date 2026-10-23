# Shared projectiles — R0 foundation

> **Status: PLANNED / documentation scaffold only.** This folder currently has no production projectile runtime. See [Ranged Combat Implementation Plan](RANGED_IMPLEMENTATION.md) for R0–R2 and the Skeleton Archer handoff.

## Purpose

Reusable projectile mechanics for arrows, bolts, thrown rocks, fireballs, magical orbs and future spells. A projectile is a transient world object, **not** a subclass of `ENTITIES/Shared/Entity.tscn` by default. Some projectiles may eventually opt into damageable/targetable health using canonical `ActorHealth` and the shared combat receive contracts; basic arrows do **not** need an HP component.

Ownership:
- **COMBAT/Projectiles/** — flight, swept collision, hit resolution, source/faction data, lifecycle and optional extension contracts.
- **COMBAT/Damage/** and **COMBAT/Targeting/** — authoritative damage and eligibility; do not duplicate.
- **ENTITIES/Shared/AI/** — generic ranged decision-making and range/LOS selection; existing chase/movement remains authoritative.
- **ENTITIES/Species/Undead/Skeletons/Archer/** — species definition, sprite and ranged configuration.
- **ITEMS/Weapons/Bows/** — bow identity/equipment and its presentation; reusable by future player equipment.
- **FEEDBACK/** — impact visuals, arrow trails and later audio; presentation never decides damage.

## Proposed minimal files (create only when implemented)

```text
COMBAT/Projectiles/
├── README.md
├── RANGED_IMPLEMENTATION.md
├── Projectile.cs             # minimal transient runtime/flight/impact
├── ProjectileProfile.cs      # authored speed, damage, range/life, collision, visuals
└── ProjectileSpawner.cs      # lightweight owner/scene-local creation and cleanup if needed
```

Do **not** make Movement/, Health/, Behaviors/, Pooled/, dozens of Resources or a global ProjectileManager until a real use case warrants them. Optional interfaces/strategies may be added incrementally without replacing the simple arrow contract.

## Baseline R0 contract

- Fire one arrow from an explicit muzzle/spawn position with a **locked shot direction**, owner/faction and immutable attack parameters. Use the same effective attacker stats/damage rules as existing C2 melee where applicable.
- Move in world space at configurable speed, maximum distance or lifetime; test the **entire travelled segment** each physics step against actors and solid scenery (prevent fast-arrow tunnelling), filtering source/self/faction, including the shooter's own collider.
- Apply damage **once** to the first eligible target hit; use the canonical `ICombatTarget.ReceiveDamage` or current live equivalent and result/defense/guard contracts. A directional shield should be able to reduce front-facing projectile hits correctly via origin/impact semantics. No duplicate per-frame hit on overlap.
- Stop/expire safely on solid scenery and out-of-bounds/streaming retirement. Unload cancels live shots safely; don't let arrows force-load distant chunks or leave unmanaged references when the owner dies.
- Separate projectile hit geometry from physical actor feet collision. Define ray/cast collision masks using **current AC world-solids layer 1 + actors layer 2**, and keep C2 melee/prop physics unchanged.
- Keep projectiles non-blocking to ordinary actor locomotion unless a future profile explicitly supports solid projectile bodies.
- Use a small generated/temporary arrow graphic when sprites are absent, rotate it to shot direction, keep reasonable world-space Z/Y-sort.
- Bounds: cap active projectile count, finite/clamped parameters, no per-projectile node trees of physics sensors/timers; avoid pooled complexity until counts justify it. No idle behavior for inactive/expired shots.

## Future RPG-inspired feature catalogue (design extension points, NOT R0 requirements)

| Capability | Example / future use | Contract consideration |
| --- | --- | --- |
| **Pierce** | Arrow continues through multiple targets | Count hits once per target and cap penetration |
| **Ricochet / bounce** | Stone or enchanted bolt rebounds off solid scenery | Configurable reflection count and surface response |
| **Fork / split** | Magical bolt spawns two smaller bolts | Spawn budget + recursion/depth cap |
| **Chain** | Lightning jumps to new hostile targets | Explicit target search budget, visited targets |
| **Return / boomerang** | Throwing axe returns to thrower | Flight mode and source-loss fallback |
| **Homing / guidance** | Enemy orb curves toward player | Separate optional steering behavior, turn-rate cap, last-known target fallback |
| **Destructible projectile** | Mystical orb has 20 HP and can be struck | Optional canonical health + receiving/targeting interface, not full Entity inheritance |
| **Parry / reflect / deflect** | Shield or spell sends bolt back | Explicit new owner/faction and new damage eligibility, no self-hit bug |
| **Area explosion** | Fireball explodes at impact/expiry | Single bounded area query and single-hit rules |
| **Damage over time / statuses** | Poison dart, burning arrow, slowing frost | Apply future shared status system; avoid projectile-specific status engine |
| **Linger / field** | Acid puddle, fire patch, lingering cloud | Transition to bounded world effect rather than forever-live projectile |
| **Sticking / embed** | Arrow remains visibly in wall/tree briefly | Cosmetic world prop separate from active damaging shot |
| **Pickup / recoverable ammo** | Recollect spent arrows | Convert to ITEMS/world pickup, not full actor |
| **Arc / gravity** | Thrown rock, mortar, lobbed potion | Optional trajectory with height illusion; distinguish render elevation from collision position |
| **Burst / spread / volley** | Shotgun-style spread, archer volley | Shared spawner with deterministic count/fan caps |
| **Speed/size/element modifiers** | RPG item affixes, spells | Data/resource-driven authored modifiers; snapshot on fire |
| **Environment interaction** | Ignite vegetation, hit destructibles, trigger traps | Explicit opt-in target interface, not arbitrary world traversal |

Inspired by RPG projectile variety (e.g., Path of Exile's pierce/fork/chain/return and Terraria's reflective/recoverable/projectile-like enemies); no need to copy those games' rules exactly. **R0 must preserve extension seams, not implement these features now.**

## Constraints and Codex handoff

Read the root README, `CodePreferences.md`, `COMBAT/Weapons/AttackExecution.cs`, `COMBAT/Damage/DamageResolver.cs`, `COMBAT/Targeting/CombatTargeting.cs`, `ENTITIES/Shared/Entity.cs`, `ENTITIES/Shared/AI/GroundChase.cs`, `ENTITIES/Shared/Movement/EntitySeparationPlan.md` and latest validation notes before coding.

Future compatibility needs to be possible without forcing generic Entity health/AI onto every arrow. Keep R0 small and choose extension interfaces only when they avoid concrete coupling. Preserve P4 saved-content fingerprints and SO2/P3 ownership. See the phase plan for tests.
