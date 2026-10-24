# Oak Forest — Biome-linked Spawn Configuration Proposal

> **Status: PLANNED / DISCUSSION ONLY (24 October 2026).** This file proposes a future data layout for Codex review. **No spawn folders, spawn .tres Resources, C# code, scenes or existing biome Resources are created or modified by this documentation task.** User approval is required for each implementation phase. Read root `README.md`, `CodePreferences.md`, `WORLD/Biomes/README.md`, current `Biome.tres`, `WorldBiomeDefinition.cs`, `BiomePopulation.cs`, and P3 streaming before implementing.

## Confirmed current Oak files / behavior

- `Biome.tres` **already exists**, is the `WorldBiomeDefinition` root and references `Environment.tres`. **Do not create another Environment.tres.**
- `Environment.tres` **already exists**, references `Atmosphere.tres`, and uses the shared `BiomeEnvironmentProfile`. Region-by-region environment switching/blending is not yet implemented.
- `Biome.tres` already holds an **inline GroundStack** with Oak base grass, accompanying patches, and disabled Dirt/Rock preview overlays. Preserve the exact existing appearance.
- `Biome.tres` already has three **inline BiomePopulation** entries for canonical Oak, Rock and Grass Resources, with streaming densities **26, 11, and 60 instances per million square world units** respectively. Preview Count and ClusterSize are not streamed density controls.
- P3 currently uses a deterministic candidate lattice, biome-based static scenery placement, protected reservations, persistent session deltas and chunk lifecycle. **Do not duplicate this generator or turn static scenery into dynamic entity spawning.**
- The shared `ENTITIES/Shared/Entity.tscn` + `EntityDefinition` is the foundation for creatures, including Skeleton Basic/Archer. Keep creature definitions in `ENTITIES/`, not duplicated under OakForest.

## Proposed future folder layout

```text
WORLD/Biomes/Definitions/OakForest/
├── Biome.tres                   # EXISTS — root biome Resource
├── Environment.tres             # EXISTS — referenced by Biome.tres
├── Atmosphere.tres              # EXISTS — referenced by Environment.tres
├── Climate.tres                 # PROPOSED — gameplay temperature/humidity baseline
├── Spawning/                    # PROPOSED — do not create until BP1
│   ├── README.md                # PROPOSED — explain ownership
│   ├── Entities.tres            # PROPOSED — entity population/spawn rules
│   └── Resources.tres           # PROPOSED — harvestable/ore deposit placement rules
└── SPAWNING_IMPLEMENTATION_PLAN.md   # THIS FILE
```

No new `Ground.tres` is needed: GroundStack is currently inline in `Biome.tres`. Moving it would be a separate, optional organization task and might change Resource references/fingerprints.

### Reference design

```text
Biome.tres (WorldBiomeDefinition)
  ├─ Environment → existing Environment.tres
  ├─ Climate → proposed Climate.tres (gameplay climate data)
  ├─ GroundStack → existing inline stack
  ├─ Populations → existing inline Oak / Rock / Grass (KEEP)
  ├─ EntitySpawns → proposed Spawning/Entities.tres
  └─ ResourceSpawns → proposed Spawning/Resources.tres
```

The three new fields (Climate, EntitySpawns and ResourceSpawns) are **proposed**, not current `WorldBiomeDefinition` properties. Use optional nullable Resource references with no behavior change when absent; ensure adding them does not accidentally invalidate existing campaign saves. Confirm compatibility/fingerprint rules before adding fields or modifying `Biome.tres`.

## Ownership and proposed configuration

### Climate.tres — gameplay temperature and humidity (core planned scope)

Climate is **gameplay data**, not merely a colour/atmosphere effect. Propose a typed, reusable `BiomeClimateProfile` Resource, linked from `Biome.tres`, with a per-biome **baseline temperature** (degrees Celsius) and **baseline relative humidity** (0–100%). Optional min/max ranges or seasonal/day-night offsets may be added after BP0 identifies existing clock, weather and seasonal contracts. Use explicit units, validated ranges and clear Inspector export groups. **Do not confuse humidity with soil moisture or current rainfall**: they are related but distinct values.

- **Temperature:** biome baseline and eventual live regional temperature; future gameplay consumers could include player exposure, survival, freezing/heat, ecology and spawn eligibility.
- **Humidity:** biome baseline and eventual live regional relative humidity; future gameplay consumers could include evaporation, wetness/drying, fire spread and ecology.
- **Resolution:** separate authored climate baselines from dynamic *current* temperature/humidity, which should later combine biome, time, season, weather and locality through one shared world climate service; avoid putting a per-frame simulation inside every biome or actor.
- **Boundaries:** specify deterministic sampling and eventual smoothing across region edges; current environment overrides resolve only at startup, so do not claim per-region climate is already live.
- **Compatibility:** null Climate reference must have a defined safe fallback; do not change existing Oak/Pink visuals, weather, seasonal effects or saved-world identity merely by adding the data contract.
- **Performance:** cache sampled climate for active region/chunk and update on relevant region/clock/weather changes at bounded intervals; no full-world per-frame climate pass or per-entity polling.

