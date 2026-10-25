# Implementation Backlog — Unfinished Work Only

> **Reconciled against GitHub `main` on 25 October 2026 (baseline `539f667`).** This is the **actionable unfinished-work queue**, not an instruction to execute every plan. It replaces the broad [24 October documentation audit](https://github.com/ben0kay/isometric-fantasy-2d/blob/539f667fed9a0dabd55cfd998a41bc6f425affd6/IMPLEMENTATION_BACKLOG.md); that historical inventory remains in Git history. **Implemented phases are removed from the active implementation lists below.** Implementation, native testing, Windows visuals/performance and user acceptance are distinct statuses.

## Working rule — finish a vertical slice before starting another

**Recommended focus (not automatically authorized):** make the **existing Oak plant content actually spawn and behave in production World** before adding more species, world systems or cosmetic plans. Then pick **one complete harvesting/inventory/persistence loop**, rather than separately expanding half-finished harvesting, item storage, UI and saves.

1. **PL3 — production vegetation hookup.** PL0 audit and PL1 shared `Plant.tscn`/controller exist; PL2's eight individual `plant_oak_XXX.tres` definitions and Oak-only biome entries are authored. **They do not yet spawn in normal World**: the streamed and preview generators still dispatch trees or PackedScene variants. PL3 must wire the shared `VegetationDefinition.Template + VisualVariants` path, existing wind/snow, stable generation, biome exclusions and saving compatibility. This is the highest-leverage immediate blocker.
2. **Validate PL1–PL3 in Godot on the target PC.** Check that plant Resources import, Oak plants are visible, Pink Meadow never receives them, wind/snow and chunk unload/revisit work, grass/trees/rocks remain correct, and existing campaign saves are rejected safely or migrated deliberately if their content fingerprint differs. **PL2 already changed Oak grass density 60 → 45**; after PL3, the authored total is 102 per million at a candidate capacity of ~108.5.
3. **Finish UI3's production storage/equipment/save ownership together with H4.** Actual `ItemDefinition`, inventory storage/hotbar, the HUD/view binding and physical pickup/harvesting **exist in disposable practice**, not persistent campaigns. UI3 requires authoritative player ownership, 1–9 binding and versioned inventory/hotbar/equipment saves. H4 requires durable harvested-source/drop states in P3/P4 and production enablement. Coordinate their contracts and sequence deliberately; neither is done just because the practice scenes work.
4. **After that, choose a single gameplay vertical slice:** PL4/H5 plant gathering, tree felling in production, or other harvestable content. Do not simultaneously start blizzards, new biomes, underwater movement, more enemies and scene-tree reorganization.

**Workflow:** Regular Chat can maintain planning, Resource/data authoring and small documentation edits; Codex owns cross-system implementation; Copilot/VS Code can run local .NET/Godot tests; user provides rendered/FPS/play-feel acceptance. One approved phase/job and one Git commit at a time. See [CodePreferences](CodePreferences.md) and [root README](README.md).

## 1. Unfinished production/gameplay work — actionable queue

Rows labeled **Planned** lack the completed behavior specified by that phase; **Partial** means scaffolding, practice behavior or preparation exists but the phase is not delivered in production; **Deferred** means a real future design, not an immediate approved job. Priority reflects suggested focus, **not automatic permission**.

### A. Finish the world-content and gathering loop first

| Ticket | Priority / state | Exactly what remains | Owning source / dependency |
| --- | --- | --- | --- |
| **PL3** | **FOCUS · Planned** | Connect `VegetationDefinition` visual variants + shared `Plant.tscn` to streamed `ChunkSceneryData`/`WorldChunkStream` **and** legacy `WorldSceneryPlacement`. Validate real World, seed/revisit, bounds, exclusions, weather/streaming, P4 compatibility. | [Plants plan](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [PL0/1 evidence](WORLD/Content/Vegetation/Plants/PL0_PL1_Validation.md) |
| **UI3 (remaining)** | **NEXT · Partial** | Practice-scene item storage/hotbar/view binding exists; **normal World player owner, equipment integration, key migration and durable campaign item/hotbar state do not**. Preserve the intended backpack design; don't rebuild the existing models/HUD. | [HUD plan](UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md), [item foundation](SYSTEMS/Inventory/ITEM_FOUNDATION_VALIDATION.md), [inventory design](UI/Inventory/PLAYER_INVENTORY.md) |
| **H4** | **NEXT · Planned** | Production tree chopping and physical pickups with saved progress, felled/removed trees, settled dropped items and inventory; P3 chunk unloading/reloading + P4 checkpoint restoration without reward duplication. H1–H3 are *fixture-only*. Requires coordinating UI3/player inventory persistence. | [Harvest plan](SYSTEMS/Harvesting/HARVESTING_IMPLEMENTATION_PLAN.md), [validation](SYSTEMS/Harvesting/H0_H3_Validation.md), [drops](SYSTEMS/Drops/README.md) |
| **PL4 + H5** | **AFTER H4 · Deferred** | Enable data-driven gathering/harvest rewards for **individual plants**, then selectively expand shared harvesting/drops to rocks, mining, enemies, containers and other sources. Reuse existing `Harvestable`, `HarvestProfile`, `DropTable`, `WorldDropSpawner` and stable world IDs; no duplicate systems. | [Plant PL4](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [H5](SYSTEMS/Harvesting/HARVESTING_IMPLEMENTATION_PLAN.md) |
| **GR0** | **LATER · Planned** | Refactor the existing four grass Tuft scenes into **one `Grass.tscn`**, retaining one `Grass.tres` with its four cosmetic artwork variants, stable ID, biome populations, wind/snow and save compatibility. Use PL1's reusable mechanism only after PL3 is proven. | [Plants contract/GR0](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [grass](WORLD/Content/Vegetation/Grass/README.md) |
| **RK0** | **LATER · Planned** | Refactor current `Rock01.tscn` to **one shared `Rock.tscn`** plus distinct rock-type definitions, each with one/many textures and variant collision/light-occluder geometry; preserve physics/shadows. | [Plants contract/RK0](WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md), [rocks](WORLD/Content/Rocks/README.md) |
| **Pine species** | **LATER · Content** | Pine PNGs are staged **art only**. Create the Pine tree definition, rooted variants/collision, optional effects, and explicit biome population; reuse shared `Tree.tscn`. This is not the same as implementing a Pine biome. | [Pine README](WORLD/Content/Vegetation/Trees/Pine/README.md) |

**Avoid a content-model regression:** one shared scene per category; one `.tres` per distinct plant/tree/grass/rock type; one or more PNG/atlas variants **inside that definition**; explicit per-biome `BiomePopulation` references with independent densities. The eight Oak plants are eight distinct types, not one aggregate Plants.tres. A future Pine biome can select some of those same Resources; volcanic plants appear only when their own biome references them.

### B. Procedural world, water and biome populations

| Ticket | Priority / state | Exactly what remains | Owning source |
| --- | --- | --- | --- |
| **WG4** | **AFTER CORE · Planned** | Shores, shallows, banks and terrain/ground blending around **already generated** lakes and rivers. Don't regenerate WG1–WG3 water or duplicate the fixed pond. | [Water plan](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **WG5** | **OPTIONAL · Planned** | Elevation-aware drainage, tributaries, wetlands; no elevation system currently owns this yet. | [Water plan](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **WG6** | **LATER · Planned** | Data-driven river/stream current mechanics and directional flow presentation, shared across streamed bodies. | [Water plan](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **WG7** | **LATER · Planned** | Deep swimming/floating and opt-in underwater transitions. Must reconcile existing RMB hop/guard and terrain/wading controls. | [Water plan](WORLD/Generation/Liquids/WATER_GENERATION_PLAN.md) |
| **P5c** | **LATER · Planned** | Major oceans/non-land and volcanic geography providers, using the **existing geography-first pipeline** rather than inventing a second biome generator. | [Generation plan](WORLD/Generation_Implementation.md), [P5a/b results](WORLD/Generation/World/Geography/P5AB_Validation.md) |
| **P5d** | **LATER · Planned** | Prioritized macro POIs, access routes/reservations, paths/crossings, cave approaches and location-scene connections **before biome assignment**. | [Generation plan](WORLD/Generation_Implementation.md) |
| **P5e** | **QA WHEN EXPANDED · Planned** | End-to-end validation of combined geography, POIs, frozen saves, chunk streaming and performance as new providers land. | [Generation plan](WORLD/Generation_Implementation.md) |
| **BP0–BP1** | **DESIGN · Planned** | Audit and optional authoring of biome-linked climate, living entity and resource/deposit spawn profiles. **Existing scenery `BiomePopulation` is already implemented**; do not replace it or create redundant plant spawners. | [Oak spawning proposal](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) |
| **BP2–BP4** | **LATER · Planned** | Persistent resource/deposit placement, bounded live enemy/wildlife populations and optional F9 spawn diagnostics/validation. No generic resident spawning system exists yet. | [Oak spawning proposal](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) |
| **BP5 / P6** | **DEFERRED** | Persistent populations and abstract *offscreen* A-Life. Avoid keeping fully active entities across the world. | [Biome spawning](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md), [P6](WORLD/Generation_Implementation.md) |
| **GS6** | **QA · Planned** | General layered-ground regression and target renderer acceptance: overlay visuals, seams, performance, F9, campaign Continue. **GS0–GS5 implementation already exists**; do not redo those phases. | [Ground plan](WORLD/Terrain/Ground/Surfaces/LAYERED_GROUND_IMPLEMENTATION.md) |
| **Caves/locations** | **DEFERRED · Scaffolding** | Underground domain, biome influence, cave entrances and actual dungeons/settlements/camps/landmarks. Coordinate with P5d/GL before implementation. | [Caves](WORLD/Caves/README.md), [Locations](WORLD/Locations/README.md) |
| **Other liquid types** | **DEFERRED · Expansion** | Lava/toxic/etc. definitions and specialized visual/gameplay responses. **Existing fixed pond, canonical water region/wading, streamed lakes and rivers are implemented**, not missing foundations. | [Liquid domain](WORLD/Liquids/README.md) |

**Already true in production:** P5a/P5b implement reservation → lake/river geography → constrained biome generation, and normal F6 World/New Campaign enables three lakes and one river. Generated water bodies and cross-biome rivers have implementation/automated evidence. Remaining shore/current/swimming work is **not** grounds to reimplement the existing planner or streamer.

### C. Combat, enemy movement and feedback

| Ticket | Priority / state | Exactly what remains | Owning source |
| --- | --- | --- | --- |
| **R2** | **WHEN COMBAT FOCUS · Planned** | Archer/projectile feel, telegraph polish, F9 ranged controls, mixed encounter and target-PC validation. **R0 shared arrows and R1 skeleton archer are already implemented**; the World documentation describes mixed Basic/Archer practice, so audit its exact delta before adding another encounter. | [Ranged plan](COMBAT/Projectiles/RANGED_IMPLEMENTATION.md), [R1 checks](COMBAT/Projectiles/R1_Validation.md) |
| **ES2** | **LATER · Planned** | Species-driven separation presets and tests; `SeparationProfile.cs` and `EntityDefinition.Separation` **already exist from ES1**, so tune/extend rather than recreate. | [Entity Separation](ENTITIES/Shared/Movement/EntitySeparationPlan.md) |
| **ES4** | **QA · Planned** | Multi-enemy crowds/corridors, stream/unload, collision and combat regression plus actual CPU/frame-time profiling. **ES1 local separation and ES3 engagement positioning are implemented**. | [Entity Separation](ENTITIES/Shared/Movement/EntitySeparationPlan.md) |
| **C3.4** | **LATER · Planned** | Distinct valid-hit/blocked/missed/guard-break feedback attached to authoritative combat outcomes. Do not rebuild existing particles or health bars. | [Combat C3](COMBAT/C3_Implementation.md) |
| **C3.5** | **OPTIONAL · Planned** | Capped camera shake and shared impact/attack audio, after common audio ownership is agreed. | [Combat C3](COMBAT/C3_Implementation.md) |
| **C4** | **LATER · Planned** | Tune movement, recovery, attack feel around existing sprint/dodge/guard/knockback; no second player motor. | [Combat plan](COMBAT/COMBAT_IMPLEMENTATION.md) |
| **C5** | **LATER · Planned** | One complete repeatable skeleton encounter: telegraphs, mix/balance, state, visual/Windows acceptance. | [Combat plan](COMBAT/COMBAT_IMPLEMENTATION.md) |

**Remove from implementation backlog:** C0–C2.5, C3.2 and C3.3a–d; R0/R1; ES0/ES1/ES3; existing shared health bars, damage numbers and hit impacts. Their **remaining graphical/performance verification** is a separate section below.

### D. Environment, seasons and weather

| Ticket | Priority / state | Exactly what remains | Owning source |
| --- | --- | --- | --- |
| **B0–B2** | **LATER · Planned** | Blizzard-specific state/F9 controls, very heavy layered directional snow, blowing ground snow, overcast lighting, thick haze and distance whiteout. **Normal snow/wind/rain/coverage already exist**; reuse them. | [Blizzard B0–B4](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **B3–B4** | **OPTIONAL · Planned** | Lens/on-screen snow, icy screen edges, storm polish and performance acceptance. Not an implemented HUD effect yet. | [Blizzard plan](ENVIRONMENT/Weather/Snow/BLIZZARD_IMPLEMENTATION.md) |
| **SE4** | **DEFERRED · Planned** | Real calendar, automatic year progression, climate/season interactions and persisted gameplay year. **SE0/SE1/SE1b/SE2/SE3 are implemented**, with varying native/visual checks still pending. | [Seasons plan](ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md), [SE3 results](ENVIRONMENT/Seasons/SE3_Validation.md) |
| **LL2** | **LATER · Planned** | General reusable light-type profiles/controllers beyond currently implemented standing torch + local occluders. | [Local lights](ENVIRONMENT/Lighting/LocalLights/README.md) |
| **LL3** | **LATER · Planned** | Stream/cap local light sources, shadow budgets, registration/lifecycle and performance on large worlds. | [Local lights](ENVIRONMENT/Lighting/LocalLights/README.md) |
| **Regional climates/weather** | **DEFERRED** | Change/resolve environment state per biome/region, real temperature/humidity and scheduled weather; current biome Resources are authored but runtime profile selection does not yet blend by region. Coordinate with BP1/SE4. | [Environment](ENVIRONMENT/README.md), [Oak spawning proposal](WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) |
| **Fire expansion** | **DEFERRED** | Tree-to-tree spread, lightning-initiated ignition, gameplay damage or offscreen fire rules. **FIRE1/FIRE2 ignition/burning/charred visuals are already implemented.** | [Fire](SYSTEMS/WorldInteractions/Fire/README.md) |

### E. World structure, UI polish, and larger future systems

| Ticket | Priority / state | Exactly what remains | Owning source |
| --- | --- | --- | --- |
| **UI4** | **AFTER UI3 · Planned** | Inventory/HUD responsive organic visuals, focus, tooltip/action UX, layout and accessibility polish. UI1/2 already supply the HUD and nonpausing overlay. | [HUD plan](UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md) |
| **UI5** | **OPTIONAL · Planned** | Status/buff rail, toasts and alerts. Do not add more screens before actual item/gameplay binding unless explicitly desired. | [HUD plan](UI/HUD/Gameplay/PLAYER_UI_IMPLEMENTATION_PLAN.md) |
| **SO3/SO4** | **OPTIONAL · Proposed** | Lightweight runtime registries/diagnostics and location/session compatibility only if justified. **SO0–SO2 scene ownership is implemented**. | [Scene organization](SCENES/World/scene%20organize.md) |
| **GL0–GL3** | **DEFERRED · Proposed** | General location/scene sessions, interiors/caves and transitions; coordinate with P5d and future P4 saves, not a second WorldScene. | [Scene organization](SCENES/World/scene%20organize.md) |
| **SAVE expansions** | **DEPENDENCY / LATER** | Versioned inventory/hotbar/equipment + H4 source/drop state first; later seasons, caves, A-Life, migrations, extended save UI/archive/large-save performance. **P4 core campaigns and Continue are implemented and previously user-accepted**. | [SAVE](SAVE/README.md), [H4](SYSTEMS/Harvesting/HARVESTING_IMPLEMENTATION_PLAN.md) |
| **Crafting, farming, building, cooking, fishing, skills, survival** | **DEFERRED · Scaffolding** | Actual mechanics and owned content, not just folder READMEs. Harvesting itself is **partially implemented in H1–H3**, so do not describe all gathering systems as absent. | [SYSTEMS](SYSTEMS/README.md), [player survival](ENTITIES/Player/Survival/README.md) |
| **Other NPC/wildlife/faction/AI systems** | **DEFERRED · Scaffolding** | Additional species, roles, ecology, per-biome residents, reusable factions/registries. Skeleton Basic and Archer and shared Entity are existing exceptions. | [ENTITIES](ENTITIES/README.md), [Species](ENTITIES/Species/README.md) |
| **Audio** | **DEFERRED · Scaffolding** | Shared mixer/music/ambient audio and cues for actual gameplay, starting when a chosen vertical slice needs them. | [AUDIO](AUDIO/README.md) |
| **Tooling / Avalonia authoring integration** | **OPTIONAL · Design** | External editing of canonical Resource definitions and validation, not an alternate runtime catalog. | [ITEMS](ITEMS/README.md), [environment](ENVIRONMENT/README.md) |

## 2. Validation/acceptance queue — NOT missing implementation

Keep these visible but **do not create duplicate features** merely because they are not accepted on Windows.

| Validation gate | Implemented code / what must still be checked |
| --- | --- |
| **Plant PL1/PL2 → PL3** | PL1 controller/variant contracts and PL2 data exist. Native Godot 4.7.2 import/build, eight Resources and bounds, and future PL3 production spawn/stream/wind/snow/compatibility require testing. Oak biome changes may reject old campaign fingerprints. |
| **P3/P4/P5a-b + WG1–WG3** | Source/headless water/geography generation and production defaults exist. Windows river/lake appearance, bank clipping/seams, frame pacing/stream hitches, long traversal and separate-process Continue need target-PC verification. Do not mark WG0–3 or P5a/b unimplemented. |
| **Item foundation / UI1–UI3** | Storage/hotbar + inventory display bind in a disposable fixture; UI1/UI2 HUD/overlay are in normal World. Native UI readability/selection/input checks remain. UI3 production persistence is **feature work**, not merely QA. |
| **H1–H3 drops and tree falling** | HarvestingPractice implements real chops, hops and pickups and passed focused headless checks. Windows feel, spread, fade, collection and render/performance checks remain; **H4 production persistence is still a new phase**. |
| **GS0–GS5** | Layered ground and optional F9 overlay controls implemented. Verify current grayscale Dirt variants (ignore historical SVG/Dust summaries), seam visuals, off/preview modes, performance and save compatibility; GS6 owns this closure. |
| **SE0–SE3 / normal snow/rain** | Seasonal masks/year-position preview/wet/snow/fire composition and SE3 mask audit exist. Native target-engine integration/rendered foliage checks pending. Blizzard B0–B4 and real SE4 year are new features, not acceptance steps. |
| **Combat / ES / Ranged** | C2/C2.5, C3.2/3.3, ES1/ES3, R0/R1 and Basic/Archer code exist; specific headless suites were run in their own validation records. Target-PC attack feel, crowds, ranged projectile visuals and known broader native teardown/test failures remain. C3.1 damage-number code needs current build/visual acceptance, not a new module. |
| **LL0/LL1, FIRE1/2, SO2** | Existing local torches/occluders, fire rendering and scene ownership are present. Target-PC shadows, effect costs, tree/sort overlap, UI/teardown and streaming cleanup remain checks, not instructions to rebuild. |

**Do not claim measured FPS or clean Godot 4.7.2 compilation from a documentation/source audit.** Older reports sometimes used Godot 4.5.1 plus a direct Roslyn build because the standard target SDK/editor was unavailable. Respect each validation record's actual tested version. If user has Copilot test locally, capture commands/exit codes/screenshots/profiler evidence and update the owning plan.

## 3. Documentation drift — separate from implementation queue

These are **documentation repairs**, not reasons to rewrite working code. The previous detailed D01–D18 list is retained in the [prior audit snapshot](https://github.com/ben0kay/isometric-fantasy-2d/blob/539f667fed9a0dabd55cfd998a41bc6f425affd6/IMPLEMENTATION_BACKLOG.md). Recheck current HEAD before fixing any owner doc; do **not** carry closed/no-action D17 forward as a task.

| Group | Known example / correction to make when documentation cleanup is approved |
| --- | --- |
| Combat/ranged | [COMBAT overview](COMBAT/README.md) and [projectile overview](COMBAT/Projectiles/README.md) lag behind R1 Archer; distinguish R0/R1 implemented from R2 planned. Some C3 overview headings are historical despite completed shared particle/impact work. |
| Species/player | [Species](ENTITIES/Species/README.md), [Undead](ENTITIES/Species/Undead/README.md) and [Player](ENTITIES/Player/README.md) still describe obsolete skeleton/scaffolding or inventory baselines. Basic+Archer and inventory fixture foundations exist; production inventory still does not. |
| UI/inventory | [Player inventory design](UI/Inventory/PLAYER_INVENTORY.md) says no inventory UI/storage exists; it is an **older vision document**, not a current implementation status. Preserve design intent while linking to UI1/UI2 and item-foundation evidence. |
| Water/world | Old [WG0/WG1 validation](WORLD/Generation/Liquids/WG0_WG1_Validation.md), [WG2 validation](WORLD/Liquids/WG2_Validation.md), [Liquid domain README](WORLD/Liquids/README.md) and large [generation plan](WORLD/Generation_Implementation.md) contain older snapshots; current WG3/P5a-b/defaults are newer. Clearly label history, do not duplicate work. |
| Terrain/environment | [Ground surface README](WORLD/Terrain/Ground/Surfaces/README.md) contains multiple chronological texture descriptions; latest Dirt PNG variant state should lead. [Fire README](SYSTEMS/WorldInteractions/Fire/README.md) still claims F5 launches DEBUG although root README confirms F5 Boot and F6 scene runs. |
| Scene/local light/other | Historical SO0/SO1 trees, GS status, older seasonal plan handoffs, lighting overview and F9 descriptions need date-scoped summaries when their owning docs are next touched. Do not turn merely old text into new features. |

**Documentation priority:** fix misleading entry-point READMEs when working in those folders; avoid burning an entire implementation cycle on 18 independent wording edits before PL3. The root README's production-status table is also dated and should eventually be refreshed from owner plans, but is **not** changed by this backlog-only audit.

## 4. What was removed from the active development list

The following **implemented foundations/phases are deliberately not tickets anymore** (acceptance can still be pending):

- **World:** P0–P4, P5a/P5b, P3 chunk streaming, SO0–SO2; WG0–WG3 generated lakes/rivers; GS0–GS5 ground layers and authored PNG variants.
- **Vegetation:** shared Oak/Pink `Tree.tscn`; PL0 audit, PL1 shared plant scene/Resource contract, and PL2's eight `plant_oak_XXX.tres` + Oak population authoring. **PL3 remains unfinished.**
- **Gameplay foundations:** item catalog + reusable InventoryStorage/InventoryHotbar and fixture binding; UI0–UI2 HUD/inventory overlay; H0–H3 harvest progress, shared physical drops and fixture tree felling. **UI3 production and H4 remain unfinished.**
- **Combat/AI:** C0–C2.5, C3.2, C3.3a–d, AC0–AC4, ES0/ES1/ES3, R0/R1; existing combat health bars and C3.1 damage-number source.
- **Environment:** FIRE1/FIRE2, LL0/LL1, SE0–SE3, normal precipitation/wind/lighting and shared weather cosmetic sleeping improvements. **SE4 and blizzards remain unimplemented.**

An implemented foundation is **not** an accepted full feature. The distinction between fixture-only, production-integrated, native-tested and visually accepted is intentional.

## 5. Audit coverage and update rules

**Verified on 25 October 2026:** the GitHub recursive tree at `539f667` returned **1,844 tracked entries, including 400 Markdown files** and 318 C# files (not truncated). This removes the earlier audit's limitation of not being able to enumerate all file paths. I examined the root README/preferences, the previous backlog, current phase plans, owner READMEs, validation notes, and selected implementation classes/scenes/configs across vegetation, inventory, UI, harvesting/drops, water/geography, ground, combat/AI, seasons, snow and local lights.

**Limits:** A complete 400-document semantic rewrite and every source-level runtime path were **not** checked, and no Godot build, save migration, Windows renderer or performance test was run during this backlog edit. Source files and validation reports establish **reported implementation and contract existence**, not universal bug-free behavior. Archived `Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/` are not promoted to active work. The optional/idea-only notes are not auto-approved tickets.

**Maintenance:** After a phase is coded, the implementation owner should update its feature plan + validation record; then **remove that phase from Sections 1A–1E** and keep only genuinely open production tasks or Section 2 acceptance. New proposals belong in their owning folders and enter this queue **only with an explicit phase/status**. Preserve the exact planned/implemented/awaiting validation/accepted boundary, a dated reference and commit SHA. **Do not start the next phase from this list without user approval.**
