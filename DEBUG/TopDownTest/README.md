# Top-down visual test

> Disposable **fully 2D** forest map for the new fantasy game direction. Open `TopDownVisualTest.tscn` and press **F6** (or **F5** for the project's main scene). Requires a Godot .NET project build.

## Larger, seeded procedural scenery

- The playable grass area is now **1920 × 1280 world pixels**, four times the original area, bounded from **(-480,-320)** to **(1440,960)**. The original central clearing, player spawn and torch are retained. The path continues to the northern and eastern map borders.
- All trees and rocks are now **generated at runtime once** from `Actors._Ready()`. The old hand-placed props are no longer in the scene. You will see grass/clearing/path in the editor and **trees/rocks when you run the scene**.
- The default seed generates up to **58 trees and 24 rocks**, with per-object scale and mirroring and spacing around obstacles, the player clearing and the main path. Alter `Generation/DefaultScatterConfig.tres` for different arrangements, quantities and size ranges. The seed is repeatable; change it to get a new pattern.
- **Tree and rock collisions follow the visual changes.** The generator scales each StaticBody2D root (rather than only its Sprite) and flips CollisionPolygon2D ground footprints alongside mirrored artwork. Rock bases remain collidable without blocking the entire sprite silhouette.
- **All effects still work:** day/night and moving projected shadows, tree-canopy wind, torchlight, smooth cloud shade, haze, pollen, leaves, smoke and embers. DayNight and Atmosphere cache generated objects *after* scattering completes.
- See the [generation guide](Generation/README.md), [tree guide](Trees/README.md) and [rock guide](Rocks/README.md).

## Controls and configuration

- Arrow keys move the small test explorer; camera follows at zoom 2 by default with bounds clamped to the expanded map. The CharacterBody2D foot collider blocks trees, rocks, torch and map borders. Static actors are Y-sorted by root.
- `DefaultTopDownTestConfig.tres`: walk speed (150 pixels/sec) and camera zoom.
- `Generation/DefaultScatterConfig.tres`: world sampling bounds, seed, counts, spacing, root scale ranges and mirror chance. These are **startup parameters**; restart the test after editing.
- `DayNight/DefaultDayNightConfig.tres`: 30-second daylight cycle by default, sun-shadow length and strength and overall tree wind multiplier. Scrub/pause via the top-left UI.
- `Polish/DefaultPolishConfig.tres`: colour grading, soft shadows, cloud shade, flickering torch and related UI switches.
- `Atmosphere/DefaultAtmosphereConfig.tres`: ground haze, pollen, leaves, smoke and embers. Its comparison controls are at the top right.
- `Trees/Tree01.tscn` and `Rocks/Rock01.tscn`: edit shared artwork anchor and physically grounded collision polygons. The test rock uses `Artwork/rock_01.webp`; the tree uses `Artwork/tree_01.png`. These are **debug-only**, not production content.

The world grass/path and bounds are still **authored directly in the scene**, not procedurally meshed terrain. If changing the **world size**, update the camera, collisions, grass region, haze and cloud polygons as described in the generation guide; merely changing the `WorldBounds` config doesn't rebuild the map.

## Validation and scope

The update stays inside `DEBUG/TopDownTest/`, with no new production world, environment, items or survival systems. Procedural props are instantiated only at scene startup and never scattered each frame. Rock/tree spacing is approximate; no complete pathfinding or world streaming is included.

Scene resource paths, node order, prop counts, overlay dimensions and generated collider-transform logic were inspected statically. The environment used for this change does **not** include Godot/.NET, so compile/visual/physics tests need to be run locally. Use **Debug → Visible Collision Shapes** and watch Godot's output for generated totals.
