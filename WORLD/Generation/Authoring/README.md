# World Author — BC0 / BC1

> **PreviewMap / BC2 — 26 October 2026:** BC2 delivered, awaiting local validation: optional biome affinity controls placement; normal worlds use schema 11 / generator 4. `DEBUG/PreviewMap/PreviewMap.tscn` offers metadata-only seed/regenerate and three map layers. BC0/BC1 details below describe the prior implementation; current behavior is in this handoff. [Current handoff](BC2_Validation.md).


BC0 audit/contracts are complete; BC1 implementation awaits native/local validation. Normal World F6 and New Campaign reference `DefaultWorldAuthor.tres` through `DefaultWorldGeneration.tres → Authoring`. Null Authoring preserves the previous planning path. Current bounds and P3 chunk sizes/radii are unchanged.

## Inspector authoring

- **OceanCentreZone:** normalized allowed centre preference, not the full coastline's enclosing rectangle. Default middle 50% of each axis allows seeded offsets. First half of the existing bounded ocean attempts prefer this zone; remaining attempts use legal world bounds and emit a warning when falling back. `RequireOceanInZone` disables that location fallback. Ocean count, required presence and diameter remain exclusively in WaterGenerationMasterConfig.
- **Climate:** northern/southern Celsius and humidity baselines plus smooth boundary warp. A continuous seeded value field produces connected broad latitude provinces with organic transitions. Optional enchanted anchor/extent/transition marks a soft elliptical thematic province; it does not fabricate biome content or change weather. Cold <2°C, hot ≥24°C; these are version-1 overview classifications, not snow eligibility rules. Baseline values interpolate continuously between them.
- **StartZone:** default middle land search (BC5 menu not implemented). The existing start footprint is deferred until water and the macro map exist. Candidate checks cover world/request-zone bounds, authored pond, every water footprint/bank, other reservations and configured POI footprint clearance. Start footprint uses its maximum authored scale conservatively. No constraints silently relax. Start-zone guidance overrides the old fixed anchor only for authored worlds; it cannot create duplicate start reservations.
- **MajorPoiClearance / ShoreClearance / StartAttempts:** world-unit clearances and bounded search count. Actual POI footprints must be supplied by future canonical providers. Clearance is from footprint edges and includes their own reserved clearance. BC1 does not claim pathfinding reachability across future cliffs/POIs; nearby P3 navigation remains authoritative. BC5 must extend clearance categories/overrides and connectivity checks when real POI providers exist.

`WorldAuthorPreset` is the module configuration Resource; there is no second world manager or per-biome controller. Canonical surfaces/content remain under WORLD/Terrain and WORLD/Content.

## Frozen data and lifecycle

`WorldClimatePlan` has no Nodes, mutable noise Resources, grid, cache or processing callbacks. It copies scalar parameters, validates/hashes them together with seed/version/bounds, and samples by position without allocation. `DeriveSeed` supplies explicit version/domain keys; ocean, climate and middle-start streams are independent. Existing feature/water/biome algorithms retain their own established version contracts. Seeds do not depend on query/exploration order.

New authored worlds use blueprint **schema 10**, macro generator **3**, geography **2**, and climate field **1**. Climate and initial Middle clearance policy are stored alongside exact frozen start reservation, ocean polygons and biome cells. CampaignStore validates climate hashes on read/write; Continue restores saved field parameters, never the current Resource. Legacy schemas 4–9 remain recognized and receive no invented climate. Content fingerprints remain strict: this generation/config change has **no arbitrary old-campaign migration bridge**. Use disposable F6 or a fresh campaign; old files remain intact even if content compatibility refuses Continue.

F7 controls are created after startup but rasters are **not**. First opening/toggling generates at most eight rows per callback with a soft 2ms check between rows; large row/water sampling and final texture uploads are not hard real-time guarantees. Two completed layers are cached, hidden partial work pauses, settled/hidden maps disable processing, and unload releases images/textures. No scenery chunks are created for map previews. Zoom/pan/tiled caches/profiler remain BC6.

See [BC0 audit and validation handoff](BC0_BC1_Validation.md) and [remaining phase plan](WORLD_AUTHOR_IMPLEMENTATION_PLAN.md).
