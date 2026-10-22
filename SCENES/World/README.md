# Production Oak Forest world
Open `World.tscn`, build C# in Godot .NET, then press **F6**. F5 still starts DEBUG.

Arrow keys move; mouse wheel zooms out up to 25% and back to the default view.
The map is 2560×1728, with 80 requested oaks, 34 rocks and 210 grass tufts; bounded placement may fit fewer objects. All actors use feet-based Y sorting and rooted collisions.

`WORLD/DefaultWorldConfig.tres` controls bounds, spawn, seed and placement limits. **RandomSeedOnLoad defaults to true.** Copy the printed `World seed` into FixedSeed and disable RandomSeedOnLoad to replay an arrangement. Same seed, same ordered definitions/settings and same Godot RNG version reproduce the same placement. Editing content/order/settings can change the result. No persistence or chunk streaming exists.

The scene only composes production modules. Trees/grass are static for this pass; optional environment and gameplay experiments remain in DEBUG.

## Validation
C# built successfully against the repository's Godot.NET.Sdk 4.7.2 / .NET 8 configuration. Runtime checks in Godot 4.5.1 .NET verified same-seed replay, different seeds, fresh random loads and replay from a logged random seed; movement, four boundary walls, collision and both camera zoom limits passed. The actual 1920×1080 scene was rendered and inspected with software OpenGL Compatibility. The unchanged DEBUG main scene also loaded. Windows D3D12 and target-machine performance still require local checking.
