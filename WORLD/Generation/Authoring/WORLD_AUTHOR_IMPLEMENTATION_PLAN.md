# World Author — geographical presets, climate gradient, large finite world and F7 profiler

> **BC4 — 26 October 2026:** BC4 implemented, awaiting native/local validation: 50× area default, bounded coarse global skeleton with stateless local boundary refinement, bounded site/frontier work, local river windows and indexed shore queries. Existing detail streamer remains local. This does not make full macro creation lazy. BC5–BC7 remain planned. [Current handoff](BC4_Validation.md).


> **SE4 / BC3 — 26 October 2026:** BC3 regional-weather foundation and SE4 calendar implemented, awaiting local validation. Position-aware rain/snow and visited-cell snowpack reuse existing effects. Remote catch-up uses current conditions on access, not weather-history replay. BC4–BC7 remain planned. [Current handoff](../../../ENVIRONMENT/Climate/SE4_BC3_Validation.md).


> **PreviewMap / BC2 — 26 October 2026:** BC2 implemented, awaiting native/local validation; standalone `DEBUG/PreviewMap/PreviewMap.tscn` and shared F7 viewer delivered. Climate affinity now controls biome regions at the existing bounds. BC3–BC7 remain planned; preview does not claim BC4 scaling or full BC6 tooling. [Current handoff](BC2_Validation.md).


> **BC0/BC1 delivery — 26 October 2026:** BC0 audit/design complete. BC1 implemented, awaiting native/local validation: WorldAuthorPreset, continuous frozen climate, middle ocean preference, protected middle land search and on-demand F7 climate layer. BC2–BC7 remain planned. The detailed proposal below remains the roadmap; current owners, budgets and limitations are recorded in the audit. [Audit / local handoff](BC0_BC1_Validation.md).


## Author clarification — 26 October 2026

**Ocean placement:** “middle/central ocean” below means a broad preferred middle area, not a required exact centre, symmetry, or fixed central anchor. Allow seeded offsets and irregular extent through normalized Inspector guidance. Keep ocean presence/scale separate from preferred location; centrality must not become a hard constraint merely from earlier wording.

**North / Mid / South starts:** the player chooses a broad area preference. Resolve a deterministic accessible land anchor within that area after major geography and major POI footprints/access reservations are known, before protecting the start and placing local scenery. Add configurable minimum distance from major POI footprints (world units), optional per-POI/category overrides, water/shore setback and obstacle/access clearance. Measure clearance from the actual footprint, not just its centre. Exclude dangerous/required POIs and protected approaches as configured; preserve ordinary biome content around the spawn. Use bounded candidates and deterministic tie-breaking. If no candidate satisfies the requested area and clearances, report the failed constraint; never silently reduce clearance, change area, erase a POI or relocate an ocean. Freeze the chosen preference, exact anchor and relevant policy/version in SAVE; Continue restores saved position. BC0 settles these contracts, BC1 reconciles ocean/start ownership, BC5 implements the selector and resolver. Future POI providers must supply planned footprints without loading distant scenes.

**Minimal content now:** Snowy Tundra and Desert are ground-only placeholder definitions registered in the normal finite World collection, alongside Oak and Pink. No new scenery/entity/deposit spawning or regional weather is included. They currently place by global weights; BC2 will add climate affinity. Their artwork and populations can be expanded later. BC0 audit complete; BC1 implemented awaiting local validation; BC2–BC7 remain planned.


> **Original design baseline — BC0/BC1 delivered above; remaining phases PLANNED.** This is the detailed Codex handoff extending the established [BC phases](../../Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md). Do not infer these features already work; **no changes to generation, map size, saves, weather, or spawn-selection UI are included in this planning document**. Implement each phase only when individually requested. Read root README and CodePreferences first; prefer brief code review and an initial build/basic check where practical, leaving long runtime/profiling checks to the user.

## Desired creator experience

A data-driven **World Author**, initially simple Godot Inspector Resources (eventually a dedicated optional editor), should let the developer describe **intent** in plain terms:

