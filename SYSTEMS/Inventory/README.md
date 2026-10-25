# Shared inventory foundation

Owner-independent plain C# models for players, future chests and NPCs. No node callbacks, timers, world queries or UI dependencies.

- `InventoryStorage`: bounded stack storage against a canonical ItemCatalog; atomic add/remove/move/swap/merge, Changed event and on-demand total weight. Stack keys are unique only within one session storage, not saved/global item IDs.
- `InventoryHotbar`: 1–9 shortcuts to physical stack keys; selection can be rejected by an equipment-owner callback. Moves preserve references. A fully merged/deleted source loses its shortcut; new stacks cannot inherit it. Binding a selected slot is deliberately rejected until its equipment owner chooses another slot. SelectionInvalidated lets the owner stop using a lost item.
- `InventoryMasterConfig` / DefaultInventoryConfig: initial storage allocation (24 slots), independent of visual layout and hotbar count. This bounds allocations and stack count; no inventory Tetris, weight ceiling, volume limit, backpack requirement or encumbrance penalties are introduced.

Models publish only successful state changes. UI updates on these events; hidden pack refreshes defer until open. Storage allocates at construction; add/remove/move paths allocate no temporary arrays/lists. Moves/lookup are bounded by configured storage. Catalog loading/validation happens at session setup, never per frame. Dispose the hotbar and unsubscribe all view/owner listeners on teardown.

## Current integration and remaining work

Only `TESTS/Inventory/InventoryPractice.tscn` creates/grants this state and opts PlayerInput into numeric hotbar mode. Production World/campaigns retain their previous empty HUD and temporary 1/2/3 weapon/shield controls. Existing axe/sword combat definitions are referenced rather than replaced. Shield item, tree harvesting, world pickups, container transfers, drag/drop/rebinding UI, crafting and durable save integration are separate work.

Next UI3 pass must establish player ownership, persistent individual equipment identity and hotbar/stack restoration, migrations/old-save behavior, and production control migration. Do not simply grant starter items each time a saved world loads. Session keys alone are not a durable save format.

[Checks and local handoff](ITEM_FOUNDATION_VALIDATION.md).
