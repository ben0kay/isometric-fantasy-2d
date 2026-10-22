# Production Oak Forest / Pink Meadow world
Open `World.tscn`, build C# in Godot .NET, then press **F6**. F5 still starts DEBUG.

Arrow keys move; mouse wheel zooms out up to 25% and back to the default view.
The map is 3840×1728, with 80 requested oaks, 36 pink blossom trees, 50 shared rocks and 210 Oak-side grass tufts; bounded placement may fit fewer objects. All actors use feet-based Y sorting and rooted collisions.

`WORLD/DefaultWorldConfig.tres` controls bounds, spawn, seed and placement limits. **RandomSeedOnLoad defaults to true.** Copy the printed `World seed` into FixedSeed and disable RandomSeedOnLoad to replay an arrangement. Same seed, same ordered definitions/settings and same Godot RNG version reproduce the same placement. Editing content/order/settings can change the result. No persistence or chunk streaming exists.

The scene composes production world and environment modules. The optional EnvironmentDebugPanel lives under DEBUG/UI/Environment and can be removed independently. F9 hides/shows its collapsible controls. Original DEBUG experiments remain intact.

## Validation
C# built successfully against the repository's Godot.NET.Sdk 4.7.2 / .NET 8 configuration. Runtime checks in Godot 4.5.1 .NET verified same-seed replay, different seeds, fresh random loads and replay from a logged random seed; movement, four boundary walls, collision and both camera zoom limits passed. The actual 1920×1080 scene was rendered and inspected with software OpenGL Compatibility. The unchanged DEBUG main scene also loaded. Windows D3D12 and target-machine performance still require local checking.

## Environment profiles
See ENVIRONMENT/README.md for ownership and validation. WORLD/Biomes/Definitions/OakForest/Environment.tres supplies optional local components (currently Atmosphere.tres); unspecified components inherit ENVIRONMENT/Profiles/DefaultEnvironmentProfile.tres. One clock is shared. Rain defaults to 55%, snow starts off. Runtime previews reset on restart.

## Second biome — 22 October 2026
Walk east from the normal spawn to find Pink Meadow beyond the curved boundary around x=1760. Bounds are (-800,-544) -> (3040,1184). Height and spawn are unchanged; camera, ground, walls and environment all derive the enlarged dimensions from the same world config.

WORLD/DefaultWorldConfig.tres selects WORLD/Biomes/Layouts/OakAndMeadow.tres. This is the small two-region composition only; no massive world sizing, streaming or encounter generation is implemented. Shared weather/clock remain the same on both sides, matching DEBUG.

Validation: C# build, three seeded population/membership checks, exact fixed-seed replay, traversal across the border, enlarged ground/camera/shadows, and shared pink snow shader passed. Native 1920×1080 software OpenGL renders were inspected at the transition, inside Pink Meadow and at full snow. Windows D3D12 remains a local check. Original DEBUG and project default scene are unchanged.
