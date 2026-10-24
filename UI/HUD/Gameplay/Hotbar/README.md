# Gameplay hotbar

`GameplayHotbar` builds configurable passive numbered slots using shared `InventorySlot` visuals. Default nine slots; mouse events pass through. `SetSelected(-1)` clears selection; future UI3 bindings may supply an authoritative selected index. No current number-key interception, item assignment, fake icons/counts or per-slot callbacks.

Layout/palette comes from `UI/DefaultUIConfig.tres`. The parent HUD fits the complete bar on viewport resize.
