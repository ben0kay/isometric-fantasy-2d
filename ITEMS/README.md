# ITEMS

Canonical inventory content: shared [ItemDefinition/ItemCatalog](Definitions/README.md), plus named item folders containing Resources and artwork. Physical trees/rocks remain in WORLD. Enemy equipment retains its species-owned folder convention.

## Implemented foundation

`DefaultItemConfig.tres` references the explicit `DefaultItemCatalog.tres`. Three inventory definitions are registered: starteraxe, startersword and wooden_log. Axe/sword wrapper Resources reference their existing EquippedItemDefinition combat/art profiles; those profiles and IDs remain intact. General item IDs occupy a separate catalog namespace from combat profile IDs.

Raw gathered materials live under [Resources/](Resources/README.md); processed materials remain under Materials. Existing Tools/Weapons paths are retained. New shared schema lives under Definitions; reusable storage/hotbar rules live under [SYSTEMS/Inventory](../SYSTEMS/Inventory/README.md), not under UI or Player.

Play `TESTS/Inventory/InventoryPractice.tscn` with F6 to see actual items, counts, weight and equipment selection. This is a disposable fixture; normal World/campaign inventory remains unbound until persistence and player ownership are integrated. No tree yield, shield item, physical pickup, crafting or external Avalonia editor is implemented by this pass.

See [validation and remaining work](../SYSTEMS/Inventory/ITEM_FOUNDATION_VALIDATION.md).
