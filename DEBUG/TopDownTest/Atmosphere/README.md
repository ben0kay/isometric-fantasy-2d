# Disposable atmosphere preview

Open the existing TopDownVisualTest.tscn or press F5. The top-right **Atmosphere comparison** panel independently toggles ground haze, pollen, falling leaves, torch smoke and embers. Existing day/night and polish controls remain at left. All five effects start enabled.

## Look and controls

- **Haze:** patchy, slowly drifting, ground-level mist with clear gaps. More visible at night/dawn, reduced at midday. It is drawn above the terrain/shadows but below actors, preserving tree and player readability.
- **Pollen:** sparse tiny specks with a slow drift and subtle brightness changes; much fainter at night.
- **Leaves:** occasional small brown leaves fall from the existing tree crowns, flutter sideways, tumble, then fade. They do not accumulate on the ground.
- **Smoke:** faint, soft expanding puffs rise from the torch and drift sideways.
- **Embers:** tiny orange sparks rise quickly from the torch and fade. They stay luminous at night.

Smoke and embers follow the existing torch visibility switch. Their individual switches remain independently selectable.

Edit **DefaultAtmosphereConfig.tres**:
- HazeDensity, HazeColour, PatchSize: opacity/tint and the size of mist patches.
- DriftVelocity: shared X/Y mist and pollen drift in world pixels per second; X also influences leaf/smoke/ember trajectories.
- PollenCount, LeafCount, SmokeCount, EmberCount: independent intensity settings. Defaults are 35, 10, 14, 10 with hard caps of 100, 40, 40, 40.
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