**Initial illustrative values only:** Oak Forest 15°C / 65% RH. These are design placeholders, **not** measured climate, current authored values or mandatory gameplay balancing.

### Spawning/Entities.tres — living creatures, NPCs, enemies

Biome selects eligible canonical `EntityDefinition` Resources and local population rules. A **shared** world entity population service (not one per biome) performs actual placement and active lifecycle. Proposed per-entry settings:

- Entity definition reference, Enabled, relative **selection weight**, and **population density / target count** as distinct concepts.
- Optional group-size range, maximum active nearby, minimum player distance, preferred spawn terrain, biome-boundary avoidance.
- Optional conditions later: time of day, weather, season, danger level, habitat and player progression. Defer these until the owning systems exist.
- Clear resident/persistent vs roaming/respawning policy; death/removal must not reset simply because a chunk unloads.

**Illustrative design-only weights:** Deer 45, Rabbit 35, Wolf 15, Skeleton 5. These are not authored Resources and do not imply Deer/Rabbit/Wolf exist in the game. Avoid creating placeholder species solely to populate this example.

### Spawning/Resources.tres — deposits and gatherables

Biome selects canonical harvestable/resource definitions; it must not own copied ore/rock/plant assets. Proposed settings:

- Resource definition, Enabled, placement density (prefer area-based units consistent with P3), optional weight when selecting deposit type.
- Spawn surface/ground constraints, biome-edge clearance, minimum spacing, clustering/vein parameters, reservation and terrain/feature exclusions.
- Stable deterministic world seed + coordinates/identity; persist depleted/mined state through SAVE and chunk unload/reload.
- Future ore geology (veins, depth, cliffs, caves, mineral bands) should eventually be controlled by terrain/geology generation. **Do not sprinkle deep ore arbitrarily on grass simply because a biome weight is high.**
- Optional respawn policy only for appropriate gatherables (e.g., herbs), **not automatically for ore**.

**Illustrative design-only types:** stone, copper, iron, rare gold and herbs. Check canonical resource content and item/harvesting foundations before implementing; `ITEMS/` is currently largely scaffolding.

### Existing decorative scenery stays as-is

**Keep Oak trees, existing Rocks and decorative Grass in `Biome.tres → Populations` for now.** They already work with P3's deterministic scenery placement, density and collisions. `Resources.tres` should only introduce gameplay deposits/gatherables when their definitions and persistence exist. Avoid double-spawning the same rock as both scenery and mineable deposit; later conversion of rocks/trees into harvestable entities requires an explicit migration decision.

### Other optional ideas — defer unless useful

- `Spawning/Events.tres`: event-driven ambushes, wandering caravans, migration, rare encounters. Do not add until there is a world event system.
- `Spawning/README.md`: concise ownership and Inspector guide once folder exists.
- Shared C# `BiomeEntitySpawnProfile` / `BiomeResourceSpawnProfile` Resource classes should live in `WORLD/Biomes/Shared/` **or** a clearly owned `WORLD/Generation/` spawn contract folder after BP0 review, **not** inside OakForest. Other biomes should reuse the same types.
- Separate density from selection weights. Respect finite caps and stable RNG for deterministic content; a higher weight is not a guarantee of more creatures.
- Consider F9 **read-only** diagnostics for biome, nearby entity counts, candidate rejection and spawn conditions; avoid permanent polling or creating another overlapping debug panel.

## Phases for Codex discussion

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **BP0** | Audit current biome population, world streaming, Entity placement and SAVE contracts; settle exact field types, climate baseline/dynamic semantics and spawn semantics | Regular Chat | Planned |
| **BP1** | Add optional biome-linked Climate, entity-spawn and resource-spawn profile Resources and Oak files, initially **configuration only**; no new gameplay spawns | Codex (or Regular Chat for isolated Resources after audit) | Planned |
| **BP2** | Integrate deterministic resource/deposit placement with chunk lifecycle and depletion persistence, once canonical definitions exist | Codex | Planned |
| **BP3** | Implement bounded biome-driven active entity population with proper spawn/despawn/death behavior | Codex | Planned |
| **BP4** | Add optional F9 spawn diagnostics and local tests | Codex + Copilot testing | Planned |
| **BP5** | Plan persistent populations, roaming and abstract off-screen A-Life (coordinate with P6) | Regular Chat planning → Codex | Planned |

### BP0 — design and dependency audit
**Scope:** inspect current climate-related weather/clock/season systems and their gameplay consumers, then `WorldBiomeDefinition`, `BiomePopulation`, P3 `ChunkSceneryData` and streamed actor placement, `EntityDefinition`, existing debug/practice skeletons, SAVE world blueprint/fingerprints and persistence. Decide whether Resources.tres should reuse static candidate placement or use a specialized deposit system. **Out of scope:** edits to existing Resources, spawn systems or scenes. **Pass:** short, accurate interface/field design and compatibility strategy, including temperature units, humidity semantics and fallback behavior. **Performance:** identify generation-only work vs active entity budgets and chunk register/unregister. **Recommended:** Regular Chat.

