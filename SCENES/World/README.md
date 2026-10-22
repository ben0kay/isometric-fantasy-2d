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

## First production creature — Skeleton Basic

The `EntityPlacement` node in `World.tscn` spawns **one Skeleton Basic** relative to the player's spawn after seeded scenery generation. The closest unobstructed grid cell is used, so randomized trees/rocks should not overlap the skeleton. Disable `EntityPlacement.Enabled` to remove the test creature without deleting species assets.

Its reusable scene and configuration are in `ENTITIES/Species/Undead/Skeletons/Variants/Basic/`; copied static SVG art is in `ENTITIES/Species/Undead/Skeletons/Visuals/Sprites/`. Navigation is shared under `ENTITIES/Shared/Navigation/` and built only once. The skeleton detects and chases the player, routes around tree/rock collision footprints, and stops near the player. It cannot attack.

Skeleton Basic supports 3 HP, a damage flash and death fade via `TakeDamage(int)`. **This is not currently player-testable through left-click:** production player combat/axe is still a placeholder and the debug axe has not been promoted. No debug runtime dependencies were introduced. No loot, multiple enemies, pond avoidance or respawning in this pass.

**Testing:** Open this production scene and press F6. Check that the skeleton appears near the starting player and follows around generated trees and rocks. Run different world seeds. Godot/.NET compilation and in-engine collision behaviour still require verification on the user's machine.


Production FIRE1/FIRE2: build C# and F6 this scene; use the top-centre **Ignite nearest oak** / reset buttons. The optional `FireDebugPanel` can be removed while the `Fire` runtime and tree receivers remain. See [Fire documentation](../../SYSTEMS/WorldInteractions/Fire/README.md).
