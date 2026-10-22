# FIRE1 — manual oak lifecycle and art transition

**Implemented 21 October 2026. FIRE1 only.** Run TopDownVisualTest, approach an oak, then use the top-centre **Ignite nearest oak** button. The closest healthy oak within 320 world pixels crossfades after a 1-second delay over 5 seconds into the uploaded charred leafless oak. **Reset all debug oaks** restores them without reloading. No tree is spawned or teleported. UI reports no eligible oak when none is nearby. Pink blossom trees are not burnable.

This is the state/art foundation. There are deliberately no flames, smoke, lights, lightning integration, spreading, damage, drops, ash or collapse yet. FIRE2–FIRE4 remain planned.

## Modules and stable public contract

- `FireMasterConfig.cs` / `DefaultFireConfig.tres`: enabled flag, healthy/dead texture resources, source-pixel ground anchors, delay, transition duration, optional reignition and nearby search radius. Timers use real elapsed seconds, independently of day/night speed.
- `Simulation/BurnableTree.cs`: optional oak child; owns `FireTreeState` (`Healthy=0`, `Burning=1`, `Burnt=2`). Read-only `State`, `BurnProgress`, `WorldImpactPosition` and `IgnitionSource`.
- `bool TryIgnite(Vector2 worldImpactPosition, string source = "Debug")`: true only for a fresh accepted ignition; burning requests and burnt requests with MayReignite=false return false. Reignition explicitly resets before starting again when enabled.
- `ResetBurn()`: healthy art/state, original offset/tint, healthy axe foliage; removes an active temporary blend. Does not replace the material currently owned by wind/snow.
- Signals/C# events: **Ignited(Vector2 worldImpactPosition, string source)**, **BurnCompleted()**, **BurnReset()**, **AppearanceChanged()**. Ignited/BurnCompleted occur once per successful cycle. AppearanceChanged fires at final swap and reset. Subscribe for later effects; do not create a second burn timer.
- `Debug/FireTestController.cs` / `FireTest.tscn`: one removable sibling instance, exported ActorsPath, cached oak set, nearest healthy selection with actor-name tie breaking, reset/status controls, event-driven cached-shadow refresh.

Idle and burnt receivers have processing disabled. Each active burn uses one temporary Sprite2D overlap, removed on completion/reset. The same permanent Sprite2D ends with the dead texture; root StaticBody2D, identity, transforms, mirror flag, collision and placement remain intact.

## Exact artwork and anchor decisions

`Artwork/oak_charred_source.png` is a byte-identical copy of root **Charred Leafless Oak Sprite.png**, Git blob `cb9f85a132406bb567a09fc328b910d223ead325`. Both root uploads remain unchanged, including the unused Charred Oak Tree Sprite.png. No image was regenerated, stretched or repainted.

Healthy art is 1086×1448; charred art is 1122×1402. Transparent extents and root geometry differ. Runtime alignment uses explicit source-pixel anchors **healthy (548,1290)** and **charred (565,1235)**, at the same existing 0.115 uniform sprite scale. Sprite.Offset supplies the difference; Sprite.Transform is preserved so the axe helper's cached rest transform remains valid. FlipH reflects each anchor within its own texture width before computing the offset; positive whole-root variation scale then applies normally. The charred image's roots/branches have their own authored shape; this is not an identical pixel silhouette with leaves erased.

The temporary dead overlay inherits the living sprite transform and mirrors, uses the computed dead offset and has no custom fire shader. SelfModulate alpha crossfades only the living sprite, avoiding inherited-alpha suppression of its child. The final texture/offset swap happens on the permanent sprite. Minor crossfade translucency and surviving branch wind are accepted FIRE1 limitations for later masked erosion.

## Narrow integration hooks

- Oak Tree01.tscn gains one BurnableTree child/config. PinkBlossom01.tscn and generation stay unchanged.
- DayNight gains `RefreshShadowAppearance(Node2D body)`: refreshes texture, offset, flip/filter/centred flags on the existing matching cached shadow once, without rebuilding/spawning shadows. FireTest subscribes to AppearanceChanged.
- TreeHitFeedback gains independent `SetHealthyLeaves(bool)` / read-only HealthyLeaves. Ignition clears live leaf particles and prevents healthy leaf bursts during/after burning; wobble/collision still work. Reset re-enables foliage. The hit receiver does not depend on fire classes.
- Snow materials are never cached/replaced/restored by fire. Snow can change or be removed during a burn; final/reset use the current material identity. Charred wood does not have a dedicated snow profile yet. Existing healthy-crown snow and wind shaders remain in place.

## Removal

Reset oaks first, then remove FireTest from the main test scene and BurnableTree from Tree01, including their ext_resource references, and delete Fire/. No production files were touched. The generic DayNight refresh method and healthy-leaf switch can remain unused or be removed. Removing a receiver during play restores its healthy appearance. Removing the controls alone leaves receiver state available; it disconnects the UI by scene lifetime, so keep the controller while using its shadow event wiring.

## Remaining handoff

FIRE2 should subscribe to Ignited/BurnCompleted/BurnReset, use BurnProgress, keep one entity and use the existing shared Weather wind. Add bounded GPU effects and a proper erosion mask separately. FIRE3 must implement explicit oak targeting; current lightning is still open-ground only. FIRE4 covers accurate partial-burn shadows, charred-branch snow, ambient leaf eligibility and aftermath.

Known limits: mid-transition shadow retains the healthy silhouette until final swap; ambient Atmosphere leaves can still originate at charred oak anchors; charred branches retain the existing wind shader; no charred-wood snow profile; the alpha crossfade is deliberately basic. The optional controller caches the startup oak set, suitable for this fixed generated test rather than dynamic world streaming.

## Validation and handoff evidence

All debug C# compiled with .NET 8 and Godot 4.5.1 source generators/native API. Actual software OpenGL Compatibility renders were inspected at mid-blend and burnt stages: grounded normal/mirrored art and the new shadow silhouette. Runtime checks passed for once-only ignition/completion signals, repeated-ignition rejection, idle/burnt processing disabled, both mirrored source anchors within 0.01 world pixels, unchanged entity/root/collider/material, no extra shadows/remaining overlay, axe foliage suppression/reset, snow enabled during a cycle and removed before reset, and nearest/no-range controls. Existing camera/enemy test changes were retained. Rendering used reduced decorative grass counts locally only. The existing seeded scatter logs 115/116 trees (bounded placement); FIRE1 does not change generation. Windows D3D12 and hardware performance were not tested.
