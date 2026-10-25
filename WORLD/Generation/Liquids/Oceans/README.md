# Major ocean geography — P5c ocean portion

`OceanPlanner` creates seeded, organic ocean polygons **before** lakes/rivers and biome assignment. It uses P2 reservations and fixed-pond exclusion, with no biome-ID dependency. The first provider is a large bounded regional sea/ocean within the finite world, not infinite surrounding sea, continents/islands, elevation, tides, outlets or volcanic terrain. Volcanic geography remains a separate P5c follow-up.

Settings live in the existing `WaterGenerationMasterConfig` under **Major oceans**; no extra master/profile Resource:

| Setting | Default / meaning |
| --- | --- |
| OceanCount | Generic config 0; production preset 1. Accepted 0–2, combined water bodies ≤32. |
| OceansRequired | true; failure after bounded attempts aborts with context. false allows an explicit warning/skip. |
| MinimumOceanDiameter / MaximumOceanDiameter | 32000–48000 world units, accepted 1000–200000. Organic aspect/outline means bounds differ from the nominal base diameter. |
| AttemptsPerOcean | 64, accepted 1–128. Candidates sample only where conservative footprint clearance fits finite bounds, then respect required reservations/other oceans. |

The shoreline has 128–1024 world-scale samples. Ocean geometry is a typed canonical PlannedWaterBody (`IsOcean`), shared with liquid fields/wading/rain/nav. It is not a lake guessed by size or ID string. New worlds containing ocean geometry use WaterPlan v4 / geography v2 / macro v3 / blueprint schema 9. Old water hash layouts are unchanged. A required ocean can still fail for impossible authoring or conflicting reservations; it never silently turns into a biome.

Ocean cells whose **clipped cell center** lies underwater get region ID -1. They do not receive land biomes or region seeds. Partial coast cells can retain a land biome; exact water geometry still blocks scenery there. Ground palette maps provide neutral underwater floor values rather than indexing a fabricated ocean biome. BiomeAt/RegionAt return null for non-land. Freshwater Hydrology flags govern lakes/rivers; they do not make ocean fillable by a forest. Protected start and fixed authored pond remain preserved.

The existing shared liquid runtime crops fields to nearby chunks, including interior ocean tiles with no coast nearby. No giant full-ocean texture/mesh, ocean-specific process loop or full-world physics/nav grid is introduced. Distance fields use the existing saturated distance/local-edge/row-crossing raster path. Coast geometry is validated once; shared polygon validation now rejects disjoint edge bounds before intersection distance calculations.

Production **World.tscn and New Campaign** enable one ocean, three lakes and one river. The world is now approximately 10× the old area:

| | Before | Now |
| --- | --- | --- |
| World bounds size | 45000 × 33750 | 142304 × 106728 |
| Macro cell size/grid | 256; 176 × 132 | 512; 278 × 209 |
| Macro cells | 23232 | 58102 |
| Streaming chunk size/radii | 512; active 2 / preload 3 / unload 4 | Unchanged |

This is roughly 3.16× each axis, not 10× each axis (which would be 100× area). Biome target sizes remain unchanged, so the larger world has more biome regions. Macro planning/textures scale with the coarse grid; detailed scenery stays nearby. F7 fits the whole-map texture to a responsive viewport panel. Existing campaigns preserve their original dimensions, coarse resolution and frozen geography.

[Checks and target-PC handoff](P5C_Validation.md). Shore materials (WG4), currents (WG6), swimming/diving (WG7) and ocean ecology are not implemented by this pass; the ocean currently reuses the same wading behaviour as other water.
