# Player inventory ownership

`PlayerInventory` binds the shared InventoryStorage/InventoryHotbar to real Player equipment and input. `DefaultPlayerInventory.tres` references canonical catalog/storage settings and starter axe/sword. New campaigns and legacy saves without inventory receive that authored loadout once; restored inventories receive no extra items.

1–9 selects shortcuts, I opens the existing backpack, E collects nearby settled drops. G preserves the temporary shield toggle without pretending a shield item exists. Save captures exact stack keys, quantities, slots, shortcuts, selection and shield flag; held guard/actions are not resumed. UI reads these same models. Storage rules remain under SYSTEMS/Inventory.
