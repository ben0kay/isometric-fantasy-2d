# COMBAT

> **Status:** C0/C1 implemented: shared actor health, cached stats and damage results. [Validation](C1_Validation.md) records checks and remaining manual work. [C2–C5](COMBAT_IMPLEMENTATION.md) remain planned.

Shared combat resolution for player, creatures, enemies and NPCs: damage, hit testing, projectiles, weapons and defense. Player input/weapon usage stays in ENTITIES/Player/Combat. Timed generic conditions belong in SYSTEMS/Effects.

## Folder map

- [Damage/](Damage/README.md): Implemented shared damage request/result/resolver; damage categories and resistances remain future work.
- [HitDetection/](HitDetection/README.md): Reusable melee hitboxes, collision/hurtbox detection, impact validation and range checks..
- [Projectiles/](Projectiles/README.md): Common arrows, spells, thrown objects and collision/lifetime helpers; weapon item definitions belong in ITEMS/Weapons..
- [Weapons/](Weapons/README.md): Shared combat attack profiles, cooldowns, swing rules and hit handling.
- [Defense/](Defense/README.md): Armor mitigation, blocks, shields, dodge rules and defense modifiers used by all combat-capable actors..
- [Targeting/](Targeting/README.md): Generic targeting, visibility, hostile/neutral relations and range queries where needed by creatures, NPCs and player abilities..

## Conventions

- `CombatMasterConfig.cs` and `DefaultCombatConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
