# COMBAT

> **Status:** C0–C2.5 and C3.2 implemented: shared health/stats/damage plus player/enemy attack execution and contact validation. See [C1](C1_Validation.md), [C2 results and limitations](C2_Validation.md), and [remaining C3–C5 plan](COMBAT_IMPLEMENTATION.md).

Shared combat resolution for player, creatures, enemies and NPCs: damage, hit testing, projectiles, weapons and defense. Player input/weapon usage stays in ENTITIES/Player/Combat. Timed generic conditions belong in SYSTEMS/Effects.

## Folder map

- [Damage/](Damage/README.md): Implemented shared damage request/result/resolver; damage categories and resistances remain future work.
- [HitDetection/](HitDetection/README.md): Implemented contact-window broad phase, receiving geometry and obstruction checks.
- [Projectiles/](Projectiles/README.md): Common arrows, spells, thrown objects and collision/lifetime helpers; weapon item definitions belong in ITEMS/Weapons..
- [Weapons/](Weapons/README.md): Shared combat attack profiles, cooldowns, swing rules and hit handling.
- [Defense/](Defense/README.md): Armor mitigation, blocks, shields, dodge rules and defense modifiers used by all combat-capable actors..
- [Targeting/](Targeting/README.md): Implemented minimal receiving factions/liveness/geometry; richer AI relations remain planned.

## Conventions

- `CombatMasterConfig.cs` and `DefaultCombatConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.

**C2.5:** Shared equipped-item definitions, forward slash/thrust, starter axe/sword selection, directional shield guard and visual hop are implemented. [Results and handoff](C2_5_Validation.md) distinguish headless checks, container build limits and target-PC visual acceptance. C3.2 shared knockback/stagger is implemented; see [tuning and validation](C3_2_Validation.md).
