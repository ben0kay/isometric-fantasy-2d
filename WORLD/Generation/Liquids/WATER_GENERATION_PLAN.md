# Procedural Lakes, Ponds, Rivers & Biome Water Suitability — Implementation Plan

> **UPDATED — 24 October 2026. WG0 complete; WG1 implemented, awaiting target-PC visual/performance acceptance.** See [audit, decisions, validation and WG2 handoff](WG0_WG1_Validation.md). WG2 is implemented; target-PC visual/performance acceptance is pending ([WG2 evidence](../../Liquids/WG2_Validation.md)). WG3 is now implemented; see [river status/validation](Rivers/WG3_Validation.md). WG4–WG7 remain planned. Do not implement phases without explicit approval. Read root README.md, CodePreferences.md, WORLD/Generation/README.md, WORLD/Generation/Liquids/README.md, WORLD/Liquids/README.md, WORLD/Generation/World/Features/README.md, WORLD/Runtime/Streaming/README.md, and relevant actual C# scripts/scenes before coding. P5 world geography and the separate Oak spawning plan remain independent.

## Phase overview

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **WG0** | Audit current pond, macro map, reservations, liquid regions, streaming and SAVE; finalize contracts | Regular Chat | Complete |
| **WG1** | Deterministic macro water plan: biome suitability + organic pond/lake footprints | Codex | Implemented; target-PC acceptance pending |
| **WG2** | Render/stream generated lakes using existing liquid behavior; exclude scenery and navigation | Codex | Implemented; target-PC acceptance pending |
| **WG3** | Generate continuous cross-biome rivers with noise-driven meanders and sensible endpoints | Codex | Implemented; target-PC acceptance pending |
| **WG4** | Add shoreline, shallows, banks and terrain blending/carving where supported | Codex | Planned |
| **WG5** | Elevation-aware drainage, tributaries and wetlands (optional) | Regular Chat → Codex | Planned |
| **WG6** | Shared directional river/stream currents and surface flow visuals | Codex | Planned |
| **WG7** | Deep-water swimming, floating and opt-in underwater scene transitions | Codex | Planned |

## Confirmed foundations and limitations

- P1 already creates a deterministic finite coarse biome ownership map from weighted sites; it does **not** generate noise-based elevation, climate, coastlines or water. Biome region shapes are currently a coarse partition.
- P2 plans deterministic polygon reservations, including a protected starting area that rejects water. Its placement kind includes Water; reservations **do not** yet create/render water.
- P3 streams nearby chunks and deterministic scenery with chunk lifecycle, exclusions and stable identity. New water must be compatible with these contracts, not independently rerolled per chunk.
- P4 provides saved world blueprint and chunk state. Existing save schema/fingerprints need audit before new generated water is added.
- A production WorldPond.tscn exists, with liquid region/wading/splash foundations. Current pond is an authored/fixed overlay, **not** a procedurally carved terrain lake. Preserve its appearance and DEBUG prototype.
- WORLD/Generation/Liquids/ is already the designated future generator folder; WORLD/Liquids/ owns definitions, region queries, surfaces and interactions. The WORLD/Liquids/README.md has a contradictory historical 'foundation only' paragraph; trust current scripts/scenes after WG0 audit.
- The current world bounds default to 45,000×33,750 and stream chunks to 512 world units. The macro biome grid is coarse, not the shoreline mesh resolution.

## Architecture and folder ownership

```text
WORLD/
├── Biomes/
│   ├── Shared/                        # shared BiomeHydrologyProfile.cs (implemented)
│   └── Definitions/OakForest/
│       ├── Biome.tres                  # existing; future optional Hydrology link
│       └── Hydrology.tres              # PROPOSED, not created
├── Generation/
│   └── Liquids/
│       ├── README.md                   # existing
│       ├── WATER_GENERATION_PLAN.md    # this plan
│       ├── Ponds/                      # future, create only when needed
│       ├── Lakes/                      # future
│       ├── Rivers/                     # WG3 planner and frozen path data
│       └── Carving/                    # future
├── Liquids/                            # existing shared types, regions, visuals, interactions
└── Terrain/                            # future banks, shore transitions, elevation
```

Keep liquid **shape and location** separate from liquid **type** (water/lava/toxic). Biomes provide suitability/permissions; they do not duplicate liquid shaders or spawn one manager per biome. Avoid creating all speculative directories and .tres files up front.

## Biome-driven hydrology proposal

Add an optional, reusable `BiomeHydrologyProfile` linked from `WorldBiomeDefinition` after WG0. Proposed Inspector settings:

