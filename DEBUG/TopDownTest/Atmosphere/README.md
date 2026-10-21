# Disposable atmosphere preview

Open the existing TopDownVisualTest.tscn or press F5. The top-right **Atmosphere comparison** panel independently toggles ground haze, pollen, falling leaves, torch smoke and embers. Existing day/night and polish controls remain at left. All five effects start enabled.

## Look and controls

- **Haze:** patchy, slowly drifting, ground-level mist with clear gaps. More visible at night/dawn, reduced at midday. It is drawn above the terrain/shadows but below actors, preserving tree and player readability.
- **Pollen:** sparse tiny specks with a slow drift and subtle brightness changes; much fainter at night.
- **Leaves:** small brown leaves fall from generated tree crowns, flutter sideways, tumble, then fade. **Stronger absolute wind produces more falling leaves, more sideways/downwind travel and livelier flutter.** A negative Weather wind blows left; a positive value blows right. They do not accumulate on the ground.
- **Smoke:** faint, soft expanding puffs rise from the torch and drift sideways.
- **Embers:** tiny orange sparks rise quickly from the torch and fade. They stay luminous at night.

Smoke and embers follow the existing torch visibility switch. Their individual switches remain independently selectable.

Edit **DefaultAtmosphereConfig.tres**:
- HazeDensity, HazeColour, PatchSize: opacity/tint and the size of mist patches.
- shared Weather wind velocity: shared X/Y mist and pollen drift in world pixels per second; X also influences leaf/smoke/ember trajectories.
- PollenCount, LeafCount, SmokeCount, EmberCount: independent baseline intensity settings. Defaults are 35, 10, 14, 10 with hard caps of 100, 40, 40, 40.
- LeafWindCountBoost (default 1): multiplies how many **leaf slots** are active as absolute wind increases: `clamp(LeafCount × (1 + abs(WindStrength) × LeafWindCountBoost), 0, 40)`. With LeafCount=10 and the default boost: wind 0 → 10 slots, wind ±1 → 20, wind ±3 → 40. Leaves are age-staggered and invisible for part of their lifecycle, so the number **drawn in one frame** is lower.
- LeafWindDrift (default 2): multiplies signed `DebugWeather.Velocity` to push leaves over time in the wind direction, with slightly larger flutter and spin in stronger winds. It is **separate from** the shedding-count boost. Existing leaves reverse smoothly when the wind direction changes, rather than teleporting.
- Both settings are in the `Leaves / wind` group of `AtmosphereMasterConfig.cs` and editable via `DefaultAtmosphereConfig.tres`. The wind strength and direction themselves stay under **Weather's existing slider/config** (−3 left, 0 calm, +3 right).
- Five enable switches match the comparison panel.

Settings are read live, so use the Remote Inspector to tune values while running. Checkbox edits change the resource only in memory and reset on restart. Remote Inspector edits do not automatically refresh checkbox states.

The day/night clock controls lighting, not animation time. Pausing the clock holds the light while mist and particles keep moving; this helps compare dawn/night effects.

## Ownership and limits

All new code/resources/shader/UI live in **DEBUG/TopDownTest/Atmosphere/**. The parent debug scene gains one instance. No WORLD, ENVIRONMENT or production entity files are modified; imported artwork is untouched.

Mist uses two inexpensive smooth-noise layers anchored to map coordinates, so moving the camera does not drag the mist along with the screen. It suggests low fog on a flat map; it is not volumetric and does not physically curl around trunks. The fixed overlay covers this test's 960×640 map.

One C# drawing node renders bounded, deterministic particle trajectories rather than spawning a Node per particle. Tree positions are cached once. It uses primitive dots/leaf polygons and a small built-in radial gradient for smoke; no asset downloads or generated bitmap artwork are required. Sparse leaves can pass visually in front of scenery because particles share a simple foreground layer. No particle collisions, seasons, gust simulation, sound, sun shafts or fireflies are added in this pass.

The drift setting is a debug atmosphere control, not a production weather/wind system. Existing tree sway still uses the day/night config's wind multiplier.

## Validation

The haze shader and native scene resources are validated with headless Godot 4.5.1, and static checks cover scene resource counts, dependencies, particle bounds and alpha ranges. Matching Godot .NET 4.7.x and a C# compiler are not installed here; full C# compilation, GPU rendering, visual tuning and performance measurements remain pending.

Try noon versus 06:00 and 22:00 using the existing time slider, toggle each effect separately, walk across mist patches, and disable the torch to confirm both emissions disappear.


### Shared noise seam correction

The shader-calculated grid noise and its later simplex replacement are superseded. Both overlays now sample **Atmosphere/AtmosphereNoise.tres**, a shared 512×512 seamless FastNoiseLite texture with linear filtering and repeat enabled. OrganicNoise.gdshaderinc performs only smooth texture sampling/warping; it no longer calculates gradient cells or sine hashes in the fragment shader. Both scene materials explicitly bind the same noise texture.

This addresses the shared path behind the visible straight cutoffs instead of changing only the pattern's shapes. The exact artifact reported on the user's graphics backend was not reproduced locally, so backend-specific diagnosis is not claimed as confirmed.

Validation now includes actual rendered Godot 4.5.1 Compatibility images using software OpenGL: haze with ambient night tint and a PointLight2D, plus cloud shade in isolation. Both rendered outputs were inspected for continuous patches. These are isolated rendering tests, not a C# build or full-scene screenshot in Godot .NET 4.7.x on the user's backend. Existing controls, day/night blending and drift are retained.

The texture is generated once and shared; there are no per-frame image uploads. Its seamless setting prevents a hard tiling edge, and explicit sampler filtering is independent of the pixel-art filtering used for scenery.

## Expanded map and generated trees

The new debug world is 1920×1280 (origin -480,-320). Ground haze covers the enlarged polygon and samples its edge fade against the configurable debug scatter bounds instead of the old 960×640 values. Pollen wraps across the full area. Tree crown positions are collected **after runtime generation**, and the leaf sources are distributed across more of the available trees. Smoke/embers remain near the unchanged central torch. These are still capped preview particle counts, not scaled-up particle counts per square metre.

## Shared wind integration

The separate atmosphere DriftVelocity setting has been removed. DebugWeather integrates one shared displacement for haze, cloud shade and pollen, while leaves/smoke/embers integrate per-cycle drift without teleporting when wind changes. Haze samples negative displacement so visible movement follows wind direction. Controls and clock remain independent. See Weather/README.md for current validation.

## Wind-linked leaf shedding (debug only)

The leaf effect now reuses the shared Weather wind vector. `LeafCount` is the calm-weather baseline. Stronger wind activates more of the existing 40 leaf slots; newly active slots fade in, instead of instantly appearing at full opacity. Wind also changes horizontal travel, flutter amplitude and tumble speed, while preserving the leaf starting positions beneath generated tree canopies. A signed velocity is integrated per leaf lifetime, so slider direction reversals bend the flight paths without jumps.

**Try it:** Keep Falling leaves enabled. Set the Weather wind slider to 0 and observe sparse mostly vertical falling. Move it to +3 to see more leaves blowing to the right, then −3 to see them drift left. Tweak `LeafWindCountBoost` and `LeafWindDrift` independently if the effect is too busy or too forceful. Counts remain hard-capped at 40 and no particle nodes are spawned.

Only the debug atmosphere renderer/configuration has changed. Wind, rain, tree sway, weather controls and production folders are otherwise untouched. Gameplay testing in the Godot .NET editor is still needed.
