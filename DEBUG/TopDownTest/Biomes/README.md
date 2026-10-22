# Disposable adjacent biomes

The existing forest, trail, clearing and lake are **Oak Forest**. Walk east past approximately x=1760 into **Pink Meadow**, extending to x=3040. The world is now 3840×1728, from (-800,-544) to (3040,1184). There are 80 oak trees and 50 shared rocks overall. Pink Meadow now has **pink blossom trees** and rocks against pink ground, while oak trees remain in the Oak Forest. The two species share the same wind sway shader and removable SnowTest adapter. No decorative grass is placed in Pink Meadow, and no production systems were changed.

## Files and tuning

- `BiomeDefinition.cs`: identity, four ground colours, separate tree/tuft/rock permissions, and an optional tree PackedScene plus desired test population.
- `OakForest.tres` and `PinkMeadow.tres`: independent editable definitions. The forest retains the original green palette.
- `BiomeMasterConfig.cs` / `DefaultBiomeConfig.tres`: west/east references, boundary x, curve amplitude/scale and blend width. This is deliberately a two-region prototype, not a production biome generator.
- `BiomeBlend.gdshaderinc`: matching continuous boundary formula for the existing grass surface material. Broad patches and fine grain still belong to Ground; weather, snow, gravel and sunlight remain separate.

The shared config is assigned to Ground and Actors. Each biome now selects its own tree scene/count: `OakForest.tres` keeps 80 oak trees, and `PinkMeadow.tres` targets 36 pink blossom trees. Placement reads definitions once at startup; restart after changing counts, templates, permissions or geometry. Ground palette/transition uniforms update live. Biome palette colours now override the legacy GroundMasterConfig palette when biome resources are assigned; existing variation/detail switches still work across both surfaces, including the pink tint when Natural grass is off.

Trees from either side reserve the curved transition plus a canopy margin; neither pink blossoms nor oaks should hang across the border. Rocks are allowed on both sides and keep gravel/collision/shadow behaviour. Liquid exclusion still applies. Scenery positions change with the enlarged sampling region; the seed remains deterministic. Grass sampling remains bounded and may place fewer clumps when candidates land in the empty biome.

## Containment and removal

Everything lives in DEBUG/TopDownTest. No production WORLD folders were changed. To remove the comparison, clear the Biomes references on Ground and Actors, remove the biome shader include/palette blend, and restore the previous world bounds if desired. No per-frame biome searches, nodes per ground tile, new physics bodies or generated textures are introduced.

## Validation

All debug C# compiled against GodotSharp 4.7.2 / .NET 8 and the native Godot 4.5.1 source generators. Full-scene software OpenGL Compatibility rendering was inspected at the transition and inside Pink Meadow. Three seeds (213876, 12345, 901) each placed all 80 trees and 50 rocks; pink-side rock counts were 25, 29 and 30, with no trees or tufts beyond the reserved border. Render checks reduce decorative grass counts locally only; committed grass settings are unchanged. Windows D3D12 and hardware performance are not validated.


## Wider transition and shallow snow depth

Biome blending now spans 240 world pixels (previously 120), editable with DefaultBiomeConfig / BlendWidth. SnowDepth.gdshaderinc adds smooth shallow drifts and restrained bank/hollow shading without altering distributed coverage or the exact 0/100% coverage endpoints. SnowMasterConfig exposes DriftScale, DriftContrast, PlayerSinks (on by default) and MaximumSinkPixels (4). Snow itself remains opt-in as before. Player burial begins above 25% settled snow (a small dip at the default 60%), varies with the same world-space depth field, and reaches at most 4 pixels; body/collider/camera and movement speed remain unchanged.

PlayerSurfaceVisual is the single sprite/material owner shared by optional snow and lake tests. Liquid immersion takes precedence; snow produces no water rings and keeps the projected sun shadow. Toggles/melting ease the sprite back up. Removing both experiments restores its original position/material. The helper is installed by either test, so snow burial also works with the Liquids scene removed.

## Shader Wind and Snow — shared vegetation feature

PinkBlossom01.tscn reuses the **exact same TreeSway.gdshader** used by the oak, with its own smaller-sprite `PinkBlossomWind.tres` material. DayNight already caches generated Tree-prefixed actors, projecting matching shadows and passing the same shared signed wind to their materials. SnowTest uses the existing shared TreeSnowAdapter, enhanced with optional per-sprite `snow_foliage_profile` metadata (0 green/default, 1 pink) so its snow shader can recognise pink blossoms without incorrectly snowing the brown trunk. This is a reusable colour-profile mechanism, not a bespoke pink-tree snow controller.

The new tree uses a rooted convex collision footprint, shader-animated upper canopy, size/mirror variation and default snow coverage like the oak. Image is a transparent, palette-optimised sprite derived from the approved pink-tree concept. The editor displays a **Shader Wind and Snow** label above SnowTest controls.
