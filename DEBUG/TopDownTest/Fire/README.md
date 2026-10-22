# FIRE1 + FIRE2 — oak burning lifecycle, GPU fire and foliage erosion

**Implemented 21 October 2026. FIRE1 only.** Run TopDownVisualTest, approach an oak, then use the top-centre **Ignite nearest oak** button. The closest healthy oak within 320 world pixels crossfades after a 1-second delay over 5 seconds into the uploaded charred leafless oak. **Reset all debug oaks** restores them without reloading. No tree is spawned or teleported. UI reports no eligible oak when none is nearby. Pink blossom trees are not burnable.

FIRE1 is the state/art foundation. **FIRE2 is now implemented** with active GPU flames, smoke, embers, a shared-wind response, warm flickering light and per-pixel foliage erosion. Lightning tree targeting, wildfire spread, gameplay damage, loot, ash piles and collapse are still out of scope (FIRE3/FIRE4).

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

## FIRE2 — implemented GPU visuals and erosion

FIRE2 listens to FIRE1's existing `Ignited`, `BurnCompleted` and `BurnReset` signals. The original `BurnableTree` still owns its one burn timer, controls the charred image, and performs the final texture swap. Only its temporary blending mode was extended: successful shader attachment shows the dead overlay **behind** the healthy image while the GPU removes green foliage in irregular patches; unsupported shaders retain the original FIRE1 crossfade.

### Files and responsibilities

- `Visuals/BurningTreeEffect.cs`: one temporary child only while an oak is burning or its smoke is finishing. Generates flame, soft smoke and ember textures **once**, reusing them for all burning trees. Uses three bounded `GpuParticles2D` emitters and one warm `PointLight2D` per burning tree. The emitters use `LocalCoords=false`, allowing earlier particles to finish drifting even after the tree or wind moves; they stop emitting when the fire goes out. The firelight flickers and fades with intensity.
- `Visuals/FireErosionAdapter.cs`: temporarily clones **the current** tree wind/snow material only for an actively burning oak, extends its shader at the known `COLOR` output anchor, preserves existing sway/snow code and syncs shared weather wind and current snow parameters. Does not alter the shared tree or grass shaders. Material is restored on completion/reset, respecting an external SnowTest material swap.
- `Visuals/FireErosion.gdshaderinc`: two noise scales gradually darken and erase the healthy upper canopy; the base/trunk survives until near the end, avoiding an obvious double trunk. The original charred leafless PNG shows through from underneath. No CPU texture repainting.
- `Simulation/BurnableTree.cs`: adds `SetFoliageErosion(bool)` as a small *optional* visual hook; its original public state machine/events are otherwise unchanged.
- `Debug/FireTestController.cs`: listens for ignition and spawns visuals only for fresh burns, with an Inspector-configurable concurrent-effects cap. An oak beyond the cap still burns using FIRE1's art-only transition. Existing nearest-oak/reset/status buttons remain and the scene still removes cleanly.
- `FireMasterConfig.cs`: new Inspector groups for flames, smoke, ember counts/intensities, wind response, warm light, temporary-effect cap, smoke tail and char tint. All are reusable appearance parameters.

### Behaviour and integration