- `AllowFreshwaterLakes` / `AllowFreshwaterRivers`: explicit hard exclusions, default permissive for compatibility.
- `LakeSuitability` / `RiverSuitability` (0–1): soft weights that influence site placement, footprint extension and river routing, **not** a guarantee of a feature.
- Optional `PreferredLakeSize`, `WetlandSuitability`, `ShoreStyle` later; do not conflate gameplay temperature/humidity with surface-water permission.
- Allow biome-specific liquid types later (e.g. volcanic lava pools), but do not assume volcanic biomes universally forbid all liquids.
- Sample suitability over **the full proposed footprint/path**, not just its center. For mixed-biome lakes, define whether every touched biome must allow water or whether a boundary may clip/shrink the footprint.
- If a river encounters prohibited terrain, **do not fade a rendered ribbon to zero at a biome boundary**. Route around it, narrow toward a valid terminal basin, sink/evaporate where explicitly supported, or reject/regenerate that river; maintain a valid topological endpoint.
- Use a safe null profile fallback and explicit data/version compatibility for existing Oak/Pink and saved campaigns.

Illustrative suitability (not actual authored values):

| Biome | Lake | River | Hard freshwater prohibition |
| --- | --- | --- | --- |
| Oak Forest | 0.8 | 0.8 | No |
| Pink Meadow | 0.5 | 0.8 | No |
| Volcanic Wastes | 0.0 | 0.1 | Optional yes |
| Desert | 0.1 | 0.2 | No; rare oasis possible |
| Swamp | 0.9 | 0.9 | No |

## Shared water-body data and future gameplay boundaries

Keep one extensible, data-driven water-body record across ponds, lakes, rivers and oceans: stable ID, liquid definition, footprint/path, optional depth profile, flow profile, and optional underwater destination. Only create concrete Resources and classes when their phase needs them. **No per-body temperature setting:** temperature is supplied by the biome/world climate service at a sampled position; for cross-biome rivers it may vary along the river. Climate is gameplay data and should be queried through one shared interface when implemented, not simulated by each liquid body.

- **Geometry:** pond/lake outline or river corridor, deterministic seed, shoreline and depth sampling; no geometry regenerated separately by chunks.
- **Liquid type:** reusable water/lava/etc. definition; never assume all liquid is freshwater.
- **Depth/interaction:** separate wading, swimming and eligible diving thresholds; depth does not automatically mean a new scene.
- **Flow:** still for typical ponds/lakes; rivers/streams carry a directional velocity field with configurable strength, bends and local calm/rapid areas.
- **Underwater access:** opt-in per body or authored entry zone, with stable destination/return anchor; most bodies have no underwater scene.
- **Climate:** temperature and humidity originate from biome climate plus future weather/season/time rules; no duplicated water-body temperature.
- **Persistence:** save world/body identity and transitions without loading underwater scenes until entered.

## Shape-generation rules

**Lakes/ponds:** choose deterministic macro site and size, create a bounded organic outline (noise-warped radius/implicit signed distance), validate non-self-intersection, width/area, minimum separation, protected-start/P2 exclusions, and biome eligibility. Use seeded world-coordinate noise so borders are independent of chunk load order. Store one authoritative polygon or compact geometry descriptor and spatial index; chunks query/crop it. Generate shore/depth distance fields only as needed and reuse the existing liquid-region interface. Start with one small organic pond/lake, not dozens.

**Rivers:** construct one continuous world-space polyline/spline or centerline with variable width. Choose explicit start and end (source, lake, ocean, terminal basin or future sink), then route through a low-resolution cost field incorporating biome suitability, hard exclusions, reservations and (when available) elevation. Apply bounded noise-driven lateral bends **after** route planning; ensure no disconnected segments, self-crossings or zero-width gaps. A random noise mask alone is insufficient for drainage. Reserve/mark a continuous corridor and keep flow direction and bank width coherent across biome and chunk boundaries.

**Biome shapes:** optional separate follow-up, not a requirement for first water generation. Existing P1 ownership grid may later apply coordinate-stable noise distortion/smoothing at region borders, preserving full land assignment and deterministic hashes. Do not silently rewrite P1 macro topology or old saved worlds to improve lake outlines.

**Geography order:** [Generation_Implementation.md](../../Generation_Implementation.md) is the authoritative P5 plan: required constraints → major geography (including major water) → important POI/access reservations → biomes → local detail. Current WG1–WG3 water still consumes an already assigned biome map; P5a/P5b must deliberately invert that dependency and version new-world outcomes. Biome rules must then respect established major rivers, not erase them. Preserve protected/required features and exact legacy save restores. WG4 appearance can proceed independently; drainage/branching networks are separately scoped WG5 work.

## Detailed phases

