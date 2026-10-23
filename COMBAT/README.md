# COMBAT

> **Status:** C0–C2.5 and C3.2 implemented: shared health/stats/damage plus player/enemy attack execution and contact validation. See [C1](C1_Validation.md), [C2 results and limitations](C2_Validation.md), and [remaining C3–C5 plan](COMBAT_IMPLEMENTATION.md).

Shared combat resolution for player, creatures, enemies and NPCs: damage, hit testing, projectiles, weapons and defense. Player input/weapon usage stays in ENTITIES/Player/Combat. Timed generic conditions belong in SYSTEMS/Effects.

## Folder map

- [Damage/](Damage/README.md): Implemented shared damage request/result/resolver; damage categories and resistances remain future work.
- [HitDetection/](HitDetection/README.md): Implemented contact-window broad phase, receiving geometry and obstruction checks.
- [Projectiles/](Projectiles/README.md): R0 straight-arrow flight, swept collision, immutable contact data, scene-local budgets and cleanup are implemented. Archer/other flight modes remain planned; weapon identity belongs in ITEMS/Weapons.
- [Weapons/](Weapons/README.md): Shared combat attack profiles, cooldowns, swing rules and hit handling.
- [Defense/](Defense/README.md): Armor mitigation, blocks, shields, dodge rules and defense modifiers used by all combat-capable actors..
- [Targeting/](Targeting/README.md): Implemented minimal receiving factions/liveness/geometry; richer AI relations remain planned.

## Conventions

- `CombatMasterConfig.cs` / `DefaultCombatConfig.tres` expose independent enemy-melee, knockback, stagger and combat-impact-particle masters. F9 → Combat edits these live in memory and restores the original values when its panel unloads. Turning push/stagger Off suppresses that existing reaction axis on the next motor tick; impact Off stops new bursts while existing particles finish.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.

**C2.5:** Shared equipped-item definitions, forward slash/thrust, starter axe/sword selection, directional shield guard and visual hop are implemented. [Results and handoff](C2_5_Validation.md) distinguish headless checks, container build limits and target-PC visual acceptance. C3.2 shared knockback/stagger is implemented; see [tuning and validation](C3_2_Validation.md).

**C3.3c+d:** Shared Bone/Flesh profiles now receive authoritative contact source, world point, direction and DamageResult. Actor-owned impact canvases are lazy, bounded and dormant when settled. [Material feedback and validation](C3_3cd_Validation.md).
