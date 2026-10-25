# Disposable item/inventory practice

Open `InventoryPractice.tscn` and press F6. Real Player and GameplayHUD display starter axe, starter sword and 12 wooden logs. I opens/closes the populated backpack; Esc closes it. 1 axe, 2 sword, 3 logs/empty hands, 4–9 empty hands. The temporary shield toggle is absent in this fixture because no shield item exists. Close the pack to use the Add 5 test logs button; reopen to see the stack/weight update. Tooltips describe item/unit weight; worn equipment spaces remain previews. No saves, tree harvesting or physical drops.

`ValidateItemInventory.tscn` runs authoring/transaction invariants and actual player/input/UI wiring across two fixture lifecycles. No real campaign files are read or written. See [validation](../../SYSTEMS/Inventory/ITEM_FOUNDATION_VALIDATION.md).
