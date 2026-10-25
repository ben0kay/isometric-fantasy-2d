# Inventory presentation components

`InventorySlot` is a rounded slot with a caption, optional icon/count/tooltip and explicit selected-state styling. It owns no item stack, input, equipment or save data. Hotbar and equipment placeholders share it; future container views can reuse it. Changes are explicit, with no frame callbacks. Frame/palette values come from UIMasterConfig.

Display receives an InventoryStack value from the owning view. Storage authority stays outside UI; passive hotbar slots ignore mouse, while bound backpack cells stop mouse input to support tooltips.