### BP1 — configuration skeleton only
**Scope:** implement reusable typed Resource classes with Inspector groups and optional links from `WorldBiomeDefinition`; add `Climate.tres`, `Spawning/Entities.tres`, `Spawning/Resources.tres`, `Spawning/README.md` to Oak; link from existing `Biome.tres`. Leave arrays empty/default disabled until actual species/deposits are selected. **Preserve:** existing Oak/Pink visuals, populations, environment, GroundStack, save compatibility, P3 performance. **Out of scope:** actual spawning, ore mining, entity AI, unrelated biome conversion. **Pass:** build/import, existing macro/P3/P4 tests, old saves and Oak/Pink appearance unchanged; null links on other biomes remain valid. **Performance:** no per-frame callbacks or extra chunk work when configs are empty. **Recommended:** Codex for multi-file Resource/save integration; isolated documentation/config can be Regular Chat after BP0.

### BP2 — deterministic resource placement
**Scope:** add actual canonical resource/deposit definitions as separately approved, seeded placement with P2 reservations/spacing/biome boundaries, stable IDs and saved depletion. **Out of scope:** complete mining economy or geology/caves P5; do not duplicate P3 scenery. **Pass:** same seed/order yields same sites, chunk return preserves depletion, no collisions at borders, unloaded chunks do no work, saves restore correctly. **Recommended:** Codex.

### BP3 — living entity spawning
**Scope:** biome-weighted active populations, caps, spawn distance, group selection, navigation/collision validation and lifecycle. Reuse `Entity.tscn`, species Resources and existing AI; preserve existing F6 practice actors and authored persistent actors. **Out of scope:** full off-screen A-Life, ecology, combat rewrite. **Pass:** correct biome eligibility, no spawning inside protected/blocked areas, no duplicate respawns on chunk reentry, death persistence policy defined, no per-entity idle spawn polling. **Performance:** shared manager driven by chunk/player-area changes or staggered bounded intervals; sleep when inactive, pool/reuse where justified, release on unload. **Recommended:** Codex.

### BP4 — debug and validation
**Scope:** fit diagnostics into existing F9 category framework; show eligible biome/profile, population cap, nearby live counts, rejection reasons and spawn enable/disable; avoid unnecessary update work while hidden. **Pass:** debug toggles never change saved Resources; disabled system is dormant; tests cover active/idle, spawn, unload, reenter, death, two-biome boundaries, FPS/memory. **Recommended:** Codex implementation, Copilot/user local checks.

### BP5 — future A-Life handoff
**Scope:** propose low-resolution off-screen population state and reconciliation when streamed areas activate, with SAVE integration. **Out of scope:** running full AI/physics across the entire 45k×33.75k world. **Pass:** architecture review before implementation, deterministic restore and bounded update budgets. **Recommended:** Regular Chat planning, Codex implementation only after approval.

## Optional future biome ideas — one-line backlog (not approved work)

- **Terrain.tres:** biome elevation/slope/moisture affinities to guide P5 terrain and region placement.
- **Vegetation distribution:** streamed tree clustering, clearings, undergrowth and spacing while retaining existing P3 populations.
- **Biome tags:** descriptive labels such as Forest, Wet, Dangerous or Magical for future systems without biome-name checks.
- **Locations.tres:** biome-weighted ruins, villages, camps, cave entrances and landmarks integrated with P2 reservations.
- **Biome danger rating:** optional threat tier influencing encounter selection and player progression.
- **Seasonal climate curves:** per-biome annual temperature/humidity variation tied to a future shared seasonal calendar.
- **Regional weather preferences:** rain, snow, fog and storm likelihood using the existing environment/weather system.
- **Geology profile:** mineral bands, depth and ore-vein suitability linked to future P5 terrain/caves.
- **Habitat transitions:** edge blending or overlap rules for entities, vegetation and climate between neighboring biomes.
- **Rare encounter events:** optional biome event profile for ambushes, migrations and traveling NPCs.

## Decisions to discuss before BP1

1. Should `Resources.tres` initially cover only **static ore/deposits**, with herbs/plants deferred until item harvesting exists?
2. Should wildlife/enemies use **persistent residents**, **respawning encounters**, or a configurable mix? This determines death/save behavior.
3. Should biome boundaries permit overlapping habitat zones, or hard membership initially?
4. How should resource deposits intersect with future P5 geology/caves?
5. Should spawn weights be per-biome relative weights, with explicit density/caps to prevent accidental overpopulation? **Recommended yes.**
6. What temperature/humidity baselines and fallback should Oak/Pink start with, and which gameplay system first consumes the values?

**Handoff:** This Markdown file is the only authorized change in the current job. Codex should not implement BP0–BP5 automatically just because this proposal exists. One commit per explicitly approved job; record validation and update owning notes when implementation is authorized.