- **One enormous ocean broadly near the MIDDLE, with seeded offsets allowed** of the finite world (an irregular saved geography body, not a fixed-centre requirement, a perfectly circular lake or a tinted biome).
- **Very cold climate FAR NORTH** (permanent snow/ice if even summer remains below freezing), **cold northern forest** below that, **temperate EAST/WEST middle** with distinct seasons, **warmer SOUTH**, and **hot/dry FAR SOUTH** with deserts/volcanic lands when actual biome content exists.
- Optional strange enchanted/corrupted provinces in a preferred broad map area, ignoring ordinary climate via deliberate authorable exceptions.
- Configurable **region anchors/extent, minimum/maximum size, intensity, transition width, irregularity, required/preferred priorities, exclusions and seed variations**. A normalized `(x,y)` placement anchor means north/centre/south scales with future world size; **+Y is south in Godot**.
- Optional **new campaign spawn preference: North / Temperate Middle / South**. Resolve an actual safe land spawn in that broad zone *after the macro plan exists but before detailed initial chunks load*. Save the chosen location and never relocate a Continue campaign.
- View the **entire coarse world in F7** (climate, biome, oceans, features, selected spawn), zoom/pan/scroll to inspect it, optionally generate finer **map-only** samples progressively with a progress indicator. The map is a view of the same authoritative seed/blueprint, not a second world generator.
- World Author controls **new-world generation**. A World Profiler read-only section reports generator stage durations, cell counts, region counts, preview cost, caches and nearby chunk workload; it should not own game generation or perform steady expensive polling.

The geography should be **organic and coherent**, not five horizontal painted stripes. North-to-south temperature is an underlying gradient modified by oceans, future mountains/altitude, magical influences and authored broad zone fields. A central ocean may interrupt middle-land continuity: choosing "Middle" should find a suitable **shore/inland temperate land position**, not put the player into the ocean.

## Four separate inputs, one deterministic output

| Level | Authoritative source | Role |
| --- | --- | --- |
| World authoring | `WORLD/Generation/World/WorldGenerationMasterConfig`, existing `WorldLayoutPreset`, `WaterGenerationMasterConfig`, and small future typed authoring Resources under `WORLD/Generation/Authoring/` | New-world **instructions**: seed, finite bounds, central ocean zone, climate north/south gradient, magical anchors, spawn selection zones. Avoid a second independent WorldMasterConfig. |
| Frozen world skeleton | `WorldGenerationPipeline`, `WorldGeographyPlan`, `MacroWorldMap`, future `WorldClimatePlan`/coarse sector plan or equivalent | **One canonical**, seed/versioned, position-queryable geography and climate identity. May be planned with low-resolution global data and deterministic independently addressable sectors; saved/hashed before mutable gameplay. |
| Biomes and local procedural detail | `WorldBiomeDefinition`, optional `BiomeClimateProfile` and future affinity, existing `WorldChunkStream` and `ChunkSceneryData` | Choose compatible biome regions **inside climate provinces**, then generate nearby ground/scenery from world seed + stable coordinate/sector/chunk keys. Never spawn every tree at creation. |
| Live environment and persistence | `ENVIRONMENT`, `SAVE`, `WORLD/Runtime/Persistence` | Later: combine zone + biome + season/weather at positions for snow/foliage, save edits and selected spawn, restore existing frozen geography on Continue. |

**Important:** one global coarse plan is often the cheapest way to guarantee a single central ocean and continuous north/south provinces. **Fully local independent random chunk generation cannot guarantee coherent giant oceans, connected biome regions, identical F7/map results or saved geography without an agreed global/sector contract.** Keep the canonical *macro skeleton* global/cheap; **defer high-resolution macro refinement** and every gameplay/scene object to deterministic queried sectors/chunks. The implementation must choose and document one seam-safe ownership scheme, not claim both full synchronous maps and lazy sectors simultaneously without a design.

## Actual code today — source audit baseline

- `WorldGenerationPipeline.Build` **synchronously builds** P2 feature reservations, full major-water geometry and a **complete coarse `MacroWorldMap`** via `MacroWorldBuilder.Build` on first F6 World / New Campaign. `WorldBlueprintRestore` loads this frozen map on Continue; it **does not rerun new-world generation** on load.
- `MacroWorldMap` owns an `int[]` of region IDs with position-based `RegionAt/Position` and `BiomeAt` queries. It is **not** every local chunk's detailed terrain. The existing world preset is **142,304 × 106,728 world units**, with **512-unit macro cells**, i.e. about **58,102 coarse cells**.
- `WorldChunkStream.Initialize` loads only the active **radius-2** ring around player (25 nearby chunks); prefetch radius 3 and unload radius 4 retain bounded local scene data. Chunk size is also currently 512 units but is **independently configurable from macro-cell size**. On movement, local scenery/navigation/resources are generated from map ownership and coordinate-keyed deterministic candidates.
- `MacroMapDebugPanel` F7 currently constructs a **full coarse Image at panel initialization**, including water/feature sampling, even before F7 is opened; it fits the whole world and **does not have zoom/pan or progressive progress**. It **does not load all gameplay chunks**.
- `CampaignSession.New(profileId,seed?)` chooses a seed but has **no spawn zone parameter**. `WorldScene` currently sets fresh player spawn from `FeaturePlan.StartingArea.Anchor` or the world centre. The `DefaultWorldLayout.tres` currently contains a **fixed central protected start**; that conflicts with a future giant central ocean and North/South starting choices unless the startup/reservation policy changes.
- `WorldGenerationMasterConfig.MaximumCells` defaults to **262,144**; `MacroWorldBuilder` validates finite bounds and a higher **hard guard of 4,194,304**, then estimates site count and enforces `MaximumRegions`. Current site placement/region growth and world setup are **not proven to scale linearly** to a 50× area. `WorldBlueprintManifest` stores the coarse region-cell array and hash; SAVE fingerprints recursively bind authored content. **Do not increase constants as a substitute for redesign.**
- **BP0/BP1** added an Oak baseline `Climate.tres` of 15°C/65% RH, but it is **not** a current temperature/weather rule. **BC0–BC3 are planned** and do not yet enforce the author's north/south climate profile.

