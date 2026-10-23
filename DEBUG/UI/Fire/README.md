# Production fire test controls

Run World with F6, then open **F9 → Environment → Tree fire**. Ignite nearest tree searches healthy registered receivers within 320px of the player; Reset restores registered trees in this world. Buttons now share Environment’s scrollable container and F9 visibility.

`WorldFireDebugPanel` remains a removable command owner under DebugUIRoot. `AttachTo` places its widgets into the shared drawer; it has no process callback. Removing it removes its hosted controls. Production FireRuntime/Burnable behavior remains independent. Receiver scans occur only on button presses, never each frame.
