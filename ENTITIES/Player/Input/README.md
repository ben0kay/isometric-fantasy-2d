# Player input

`PlayerInput.cs` is the **single player-controls script**: arrows supply normalized movement, left mouse requests tool use, and **right Shift** holds sprint. Release and press **right Shift twice within 0.28s** (configurable on `PlayerMasterConfig`) to emit `DodgeRequested` toward the mouse cursor on the second press. Key auto-repeat is ignored; left Shift does not trigger these actions. Gameplay/UI input is handled through `_UnhandledInput`.

Input **emits intentions only**; it doesn't move the player or calculate attack damage. The `PlayerMovement` motor owns speed, dodge cooldown, duration and physics collisions. No animation or dodge invulnerability is added. Current limitations: focus loss may leave a held key state until release; future input rebinding/gamepad support requires expanding this script without duplicating controls.