### WG0 — audit and interface design
**Objective/dependencies:** inspect current production pond scene and C#, liquid region polygon/distance queries, P1 biome generation, P2 reservation geometry, P3 scenery/nav exclusions, P4 manifests/snapshots, terrain ground layers and F7/F9 diagnostics.
**Scope:** specify the minimum reusable water-body record (stable ID, type, footprint, seed/version, ownership, bounds), hydrology profile, geometry query contract, save compatibility, startup/preview differences and planned location of settings (shared MasterConfig vs per-biome).
**Out of scope:** new scenes, code or .tres.
**Preserve:** current fixed pond, Oak/Pink visuals, existing saves, start reservation and existing liquid effects.
**Performance:** establish macro planning budget, spatial query/index strategy and zero idle-cost goal.
**Pass:** written interface decisions, identified actual source files and risks; no unverified assumptions.
**Assistant:** Regular Chat (Codex for architecture review if needed).

### WG1 — macro pond/lake generation
**Objective:** add opt-in seeded organic water-body planning and biome suitability with no runtime water instantiation yet.
**Scope:** shared typed config, optional Hydrology profile, deterministic placement/shape and compact frozen footprint output, water vs P2 reservation checks, coarse F7 overlay if easy.
**Out of scope:** river routing, terrain excavation, liquid effects, dynamic water simulation.
**Preserve:** legacy fixed pond, default visual world and no-water fallback; avoid double pond.
**Performance:** generate once per seed or load frozen plan, bounded retries, no per-chunk noise regeneration or idle loops.
**Pass:** repeated seed yields identical geometry, changing unrelated biome config does not unpredictably reorder streams, hard exclusions and protected start respected, shapes valid, stable world identity/save strategy documented.
**Assistant:** Codex (shared generation + save integration); Copilot local tests.

### WG2 — streamed water presentation and gameplay
**Objective:** make the WG1 lake visible/playable using the existing liquid definition, region, wading/splash and shaders.
**Scope:** bounded chunk-intersection queries, materialize/release nearby geometry, prevent trees/rocks from spawning inside water, handle collision/navigation and overlapping chunk edges, keep shore coordinate continuity.
**Out of scope:** new liquid type, river flow, full carving/height.
**Preserve:** production pond behavior, wind/rain/snow interactions, existing scene ownership and F9 controls.
**Performance:** one shared region/definition, cached polygon/distance data, bounded chunk lifecycle, no per-water-tile processing; unload rendering/physics outside active chunks, keep cheap macro metadata.
**Pass:** cross-chunk lake seams invisible, no duplicate water/colliders, walk/wade works, scenery/nav exclusions correct, leave/return stable, idle/active FPS and allocations profiled.
**Assistant:** Codex; Copilot/user graphical checks.

### WG3 — winding rivers across biomes
**Objective:** route a coherent river through multiple biome regions with changing width and organic bends.
**Scope:** deterministic source/destination, macro path and corridor, biome suitability/exclusions, branch-free initial river, integration with WG2 streaming and rendering, terminal behavior on prohibited biome.
**Out of scope:** realistic fluid simulation, waterfalls, full drainage network, tributaries.
**Preserve:** stable lake IDs, P2 protected areas, no unexpected biome replacement.
**Performance:** route once at macro generation, index corridor segments for chunk lookup, no continuous river-flow physics for static appearance.
**Pass:** connected path across several chunks/biomes, correct exclusions, no abrupt disappearance, consistent banks/wading, same seed reproducible, no streaming hitches.
**Assistant:** Codex; Copilot local tests.

WG3 shape follow-up: smooth cubic macro routing, broad S meanders (`RiverBendStrength`, `RiverMeanderLength`) and independent closely sampled bank offsets are implemented. New river blueprints use schema 7/water version 3; old frozen shapes remain unchanged. WG4 below remains planned; reference image 5 guides earthy shore texture/edge overlap, not the already implemented river geometry.

### WG4 — banks, shallows and terrain transitions
**Objective:** believable shores rather than blue shapes pasted onto grass.
**Scope:** shoreline width/colour/material masks, shallow-to-deep appearance, ground exclusions/edge blending, optional banks or carved masks coordinated with WORLD/Terrain/Transitions and GroundStack.
**Out of scope:** full 3D elevation, erosion or global water table.
**Preserve:** existing Oak/Pink ground tint, dirt/rock patch layers, snow/rain/wind and terrain shader seams.
**Performance:** cached per-region distance/masks, material sharing, update only on geometry or environment changes.
**Pass:** no stretched/blurry banks, consistent scale at zoom, no seams/overdraw spikes, graceful intersections with biome colour transitions.
**Assistant:** Codex; user visual acceptance.

