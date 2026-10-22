# ENVIRONMENT / Wind / Visuals

**Status: future reusable presentation hooks; no production shaders or adapters yet.**

The Visuals folder will hold reusable **wind-response techniques**, not its own wind generator. Its effects should consume the current `WindState` from `../Simulation/`.

## Intended examples

- **Vegetation bend/sway:** shared, root-anchored animation shader(s) for tree crowns, grass blades, plants and reeds. Per-species art and resistance/strength settings remain in `WORLD/Content/Vegetation/`.
- **Directional drift hooks:** generic uniform/adapter conventions for leaves, pollen, smoke and embers; each particle system remains under `ENVIRONMENT/Atmosphere/Particles/`.
- **Weather presentation:** precipitation direction comes from the same wind data, but rain/snow drawing code remains under `ENVIRONMENT/Weather/`.
- **Other consumers:** cloud shade/fog drift and ripples on liquids, without moving cloud or lake implementation code into this folder.

### Debug references to evaluate on migration

- `DEBUG/TopDownTest/Trees/TreeSway.gdshader`: stationary roots with animated foliage.
- `DEBUG/TopDownTest/Trees/Tree01Wind.tres` and `DEBUG/TopDownTest/Grass/GrassWind.tres`: same shader, different per-object bend settings.
- `DEBUG/TopDownTest/Atmosphere/DebugAtmosphere.cs`: integrated drift and wind-linked leaf shedding.
- `DEBUG/TopDownTest/Weather/DebugWeather.cs`: single wind source, not a reusable production weather system yet.

Keep renderers GPU-friendly. Reuse shared materials where possible; avoid updating thousands of individual object shader uniforms each frame. New visuals can be placed in subfolders **when actual reusable implementations exist**, rather than adding speculative empty categories now.
