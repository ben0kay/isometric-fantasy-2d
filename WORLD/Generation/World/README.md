# Finite macro generation — P0/P1

Implemented: a complete finite coarse biome map, independently queryable without scenery. `WorldMasterConfig.Generation` references `DefaultWorldGeneration.tres`. The playable 3840×1728 preview still uses its original adjacent layout; F7 shows the new blueprint. Streaming, geography directives and durable saves are later passes.

## Authoring

Edit `Biomes` in the generation Resource to reference any number of canonical `WorldBiomeDefinition` Resources. Two enabled biomes fill the map with two types; adding two more makes four eligible types without new generator branches. Entries need unique stable IDs. Null references, invalid settings and all-zero weights fail explicitly. Disabled entries do not participate; zero-weight entries never get sites. Selection is probabilistic: every biome is eligible, but a tiny map or a rare weight does not guarantee an occurrence.

Each biome has inline `EnabledForGeneration`, `GenerationWeight`, `MinimumRegionDiameter`, `MaximumRegionDiameter` and `OverviewColour` settings. Weight is relative **region selection**, not exact land percentage. Diameter is a **target in world units**, not a strict geometric bound: neighbouring competition and finite edges alter actual areas. Adjacent regions can have the same biome. No extra per-species generation Resources are needed. Existing population `Count` remains the preview count; density conversion belongs to P3, not this map-only pass. Climate affinity is deferred until its behaviour can be evaluated with more biome types.

Default bounds are 45000×33750 world units, with 256-unit coarse cells (176×132). At the current 150-unit walking speed, crossing the unobstructed long axis takes five minutes. This is a provisional medium **world** test size, independent of biome diameter. Three/eight-minute long axes would be 27000/72000 units; no final game scale is locked. Typical 4000–8000 target biome diameters correspond to roughly 27–53 seconds of unobstructed walking. Chunk size and camera load radius are not these values.

## Algorithm and limits

1. Validate before allocating. Sort by stable biome ID so rearranging Inspector entries does not reshuffle the map.
2. Estimate a site count from weighted mean target region area. Choose each site's biome and diameter from deterministic integer RNG. Best-of-eight candidate sampling spreads the sites.
3. Grow regions from all sites through a priority queue with positive coordinate-keyed edge costs and size-dependent expansion speed. Every claimed cell connects to an already claimed cell from its region, so all regions remain four-connected and all cells get exactly one owner.
4. Freeze value-only biome/region records and the ownership grid. `RegionAt`/`BiomeAt` return null outside finite bounds. Right/bottom edge cells may be partial. SHA-256 covers seed/version/bounds/grid/region metadata; independent world IDs are excluded.

This first version is a coarse connected partition, not a coast/terrain/climate simulator or final detailed border shader. No landmask exists yet: **all bounds are eligible land**. The debug image darkens region boundaries, including those between two regions of the same biome. Percentages/min-max counts describe coarse cells, not exact clipped land area. Future terrain rendering must interpolate/blend this blueprint consistently across chunk edges.

Generation is synchronous, once per preview startup. MaximumCells and MaximumRegions reject excessive requests; there is no unbounded random retry loop. Growth is O(cells log cells), site sampling O(regions²). Default developer target: under 250 ms macro generation, under 262144 cells and 4096 sites. This is a profiling target, not a guarantee for every combination up to the limits. Do not raise limits blindly; cancellation/asynchronous staging is later streaming work.

## Ownership and validation

`MacroWorldBuilder` produces geography. `MacroWorldMap` owns frozen queries/hash. `WORLD/Runtime/Persistence/WorldBlueprintManifest` copies a minimal value-only SAVE handoff. DEBUG/UI/Generation only observes it; removing that scene node leaves generation operational. See `P0_P1_Audit.md` for integration, measured baseline and remaining migration points, and `TESTS/Generation/README.md` for repeatable checks.
