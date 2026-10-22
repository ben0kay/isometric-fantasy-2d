# Top-down debug camera: wheel zoom

**Status:** active, disposable feature for `DEBUG/TopDownTest/TopDownVisualTest.tscn`. This is unrelated to the 3D `CAMERA/` production prototype.

`TestCameraZoom.cs` is attached directly to the player's existing `Actors/Player/Camera` `Camera2D`. It owns its own `Zoom` and leaves movement, camera following, original world limits, camera offset and the weather/lighting rendering alone.

## Behaviour

- **Mouse wheel down:** smoothly **steps** outwards across several wheel ticks, revealing more world.
- **Mouse wheel up:** steps back toward the **unchanged starting zoom**, never past it.
- Starting camera zoom is still **2.0** (or the `TopDownTestConfig.CameraZoom` Inspector value).
- Farthest view is **1.6 zoom** at default config (`2.0 / 1.25`): the visible world is **25% wider and 25% taller**, without permitting any closer-than-default view.
- Wheel controls respect UI-handled events through `_UnhandledInput`: scrolling within a control that consumes the wheel will not zoom the world behind that control.
- The camera still follows the player and respects its existing map limits; scrolling doesn't change the player's collision, speed or physics.

## Data-driven settings

In `DEBUG/TopDownTest/TopDownTestConfig.cs`, with the resource `DefaultTopDownTestConfig.tres`:

| Property | Default | Meaning |
| --- | --- | --- |
| `CameraZoom` | `2.0` | Existing default, also maximum zoom-in |
| `CameraMaxViewMultiplier` | `1.25` | Outermost view factor, i.e., 25% more width/height |
| `CameraZoomWheelStep` | `0.05` | View-multiplier increment per wheel tick |

**No new input actions or player motion hooks.** The zoom handler runs only for mouse wheel events, with no per-frame camera zoom computation, new post-processing, or additional scene nodes.

## Test checklist

1. Rebuild the C# project after pulling, then launch `TopDownVisualTest.tscn`.
2. Confirm the game still starts at **2.0** zoom with the same camera positioning.
3. Scroll down five wheel notches; zoom should clamp to **1.6**, showing more surroundings.
4. Continue scrolling down: zoom must **not** go below 1.6.
5. Scroll up to 2.0; continue scrolling up: zoom must **not** exceed 2.0.
6. Walk to map edges and near the lake; existing camera bounds and world-space effects should still work.
7. If a UI slider uses the wheel, it should receive it before the camera does.

For removal, delete this camera script reference and `Config` assignment from the `Camera` node in `TopDownVisualTest.tscn`, restore player movement's previous one-time camera zoom initialization (or set the fixed zoom in the scene), and remove this directory. No production camera was modified. Shader/rain/lightning and other visual systems were not changed.

**Verification:** source-level consistency checked in connected GitHub. Compiling in Godot and visual play-testing on the user's runtime are still required.
