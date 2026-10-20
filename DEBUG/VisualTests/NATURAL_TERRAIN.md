# Rolling terrain experiment (DEBUG-only)

**Scene:** `NaturalTerrainTest.tscn` (new default F5 scene). The older `Courtyard2DProps.tscn` and `Courtyard3DProps.tscn` remain available for comparison.

This disposable test creates **90 × 90 metres of real 3D rolling terrain**, with a single triangulated collision surface, gentle rolling slopes, subtle green color variation, **70 instances of the PNG tree sprite**, collision only around trunks, and direct warm sunlight with real shadows. The player remains the existing **2D PNG Sprite3D** on a CharacterBody3D. Arrow keys move relative to the fixed isometric camera.

## Tuning

Open `DefaultNaturalTerrainConfig.tres` in the Godot Inspector:
- **Ground:** `WorldWidth`, `WorldDepth`, `GridStep`, `HillHeight`, `NoiseHeight`, `TerrainSeed`.
- **Forest:** `TreeCount`, `TreeHeight`, `MinimumTreeSpacing`, `TreeSeed`.
- **Playtest:** `PlayerWalkSpeed` defaults to **7.5** (normal shared player default remains **4**).
- **Sun:** `SunPitch`, `SunYaw`, `SunEnergy`. Intended upper-right directional sunshine at the locked camera angle; fine-tune after visual review.

The test duplicates the player's config at runtime, so speed does not modify other scenes. Trees use `DEBUG/VisualTests/Artwork/tree_01.png`; player uses `ENTITIES/Player/Visuals/Sprites/player.png`. Their source assets, gameplay code and camera are unchanged.

## Troubleshooting: player falls through the world

This test depends on the **Godot .NET C# script** to generate the visible rolling terrain and its collision. If the script does not compile or run, both terrain and trees may be missing, even though the scene loads.

A **low, flat static safety floor** now exists in the saved `.tscn` at approximately Y = -7 (below all normal generated hills). It prevents infinite falling even if generation fails, but it is **not** the intended rolling landscape. If you land on flat dark ground with no trees, **the procedural C# code did not execute**:

1. Open the project using the **Godot .NET** editor.
2. Press **Build** (hammer icon) or use **Project → Tools → C# → Build Project** before pressing F5.
3. Open Godot's **Output** and **Debugger → Errors** tabs; resolve C# compile errors first.
4. On success the Output prints `[NaturalTerrainTest] Terrain surface and 3D collision created`, then a setup-complete/tree-count message.
5. If the messages print but you still pass through terrain, check collision shapes via **Debug → Visible Collision Shapes** and report the errors/logs.

The fallback floor is intentionally lower than the terrain, so visible slopes and player ground contact still depend on successful generation. If terrain is absent, the fallback is an emergency stop, not a terrain substitute.

## Notes and limitations

- Terrain generated once when entering scene; intentionally **not** a production chunk-streaming/world generator.
- Slopes are a real mesh and real trimesh collider; no cliffs, caves or terrain digging in this test.
- Flat tree cards still have approximately round trunk colliders and will show some planar lighting artifacts. No occlusion/fade shader yet.
- World bounds are finite; there is no boundary wall in this visual test. Turn back at the edge.
- All lighting and style defaults are a starting point, not verified as final screen-space upper-right until reviewed in Godot.
- **Not runtime-tested here:** Godot/.NET import, C# compilation, collision and shadow rendering should be validated locally.

## How to try it

Run `git pull`, open the project in Godot .NET, allow its C# project to build, then press F5. To compare the old scene, open `Courtyard2DProps.tscn` and press F6.

## Technical files

- `NaturalTerrainTest.cs` — creates one ground mesh/collider, places tree cards, and sets debug-only speed and sun direction.
- `NaturalTerrainConfig.cs` / `DefaultNaturalTerrainConfig.tres` — Inspector knobs for this scene only.
- `NaturalTerrainTest.tscn` — lighting, camera, player, and named grouping nodes.
