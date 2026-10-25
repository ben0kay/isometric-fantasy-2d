# Shared inventory foundation

Owner-independent plain C# models for players, future chests and NPCs. No node callbacks, timers, world queries or UI dependencies.

- `InventoryStorage`: bounded stack storage against a canonical ItemCatalog; atomic add/remove/move/swap/merge, Changed event and on-demand total weight. Stack keys are local to one storage and persist with its saved inventory; they are not global item IDs.
- `InventoryHotbar`: 1–9 shortcuts to physical stack keys; selection can be rejected by an equipment-owner callback. Moves preserve references. A fully merged/deleted source loses its shortcut; new stacks cannot inherit it. Binding a selected slot is deliberately rejected until its equipment owner chooses another slot. SelectionInvalidated lets the owner stop using a lost item.
- `InventoryMasterConfig` / DefaultInventoryConfig: initial storage allocation (24 slots), independent of visual layout and hotbar count. This bounds allocations and stack count; no inventory Tetris, weight ceiling, volume limit, backpack requirement or encumbrance penalties are introduced.

Models publish only successful state changes. UI updates on these events; hidden pack refreshes defer until open. Storage allocates at construction; add/remove/move paths allocate no temporary arrays/lists. Moves/lookup are bounded by configured storage. Catalog loading/validation happens at session setup, never per frame. Dispose the hotbar and unsubscribe all view/owner listeners on teardown.

## Current integration and remaining work

Production World now uses `ENTITIES/Player/Inventory/PlayerInventory` and its shared starter profile. Campaign schema 3 persists storage and shortcuts. The practice fixture remains disposable. Shield items, container transfers, drag/drop/rebinding UI and crafting remain separate work. See [H4](../Harvesting/H4_World_Validation.md).