### WG5 — advanced hydrology (optional)
**Objective:** future elevation-aware drainage and water networks.
**Scope:** macro elevation, downhill flow, tributaries, lakes as reservoirs, wetlands, river outlets, erosion and optional waterfalls if terrain system supports them.
**Out of scope:** full fluid simulation across unloaded world.
**Preserve:** old seeds/campaigns via versioned migration or explicit new-world-only changes.
**Performance:** precompute coarse geography, bounded active effects only; dormant offscreen simulation.
**Pass:** coherent flow graph, no cycles/unreachable outlets unless intentionally endorheic, deterministic save/restore and measured acceptable startup cost.
**Assistant:** Regular Chat design then Codex after approval.

### WG6 — river and stream flow

**Objective:** Make water visibly move downstream and optionally carry immersed actors. **Dependencies:** WG3 river centerline, existing liquid renderer and player movement integration. **Scope:** deterministic direction and speed samples, adjustable current strength, stream/rapid zones, directional surface animation, optional player drift. **Out of scope:** full fluid physics, individual water-particle simulation, diving. **Performance:** precomputed/cached flow segments; query only for immersed active actors and visible effects; zero flow stays dormant. **Pass:** smooth downstream direction across biomes/chunks, no visual seam, correct player push, calm lakes unaffected, profile changes wake/sleep correctly. **Assistant:** Codex.

### WG7 — swimming, floating and underwater scenes

**Objective:** Enable deep-water traversal and optional separate underwater exploration. **Dependencies:** WG2 depth/immersion, player input mapping, world-scene handoff and SAVE contracts. **Scope:** shallow wade vs deep swim, hold **right mouse button** (existing jump input) to actively stay afloat, **Ctrl** to descend only in eligible dive zones, scene transition to a selected underwater destination, safe return to the same body/location and campaign state. Respect existing land crouch/jump bindings; resolve conflicts in WG0. Most lakes and rivers can remain non-diveable; selected deep ponds and oceans may opt in. **Out of scope:** mandatory underwater scenes, full underwater ecology, finalized oxygen/drowning rules. **Performance:** no per-body input polling; underwater scenes unloaded until entered; dormant when player is away. **Pass:** right mouse affects afloat state, Ctrl does not transition in shallow/non-eligible water, return location and save reload stable, no duplicated surface scene. **Assistant:** Codex.

## Validation and F9/F7 suggestions

- F7: show planned water polygons/corridors, seed/IDs, biome suitability and blocked placements (debug only).
- River geometry controls stay Inspector-only per user preference: bend strength, bank irregularity and span/width ranges. No in-game geometry sliders are requested.
- Tests: repeatability across seeds; two biomes and one-biome cases; zero enabled biomes still fail as P1 specifies; hard exclusions; reservation conflicts; concave shore queries; chunk borders; fixed pond coexistence; saved-world reload; water collision/nav; graphical zoom, FPS and memory.
- Explicit performance handoff after each implementation: trigger, shutdown, remaining idle callbacks, bounded scaling, tests actually run and deferred target-PC checks.

## Optional future ideas (not part of initial phases)

- Ponds connected to rivers, seasonal drying/flooding and spring sources.
- Swamps/marshes, river deltas, waterfalls and underground sinks.
- Biome-specific water palettes, algae, frozen lakes and lava basins.
- Shore vegetation, reeds, beaches and resource spawns conditioned on distance to water.
- Boats, aquatic wildlife, oxygen/stamina, underwater lighting and exploration content after core water controls mature.
- Waterfalls, rapids, water-body connectivity and shoreline vegetation responding to seasonal climate.
- Wetness, freezing and thawing derived from biome climate rather than per-water-body temperatures.

## Decisions before WG1 — resolved in WG0_WG1_Validation.md (historical questions)

1. Should initial generated lakes coexist with the authored fixed pond, or should it become a compatibility-preview-only feature? **Recommend preserve it initially and avoid overlapping/double placement.**
2. Should hard freshwater exclusions apply to every footprint sample, while suitability softly weights candidate placement? **Recommend yes.**
3. Are first lakes flat water overlays with wading, then WG4 banks/carving, or is shoreline carving a prerequisite? **Recommend overlays first, with honest visuals.**
4. What is the minimum size range for the first lake relative to 512-unit chunks?
5. Should generated water be new-world-only initially, with old P4 saves retaining the previous blueprint until a migration is explicitly designed? **Recommend yes if save contracts cannot safely extend.**

**Current handoff:** WG0/WG1 were subsequently authorized and implemented; consult WG0_WG1_Validation.md. WG2 was subsequently authorized and implemented; see ../../Liquids/WG2_Validation.md. WG3 was subsequently authorized and implemented; see Rivers/WG3_Validation.md. Do not begin WG4–WG7 automatically. One commit per job, update owning README/status and record actual validation after authorized work.
