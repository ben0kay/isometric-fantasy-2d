# Major geography — P5a/P5b

`WorldGeographyPlan` is immutable planning data independent of biome IDs/Resources. It owns the world seed/bounds and references the canonical frozen `WaterBodyPlan`; no copied river renderer, per-biome manager or new config Resource. `WorldGenerationPipeline` runs required P2 reservations → major water → compatible macro biomes. Existing `WorldGenerationMasterConfig.Water` remains the geometry authoring entry point. Oceans/volcanic geography and major POI/access providers remain P5c/P5d; do not implement them on sight.

`WaterAt(Rect2)` supplies conservative lake/river footprint flags. `GeographyBiomeRules` builds one temporary byte per macro cell, including partial edge cells and narrow river intersections. Every touched cell must permit the relevant water kinds; this is conservative coarse eligibility, not a new terrain landmask. All current cells still have an underlying land biome, including beneath water. The fixed authored pond stays a separate compatibility landmark and generator exclusion; its biome footprint is not inserted into this major generated-water mask.

## Biome rules

Reuse optional `WorldBiomeDefinition.Hydrology`:

- `AllowFreshwaterLakes` / `AllowFreshwaterRivers`: hard exclusions on biome assignment in cells touched by that major water kind. Missing profiles allow both.
- `LakeSuitability` / `RiverSuitability`: finite 0–1 preferences, applied as factors `0.1 + 0.9 * suitability` to site selection and inverse growth cost. Mixed cells multiply factors. Zero remains allowed if the hard flag permits it; it never deletes water. Null/default profiles have factor 1.
- Positive generation weights select biome sites; zero-weight/disabled biomes cannot rescue an impossible water cell. Fail with cell/type context if no eligible biome exists.

Sites grow only into compatible cells. If hard constraints leave unclaimed components, deterministic compatible fallback sites fill them, maintaining four-connected nonempty regions. These count toward the existing MaximumRegions budget; exceeding it fails explicitly. Regions remain target-size growth results, not guaranteed geometric diameters. If no geography preferences/exclusions apply, original region cells remain unchanged; versioned hashes still distinguish the new generator.

`RequireMultipleRiverBiomes` belongs only to the explicit legacy biome-first water API/tests. New major rivers do not depend on the future biome count or identities and are not moved to force multiple biome crossings. A river may cross one or several compatible biomes.

## Lifetime, limits and persistence

One new-world planning pass; no Nodes, subscriptions or frame callbacks. Required feature/water planners retain bounded retries/grids. Geography mask allocation is bounded by the validated macro grid (one byte/cell); four preference tables are O(enabled biomes). Biome queue and fallback sites retain macro cell/region limits. Mask/rules die when biome planning returns; frozen geography remains with WorldScene and existing water streaming handles local presentation/unload. Planning stays synchronous; profile startup rather than claiming FPS gains.

New production blueprints use schema 8, geography contract version 1 and macro generator version 2. Water keeps its existing version-1 lake/version-3 smooth-river records; its exact hash and the versioned macro map hash cover saved outcomes. Geography has no duplicate mask snapshot: Continue reconstructs its query wrapper from frozen water, restores the frozen biome map and never evaluates current hydrology. Schema 4–7 / macro version 1 retain exact hashes/outcomes; no automatic migration or reroll. Older executables reject schema 8. Existing scene/resource fingerprints are unchanged.

See [validation and commands](P5AB_Validation.md). Open `TESTS/Generation/GeographyFirstPractice.tscn` with F6; R cycles river sections, F7 shows the map. Only the first cloned test biome permits water. Normal World/New Campaign now enables three lakes and one river in its authored config; practice scenes are optional.
