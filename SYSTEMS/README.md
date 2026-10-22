# SYSTEMS

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

Reusable interactive gameplay mechanics that connect WORLD objects, ENTITIES, ITEMS and ENVIRONMENT without hard-coding each feature into the player or a specific world object.

## Folder map

- [WorldInteractions/](WorldInteractions/README.md): World-object interaction mechanics; [Fire/](WorldInteractions/Fire/README.md) is scaffolded here for future extraction from DEBUG. Nothing runs in production yet.
- [Interaction/](Interaction/README.md): Generic interaction resolution and requests for doors, containers, animals and objects.
- [Harvesting/](Harvesting/README.md): Shared resource extraction/chopping/mining/foraging rules and loot-plan execution.
- [Crafting/](Crafting/README.md): Shared recipes, ingredient rules, production, workstations and output item references.
- [Farming/](Farming/README.md): Sowing, crop growth, watering, soil and planting rules.
- [Building/](Building/README.md): Future modular player construction, placement validation, walls, roof rules, multi-floor and materials; reusable structure artwork/definitions live in WORLD/Content/Structures.
- [Cooking/](Cooking/README.md): Cooking process, preparation conditions, heat sources and recipes; cooked products remain item definitions under ITEMS..
- [Fishing/](Fishing/README.md): Fishing mechanic, bait/tool requirements, catches and interaction with WORLD/Water.
- [Skills/](Skills/README.md): General skill use, XP and progression mechanics; player-specific stat/profile ownership remains with the player..
- [Effects/](Effects/README.md): Reusable timed modifiers and gameplay effects such as poison, warming, chilled, slowed and regeneration.

## Conventions

- `SystemsMasterConfig.cs` and `DefaultSystemsConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
