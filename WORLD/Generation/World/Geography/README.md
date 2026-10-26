# Major geography — P5a/P5b

BC2 composes optional ClimateBiomeRules with these water rules for initial sites, every growth edge and gap filling. Null world climate retains this original geography-only path. Climate preferences are generation-only cached data; macro generator 4 / blueprint 11 identifies changed placement. See ../../Authoring/BC2_Validation.md.


`WorldGeographyPlan` is immutable planning data independent of biome IDs/Resources. It owns the world seed/bounds and references the canonical frozen `WaterBodyPlan`; no copied river renderer, per-biome manager or new config Resource. `WorldGenerationPipeline` runs required P2 reservations → major water → compatible macro biomes. Existing `WorldGenerationMasterConfig.Water` remains the geometry authoring entry point. P5c major oceans/non-land are now implemented; volcanic geography and major POI/access providers remain planned.

`WaterAt(Rect2)` supplies conservative lake/river footprint flags. `GeographyBiomeRules` builds one temporary byte per macro cell, including partial edge cells and narrow river intersections. Ocean flags classify clipped cell centers as non-land; partially intersecting coast cells may retain an underlying land biome. Exact water polygons still exclude scenery throughout the entire ocean footprint. Every touched cell must permit the relevant water kinds; this is conservative coarse eligibility, not a new terrain landmask. Lake/river cells retain underlying land biomes. Ocean-center cells have no biome/region, and land growth never enters them. The fixed authored pond stays a separate compatibility landmark and generator exclusion; its biome footprint is not inserted into this major generated-water mask.

## Biome rules

Reuse optional `WorldBiomeDefinition.Hydrology`:

- `AllowFreshwaterLakes` / `AllowFreshwaterRivers`: hard exclusions on biome assignment in cells touched by that major water kind. Missing profiles allow both.
- `LakeSuitability` / `RiverSuitability`: finite 0–1 preferences, applied as factors `0.1 + 0.9 * suitability` to site selection and inverse growth cost. Mixed cells multiply factors. Zero remains allowed if the hard flag permits it; it never deletes water. Null/default profiles have factor 1.
- Positive generation weights select biome sites; zero-weight/disabled biomes cannot rescue an impossible water cell. Fail with cell/type context if no eligible biome exists.

Sites grow only into compatible cells. If hard constraints leave unclaimed components, deterministic compatible fallback sites fill them, maintaining four-connected nonempty regions. These count toward the existing MaximumRegions budget; exceeding it fails explicitly. Regions remain target-size growth results, not guaranteed geometric diameters. If no geography preferences/exclusions apply, original region cells remain unchanged; versioned hashes still distinguish the new generator.

`RequireMultipleRiverBiomes` belongs only to the explicit legacy biome-first water API/tests. New major rivers do not depend on the future biome count or identities and are not moved to force multiple biome crossings. A river may cross one or several compatible biomes.

## Lifetime, limits and persistence

One new-world planning pass; no Nodes, subscriptions or frame callbacks. Required feature/water planners retain bounded retries/grids. Geography mask allocation is bounded by the validated macro grid (one byte/cell); four preference tables are O(enabled biomes). Biome queue and fallback sites retain macro cell/region limits. Mask/rules die when biome planning returns; frozen geography remains with WorldScene and existing water streaming handles local presentation/unload. Planning stays synchronous; profile startup rather than claiming FPS gains.

New production blueprints use schema 9 / geography contract version 2 / macro generator version 3. Ocean water is version 4 (type flags included in its hash); lake/river-only plans retain v1/v3. Region ID -1 is allowed only by the new macro version. Schemas 4–8 restore their original data/versions, including smaller dimensions and no oceans. Geography derives masks from frozen water and never reruns on Continue. The exact shipped preset change has a narrow source-hash content-compatibility gate; unrelated content edits remain strict. See [P5c checks](../../Liquids/Oceans/P5C_Validation.md).

See [validation and commands](P5AB_Validation.md). Open `TESTS/Generation/GeographyFirstPractice.tscn` with F6; R cycles river sections, F7 shows the map. Only the first cloned test biome permits water. Normal World/New Campaign now enables one required ocean, three lakes and one river in its authored config; practice scenes are optional.
