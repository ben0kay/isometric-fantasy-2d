# Finite macro generation — P0–P3

P3 now makes this map playable through nearby streaming; see [streaming guide](../../Runtime/Streaming/README.md). The remaining P0–P2 descriptions below document the original macro foundation; preview-only statements are superseded by this update. Biome regions remain separate from chunk size. Population density now controls streamed placement; Count/clusters remain for the explicit compatibility preview.

Implemented: a complete finite coarse biome map, independently queryable without scenery. `WorldMasterConfig.Generation` references `DefaultWorldGeneration.tres`. The default production world now streams this map; the original adjacent layout is retained in explicit preview mode. F7 shows the map. P2 now adds required/preferred polygon reservation directives via the referenced Layout Resource; see [features](Features/README.md). Local streamed ground/scenery is implemented in P3. P4 durable per-profile/run/world saves and Continue are implemented; real terrain/landmarks (P5) remain future. **F7** is the macro/reservation map; **F8** saves an active P4 campaign, not the map. See [SAVE](../../../SAVE/README.md) and [phase guide](../../Generation_Implementation.md).

## P5a/P5b current production creation

`WorldGenerationPipeline` runs required feature reservations → biome-independent water geography → compatible biome growth. `MacroWorldBuilder.Build(config, seed, geography)` uses generator version 3 (ocean/non-land support); saved version 2 remains supported; the two-argument method remains the exact version-1 baseline for legacy tests. Continue restores either version without running a planner. Hydrology hard flags restrict touched coarse cells; soft preferences influence eligible site selection/growth. Isolated components receive bounded connected fallback regions. See [geography ownership/settings/validation](Geography/README.md). The original unconstrained algorithm below remains the foundation. Major oceans are now implemented in P5c; volcanic regions/POIs remain planned.

Normal World/New Campaign now uses inline `Water` settings in `DefaultWorldGeneration.tres`: Enabled=true, OceanCount=1, LakeCount=3, RiverCount=1. Edit that Resource's Water group in the Inspector. Existing smooth-river span/width/bank defaults apply. This is production configuration; no practice scene is required. Existing saved geography is never rerolled.

## Authoring

Edit `Biomes` in the generation Resource to reference any number of canonical `WorldBiomeDefinition` Resources. Two enabled biomes fill the map with two types; adding two more makes four eligible types without new generator branches. Entries need unique stable IDs. Null references, invalid settings and all-zero weights fail explicitly. Disabled entries do not participate; zero-weight entries never get sites. Selection is probabilistic: every biome is eligible, but a tiny map or a rare weight does not guarantee an occurrence.

Each biome has inline `EnabledForGeneration`, `GenerationWeight`, `MinimumRegionDiameter`, `MaximumRegionDiameter` and `OverviewColour` settings. Weight is relative **region selection**, not exact land percentage. Diameter is a **target in world units**, not a strict geometric bound: neighbouring competition and finite edges alter actual areas. Adjacent regions can have the same biome. No extra per-species generation Resources are needed. Existing population `Count` remains the preview count; density conversion belongs to P3, not this map-only pass. Climate affinity is deferred until its behaviour can be evaluated with more biome types.

Production preset bounds are 142304×106728 world units, approximately 10× the original 45000×33750 area (3.16× per axis). Macro cells are 512 units, giving 278×209 = 58102 cells, rather than increasing macro texture/storage cost tenfold. Biome target diameters/weights remain unchanged, so there are more regions across the larger land area. Streaming chunks remain 512 units with the same active/preload/unload radii. Unobstructed long-axis walking at 150 units/second is about 15.8 minutes; no final game scale is locked. F7 shows the entire map fitted to the viewport. Existing campaigns retain their original bounds/cell size.

## Algorithm and limits

1. Validate before allocating. Sort by stable biome ID so rearranging Inspector entries does not reshuffle the map.
2. Estimate a site count from weighted mean target region area. Choose each site's biome and diameter from deterministic integer RNG. Best-of-eight candidate sampling spreads the sites.
3. Grow regions from all sites through a priority queue with positive coordinate-keyed edge costs and size-dependent expansion speed. Every claimed cell connects to an already claimed cell from its region, so all regions remain four-connected and all cells get exactly one owner.
4. Freeze value-only biome/region records and the ownership grid. `RegionAt`/`BiomeAt` return null outside finite bounds. Right/bottom edge cells may be partial. SHA-256 covers seed/version/bounds/grid/region metadata; independent world IDs are excluded.

This first version is a coarse connected partition, not a coast/terrain/climate simulator or final detailed border shader. P5c stores non-land ocean cells as region ID -1; BiomeAt/RegionAt return null there. Region counts cover remaining land only. The water polygon remains authoritative for exact shoreline/scenery/wading queries; coast-cell classification uses clipped cell centers. The debug image darkens region boundaries, including those between two regions of the same biome. Percentages/min-max counts describe coarse cells, not exact clipped land area. Future terrain rendering must interpolate/blend this blueprint consistently across chunk edges.

Macro generation is synchronous when creating a new world; P4 Continue restores its frozen blueprint instead of generating a new map. MaximumCells and MaximumRegions reject excessive requests; there is no unbounded random retry loop. Growth is O(cells log cells), site sampling O(regions²). Default developer target: under 250 ms macro generation, under 262144 cells and 4096 sites. This is a profiling target, not a guarantee for every combination up to the limits. Do not raise limits blindly; cancellation/asynchronous staging is later streaming work.

## Ownership and validation

`MacroWorldBuilder` produces geography. `MacroWorldMap` owns frozen queries/hash. `WORLD/Runtime/Persistence/WorldBlueprintManifest` copies a minimal value-only SAVE handoff. DEBUG/UI/Generation only observes it; removing that scene node leaves generation operational. See `P0_P1_Audit.md` for integration, measured baseline and remaining migration points, and `TESTS/Generation/README.md` for repeatable checks.
