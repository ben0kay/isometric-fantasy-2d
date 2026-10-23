# Shared projectiles — R0 foundation

> **Status: R0 IMPLEMENTED.** Shared straight point-arrow runtime, Resource snapshots and scene-local shot budgets are implemented; [validation and integration contract](R0_Validation.md). R1 adds a shared archer and practice scene; R2 mixed encounters/extra controls remain planned. See [Ranged Combat Implementation Plan](RANGED_IMPLEMENTATION.md) for R0–R2 and the Skeleton Archer handoff.

## Purpose

Reusable projectile mechanics for arrows, bolts, thrown rocks, fireballs, magical orbs and future spells. A projectile is a transient world object, **not** a subclass of `ENTITIES/Shared/Entity.tscn` by default. Some projectiles may eventually opt into damageable/targetable health using canonical `ActorHealth` and the shared combat receive contracts; basic arrows do **not** need an HP component.

Ownership:
- **COMBAT/Projectiles/** — flight, swept collision, hit resolution, source/faction data, lifecycle and optional extension contracts.
- **COMBAT/Damage/** and **COMBAT/Targeting/** — authoritative damage and eligibility; do not duplicate.
- **ENTITIES/Shared/AI/** — generic ranged decision-making and range/LOS selection; existing chase/movement remains authoritative.
- **ENTITIES/Species/Undead/Skeletons/Archer/** — species definition, sprite and ranged configuration.
- **ENTITIES/Species/Undead/Skeletons/Archer/Equipment/Bow/** — this enemy bow definition and artwork; player inventory weapons stay under ITEMS/.
- **FEEDBACK/** — impact visuals, arrow trails and later audio; presentation never decides damage.

## Runtime files

```text
COMBAT/Projectiles/
├── README.md
├── RANGED_IMPLEMENTATION.md
├── Projectile.cs             # transient runtime/flight/impact
├── ProjectileProfile.cs      # authored speed, damage, range/life, collision, visuals
└── ProjectileSpawner.cs      # scene-local budget, bounds and creation
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

## Implemented R0 API

Place one `ProjectileSpawner` under the appropriate scene-local Y-sort actor/effect root. Set its **world-space Bounds** and, for streamed worlds, `IsPositionLoaded` to the existing stream membership check. Call `TryFire(source, profile, worldMuzzle, lockedDirection, out shot)` with a living canonical combat actor. No automatic production manager or World scene wiring is added by R0.

`ProjectileProfile.Impact` reuses an AttackProfile’s snapshotted contact/reaction values and effective attacker stats; melee timing/reach/presentation fields do not control arrow flight. Speed, distance, lifetime and simple arrow visuals belong to ProjectileProfile. Profiles must retain collision mask3. The decorative shaft/height is independent of the zero-width ground-plane ray, which intersects existing actor/scenery physics bodies. No actor feet shapes/layers are changed. Muzzle positions use the collision/ground plane, not the elevated artwork.

First obstruction stops the arrow, including friendly/ineligible bodies; eligible targets receive one canonical damage call. Solid tool receivers are never chopped. Incoming flight bearing drives directional guard; impact particles and optional reactions reuse existing contracts. Owner death/suspension/free cancels remaining shots. `Finished` exposes impact/expiry/boundary/source-loss/cancellation for future presentation; scene removal emits no late callback. Homing/health/penetration components are not added speculatively.

Default cap64 active arrows per spawner (bounded1–256), one node and reusable ray query per live shot. Spawner has no process callbacks, and retired shots stop immediately and queue-free. Result dictionaries still allocate per cast. A loaded-area predicate is sampled every16px along a step, capped at64 subsegments (65 checks); steps above1024px retire conservatively. World rectangles are exact, while arbitrary tiny holes in custom membership predicates require a stronger segment policy in a future integration. Do not claim continuous membership proof from sampling. Normal512px chunk membership is the intended R1 consumer; no queries force-load chunks.

## R1 integration

WorldScene now lazily creates one projectile owner only when ranged placement needs it. Bounds use the finite world, not an individual scenery chunk; membership delegates observe the current stream without loading it. World-bound arrows and ranged LOS use the exact bounded crossed-chunk check through `ProjectileSpawner.IsSegmentLoaded`. Standalone R0 callers without that delegate retain sampled membership behavior. See [R1 integration and validation](R1_Validation.md).
