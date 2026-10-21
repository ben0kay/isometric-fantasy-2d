# Disposable day/night and sun-shadow preview

F5 starts the existing clearing at 08:00. **A complete 24-hour day/night cycle takes 30 seconds** by default. Use the top-left time slider to scrub; toggle Pause to inspect a fixed time. UI stays readable at night because it lives on its own CanvasLayer.

Edit **DefaultDayNightConfig.tres** in the Inspector:
- CycleSeconds: seconds per complete 24-hour loop (30 by default).
- StartHour: starting time, 0–24.
- NoonColour / HorizonColour / NightColour: scene tint at midday, sunrise/sunset and night.
- ShadowOpacity: maximum sunlight shadow alpha.
- MinimumLength / MaximumLength: projected height multipliers (0.18 and 1.65), not pixels. Tall sprites naturally cast larger shadows.
- SunDirectionOffsetDegrees: rotate the demonstration sun path.
- WindStrengthMultiplier: shared strength for all trees in this test; 0 disables wind. Each tree material retains its own base strength/speed/canopy cutoff.

The settings are read while running, so they can also be adjusted through the Remote Inspector. StartHour initializes the clock only; scrub the slider to change current time.

## Implementation

One C# controller uses CanvasModulate to blend night blue, warm dawn/dusk and daylight. Sunrise is 06:00, noon 12:00, sunset 18:00. During daylight a finite, skewed silhouette projects from each tree, rock and the player's ground anchor. Length grows toward sunrise/sunset and shortens at noon; opacity fades to zero below the horizon. Contact shadows remain at the roots/feet.

Projected shadows and static prop contact ovals are visual helpers created once at scene startup. Terrain/scenery/collision remain authored. The moving player shadow updates from its body each frame. Ground renders at Z=-10, helper shadows at -5 and normal actors at 0, preventing tree shadows from drawing over sprites. Existing player contact shadow is retained.

Tree silhouette materials reuse the crown sway shader in shadow-only mode. Tree visuals and silhouettes share a scene-wide wind multiplier; materials are cloned for the test so no saved resource values are changed at runtime. Tree/shadow wind phases can differ slightly because phase derives from each rendered sprite's transform.

The controller caches 18 casters (12 trees, 5 rocks and the player), uses shared materials, and does not search the scene every frame. Each projected shadow adds a sprite draw and transparent overdraw. Profile before scaling to large forests; this is a contained visual experiment.

## Limits and validation

These are **art-directed flat-ground projections**, not 3D light simulation: shadows do not climb walls/actors, account for terrain elevations or mutually occlude. Semi-transparent overlaps darken. Some sunset angles can flatten a silhouette nearly edge-on; direction offset is experimental. No moon shadows or torch system is included. The imported sprite's baked lighting remains visible while the whole canvas changes colour.

Static resource checks and a full-cycle numerical check cover wrapping, twilight values, finite shadow lengths, daytime/nighttime opacity and projection anchoring. No Godot/.NET executable is installed here, so C# compilation, shader compilation and actual rendering/play-testing are still pending.

Everything lives under DEBUG/TopDownTest/. For later promotion, move the config/controller/materials into your chosen environment folders, update resource paths and replace the demo clock controls with your game clock. Production folders currently have no dependency on this prototype.
