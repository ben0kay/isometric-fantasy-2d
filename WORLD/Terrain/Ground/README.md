# WORLD / Terrain / Ground

> **Status:** Production folder foundation only; no ground-surface renderer or runtime terrain has been promoted from DEBUG.

## Responsibilities

**Ground owns what terrain exists:** ground types (grass, dirt, mud, sand, stone, snow-covered ground), their texture/visual definitions and scene/tile resources. A ground type may reference a reusable **surface profile** describing its appearance.

**[Surfaces/](Surfaces/README.md) owns how ground looks:** shared, world-space organic colour variation, sampled noise, fine detail and material/shader composition. Avoid creating a new controller or duplicate shader for every biome merely to change colours.

**[../Transitions/](../Transitions/README.md) owns boundaries:** blending between neighbouring terrain/biome regions, including grass-to-dirt or grass-to-sand transitions. Keep transition masks separate from a surface's internal mottling.

### Other folder boundaries

- **[../../Biomes/](../../Biomes/README.md)** selects ground definitions/profiles, palettes and content permissions. Multiple biomes may reference the same ground type or shared surface mechanics, with different parameter values.
- **[../../Content/Vegetation/Grass/](../../Content/Vegetation/Grass/README.md)** owns individual decorative grass tuft artwork and reusable vegetation objects, **not** grass terrain pixels.
- **[../../Generation/Terrain/](../../Generation/Terrain/README.md)** owns later algorithms assembling world terrain. **[../../Generation/Placement/](../../Generation/Placement/README.md)** controls scenery/decorative-object placement; the surface shader never scatters objects.
- **[../../../ENVIRONMENT/](../../../ENVIRONMENT/README.md)** owns time, lighting, weather, wind and seasonal simulation, rather than Ground.

## Example: Oak Forest

1. The Oak Forest biome references a grassy ground definition and supplies its green/brown palette and other preferences.
2. Its surface profile uses shared world-space noise, patch colours and fine detail to create an organic grass appearance.
3. Ground artwork/material is drawn in the world scene. A transition system independently handles neighbouring biome edges.
4. Separately placed grass tufts/trees/rocks sit above the ground and have their own assets, placement and optional wind/weather response.

**Future surface differences:** Sand, mud and cobblestone can reuse the common noise/blending helpers while choosing appropriate textures or shader compositions. A grass-specific look must not be forced onto every material.

## Current DEBUG reference, not production

- [Debug Ground](../../../DEBUG/TopDownTest/Ground/README.md) — `DebugGroundSurface.cs`, `GrassMaterial.tres`, `Shaders/GrassSurface.gdshader`, patch colours, fine detail and noise sampling.
- [Debug Biomes](../../../DEBUG/TopDownTest/Biomes/README.md) — `OakForest.tres`, `PinkMeadow.tres` and biome palette/configuration.
- [Debug Grass](../../../DEBUG/TopDownTest/Grass/README.md) — separately scattered decorative grass tufts.

Paths above are from `WORLD/Terrain/Ground/`: `../../../DEBUG/` points to the repository-level DEBUG folder.

## Promotion guidelines

- First reproduce the current Oak Forest ground look **without visual changes** in a small production-world scene.
- Copy/adapt only the required shaders, noise assets, Resources and material wiring from DEBUG, replacing debug-specific names and paths. Keep DEBUG working until a production replacement is tested.
- Move shared visual logic into `Surfaces/`; keep palette/config data in a biome or reusable surface-profile Resource, rather than hardcoding Oak Forest inside shaders.
- Preserve a single source of truth for each setting and clear separation between ground rendering, biome selection, transitions and decoration placement.
- Read the root `README.md` and `CodePreferences.md` before implementation.

**No runtime code, scenes, shaders or .tres profiles have been added by this documentation pass.**