## World size: 50× means **50× AREA**, not 50× each axis

Target comparison, using roughly the **same aspect ratio**; new numbers are illustrative and **must not be written into `.tres` until BC4 is approved**:

| Setting | Current (actual) | 50× area candidate |
| --- | --- | --- |
| Bounds / area | 142,304 × 106,728 ≈ 15.19 billion world units² | approximately **1,006,000 × 755,000** ≈ 759 billion units² |
| Linear axis multiplier | 1× | √50 ≈ **7.07× per axis** |
| If macro cells stay 512 | 278 × 209 = 58,102 | roughly **2.9 million** coarse cells, above current default cap |
| If macro cells use 2048 | n/a for current preset | roughly **492 × 369 ≈ 182,000** coarse cells, within today's default cell-count budget |
| Detailed streaming | 512-unit chunks in nearby radius | **keep current local streaming radius initially**; total distant chunks are NOT loaded |

**Initial scale recommendation:** first trial 50× **area** with **2048-unit macro biome cells**, a **coarser climate layer (e.g., 4096–8192 world units)** and existing **512-unit gameplay chunks**, with finer deterministic region/detail queries only where warranted. These example scales are tuning candidates, not mandated architecture: larger cells can make biome boundaries visibly blocky if no separate sub-cell border/shaping function is provided. **Preserve organic water polygons and shores independently of coarse biome resolution.** Scale major-biome target diameters/region counts with the landmass; do not keep thousands of tiny 4–8k patches simply because the map increased.

**Required BC4 engineering checks:** cell count, region count/site-selection cost (the present site sampling has a nontrivial region-count cost), macro bitmap/serialization sizes, startup/Continue duration, large-distance world coordinates and float/physics/render precision, water planner cost, F7 raster cost and memory, scene/chunk cancellation, limits/camera and saved origin/bounds. **No idle or camera-dependent whole-world traversal.** A better future approach may use a global **lower-resolution semantic skeleton + deterministic independently keyed regions/sectors/tiles** with halos, stable ownership and bounded on-demand refinement instead of holding large final high-resolution cell maps. Verify equal results for arbitrary query order, crossing sector borders and save/Continue.

## Climate and seasons — not a simple north-only on/off flag

One natural year can affect every climate, but **the visual consequences differ**. Authored north/south temperature trend plus a future seasonal offset and current weather gives effective local temperature:

| Region | Illustrative baseline behavior | Winter/summer appearance |
| --- | --- | --- |
| Far north | Summer can stay below freezing | Permanent snow/ice on ground, no need for endlessly emitting snowfall |
| Subarctic north | Cold winters, mild short summers | Deep winter snow; spring melt; hardy northern foliage |
| Temperate centre / eastern/western coasts | Seasonally crossed freezing point | Oak/Pink-like forest changes colour; winter snow conditional on cold + precipitation |
| Warm south | Winter above freezing in lowlands | Still cooler in winter but generally rain rather than snow; colours/growth may change |
| Far south desert/volcanic | Very hot/dry most of the year | Little precipitation/snow; possible exceptional mountain or magical frost |

**Biome identity remains independent of local weather.** Oak in a colder zone does not become an entirely different Oak C# class or a new snow manager. `BiomeClimateProfile` may eventually supply intrinsic baseline/seasonal response, while **world position provides regional climate**; the *effective* climate combines both, avoiding a contradictory "Oak is always 15°C" interpretation. Magical fungi may keep blue/purple colour or glow through frost. `SE4` is the future automatic calendar; do not silently introduce it while implementing map layout.

