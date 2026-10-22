# Disposable adjacent biomes

The existing forest, trail, clearing and lake are **Oak Forest**. Walk east past approximately x=1760 into **Pink Meadow**, extending to x=3040. The world is now 3840×1728, from (-800,-544) to (3040,1184). There are 80 oak trees and 50 shared rocks overall. Pink Meadow intentionally has only pink ground and rocks: no trees or decorative grass sprites. No new artwork or production systems were added.

## Files and tuning

- `BiomeDefinition.cs`: identity, four ground colours and separate tree/tuft/rock permissions.
- `OakForest.tres` and `PinkMeadow.tres`: independent editable definitions. The forest retains the original green palette.
- `BiomeMasterConfig.cs` / `DefaultBiomeConfig.tres`: west/east references, boundary x, curve amplitude/scale and blend width. This is deliberately a two-region prototype, not a production biome generator.
- `BiomeBlend.gdshaderinc`: matching continuous boundary formula for the existing grass surface material. Broad patches and fine grain still belong to Ground; weather, snow, gravel and sunlight remain separate.

The shared config is assigned to Ground and Actors. Placement reads it once during startup; restart after changing permissions or geometry. Ground palette/transition uniforms update live. Biome palette colours now override the legacy GroundMasterConfig palette when biome resources are assigned; existing variation/detail switches still work across both surfaces, including the pink tint when Natural grass is off.

Trees and tufts reserve the whole transition plus an art margin, so forest foliage does not hang over fully pink ground. Rocks are allowed on both sides and keep gravel/collision/shadow behaviour. Liquid exclusion still applies. Scenery positions change with the enlarged sampling region; the seed remains deterministic. Grass sampling remains bounded and may place fewer clumps when candidates land in the empty biome.

## Containment and removal

Everything lives in DEBUG/TopDownTest. No production WORLD folders were changed. To remove the comparison, clear the Biomes references on Ground and Actors, remove the biome shader include/palette blend, and restore the previous world bounds if desired. No per-frame biome searches, nodes per ground tile, new physics bodies or generated textures are introduced.

## Validation

All debug C# compiled against GodotSharp 4.7.2 / .NET 8 and the native Godot 4.5.1 source generators. Full-scene software OpenGL Compatibility rendering was inspected at the transition and inside Pink Meadow. Three seeds (213876, 12345, 901) each placed all 80 trees and 50 rocks; pink-side rock counts were 25, 29 and 30, with no trees or tufts beyond the reserved border. Render checks reduce decorative grass counts locally only; committed grass settings are unchanged. Windows D3D12 and hardware performance are not validated.