- In the main test, press **Ignite nearest oak** near a forest oak. FIRE2 should show the flame band around the upper trunk/crown, rising smoke, glowing embers drifting with the **same** `Weather.WindStrength` as the grass, trees, snowfall and clouds, plus flickering nearby light. Changing wind between -3 and +3 affects **newly emitted particles** without teleporting particles already in flight.
- `BurnProgress` drives the erosion mask (with FIRE1's ignition delay respected). The charred PNG and pre-authored source anchors remain unchanged. At completion, FIRE1 switches the original permanent Sprite2D to charred art; FIRE2 restores the material and keeps smoke emitting weakly for `SmokeTailSeconds`, then lets existing GPU particles finish before removing the effect.
- **Reset all debug oaks** removes fire, glow, smoke, embers and erosion immediately and returns the original material and art. Neither BurnableTree nor FX adds runtime physics bodies, damage, a new wind system or independent lightning targeting.
- Shared `SnowTest/TreeSnowAdapter` and DayNight continue to own general vegetation snow/wind and cached silhouettes. FIRE2 clones their **runtime** material only during an active burn. Mid-burn snow coverage is copied, and the effect can rebind if another adapter changes the sprite material. Shadow images still use healthy leaves until the FIRE1 final swap; the partial-shadow treatment belongs to FIRE4.
- At most `MaxActiveEffects` oaks allocate FX at once (default 10); only those FX and actually burning tree states process every frame. Each oak has no ongoing fire work while healthy or fully burned. One small 14×24 flame, 28×28 smoke, and 5×5 ember image plus a radial light/noise texture are shared globally. There are **no per-frame texture writes**.
- The original tree/rock scene count, scatter/collision, pink blossom biome, Lightning ground strikes, weather rain, rock snow and player input were not changed.

### Verification checklist for FIRE2

1. Godot .NET: rebuild project, run `TopDownVisualTest.tscn`; confirm no C# or shader compile errors.
2. Ignite an oak with SnowTest OFF. Confirm orange/yellow flames, rising grey smoke, sparks, warm light and **irregular** green-leaf removal revealing the charred oak, with no visibly offset trunks.
3. Test shared Wind slider at **-3 / 0 / +3** while a tree burns; smoke drifts in the intended direction, sparks respond more than flames, and original tree sway remains present.
4. Test at day and night. Firelight should illuminate the surroundings but vanish after burnout/reset.
5. Enable SnowTest, ignite an oak, and change snow coverage while burning; check shader compatibility, materials after reset and a stable charred tree after completion.
6. Press Reset all during a burn and immediately after burn completion; verify no stuck snow material, temporary FX or extra collision.
7. Ignite 1, then 5, then up to 10 separate oaks; observe frame time and GPU particle workload. New FX are bounded by `MaxActiveEffects`; later ignition still performs the FIRE1 art-only transition when the cap is reached.
8. Removing the `FireTest` scene while running must remove any live FX without deleting permanent tree entities. FIRE3 lightning targeting and FIRE4 partial shadows are **not** included.

**Validation scope:** code/resource/scene paths were reviewed in the GitHub connector. The current environment cannot run the user's Godot .NET build or visually validate Forward+/Compatibility shaders; complete the checks above locally.

## Remaining handoff

FIRE2 now uses those FIRE1 signals, one per-burning-tree helper, shared wind, bounded GPU FX and an irregular GPU erosion mask. **Next: FIRE3** needs explicit oak targeting; current lightning is still open-ground only. FIRE4 covers accurate partial-burn shadows, charred-branch snow, ambient leaf eligibility and aftermath.

Known limits: mid-transition shadow retains the healthy silhouette until final swap; ambient Atmosphere leaves can still originate at charred oak anchors; charred branches retain the existing wind shader; no charred-wood snow profile; FIRE2 replaces the alpha crossfade only when the current shader can be extended. The optional controller caches the startup oak set, suitable for this fixed generated test rather than dynamic world streaming.

## Validation and handoff evidence

All debug C# compiled with .NET 8 and Godot 4.5.1 source generators/native API. Actual software OpenGL Compatibility renders were inspected at mid-blend and burnt stages: grounded normal/mirrored art and the new shadow silhouette. Runtime checks passed for once-only ignition/completion signals, repeated-ignition rejection, idle/burnt processing disabled, both mirrored source anchors within 0.01 world pixels, unchanged entity/root/collider/material, no extra shadows/remaining overlay, axe foliage suppression/reset, snow enabled during a cycle and removed before reset, and nearest/no-range controls. Existing camera/enemy test changes were retained. Rendering used reduced decorative grass counts locally only. The existing seeded scatter logs 115/116 trees (bounded placement); FIRE1 does not change generation. Windows D3D12 and hardware performance were not tested.