Rain/snow shaders and GPU emitters are **shared**; later BC3 uses spatial masks/chunk-driven climate updates so one camera can see snowy north and thawed south, without per-flake/per-tree temperature polling.

## Spawn choice: North / Middle / South

**Future New Campaign menu only**, not implemented today. The selector chooses a **region preference** rather than a hard-coded world coordinate:

1. After seed + world author preset is chosen, generate or retrieve the **authoritative macro geography and climate skeleton**. If the user picked North, prefer suitable **northern accessible land** in the allowed cold/seasonal belt; Middle prefers temperate **land outside the central ocean**; South prefers warm land.
2. Search **bounded** deterministic candidates for a valid spawn anchor; honor required world-feature constraints, ocean/lake/river geometry, shoreline/setback, configured minimum clearance from major POI footprints/approaches, blocking scenery and reasonable chunk/nav accessibility. Reserve/protect the selected spawn site **before final local population placement** and reconcile the old hard-coded `feature.start` fixed centre. The central-ocean directive and start request must not compete silently.
3. If no safe site exists, explicitly report which condition failed. A documented fallback to nearest suitable land is acceptable **only if authorized**; don't silently place the player in water or overwrite the required ocean.
4. Save the chosen **world position + selected region/preference + frozen generator/world ID** in a versioned campaign launch/blueprint. On Continue restore the exact location; no re-rolling start based on the current seed or `.tres`.
5. Only initialize the existing nearby streamer around that position. Do **not** generate the path between the middle map and selected south/north spawn.

Do not hardcode North/South species lists, temperature checks or duplicated spawn managers into `WorldScene`. The same chosen biome-definition Resources and seed determine local ground/trees/rocks/etc., with the existing population allowlists and world-content ownership.

## F7 World Author preview and World Profiler

This is a **read-only view of canonical planning data**, independent of gameplay streaming. Separate the lightweight overview from the optional finer preview:

- **Fast overview first**: when opened, draw a **low-resolution global thumbnail** of ocean/coast, climate gradient/provinces, biome regions and author directives; optionally show spawn candidate area and player location. F7 should start responsive without allocating/updating the whole high-resolution map while hidden.
- **Zoom/pan**: mouse-wheel zoom anchored under cursor, drag to pan, fit-to-world/reset, optional minimap and coordinate readout; layers/toggles for **geography, climate/temperature, humidity, biome IDs/region outlines, ocean/water, POIs, spawn candidates and chunk grid**. Only request viewport-related detail; never render every local prop.
- **Optional "Generate full map preview"**: independent cancelable/budgeted pass over the **global low-fidelity macro map or tiled thumbnails**, not playable chunks. Show stage name (geography, climate, biomes, raster), percent/work units, current resolution, elapsed time and error/cancel state. User accepts a slow optional preview; **normal F6/New Campaign gameplay should not wait for that optional rendering pass**.
- **Progressive resolution**: global colour thumbnail first; on zoom request higher-res deterministic tiles for the visible section and cache with a bounded LRU/eviction budget. Generated map tiles should sample **the same frozen seed/blueprint** as World and preserve seam continuity. The map can preview *undiscovered* geography for debugging; that reveal is DEBUG-only, not gameplay exploration/fog-of-war.
- **World Profiler read-only counters**: actual per-stage generation duration (new-world and restored-world distinguished), number/area of climate/biome regions, macro cells/sector cache entries, generated preview tiles and time, memory estimate, streamed chunk queue/counts and generation seed/version/hash. Display data on demand or throttled; zero overhead for disabled panels beyond already-collected counters. Do not invent frame-time numbers; measure at target hardware if requested.
- **Safety**: avoid long synchronous `Image.SetPixel` loops/water shape scans on startup. Time-slice large raster work and upload Godot Images/Textures on main thread; only run pure managed calculations on workers when thread-safety and deterministic ownership have been checked. F7 cannot mutate SAVEs, replan an active campaign, instantiate distant chunk scenes, move the player, or cause persistent discovery.

## Phases and recommended implementation order

Preserve the existing **BC0–BC3 identifiers**; this plan extends the track to **BC7**. BC3 remains the regional weather step; BC4–BC7 are newly introduced.

