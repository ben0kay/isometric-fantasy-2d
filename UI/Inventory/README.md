# UI / Inventory

Reusable inventory **presentation**: player backpack, item inspection, sorting, drag-and-drop and opening world storage containers. Inventory data and authoritative item/transfer rules live outside UI.

- [PLAYER_INVENTORY.md](PLAYER_INVENTORY.md): Detailed design notes for a physical-looking, weight-based backpack with auto-stacked gathered resources, individual equipped gear, soft encumbrance and future Avalonia data authoring.
- [Containers/](Containers/README.md): Future shared chest/loot/container views using the same inventory components.

`PlayerInventoryView.cs` now provides the UI2 scrollable backpack/equipment placeholder overlay. The shared gameplay HUD creates it once. Its factory-created close button raises an event; PlayerInput owns I/Esc and command gating. Hidden views are dormant; layout responds only to viewport resizing. The world continues, so opening the pack provides no protection from enemies.

**Status:** UI2 presentation implemented; shared storage and mass display now exist in the disposable item fixture; production save binding, carry restrictions, transfers, sorting and drag/drop remain pending. [Validation](../HUD/Gameplay/UI1_UI2_Validation.md).

**Optional populated view:** PlayerInventoryView.Bind now displays canonical item icons/counts/tooltips and total mass through shared storage events. Slot widgets are created once; hidden refreshes defer until Open. InventoryPractice supplies the model; production player ownership, saves and transfers remain pending. See [shared inventory](../../SYSTEMS/Inventory/README.md).
