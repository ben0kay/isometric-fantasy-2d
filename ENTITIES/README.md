# ENTITIES — Player, species, factions, roles and shared actor mechanics

This is the canonical home for living actors and their **species-specific definitions, scenes and visuals**. `Player/` owns implemented production input, camera/movement and axe. **`Shared/` provides a real Resource-driven Entity, actor health/stats, hit/death feedback, optional chase and navigation.** The production Skeleton Basic uses it. Other species/factions/roles mostly remain scaffolding; full populations/A-Life are future.

## Main structure

| Folder | Purpose |
| --- | --- |
| [Player/](Player/README.md) | Existing playable character, controls, movement, survival, equipment, inventory. **Do not duplicate.** |
| [Species/](Species/README.md) | What the entity **is**: Human, Goblin, Skeleton, Wolf, Dragon, etc. One canonical species folder per entity type. |
| [Factions/](Factions/README.md) | Who it **belongs to**: kingdoms, bandits, clans, necromancer armies, independent traders. |
| [Roles/](Roles/README.md) | What it **does**: guard, knight, soldier, bandit, witch, mage, merchant, farmer, shaman, archer. |
| [Shared/](Shared/README.md) | Reusable actor-specific AI, navigation, perception, movement, health and data definitions. |

## Important separation

- **Species is not hostility.** Passive, neutral, territorial, predatory and hostile are runtime behaviour/disposition settings, **not content folder names**. A wolf could flee, patrol or attack depending on its circumstances.
- **Species is not role.** Human knights, human bandits and human mages should reuse the Human definition with different role/equipment/faction resources. Goblin archers/warriors can be species **variants** when they are concrete loadout/presentation archetypes.
- **Species is not faction.** Humans, elves and dwarves can belong to the same kingdom; a friendly skeleton and a necromancer-controlled skeleton can use one underlying species.
- **Gameplay entities vs ambience:** A firefly or bird that is *only* an atmosphere/particle effect belongs in `ENVIRONMENT/` (or its visual effect owner), not necessarily as a fully simulated ENTITIES actor. Real huntable, interactive or damageable ones do belong here.
- **No biome-owned creature copies.** `WORLD/Biomes/` and `WORLD/Caves/` reference canonical species definitions and provide spawn restrictions, weights and populations. Content stays here. See [WORLD/README.md](../WORLD/README.md).
- **Keep combat and items centralized.** Shared damage/weapon resolution belongs in `COMBAT/`; item definitions in `ITEMS/`; global interaction/gameplay services in `SYSTEMS/`. This folder can contain actor health state, animation reactions, AI, equipment references, etc.
- **DEBUG is separate.** `DEBUG/TopDownTest/Enemies/` remains an independent experiment; one production test Skeleton Basic already exists using the [shared Entity](Shared/README.md). No DEBUG dependency is needed.

## Data-driven foundation and future authoring

`EntityDefinition`, `Entity.tscn` and optional combat profiles already provide a common Resource-driven foundation. Richer species/roles/factions and the Avalonia editor remain future. A species folder can later contain a base definition, a packed scene, `Visuals/`, `Variants/` and optionally unique behaviour scripts. Do not create a C# script per creature unless it genuinely needs one.

Use `EntityMasterConfig.cs` / `DefaultEntityConfig.tres` already present here for genuinely shared top-level settings only; don't invent redundant empty MasterConfigs for every subfolder.

**Status:** Shared Entity, player and one production test skeleton exist; C0/C1 shared HP/cached stats/damage results are headless-tested ([C1](../COMBAT/C1_Validation.md)). C2 adds shared player/enemy attack execution and contact validation ([results/limits](../COMBAT/C2_Validation.md)); C3–C5 encounter polish, widespread populations and offscreen A-Life remain planned.