| Phase | Concrete delivery / user-visible result | Scope |
| --- | --- | --- |
| **BC0 — World Author contract** | Review current owners, choose region/direction Resource format, north/south climate gradient, central-ocean constraint, hierarchical-seed plan, **50× area target**, campaign spawn semantics, frozen save/version/migration, budget and stage interfaces. Document author preset + basic F7 acceptance. | Implemented — audit complete |
| **BC1 — Author directives + climate provinces** | Typed Inspector-first World Author preset (central ocean / cold north / temperate centre / hot south / optional magic area); major ocean planner honours a normalized centre zone; seed creates connected climate provinces. **Basic F7 climate layer**, lightweight map, no full landscape spawn. | Implemented — default new-world preset switched; awaiting local validation |
| **BC2 — Climate-aware biome regions** | Shared biome placement affinity, optional hard requirements and rare magic exceptions; weights/diameters within compatible climate areas; placeholder Oak-like/frost/fungi/desert Resources show **coherent clusters** in F7. | Implemented — awaiting local validation; no ore/entity spawns |
| **BC3 — Regional weather/snow response** | One position-aware climate/weather resolver drives existing snow/rain/snowpack/foliage; temperate Oak is winter-seasonal, far north has enduring pack, south usually snow-free, fantasy tint preserved. Handles a camera straddling zones. | Implemented foundation with SE4 — awaiting local validation |
| **BC4 — 50× finite world and hierarchical/lazy macro data** | Scalable world-size preset, safe macro-cell/region budgeting and **global low-res skeleton with bounded deterministic refinement** (or measured equivalent); 50×-area map generates/reloads without entire detailed World, restores same boundaries from any starting region. | Implemented — awaiting native/local validation |
| **BC5 — Spawn-region selection** | New Campaign choice **North / Temperate Middle / South**; deterministic safe land anchors, start reservation and SAVE/Continue ownership. Spawns directly into chosen local chunks. | Existing F6 default unchanged unless deliberately configured; no cross-world travel preloading |
| **BC6 — Zoomable F7 + progressive preview/profiler** | Full-world fit, wheel zoom, drag/pan, layered map inspection, optional budgeted full map-only preview with percent/progress/cancel, on-demand higher detail and read-only timings/counters. | DEBUG-only UI; no giant world scene/raster creation at launch |
| **BC7 — Optional dedicated World Author UI** | If typed Godot Inspector Resources become cumbersome, a separate authoring panel/editor workflow for choosing preset, positioning zones, seed regeneration and preview comparison **before creating a campaign**. Reuse BC1 data/BC6 renderer; no duplicate world generator. | Optional polish; don't build before the data model proves useful |

**Suggested practical order:** **BC0 → BC1 → BC2** on today's finite size using colour-coded placeholders; then **BC4 → BC6 → BC5** for the 50× experience; and **BC3** after layout/weather boundaries are satisfactory. **BC7** is optional later. If the user wants the 50× world as early as possible, **BC4 may follow BC0 before BC1** provided climate-plan contracts and save ownership are coordinated. No need to implement every phase in one risky pass.

### Per-phase minimum acceptance

- **BC0:** clear single-owner data/ID/seed plan, time/resolution budgets and save migration boundary. No phony claim that current macro map is already lazy.
- **BC1:** the same fixed seed produces same central ocean and coherent north/south climate; another seed varies outlines within guidance. F7 sees low-cost climate overview; the default feature/start cannot occupy the mandated ocean.
- **BC2:** placeholders self-group by climate, Oak remains possible in appropriately temperate/northern seasonal zones, only valid land has biome IDs and all existing hydrology rules remain respected. One/two-biome fallback covered.
- **BC4:** reproducible widely separated coordinate queries regardless of exploration order; safe chunk borders and organic coast; macro counts within chosen budget; load/Continue preserve region IDs and saved changes; stable local rendering at large coordinate magnitudes. **Native target-PC performance acceptance remains the user’s job.**
- **BC5:** North/Middle/South each selects **valid land** under the same world seed. Initial local chunks load normally; F7 marks spawn; no ocean spawn or campaign respawn relocation.
- **BC6:** unopened F7 adds no expensive startup raster; overview is quick, detailed map is optional/cancellable and does not allocate world scene nodes; zoomed tiles align with overview; displayed progress/counters reflect real work.
- **BC3:** cold/warm boundary visually correct in one camera; normal off/rain/snow behavior, wind, wetness and seasonal foliage still function; disabled precipitation returns to dormant state.

Each implementation phase: review affected sources and diff, run basic build checks when practical, then hand off graphical/performance/extended testing to the user per [CodePreferences](../../../CodePreferences.md). Reevaluate SAVE **schemas, content fingerprints and blueprint formats** before touching generation. Use **one commit per approved job**, not one giant implementation commit for all BC phases.
