# Inventory presentation components

`InventorySlot` is a passive rounded slot with a caption and explicit selected-state styling. It owns no item stack, input, equipment or save data. Hotbar and equipment placeholders share it; future container views can reuse it. Changes are explicit, with no frame callbacks. Frame/palette values come from UIMasterConfig.
