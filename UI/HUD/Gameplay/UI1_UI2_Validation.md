# UI1/UI2 — implementation and validation

Implemented 24 October 2026; awaiting Windows visual/performance acceptance. Included in the single commit titled `feat: add gameplay hotbar and non-pausing inventory overlay` (find SHA with `git log -1 --format=%H -- UI/HUD/Gameplay/UI1_UI2_Validation.md`). No save schema, item database or authored combat definitions change.

## Behavior and ownership

World owns one layer-10 GameplayHUD scene. It builds a responsive nine-slot empty hotbar plus reusable PlayerInventoryView. PlayerInput owns `player_inventory` (I), Esc priority and inventory command suppression. PlayerMovement cancels voluntary dodge/guard on suppression; physics/damage/knockback continue. Existing committed attacks/hops finish; opening inventory never resets weapon cooldowns. The overlay captures background mouse clicks and scrolls on short viewports. Its only Button is created through UIButtonFactory. No fake usable items, selected item, counts, weight or equipment state.

DefaultUIConfig.tres/UIMasterConfig owns size/count/spacing/margins/palette; changes apply to subsequently constructed HUDs. UI3 owns eventual real item binding and number-key migration. UI4 remains further visual/usability polish.

## Checks run

- Direct Roslyn C# compilation with .NET SDK 8.0.415, net8 references, Godot 4.5.1 assemblies and Godot source generators: exit 0, no compiler errors.
- Godot 4.5.1 asset import completed. Its editor SDK discovery reported `.NET Sdk not found` / missing Microsoft.Build; this is **not** a passing standard editor build. Project SDK remains 4.7.2, unchanged.
- Native Godot 4.5.1 headless `TESTS/GameplayUI/ValidateGameplayUI.tscn`: **45 assertions passed**, exit 0. Covers actual I/Esc injection with button focus, nine slots, starter keys preserved, blocked combat/held arrows, guard/sprint cleanup, no tree pause, no UI process callbacks, close button, layout bounds at 1920×1080/800×600/480×360, HUD removal and two player/HUD compositions. Unhandled-key observer verifies Esc does not leak to campaign-style handling. Camera physics-interpolation warning is existing.
- Existing `TESTS/Combat/ValidatePlayerActions.tscn`: exit 1, `Thrust rotated away from aim`. Recompiled with unchanged HEAD PlayerInput/PlayerMovement and without the new UI/test sources: **same failure**. Final sources restored and final UI checks rerun successfully. Combat suite is not reported green.
- `git diff --check`: passed.

Headless assertions establish behavior/layout bounds, not rendered readability, actual Windows frame cost or full campaign scene acceptance.

## Local commands

Use the project's normal Godot .NET installation compatible with Godot.NET.Sdk 4.7.2. Substitute your executable path for `godot`.

```powershell
dotnet build
godot --headless --path . --editor --import
godot --headless --path . res://TESTS/GameplayUI/ValidateGameplayUI.tscn
godot --headless --path . res://TESTS/Combat/ValidatePlayerActions.tscn
```

Expected new UI result: `GAMEPLAY UI PASS: 45 checks`. Report full output if the existing combat failure persists on the project's target engine.

## Windows manual acceptance

1. Open World.tscn → F6. Verify nine compact empty slots bottom-center; mouse gameplay works over the passive belt. Keys 1/2/3 retain axe/sword/shield behavior.
2. Press I while walking/sprinting/guarding/dodging. Voluntary movement/guard stops, no new attacks or weapon switches happen, world/weather/enemies keep running and can still hurt/push the player. Current committed attacks/hops finish normally.
3. With the close button focused, use I and Esc; either closes. Close button also restores input. Hold arrows across close to resume walking; repress guard/sprint. Test mouse presses/releases, window focus loss and repeated opens.
4. Start a campaign and Continue it. With inventory open, first Esc closes only the inventory; next Esc opens existing campaign menu. F8, save/Continue and menu return still work. Inventory visibility is transient, not saved.
5. Check 1080p, 800×600 and a narrow window; pack content scrolls, hotbar fits, text remains readable, F9/F7 retain precedence, reload produces one HUD.
6. Profile closed/open/settled/resized/reloaded UI: no new UI `_Process`/`_PhysicsProcess`, slot polling, timers, shader churn or world scans. Input/layout events wake changes; settled views sleep. Existing world/player simulation remains intentional. Actual frame cost has not been measured here.
