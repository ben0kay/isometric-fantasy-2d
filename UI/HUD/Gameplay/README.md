# Gameplay HUD

`GameplayHUD.tscn` is instanced once as `World/GameplayUIRoot`, a screen-space CanvasLayer at layer 10 (below F9 at 20). It binds to that world's PlayerInput and owns a passive bottom-centered nine-slot hotbar and independent inventory view. No item storage lives here.

I opens/closes the backpack; Esc closes it before campaign menus. The world keeps running, while player movement and new combat commands are blocked. Existing 1/2/3 starter controls remain when closed. Slots contain no fake items or selection; real inventory/hotbar binding is UI3.

Tweak `UI/DefaultUIConfig.tres` through `UIMasterConfig`: slot count (1–9), size, spacing, bottom margin, inventory size and palette. Settings apply when a HUD is constructed. One viewport-size event handles layout; no UI frame callbacks. Input/view/viewport subscriptions release on teardown.

[Phase plan](PLAYER_UI_IMPLEMENTATION_PLAN.md) · [UI1/UI2 validation](UI1_UI2_Validation.md) · [Inventory design](../../Inventory/PLAYER_INVENTORY.md).

**Status:** UI1/UI2 implemented; headless interaction checks passed. Windows graphical acceptance and profiling pending. Status rails, toasts, alerts and item bindings remain future phases.

**UI3 groundwork:** `BindInventory(storage, hotbar)` lets an explicit inventory owner populate the existing views. InventoryPractice uses real storage/selection; normal World remains unbound. No UI owns stack quantities. [Foundation status](../../../SYSTEMS/Inventory/README.md).
