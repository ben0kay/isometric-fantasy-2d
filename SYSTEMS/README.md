# SYSTEMS

> **Status:** Mixed. WorldInteractions/Fire **FIRE1/FIRE2 are implemented in production**; most other listed interaction/harvesting/crafting/building modules remain planned foundations.

Reusable interactive gameplay mechanics that connect WORLD objects, ENTITIES, ITEMS and ENVIRONMENT without hard-coding each feature into the player or a specific world object.

[Inventory/](Inventory/README.md) now implements shared event-driven storage and hotbar references, exercised only by a disposable fixture; production player/save integration remains pending.

[Drops/](Drops/README.md) provides shared reward tables, bounded physical pickups and explicit collection for harvesting practice; no production auto-activation.

## Folder map

- [WorldInteractions/](WorldInteractions/README.md): World-object interaction mechanics; [Fire/](WorldInteractions/Fire/README.md) includes dormant-idle burn lifecycle, effects and streamed registration **in production**. Lightning-to-tree ignition, fire spread, damage and offline simulation are future.
- [Interaction/](Interaction/README.md): Generic interaction resolution and requests for doors, containers, animals and objects.
- [Harvesting/](Harvesting/README.md): H0–H3 shared extraction/reward and disposable tree-felling slice; H4 production persistence remains planned.
- [Crafting/](Crafting/README.md): Shared recipes, ingredient rules, production, workstations and output item references.
- [Farming/](Farming/README.md): Sowing, crop growth, watering, soil and planting rules.
- [Building/](Building/README.md): Future modular player construction, placement validation, walls, roof rules, multi-floor and materials; reusable structure artwork/definitions live in WORLD/Content/Structures.
- [Cooking/](Cooking/README.md): Cooking process, preparation conditions, heat sources and recipes; cooked products remain item definitions under ITEMS..
- [Fishing/](Fishing/README.md): Fishing mechanic, bait/tool requirements, catches and interaction with WORLD/Water.
- [Skills/](Skills/README.md): General skill use, XP and progression mechanics; player-specific stat/profile ownership remains with the player..
- [Effects/](Effects/README.md): Reusable timed modifiers and gameplay effects such as poison, warming, chilled, slowed and regeneration.

## Conventions

- `SystemsMasterConfig.cs` and `DefaultSystemsConfig.tres` remain general placeholders, but FIRE1/FIRE2 already use their own real Fire settings and profile Resources.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
