# Top-down visual test

Disposable **fully 2D** forest clearing for the new README direction. Open `TopDownVisualTest.tscn` and press **F6**, or **F5** (now the main scene). Requires Godot .NET and the existing C# project to build.

- Arrow keys move the static explorer; diagonals have the same speed. No animation or facing changes.
- CharacterBody2D and foot collider handle tree trunks, rocks and map bounds; Camera2D follows without rotation.
- Actors sort at their feet using Y sorting, so the explorer can pass behind tree canopies.
- All ground, scenery, colliders and spawn are authored in the scene and visible in the editor. Nothing generates the terrain at runtime.
- Artwork/ contains the original 32×48 explorer, grass, rock, and placeholder tree SVGs, plus the new transparent `tree_01.png`. All 12 trees now instance `Trees/Tree01.tscn`, displaying `tree_01.png` at 0.115 scale with a root-centred anchor and editable collision polygon. A subtle shader breeze moves the crown while the lower trunk stays fixed. The source PNG is preserved at full resolution; its sprite uses linear texture filtering for downscaling. This is a temporary palette/proportion test, not final production art.
- Ground details and the small player shadow are simple 2D polygons. A contained [day/night preview](DayNight/README.md) now adds a 30-second full cycle, ambient colour changes, contact shadows and projected sun shadows. No combat, survival or world generation.

**Day/night controls:** use the top-left slider to scrub time and Pause to freeze the cycle. Edit `DayNight/DefaultDayNightConfig.tres` for cycle length, colours, shadow lengths/opacity and shared test wind strength.

**Visual-polish comparison:** a second panel toggles colour grading, soft sun shadows, moving cloud shade and a flickering torch. Settings live in `Polish/DefaultPolishConfig.tres`; see [polish notes](Polish/README.md). These are disposable debug effects.

## Tuning

Edit `DefaultTopDownTestConfig.tres` in the Inspector: walking speed defaults to 150 pixels/second and camera zoom to 2. Config is separate from the input/movement scripts. Sprite nodes and scenery are editable directly in the scene. Edit `Trees/Tree01.tscn` for the shared footprint and `Trees/Tree01Wind.tres` for wind strength/speed/canopy cutoff. See the [tree guide](Trees/README.md) for new sprite shapes. The root DEBUG master config remains a generic reserved resource; this test owns its settings.

All new test code, art, resources and the scene live here; production folders have no dependency on this package. To remove it later, replace/clear the main scene in Project Settings, then delete DEBUG/TopDownTest/. Existing project input actions can stay for future use.

## Validation

Source/resource and SVG checks completed. No Godot or dotnet executable was available in the implementation environment; compilation, import, rendering and movement play-tests remain pending. Test cardinal/diagonal movement, stopping, all four bounds, trunk/rock collisions and canopy occlusion locally. The old VisualTests/NaturalTerrainTest.tscn is a retired 3D leftover and is not loaded by this test.
