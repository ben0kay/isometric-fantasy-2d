# Implementation Backlog — Unfinished Work and Acceptance

> **Re-audited 25 October 2026 against `main` at `f52b29f`.** This is a focused list of **work not yet implemented**, distinct from features that have code but still need Godot/Windows testing. It supersedes the [previous backlog snapshot](https://github.com/ben0kay/isometric-fantasy-2d/blob/029fbfc41dcb664455f2e2d7ac17bea228c76e3e/IMPLEMENTATION_BACKLOG.md). Historical plans and documentation discrepancies remain accessible there. **No phase is authorized merely by appearing here.**

## Current headline — what changed since the last backlog

| Work | Current implementation | Remaining boundary |
| --- | --- | --- |
| **PL3 — Oak plants** | **Implemented**, commit `63c3071`. Eight independently defined plants spawn in the actual streamed World and compatibility preview using one shared `Plant.tscn`, deterministic artwork, biome permissions and existing snow/wind. | Native 4.7.2/Windows check; **PL4 plant harvesting is a separate unimplemented phase**. [PL3 evidence](WORLD/Content/Vegetation/Plants/PL3_Validation.md) |
| **H4 — real harvesting, inventory and drops** | **Implemented**, commit `f92d804`. Oak/Pink trees can be chopped in production, physical logs picked up with E; player-owned inventory/hotbar (1–9) and **schema-3** harvest/source/drop/inventory persistence are wired. This also completes **UI3's core production integration**, despite its older plan still saying fixture-only. | Local Windows play-feel/performance and compatibility acceptance; H5 expansion still planned. [H4 evidence](SYSTEMS/Harvesting/H4_World_Validation.md) |
| **P5c — ocean portion / world expansion** | **Implemented**, commit `c3a88dc`. Required ocean and proper **non-land** cells, saved geography/versioning, enlarged **142,304 × 106,728** world (~10× previous area) and F7 whole-map fit. | Volcanic portion of P5c, P5d POIs and detailed WG4 shorelines are still planned. [P5c evidence](WORLD/Generation/Liquids/Oceans/P5C_Validation.md) |
| **B0 — blizzard state** | **Implemented**, commit `f52b29f`. F9/Inspector **Snowfall** slider now 0–3: 0–1 ordinary snow; 1–3 translates to smoothly changing storm severity. Coverage remains independent; no second slider. | **B1–B4 visual phases** still planned. B0 native/runtime checks pending. [B0 evidence](ENVIRONMENT/Weather/Snow/B0_Validation.md) |

**Production baseline:** one major ocean, three lakes and one river are requested in fresh World/New Campaign; existing saved worlds retain their frozen geography where their specific generator/content versions are supported. Active streaming remains bounded to nearby 512-unit chunks. Old layouts incompatible with **PL3 local-scenery generator v2** are deliberately rejected, not silently regenerated. Be careful not to describe the ocean's legacy geography migration as overriding this separate PL3 compatibility gate.

## Recommended focus — one vertical slice at a time

1. **First close integration/acceptance for what just shipped.** Build/import on Godot **4.7.2 .NET**, run PL3/H4/P5c/B0 focused tests, then play one real F6 World and one F5 campaign: plants, trees/log drops, hotbar/inventory, save/Continue, storms, F7 ocean and streamed coast. Profile startup, cross-chunk movement, shadows, scene counts and memory. This is *validation*, not a fresh implementation phase.
2. **Choose one next gameplay goal after that:** **PL4 + focused H5 plant harvesting** is the clearest short path to turning your eight new Oak plants into actual gatherable resources. Reuse H4 drop/saved inventory contracts; do not add separate plant drop and collection systems.
3. **Alternatively** choose a focused visual-world slice: **WG4** shores/shallows around existing oceans, lakes and rivers. A cosmetic/refactor-only pass like GR0 or RK0 can wait until the underlying vegetation generation is accepted.
4. Keep the rest **parked but visible**: enemy encounters, blizzard visuals, new world geography, creature spawning, audio, survival systems and structural proposals. Avoid implementing two major tracks simultaneously.

These are recommendations, not approvals. Follow [CodePreferences](CodePreferences.md): read the owning plan/README and actual code first; one commit per approved job; isolate data from common mechanics; no expensive idle work; report tests performed versus deferred.

## 1. Active implementation backlog — only genuinely missing work

### A. World content, harvesting and player interactions

| Phase / feature | Priority / state | Unimplemented scope | Primary plan |
| --- | --- | --- | --- |
| **PL4 + H5** | **Candidate next · Deferred/planned** | Extend the **existing** H4 harvesting progress, rewards, world drops, pickup and persistence to selected plants, then optional rocks/mining, enemies and containers. Data-driven per-species/tool/drop settings. The eight plants already spawn; **plant picking and yields do not**. | [Plants](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [Harvesting](SYSTEMS/Harvesting/HARVESTING_IMPLEMENTATION_PLAN.md) |
| **GR0** | Later · Planned | Convert current four Tuft PackedScenes into **one `Grass.tscn`**, with four cosmetic PNG variants in canonical `Grass.tres`. Preserve identity, biome spawn densities, snow/wind and P4 compatibility. | [Vegetation contract](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [Grass](WORLD/Content/Vegetation/Grass/README.md) |
| **RK0** | Later · Planned | Convert current `Rock01.tscn` to **one `Rock.tscn`** with per-rock-type `.tres` definitions and optional visual variants; retain rooted footprints, collision/light occluders/shadows, mirroring and saves. | [Vegetation contract](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [Rocks](WORLD/Content/Rocks/README.md) |
| **Pine tree definition** | Later · Content | PNG art exists, but **no live Pine `TreeDefinition`/variants/population**. Author species art/anchors/colliders and explicitly permit in chosen biome using shared `Tree.tscn`. Adding Pine art is not creating a Pine biome. | [Pine](WORLD/Content/Vegetation/Trees/Pine/README.md) |
| **More harvest/equipment content** | Optional · Future | Stump/regrowth rules, shield as real item, other tool/item types, lootable containers and material-specific yields. Do **not** rebuild existing wooden logs, inventory or physical pickup mechanics. | [H4](SYSTEMS/Harvesting/H4_World_Validation.md), [Items](ITEMS/README.md) |

**Non-negotiable content rule:** one shared `.tscn` per compatible gameplay category, one `.tres` per distinct tree/plant/grass/rock type, optional multiple textures/atlas variants **inside the type Resource**, and explicit biome `BiomePopulation` allowlists. A volcanic dead plant cannot spawn in Oak unless Oak explicitly references its definition; a future Pine biome can reuse selected Oak plants.

### B. Geography, oceans, water and biome spawning

| Phase / feature | Priority / state | Unimplemented scope | Primary plan |
| --- | --- | --- | --- |
| **WG4** | **Candidate next · Planned** | Actual shoreline transitions, banks, shallows, land/ground blending and carving where possible around **existing ocean/lakes/rivers**. Ocean polygons and water surfaces already exist. | [Water phases](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md), [Oceans](WORLD/Generation/Liquids/Oceans/P5C_Validation.md) |
| **WG5** | Later/optional · Planned | Elevation-aware drainage, tributaries and wetlands after any genuine height/elevation provider is established. | [Water phases](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **WG6** | Later · Planned | Data-driven water currents and directional river/stream flow visuals and player interactions. | [Water phases](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **WG7** | Later · Planned | Deep swimming/floating and optional underwater transitions, reconciling player RMB hop/guard and wading. | [Water phases](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **P5c volcanic remainder** | Later · **Partial phase** | **Oceans/non-land are implemented.** Separately add major volcanic-region geography and constraints through the same geography-first generation pipeline. This is not authorization to implement lava, volcano elevation, weather or a new biome generator automatically. | [Generation](WORLD/Generation_Implementation.md), [P5c](WORLD/Generation/Liquids/Oceans/P5C_Validation.md) |
| **P5d** | Later · Planned | Prioritize important POIs and access/reservations **before** biome assignment: major temples, roads/crossings, cave approaches, location/scene connections. | [Generation](WORLD/Generation_Implementation.md) |
| **P5e** | Validation as expanded · Planned | Cross-feature combined pipeline validation, frozen campaign geography, streaming and real target-PC performance as volcanic/POI providers land. | [Generation](WORLD/Generation_Implementation.md) |
| **BP0/BP1** | Implemented · Awaiting local validation | Audit and optional typed climate/entity/resource profiles added; Oak spawning disabled/empty. **Baseline climate is configuration only**, not zone-based biome selection or regional snow. | [BP audit](WORLD/Biomes/Definitions/OakForest/BP0_BP1_Audit.md) |
| **BC0–BC2** | **Candidate layout-first · Planned** | Create World Author preset for **OCEAN MIDDLE / COLD NORTH / TEMPERATE MID / HOT SOUTH**, basic climate F7 and biome affinity clustering with placeholder definitions. **No biome types, ore or enemies are invented**; BP0/BP1 already exist. | [BC phases](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md), [World Author plan](WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) |
| **BC3** | After layout proof · Planned | One shared position-aware snow/rain/climate resolution, temperate Oak winters, possible permanent ice far north and enchanted flora response. Preserve mixed cold/warm views without per-biome snow managers. Distinct from SE4 calendar. | [BC phases](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md), [Environment](ENVIRONMENT/README.md) |
| **BC4** | **Large-world foundation · Planned** | New **50× finite-AREA** world preset (~1,006,000 × 755,000 units) via bounded macro global/seeded sector planning and unchanged nearby chunk streaming. Current 512 macro cells would become ~2.9M, so don't just raise MaximumCells. Preserve frozen save and independent coordinate reproducibility. | [World Author scaling](WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) |
| **BC5** | Later · Planned | New Campaign **North / Middle / South** safe-spawn preference; choose a non-ocean anchor from the authored seeded map, avoid fixed-centre protected spawn collision, persist exact location on Continue. | [World Author spawn](WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) |
| **BC6** | **F7 world atlas · Planned** | Fit, zoom/pan/scroll, climate/biome/ocean/spawn layers, optional **whole-world LOW-DETAIL progressive preview** with progress/cancel and on-demand map tiles, read-only profiler counters. **Never load detailed distant chunks.** | [World Author F7](WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) |
| **BC7** | Optional authoring UX · Planned | Dedicated World Author editor/panel only if the Inspector-first BC1 Resource authoring needs better placement/seed comparison. Reuse the same plans and F7 renderer. | [World Author plan](WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) |
| **BP2–BP4** | Later · Planned | Bounded deterministic ore/deposit spawning and depletion persistence, living creature/enemy populations with chunk lifecycle, optional F9 spawn debugging. | [Oak spawn plan](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) |
| **BP5 / P6** | Long-term · Deferred | Persistent residents/abstract distant A-Life; do not keep entire worlds of fully simulated mobs active. | [Biome spawning](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md), [Generation](WORLD/Generation_Implementation.md) |
| **GS6** | Validation phase · Planned | Close ground-layer/F9 visual, seams, performance and save regression acceptance; **GS0–GS5 code is already present**. | [Ground](WORLD/Terrain/Ground/Surfaces/LAYERED_GROUND_IMPLEMENTATION.md) |
| **Caves and locations** | Long-term · Scaffolding | Underground domain, entrances and actual dungeons, settlements, camps, landmarks; coordinate with P5d and GL locations. | [Caves](WORLD/Caves/README.md), [Locations](WORLD/Locations/README.md) |
| **Other liquids** | Long-term · Expansion | New lava/toxic/etc. type definitions, shaders and specialized player/damage rules. Current pond, generated water, streams and ocean use established water infrastructure. | [Liquids](WORLD/Liquids/README.md) |

**Do not rebuild:** P0–P4 world generation/saves/streaming, P5a/P5b geography-first, **ocean portion of P5c**, WG0–WG3 lake/river planning and rendering, or the already enabled World defaults. New world bounds are now **142304×106728**; F7 shows the whole world while only nearby detailed chunks stream.

### C. Combat, enemies and movement

| Phase / feature | Priority / state | Unimplemented scope | Primary plan |
| --- | --- | --- | --- |
| **R2** | When focusing combat · Planned | Archer visual/telegraph polish, F9 ranged controls, mixed practice regression and Windows acceptance. R0 projectile and R1 Archer are already implemented; do not build another duplicate archer. | [Ranged](COMBAT/Projectiles/RANGED_IMPLEMENTATION.md) |
| **ES2** | Later · Planned | Species separation tuning/presets and focused fixtures. ES1 already added `SeparationProfile.cs` and the entity definition field; don't recreate that class. | [Separation](ENTITIES/Shared/Movement/EntitySeparationPlan.md) |
| **ES4** | Regression · Planned | Crowd/corridor/chunk, melee+ranged engagement tests and profiling. ES1/ES3 movement is implemented but still needs stress acceptance. | [Separation](ENTITIES/Shared/Movement/EntitySeparationPlan.md) |
| **C3.4** | Later · Planned | Different event-driven hit, blocked-hit, miss/defense outcomes. Canonical damage and material impact particles already exist. | [C3 feedback](COMBAT/C3_Implementation.md) |
| **C3.5** | Optional · Planned | Capped camera shake and centralized audio/attack/impact sounds. | [C3 feedback](COMBAT/C3_Implementation.md) |
| **C4** | Later · Planned | Tune movement/attack feel on existing sprint/dodge/guard/knockback framework, no duplicate motor. | [Combat](COMBAT/COMBAT_IMPLEMENTATION.md) |
| **C5** | Later · Planned | One fully tuned repeatable skeleton encounter with telegraphs and mixed ranged/melee opponents. | [Combat](COMBAT/COMBAT_IMPLEMENTATION.md) |

**Implemented, not tickets:** AC0–AC4 actor collision, C0–C2.5 shared HP/attacks/player actions, C3.2 knockback/stagger, C3.3a–d reusable particles/impact, R0/R1, ES0/ES1/ES3, shared health bars and C3.1 damage-number source. Visual acceptance differs by module.

### D. Weather, blizzards, seasons and lighting

| Phase / feature | Priority / state | Unimplemented scope | Primary plan |
| --- | --- | --- | --- |
| **B1** | When weather focus · Planned | Very heavy, layered/directional snowfall and blowing ground snow. **B0 intensity/state contract already exists**; reuse it, not a second slider/manager. | [Blizzard](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **B2** | After B1 · Planned | Icy/darker daylight, dense haze and distance-based whiteout tied to storm severity; preserve rain and regular snowfall at 0–1. | [Blizzard](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **B3** | Optional · Planned | Screen/lens snow flurries, cold icy corners and screen-edge shards; no permanent update loop at 0 intensity. | [Blizzard](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **B4** | Validation/polish · Planned | Storm tuning, reactivation, dormancy, particle budgets, performance and regression acceptance. | [Blizzard](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **SE4** | Long-term · Deferred | Real calendar/year and gameplay season effects + saves. Manual foliage SE0–SE3, including mask authoring validation, are code-implemented. | [Seasons](ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md) |
| **LL2** | Later · Planned | General shared light-type profiles and reusable lantern/campfire etc. beyond standing torch/shadow occluders. | [Local lights](ENVIRONMENT/Lighting/LocalLights/README.md) |
| **LL3** | Later · Planned | Streaming/range/cap/light-shadow budgets and performance for many active local lights. | [Local lights](ENVIRONMENT/Lighting/LocalLights/README.md) |
| **Per-region climate / weather scheduling** | Long-term · Planned | Beyond BC3's spatial snow/rain presentation: real current temperature/RH, time/season weather probabilities, full regional profile scheduling/blending. BP1 is baseline data only; coordinate with BC3 and SE4. | [Environment](ENVIRONMENT/README.md), [BC phases](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) |
| **Fire expansion** | Later · Planned | Spread between trees, automatic lightning ignition, damage/offscreen policy; FIRE1/FIRE2 tree ignition/burn/char visuals already exist. | [Fire](SYSTEMS/WorldInteractions/Fire/README.md) |

**B0 note:** existing Snowfall slider is now **0–3**. At 0–1 snowfall remains ordinary; 1–3 drives a derived 0–1 storm severity, with transition and changed-only notifications. B0 does **not** make snowfall visually more intense yet: B1/B2 do. Settled snow cover remains independent. The current emitter still caps ordinary snowfall visuals at rate 1. [B0 validation](ENVIRONMENT/Weather/Snow/B0_Validation.md).

### E. UI, scene organization and long-term gameplay

| Phase / feature | Priority / state | Unimplemented scope | Primary plan |
| --- | --- | --- | --- |
| **UI4** | After production acceptance · Planned | More polished backpack appearance, focus/tooltips/stack interactions, inventory drag/drop/rebinding and accessibility. **UI3 core state and save wiring is now implemented**. | [HUD](UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md), [Player inventory](ENTITIES/Player/Inventory/README.md) |
| **UI5** | Optional · Planned | Status/buff rail, top-left toasts and top-center alerts. | [HUD](UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md) |
| **SO3/SO4** | Optional · Proposed | Runtime registries/diagnostics and general location compatibility only when needed; SO0–SO2 organization already implemented. | [World organization](SCENES/World/scene%20organize.md) |
| **GL0–GL3** | Long-term · Proposed | Interiors/cave scene sessions and transitions; P5d and save/layer domains must be designed first. | [World organization](SCENES/World/scene%20organize.md) |
| **Further SAVE expansion** | Later · Planned | Season/year, caves, A-Life, large-save performance, archive/multiple-slot UI and other migrations. **P4 + H4 schema-3 inventory, felled sources and drops are already implemented.** | [SAVE](SAVE/README.md), [H4](SYSTEMS/Harvesting/H4_World_Validation.md) |
| **Crafting, farming, building, cooking, fishing, skills, survival needs** | Long-term · Scaffolding | Working gameplay/economy mechanics and content. **Harvesting/physical collection is now in production**; do not duplicate it. | [SYSTEMS](SYSTEMS/README.md), [Survival](ENTITIES/Player/Survival/README.md) |
| **Wildlife/NPCs/factions/roles/other enemy species** | Long-term · Scaffolding | More authored species, habitat/AI/population and community behavior; Basic + Archer skeletons and shared Entity already exist. | [ENTITIES](ENTITIES/README.md) |
| **Audio and external content tooling** | Later/optional · Scaffolding | Shared sounds/mixers/ambient tracks, or Avalonia authoring of canonical game Resources; no duplicate runtime catalog. | [AUDIO](AUDIO/README.md), [ITEMS](ITEMS/README.md) |

## 2. Validation queue — code exists; do not reimplement the feature

| Acceptance gate | What to check / evidence |
| --- | --- |
| **PL0–PL3 vegetation** | [PL3 validation](WORLD/Content/Vegetation/Plants/PL3_Validation.md): PL3 reported source checks, **not actual native Godot builds**. Run target `dotnet build`/import, PL1/PL3 fixtures, F6 World Oak/Pink, artwork anchors and 1-to-many visual variants, wind/snow, load/unload/revisit and P4 compatibility. |
| **H0–H4 / UI0–UI3 production harvesting + inventory** | [H4 validation](SYSTEMS/Harvesting/H4_World_Validation.md): source + direct compilation and **33 write / 28 read / 10 migration** automated checks reported; no Windows graphics acceptance. Test F6 real tree drops/pack/hotbar, F5 New/Continue, save mid-fall, depleted trees, full backpack, standing/settled idle cost and logs across chunk boundaries. |
| **P5a–P5c oceans + WG0–WG3** | [P5c validation](WORLD/Generation/Liquids/Oceans/P5C_Validation.md): ocean test reports **464,877 checks** under Linux Godot 4.5.1; project target Godot 4.7.2 still requires real build, F7 map fit at different resolutions, coastline/non-land clipping, coastal/deep-sea streaming/nav, startup/memory/FPS and old/new campaign Continue. |
| **B0 storm control** | [B0 validation](ENVIRONMENT/Weather/Snow/B0_Validation.md): **native checks not run**. Compile and run `ValidateBlizzardState`, `ValidateSnowSleeping` and rain/wind regressions. F9 test 0, 0.65, 1, 2, 3 and on/off/reactivation. B0 is state only; no visual whiteout before B1/B2. |
| **Cross-phase SAVE compatibility** | **High priority:** H4 schema-3 legacy inventory migration and P5c frozen-size/map bridges do *not* eliminate PL3's deliberate **scenery generator-v2** rejection of incompatible v1 campaigns. Test actual saved files for accepted and rejected versions without changing their stored world identity, generator or hashes. Never delete saves or weaken rejection to claim compatibility. |
| **GS0–GS5, SE0–SE3** | Layered Dirt/rock variants and F9 surfaces; seasonal mask/palette/weather/fire integration. Check native shader/atlas visuals, seams, appearance reset, idle cost and user acceptance. GS6 is pending QA; SE4 is genuinely new year/calendar work. |
| **Combat / ES / Ranged** | C0–C3.3, ES1/ES3, R0/R1 and Basic/Archer content are implemented to their noted scope, not all graphically accepted. Check melee timing, skeleton crowd movement, particles, projectiles, cooldowns and broader known native teardown failures. |
| **LL0/LL1, FIRE1/FIRE2, SO2** | Verify normal World local shadows/occluders, burning tree life cycle, streaming cleanup, root Y-sort, visual stability and Windows frame pacing. |

**Test provenance:** Some validation suites used **direct .NET 8 Roslyn + Godot 4.5.1 Linux** when the target 4.7.2 SDK/editor was unavailable. PL3 and B0 explicitly reported **no native runtime execution** in their implementation environment. Do not equate these records with passing the combined current World on Windows, or claim FPS improvements from source inspection.

## 3. Documentation drift — cleanup, not new gameplay

Keep document corrections separate from implementation. The [original D01–D18 audit](https://github.com/ben0kay/isometric-fantasy-2d/blob/539f667fed9a0dabd55cfd998a41bc6f425affd6/IMPLEMENTATION_BACKLOG.md) remains for detail. Do not treat a superseded historical paragraph as current implementation status.

| Area | Documentation currently requiring reconciliation |
| --- | --- |
| **HUD/player inventory** | `UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md` still says UI3 production integration and inventory persistence are missing; **H4 delivered real `PlayerInventory` and schema-3 saves**. `UI/Inventory/PLAYER_INVENTORY.md` is still vision-only and must not be read as a current source audit. |
| **Combat and species** | `COMBAT/README.md` and `COMBAT/Projectiles/README.md` understate R1 Archer integration; Species/Undead entry READMEs still call the category scaffold despite Basic + Archer. C3 overview contains older slices described prospectively. |
| **Water and world** | Older WG0/WG1/WG2 and liquid domain READMEs predate WG3/P5a-b/**P5c ocean**; the large generation plan has historical pre-P4/P5 sections. Leading current status should distinguish ocean portion delivered from volcanic remainder. |
| **Terrain and environment** | Ground surface docs include successive SVG/procedural/grayscale PNG iterations: latest Dirt variants lead. Fire README still says F5 launches DEBUG; actual F5 is Boot and F6 runs a selected scene. |
| **Player and scene organization** | Player README mentions inventory as unimplemented, now superseded by H4. Historical SO0/SO1 node hierarchies and generic environment/lighting notes should remain labelled as earlier snapshots. |

**Priority:** fix misleading owner READMEs when their modules are next edited. Don't hold up tested feature work merely to rewrite all historical documents. This backlog edit intentionally changes only this file.

## 4. Implemented phases removed from active queue

- **World / water:** P0–P4, P5a/P5b, **P5c ocean portion and world expansion**, P3 streaming, WG0–WG3, GS0–GS5, SO0–SO2. **P5c volcano, P5d/e and WG4–7 remain.**
- **Vegetation:** shared Oak/Pink Tree, **PL0–PL3**, eight separate Oak plant definitions and real World spawn. **PL4, GR0, RK0 and Pine authoring remain.**
- **Inventory / harvesting:** item catalog, shared storage/hotbar, **UI0–UI3 core**, **H0–H4** production felling, physical pickups and schema-3 save/restore. **H5 expansion and UI4/UI5 remain.**
- **Combat / actors:** C0–C2.5, C3.2, C3.3a–d, AC0–AC4, ES0/ES1/ES3, R0/R1; shared health bars, C3.1 damage-number code.
- **Environment:** FIRE1/FIRE2, LL0/LL1, SE0–SE3, ordinary rain/snow/wind and **B0** storm state. **B1–B4** and automatic SE4/calendar still planned.

**Source implemented ≠ tested on the target SDK ≠ visually accepted.** See Section 2 for outstanding acceptance work.

## 5. Coverage and update discipline

**Latest GitHub source/commit comparison (25 October 2026):** current changes since the previous backlog are `63c3071` (PL3), `f92d804` (H4 and real player inventory), `c3a88dc` (P5c oceans and large world), and `f52b29f` (B0). The previous recursive tree at `c3a88dc` had **1,873 tracked entries, including 404 Markdown and 327 C# files**, without truncation; B0 adds its own validation, profile, runtime and tests. Current plan status and implementation assertions here were cross-checked against updated feature plans, validations, changed file lists and selected owner code/Resources.

**Limits:** This is a source-and-documentation reconciliation, **not** a new complete semantic audit of all Markdown or a successful Godot/Windows build. No engine test, campaign migration or FPS profiling was performed while editing the backlog. If a recent implementation has a test record, it is identified as reported evidence for that specific revision and environment. Archive/idea-only material remains non-authoritative for implementation work.

**Maintenance:** after an authorized phase, update its own README, plan and test evidence first; then remove the delivered phase from Section 1 and add any remaining target-PC acceptance to Section 2. For a partially delivered phase (P5c ocean vs volcanic remainder), list **only** the unfinished portion. Keep one GitHub commit per assigned job and never begin another phase automatically.
