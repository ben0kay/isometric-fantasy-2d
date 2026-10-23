# Axe tool

Axe.tscn is currently auto-equipped on the player for testing, without inventory or item slots. Its EquippedTool controller receives use requests from PlayerInput and has no mouse-button event listener. Left-click away from UI triggers one cursor-aimed spin and a single nearest eligible tool hit. Swap Config or scene later for real equipment. Derived from DEBUG prototype; DEBUG untouched.

**Production artwork:** `Sprites/axe01.png` replaces the placeholder SVG in `Axe.tscn`. `ToolSprite` downscales and rotates the source so the grip sits at its local origin and the blade extends toward local +X, matching the existing orbit/swing controller. Adjust visual scale, grip offset and resting orientation in `Axe.tscn`; adjust gameplay reach, hand anchor, timing and aim in `DefaultAxeConfig.tres`. The hit query and attack rules are unchanged. The former `Sprites/axe.svg` remains unused legacy art.
