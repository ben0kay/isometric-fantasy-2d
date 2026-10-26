# BC4 — 50× area, bounded macro skeleton and local refinement

**Implemented, awaiting native/local validation.** This is a bounded global coarse skeleton plus stateless, coordinate-keyed local refinement design; it does not claim the entire macro grid is lazy or that target-PC startup/FPS has been measured. Detailed World chunks stay local. No BC5 spawn menu or full BC6 zoom/tiled atlas implementation is included.

## Shipped default and budgets

| Setting | Previous / retained Standard preset | New default |
| --- | --- | --- |
| Bounds | 142304×106728 | **1006240×754680**, origin −503120, −377340 |
| Area ratio vs previous | 1 | **49.999877×** (rounded dimensions) |
| Macro cells | 512 units / 58102 cells | **2048 units / 492×369 = 181548 cells** |
| Gameplay chunks/radii | 512 / active 2, preload 3, unload 4 | Unchanged |
| Biome target diameter | Authored 4000–8000 | Same canonical data × 7.071068 during generation only |
| Region cap | 4096 | **1024** for the shipped default |
| Cell/SAVE cap | 262144 | Unchanged |
| Major ocean diameter | 32000–48000 | 220000–300000, existing bounded irregular polygon |
| River routing | Full small-world 512 grid | Seeded **131072-unit local window**, 1024 grid / 16384 route cells |
| River span | 6000–11000 | 42000–78000; widths/art remain unchanged |

`StandardWorldGeneration.tres` preserves the previous-size preset and supports focused earlier-phase fixtures. Normal World and PreviewMap still reference **DefaultWorldGeneration.tres**, now the large preset. World size is not duplicated in the preview or streamer.

Capacity calculations performed here (not runtime measurements): raw coarse int cell IDs **0.693 MiB**; four-biome temporary climate weights **2.770 MiB**; seven shared RGBA32F palette textures **19.391 MiB**, plus upload/scratch/material overhead. Packed palette scratch buffers add ~5.54 MiB transiently. Preflight rejects >262144 cells, >4096 regions, >16M climate-cache entries, >32 MiB estimated seven-map palette, or unsupported coordinate magnitude. It does not silently increase limits.

At the shipped ±503120 coordinate magnitude, IEEE float32 adjacent spacing is **0.03125 world units** (calculated, not an FPS/physics test). Supported new presets are capped at absolute coordinate 524288; larger scales need a floating-origin/precision review. Camera integer limits, candidate IDs and chunk coordinates remain within existing ranges. In-bounds map indexing clamps partial/rounded edge cells safely; debug sampling uses an appropriate finite-edge inset.

## Engineering changes beyond bounds

1. **Creation-only spatial site bins** replace all-prior-site candidate comparisons for generator 5. At most 9 bins × 16 representatives per candidate; eight candidates/site. Region growth retains positive costs/four-connected coarse regions, now suppressing dominated queued proposals and bounding the live frontier to four times cell count. Actual frontier/enqueue counters are collected.
2. **Stateless local boundary refinement** shapes coarse land-biome edges consistently across chunk queries and GPU ground lookup. No high-resolution global map or per-chunk RNG consumption. Frozen seed/strength determine results in arbitrary query order. Ocean ownership is not warped; actual water polygons stay authoritative. The 2048-unit skeleton still limits geographic resolution; refinement adds border irregularity, not missing continents or elevation.
3. **Bounded local river windows** avoid a 2.9-million-cell whole-world route grid. One independently seeded window/slot uses existing path/footprint validation; impossible optional rivers report warnings and may skip. Lakes/river widths remain world-unit authored content, while requested major spans scale. It is not a new drainage/continent-wide river-network simulator.
4. **Immutable shoreline BVH + Y bins** accelerates nearest-edge, containment and conservative overlap queries for all frozen water bodies. Original vertices/hashes and double half-open ray semantics are retained. Index size follows polygon complexity, not world area; query paths allocate no objects and have no exploration-order cache.
5. **Packed RGBA32F palette uploads** replace ~1.27M native SetPixel calls for large-world palette creation, using reusable managed buffers. Legacy map versions retain their previous path. GPU maps remain coarse and shared, not per-world-unit or per-chunk full maps.
6. **Measured creation stage counters** expose features/water/climate/biomes/start milliseconds, frontier counts and explicitly labelled memory estimates in PreviewMap/F7. Restored Worlds report no creation timings rather than replan to fabricate counters. No continuous whole-world debug polling.

The pipeline remains synchronous at creation. PreviewMap rasterization remains on-demand in small row batches. The new budgets bound memory/work counts, not wall-clock duration; native timing, frame pacing and far-coordinate rendering acceptance remain pending.

## SAVE and old worlds

New large worlds use **blueprint schema 12 / macro generator 5**, with frozen refinement strength. Campaign schema remains 4 (SE4 calendar/snow unchanged). Complete coarse cells and region metadata remain in the blueprint; existing maximum file size stays 32 MiB. Continue restores the exact saved bounds/cell scale, grid, seed, climate, ocean/river polygons and refinement; it never reads the new default size to resize an existing world.

Generator 1–4 / blueprint 4–11 formats remain recognized; zero refinement preserves their query/hash contract. Strict authored-content fingerprints still apply, with no blanket migration bridge. Existing files are preserved on rejection. Use F6 or a new campaign for the changed generation preset. A focused fixture serializes/restores the new blueprint and checks actual JSON size against the unchanged cap.

## Validation and local handoff

Performed here: static source/diff/Resource/include contract review; numerical area/cell/memory/route/float-precision calculations; whitespace checks. **Godot and .NET unavailable: no native build, shader compile, runtime, disk Continue, FPS or empirical startup budget acceptance is claimed.**

Short local pass:

1. `dotnet build`, Godot .NET import. Optional focused check: `godot --headless --path . res://TESTS/Generation/ValidateLargeWorld.tscn`. Expected `BC4 LARGE WORLD PASS`, exit 0. Reports measured native planning time and blueprint size; checks far queries/order, frozen refinement restore, budgets and shoreline index vs independent exhaustive reference.
2. F6 `DEBUG/PreviewMap/PreviewMap.tscn`. Confirm 1006240×754680, 492×369, whole-map views and zero gameplay chunks. Record water/biome planning times, warnings and peak frontier. Same seed must reproduce identities. Optional river skips should be visible, never silent missing required oceans.
3. F6 normal World: local scenery/camera/environment work at the selected safe middle anchor. F7 uses the same coarse plan. Walk over 512-unit chunk borders and land-biome transitions; check shaped ground/content agreement, shoreline continuity and no broad seams. Float CPU/GPU edge rounding may need tuning after native inspection.
4. Locally teleport through a disposable debug session near each far corner/shore. Check motion, collision, projected shadows, camera limits, particles/snow field and nav. Only the nearby streamer window should load; no distant path or full-world scenery nodes should appear. Chunk cancellation/unload must retain the existing bounded lifecycle.
5. New Campaign → save → Continue: exact far location, map/climate/water hashes, refinement and local mutations retained. A compatible older-size frozen blueprint should keep its original bounds. Do not bypass fingerprint checks to force unrelated old content into a new generation preset.

If native startup or visual precision fails, report measured stage/counter output and screenshots before declaring BC4 accepted. BC5/BC6 are separately scoped follow-ups, not automatically authorized by this delivery.
