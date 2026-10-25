# Oak Forest

Biome.tres references the ForestGrass ground surface and canonical Oak tree, boulder, grass and **eight individually selectable oak plant definitions**. The biome owns eligibility, count, streamed density, scale and mirroring — it does not duplicate plant art, behavior or scenes.

Plant population Resources:
- `WORLD/Content/Vegetation/Plants/Definitions/plant_oak_001.tres` through `plant_oak_008.tres`
- Each uses the single shared `Plants/Plant.tscn`, one current PNG per definition and optional additional inline art variants later.
- Each Oak population starts at streamed density **2.5 per million** and preview Count **12**, with scale **0.55–0.80**.
- Grass density is **45** (previously 60) to keep combined Oak streamed density **102** within the current 96-unit candidate capacity (~108.5). Trees 26, rocks 11, grass 45 and eight plants 20.
- Pink Meadow contains no oak plant entries. A future Pine biome can reference some of the **same** plant .tres definitions at its own densities.

**PL3 is still needed to render plants in production World.** Existing generator paths have not yet been extended to instantiate the PL1 `VegetationDefinition.Template`; legacy compatibility preview may warn until then. PL2 content change also alters P4 content fingerprints: preserve existing campaigns, do not silently migrate incompatible checkpoints.

Environment.tres contains optional component overrides. Atmosphere.tres starts with the forest haze/particle settings; other components inherit the world fallback profile. Refer to ENVIRONMENT/Profiles/README.md for the data contract.
