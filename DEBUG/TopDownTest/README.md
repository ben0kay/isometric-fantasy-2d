# Top-down visual test

> Disposable **fully 2D** forest map for the new fantasy game direction. Open `TopDownVisualTest.tscn` and press **F6** (or **F5** for the project's main scene). Requires a Godot .NET project build.

## Larger, seeded procedural scenery

- The playable grass area is now **2560 × 1728 world pixels**, 1.8 times the previous map area, bounded from **(-800,-544)** to **(1760,1184)**. The original central clearing, player spawn and torch are retained. The path continues to the northern and eastern map borders.
- All trees and rocks are now **generated at runtime once** from `Actors._Ready()`. The old hand-placed props are no longer in the scene. You will see grass/clearing/path in the editor and **trees/rocks when you run the scene**.
- The default seed generates up to **80 trees and 34 rocks**, with per-object scale and mirroring and spacing around obstacles, the player clearing and the main path. Alter `Generation/DefaultScatterConfig.tres` for different arrangements, quantities and size ranges. The seed is repeatable; change it to get a new pattern.
- **Tree and rock collisions follow the visual changes.** The generator scales each StaticBody2D root (rather than only its Sprite) and flips CollisionPolygon2D ground footprints alongside mirrored artwork. Rock bases remain collidable without blocking the entire sprite silhouette.
- **All effects still work:** day/night and moving projected shadows, tree-canopy wind, torchlight, smooth cloud shade, haze, pollen, leaves, smoke and embers. DayNight and Atmosphere cache generated objects *after* scattering completes.
- See the [generation guide](Generation/README.md), [tree guide](Trees/README.md) and [rock guide](Rocks/README.md).

## Controls and configuration

- Arrow keys move the small test explorer; camera follows at zoom 2 by default with bounds clamped to the expanded map. The CharacterBody2D foot collider blocks trees, rocks, torch and map borders. Static actors are Y-sorted by root.
- `DefaultTopDownTestConfig.tres`: walk speed (150 pixels/sec) and camera zoom.
- `Generation/DefaultScatterConfig.tres`: world sampling bounds, seed, counts, spacing, root scale ranges and mirror chance. These are **startup parameters**; restart the test after editing.
- `DayNight/DefaultDayNightConfig.tres`: 30-second daylight cycle by default, sun-shadow length and strength and sun settings (shared wind now lives in Weather). Scrub/pause via the top-left UI.
- `Polish/DefaultPolishConfig.tres`: colour grading, soft shadows, cloud shade, flickering torch and related UI switches.
- `Weather/DefaultWeatherConfig.tres`: shared signed wind, GPU rain, ground splashes and overcast. Live sliders appear below the atmosphere panel. See [weather guide](Weather/README.md).
- `Atmosphere/DefaultAtmosphereConfig.tres`: ground haze, pollen, leaves, smoke and embers. Its comparison controls are at the top right.
- `Trees/Tree01.tscn` and `Rocks/Rock01.tscn`: edit shared artwork anchor and physically grounded collision polygons. The test rock uses `Artwork/rock_01.webp`; the tree uses `Artwork/tree_01.png`. These are **debug-only**, not production content.

The world grass/path and bounds are still **authored directly in the scene**, not procedurally meshed terrain. If changing the **world size**, update the camera, collisions, grass region, haze and cloud polygons as described in the generation guide; merely changing the `WorldBounds` config doesn't rebuild the map.

## Validation and scope

The update stays inside `DEBUG/TopDownTest/`, with no new production world, environment, items or survival systems. Procedural props are instantiated only at scene startup and never scattered each frame. Rock/tree spacing is approximate; no complete pathfinding or world streaming is included.

Scene resource paths, node order, prop counts, overlay dimensions and generated collider-transform logic were inspected statically. Current build/render/physics checks are detailed in Liquids/README.md; Windows D3D12 and target hardware still need local checking. Use **Debug → Visible Collision Shapes** and watch Godot's output for generated totals.

## Rain and unified wind prototype

Weather/ is another disposable comparison, not a production system. Rain starts at 55%, builds over two seconds, and can be changed from 0–100%. The signed wind slider runs -3 (left), 0 (calm), +3 (right) and drives rain slant, tree/shadow sway, haze, cloud shade, pollen, leaves, smoke and embers. Ground splashes render below actors and avoid conservative prop artwork bounds. Rain/overcast do not pause with the demo clock. Previous separate wind/drift settings were removed; Weather owns the shared values.

Lightning comparisons are also available in the Weather panel: automatic nearby flashes during rain, rare visible ground strikes, a master toggle and manual test buttons. All remain disposable; see Weather/README.md.

## Decorative wind-swaying grass clumps

The four replacement `grass_small_01.png`–`grass_small_04.png` sprites are scattered as individual tufts and natural-looking clusters by the separate Grass/ debug module. They use the existing tree wind shader, driven by the shared Weather wind slider, with no collisions or per-clump sunlight shadow. Counts and placement are editable in Grass/DefaultGrassPatchConfig.tres; see [Grass/README.md](Grass/README.md).

## Organic grass comparison

Ground/ contains the separate grass palette, fine surface-detail and sampled-noise modules, their material/config and a small C# comparison controller. The left-side controls below Polish toggle the original grass, colour patches and fine detail, and adjust variation. See [ground guide](Ground/README.md). Paths/clearing and the weather/lighting systems remain independently owned.

## Removable snow test

SnowTest/ implements the snow-accumulation note as an independent scene instance, initially off. Its left-side panel controls coverage, falling snow/intensity and automatic accumulation/melting. Tree canopies and all flat ground (including path/clearing) receive snow while wind/shadows remain independent. Toggle off to compare, or remove the SnowTest scene instance and folder to discard it. See [snow guide](SnowTest/README.md) for exact cleanup, settings and limitations.

## Lake, wading and enlarged world

[Liquids/](Liquids/README.md) adds one organic lake southeast of the clearing, with independent liquid-type data, depth colours, shared-wind ripples, a wet bank and walk-in sprite submersion. The right-side panel includes a Go to lake bank button. Its footprint excludes trees, rocks and grass at startup; ground snow stays beneath the water. The player body/camera remain stable while its sprite dips and lower part is clipped. Disabling the test restores normal traversal.

The larger world, camera, walls, grass and atmosphere bounds now agree at 2560×1728. Scattering still runs once. Gravel edges also use a wider smooth fade with restrained pebble contrast, avoiding the old broken/blotchy outer coverage. See the liquid guide for tests, limits and removal hooks.

## Rain impacts on the lake

The `Liquids/` test now creates bounded expanding ripple rings when the existing Weather rain slider is above zero. Use the new **Rain surface splashes** checkbox in the liquid panel; the original **Ground splashes** checkbox still controls dry-ground effects independently. See [Liquids/README.md](Liquids/README.md) for settings and performance limits. No production mechanics were changed.


## Adjacent biome prototype

See [Biomes](Biomes/README.md) for the Oak Forest / Pink Meadow test. The current map is **3840×1728**, bounds **(-800,-544) → (3040,1184)**, with **80 trees / 50 rocks**. The original forest and lake remain west; east of the curved x≈1760 boundary is pink ground with shared rocks only. Biome definitions own ground palettes and placement permissions; ground detail, gravel, weather and snow remain separate. These current dimensions supersede earlier map-size/count examples above.


## Equipped axe prototype

[Weapons/](Weapons/README.md) adds a permanently equipped cursor-aimed axe. Left-click outside the debug panels for a full-circle swing; arrow-key movement remains available. Close, aimed swings now produce a tree-local wobble and extra leaf burst; this is feedback only, without tree health/loot. DefaultWeaponConfig.tres controls reach, anchor, swing duration and aim response; remove the one player child instance to undo.

## Pink Blossom and Shader Wind and Snow

The eastern Pink Meadow generates 36 pink blossom trees via `Trees/PinkBlossom01.tscn`, while Oak Forest retains its 80 oaks. Both use the same procedural scatter, grounded collisions, wind shader, projected sun shadow and removable snow system. The snow shader now supports a reusable pink-blossom foliage profile; the existing axe hit helper also works on pink trees and produces pink petals. The SnowTest controls include a **Shader Wind and Snow** heading. See [Biomes/README.md](Biomes/README.md), [Trees/README.md](Trees/README.md) and [SnowTest/README.md](SnowTest/README.md). This remains entirely debug-only.
