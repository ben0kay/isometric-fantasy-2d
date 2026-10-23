# Axe tool

Axe.tscn is currently auto-equipped on the player for testing, without inventory or item slots. Its EquippedTool controller receives use requests from PlayerInput and has no mouse-button event listener. Left-click away from UI triggers one cursor-aimed spin and a single nearest eligible tool hit. Swap Config or scene later for real equipment. Derived from DEBUG prototype; DEBUG untouched.

**Production artwork:** `Sprites/axe01.png` replaces the placeholder SVG in `Axe.tscn`. `ToolSprite` downscales and rotates the source so the grip sits at its local origin and the blade extends toward local +X, matching the existing orbit/swing controller. Adjust visual scale, grip offset and resting orientation in `Axe.tscn`; adjust gameplay reach, hand anchor, timing and aim in `DefaultAxeConfig.tres`. The hit query and attack rules are unchanged. The former `Sprites/axe.svg` remains unused legacy art.

**C2:** `AxeConfig` now inherits shared `AttackProfile`; the same existing `.tres` authors timing/reach plus damage/window/cooldown/faction/mask. The production swing delegates contact to shared physics-based melee execution, preserving one nearest contact and tree wobble/leaves. No new axe resource or art changes in C2. The existing PNG grip/rotation/scale is preserved. [Validation](../../../COMBAT/C2_Validation.md).

## C2.5 starter axe

`StarterAxe.tres` is a generic `EquippedItemDefinition` referencing `PrimaryAttack.tres` (11 damage, 52 reach, 0.38s + 0.15s cooldown) and `SecondaryAttack.tres` (5 damage, 60 reach, 0.28s + 0.45s cooldown). Both retain prop hits. Primary is a forward slash, not the old full spin; alternate is a narrower linear prod. Both use C2 execution and independent Resource tuning.

PNG art/grip is reused unchanged. `DefaultAxeConfig.tres` still supplies compatibility hand/aim settings; edit starter attack Resources for actual new damage/timing. No production skeleton/resource/scene text is changed: the 50-HP test target lives in TESTS/Combat/PlayerActionsPractice.tscn. Physical knockback is deferred to C3; a prod currently does not displace targets.
