# ITEMS

> **Status:** Planning/folder foundation only. This pass does not implement systems or content.

The single home for inventory-ready item definitions: food, drinks, equipment, tools, weapons, harvested resources and materials. Physical trees, rocks and structures remain in WORLD; inventory item definitions belong here.

## Folder map

- [Definitions/](Definitions/README.md): Future item schema, stack limits, mass, value, durability, item IDs, icon references and catalogs.
- [Resources/](Resources/README.md): Raw gathered items (logs, branches, stones, ores, hides).
- [Consumables/](Consumables/README.md): Food, drinks and medicine as shared item definitions.
- [Equipment/](Equipment/README.md): Wearable and equipable item definitions.
- [Weapons/](Weapons/README.md): Item definitions for swords, bows and other weapons.
- [Tools/](Tools/README.md): Axes, picks, hoes and other tools.
- [Materials/](Materials/README.md): Processed/refined goods (planks, ingots, cloth, leather) consumed by recipes and building..
- [Misc/](Misc/README.md): Items with no clearer category.

## Conventions

- `ItemMasterConfig.cs` and `DefaultItemConfig.tres` are **blank placeholders**; don't add settings before owning mechanics are designed.
- Use shared logic and data-driven definitions when it reduces duplication; keep clearly specialized systems in their own modules.
- Folder names and documentation are navigation aids, not a promise that gameplay is already implemented.
