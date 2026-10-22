# ENTITIES — Player, species, factions, roles and shared actor mechanics

This is the canonical home for living actors and their **species-specific definitions, scenes and visuals**. `Player/` contains the existing **implemented production movement/camera foundation** and its separate future subsystems. Species/faction/role/shared directories added in this pass are **organization-only scaffolding**; none adds creatures to the game.

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
- **DEBUG is separate.** `DEBUG/TopDownTest/Enemies/` may contain a temporary skeleton demonstration; it is NOT production content and has not been promoted by creating these folders.

## Data-driven plan (not implemented)

Eventually give species, variants, roles and factions **stable IDs** and reusable Godot Resource definitions (`.tres` / C# Resource classes), which a future Avalonia editor could modify. A species folder can later contain a base definition, a packed scene, `Visuals/`, `Variants/` and optionally unique behaviour scripts. Do not create a C# script per creature unless it genuinely needs one.

Use `EntityMasterConfig.cs` / `DefaultEntityConfig.tres` already present here for genuinely shared top-level settings only; don't invent redundant empty MasterConfigs for every subfolder.

**Status:** Folders/READMEs only in this pass. No AI, combat, spawning, new enemy scenes or other runtime changes.
