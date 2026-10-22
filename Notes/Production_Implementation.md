# Production Implementation — First Playable Oak Forest World

> **Status:** Production foundation implemented. See SCENES/World/README.md for launch/configuration and validation status. The original scope below is preserved as the implementation reference. RandomSeedOnLoad defaults to true; FixedSeed enables deterministic replay.
> **Scope:** Extract/copy proven parts of `DEBUG/TopDownTest/` into the actual production folders, then assemble a minimal playable `SCENES/World/World.tscn`. Preserve the entire existing debug test intact.
> **Read first:** [root README](../README.md), [CodePreferences](../CodePreferences.md), the relevant production-folder READMEs and [DEBUG/TopDownTest/README](../DEBUG/TopDownTest/README.md). Inspect actual scripts, scenes, .tres and shader dependencies before editing.

## Goal and acceptance criteria

Build a **working, independent production World scene** with:
- The 2D playable **player**, input, movement, physics collision and following Camera2D.
- The **same visual Oak Forest grass surface** as the existing DEBUG Oak Forest, including its organic green variation / fine detail.
- Reusable **Oak tree**, **rock** and **decorative grass tuft** scene/content assets.
- Seeded, bounded, collision-aware **placement of trees, rocks and grass**, adapted from working debug code.
- A production **Oak Forest biome definition** referring to shared content and its ground/surface appearance.
- Open `SCENES/World/World.tscn` and run via F6. No requirement to change `project.godot`'s default scene until explicitly requested.

**Keep out of scope:** Day/night, projected dynamic sun shadows, torch/flicker, wind, weather, rain, lightning, snow, lake, wading, seasonal effects, fire/burning, axe/tree damage, harvesting/loot, enemies, survival stats, other biomes, caves, large procedural world geography, chunk streaming and persistence. Existing debug art that expects wind/shadow shaders may need a minimal static presentation fallback; **do not drag the full environment system into production**.

## Canonical production destinations

```text
SCENES/
└── World/
    ├── World.tscn                # composition scene: references the player, ground and placed world content
    └── README.md                 # brief setup / run instructions

ENTITIES/
└── Player/
    ├── Player.tscn               # player actor, collider, camera, sprite and input/movement nodes
    ├── Input/
    │   └── PlayerInput.cs        # copied/adapted from TestPlayerInput.cs
    ├── Movement/
    │   └── PlayerMovement.cs     # copied/adapted from TestPlayerMovement.cs
    ├── Visuals/
    │   └── Sprites/              # production player PNG, if one is available in the repository
    ├── PlayerMasterConfig.cs     # existing production config; extend appropriately
    └── DefaultPlayerConfig.tres  # existing resource; connect real movement/camera fields without duplicating settings

WORLD/
├── Biomes/
│   ├── Shared/                   # common data types / biome logic, NOT individual biome folders
│   ├── Definitions/
│   │   └── OakForest/
│   │       └── Biome.tres        # central biome data that references ground and content
│   └── README.md
├── Content/
│   ├── Vegetation/
│   │   ├── Trees/
│   │   │   └── Oak/
│   │   │       ├── Oak.tscn      # reusable tree scene and rooted collision
│   │   │       └── Sprites/      # oak art and future visual variants (e.g. charred oak)
│   │   └── Grass/                # tuft art and reusable tuft scene(s)
│   └── Rocks/                    # reusable rock scene(s), art, grounded collision
├── Terrain/
│   └── Ground/
│       ├── [ground art/types]    # ground definitions, textures/material references as needed
│       └── Surfaces/
│           ├── Shaders/          # organic patch, fine detail and noise shader modules
│           ├── Noise/            # shared noise resources
│           ├── Profiles/         # reusable ground surface profiles; Oak palette is configured by data
│           └── README.md
└── Generation/
    └── Placement/                # reusable seeded placement of scenery/decor, not a macro world generator
```

**Folder ownership rules:**
- `WORLD/Biomes/Definitions/` holds **all individual overworld biome folders**. `WORLD/Biomes/Shared/` holds common C# definitions/logic. No `OakForest/` sibling directly under `WORLD/Biomes/`.
- Cave biomes remain **separately** under `WORLD/Caves/Biomes/`. Do not move or create cave systems now.
- `WORLD/Content/Vegetation/Trees/Oak/` owns the tree's sprite, scene and collision (and future charred/seasonal variants). Biomes only **reference** content, never copy the Oak asset into their own folders.
- `WORLD/Terrain/Ground/` is the actual ground types and materials; `WORLD/Terrain/Ground/Surfaces/` is the **shared appearance logic** (world-space organic patches, shading, noise, detail and profiles).
- `WORLD/Terrain/Transitions/` eventually owns borders between ground types/biomes; do not confuse those with organic colour variation within a surface.
- Decorative grass **sprites/scenes** live in `WORLD/Content/Vegetation/Grass/`; their **placement logic** lives in `WORLD/Generation/Placement/`. Decorative grass is **not** part of the grass surface shader.
- `SCENES/World/World.tscn` is a composition scene, **not** the home of the reusable player/tree/ground implementations.

## DEBUG source → production target

| Existing DEBUG source | Proposed production target | Notes |
| --- | --- | --- |
| `TestPlayerInput.cs` | `ENTITIES/Player/Input/PlayerInput.cs` | Preserve `Input.GetVector` controls. |
| `TestPlayerMovement.cs`, test player scene nodes | `ENTITIES/Player/Movement/PlayerMovement.cs`, `ENTITIES/Player/Player.tscn` | Preserve CharacterBody2D/MoveAndSlide; separate or configure camera sensibly. |
| Current player artwork | `ENTITIES/Player/Visuals/Sprites/` | Use a production PNG **only if available** in the repository; otherwise use existing test sprite temporarily. Do not assume chat-generated images are committed. |
| `Trees/Tree01.tscn`, `Artwork/tree_01.png` | `WORLD/Content/Vegetation/Trees/Oak/` | Copy the tree, grounded collider and anchor. Avoid mandatory DayNight/wind dependencies. |
| `Rocks/Rock01.tscn`, `Artwork/rock_01.webp` | `WORLD/Content/Rocks/` | Copy the rock, footprint and artwork; avoid snow/debug groups if not used. |
| `Grass/`, grass artwork variants | `WORLD/Content/Vegetation/Grass/` + `WORLD/Generation/Placement/` | Keep object definition/art separate from scatter algorithm. No wind required yet. |
| `Ground/Shaders/GrassSurface.gdshader`, `PatchColours.gdshaderinc`, `FineDetail.gdshaderinc`, `NoiseSampling.gdshaderinc` | `WORLD/Terrain/Ground/Surfaces/Shaders/` | Preserve debug Oak Forest visual result; keep shared shader logic rather than Oak-only copies. |
| `Ground/Noise/GroundNoise.tres`, `Ground/GrassMaterial.tres`, ground config / material wiring | `WORLD/Terrain/Ground/Surfaces/Noise/`, `Profiles/` and production ground scene/material | Maintain correct shader/resource paths; omit DEBUG toggle UI. |
| `Biomes/BiomeDefinition.cs`, `OakForest.tres` | `WORLD/Biomes/Shared/`, `WORLD/Biomes/Definitions/OakForest/Biome.tres` | Adapt existing fields so palette/profile and content references are data-driven. Do not hardcode an Oak tree in generic logic. |
| `Generation/DebugWorldScatter.cs`, associated config | `WORLD/Generation/Placement/` | Reuse seed, bounded retries, prop spacing, scaling, safe collision-footprint mirroring. Remove hardcoded debug path geometry, named West/East biome split, debug liquid references and fixed demo clearing where inappropriate. |

The exact names of some future production scripts/resources may be adjusted to avoid duplicating existing classes or resource names. Verify references/UIDs after copying; Godot C# class names and .tres script references must remain valid.

## Suggested Codex implementation order

1. **Inventory and dependency check.** Inspect the current `SCENES/`, `ENTITIES/Player/`, `WORLD/` and DEBUG files, including root `CodePreferences.md`. Reuse existing folder scaffold and READMEs; don't create parallel owners.
2. **Player first.** Copy input/movement to production, create `Player.tscn`, wire colliders, sprite and following camera. Keep independent of the debug scene; test movement in a simple production scene.
3. **Ground and surface appearance.** Copy the existing Oak Forest grass surface shader/includes, material and noise to `WORLD/Terrain/Ground/Surfaces/`. Add only the minimal production configuration/profile wiring required to make Oak Forest visually match DEBUG, with palette supplied as biome/profile data.
4. **Reusable scenery.** Copy Oak and rock scenes/art/collision into `WORLD/Content/`, and decorative grass sprite definitions into the canonical Grass folder. Keep independent of optional weather/wind/snow/shadow tests.
5. **Biome profile.** Populate `WORLD/Biomes/Shared/` and `WORLD/Biomes/Definitions/OakForest/` with the extracted definition and Oak Forest Resource references; avoid duplicated content assets.
6. **Placement.** Extract minimal seeded scenery placement for a bounded single-biome map, with optional tree/rock/tuft scatter based on biome settings and clear placement exclusions. Do not build chunk streaming or the full world generator now.
7. **Assemble `SCENES/World/World.tscn`.** Use authored base ground dimensions initially if needed; place the production player and references to the production Oak Forest/placement/terrain components. Confirm F6 launch.
8. **Validate and document.** Check C# compile, Godot scene/resource loading, visible same-looking ground, collision, scatter spacing, and absence of DEBUG references in the production scene or dependencies. Update component READMEs to show what is implemented.

## Non-negotiable guardrails

- **COPY, DON'T MOVE OR DELETE DEBUG.** The entire `DEBUG/TopDownTest/` must remain runnable and unchanged unless explicitly authorized.
- **Do not build new gameplay mechanics.** Only adapt wiring/paths, separate couplings and reorganize existing proven code needed for a runnable production world.
- Preserve the Oak Forest's current surface appearance; no speculative replacement graphics or shader redesign.
- **No day/night, environment/weather, snow, water, axe/harvesting, cave or second-biome implementation in this pass.**
- Modular C#, README per major folder, documented responsibilities and inspector-editable master/profile Resources consistent with `CodePreferences.md`.
- Avoid per-frame scatter, redundant material instances and per-object updates where a shared/cached approach suffices.
- **One GitHub commit for the complete requested implementation job**, per project preferences.
- If a useful mechanism cannot be copied safely without a large redesign, leave it in DEBUG and document the dependency rather than quietly expanding scope.

## Completion checklist for Codex

- [x] `SCENES/World/World.tscn` opens and runs.
- [x] Production player moves with arrow keys, collides and is followed by Camera2D.
- [x] Oak Forest ground retains its familiar organic grass colour patches and fine surface detail.
- [x] Oak trees, rocks and grass decorations use reusable production assets and correct anchors/footprints.
- [x] Deterministic startup scenery placement works without debug paths, hardcoded second biome or lake dependencies.
- [x] Oak Forest biome and surface settings are data driven and point to canonical production resources.
- [x] Production files do not depend on `DEBUG/TopDownTest/` paths/classes/resources.
- [x] All existing debug experiments stay intact and runnable.
- [x] Updated READMEs clearly distinguish newly implemented production code from remaining design scaffolding.

---

# Phase 2 — Production Environment and Debug UI Controls

> **Status: IMPLEMENTED — 22 October 2026.** Phase 1 production world is already implemented; see `SCENES/World/README.md`. This phase **copies and adapts** working environmental effects from `DEBUG/TopDownTest/` into production without moving or deleting anything from the original debug test.
>
> **Important UI decision:** All developer-only environment sliders, checkboxes, test buttons and comparison panels must live under **`DEBUG/UI/`**, not inside `ENVIRONMENT/` or the player-facing `UI/`. Production systems must run even if every debug panel is removed.

## Phase 2 goal

Add the environmental visual effects already proven in the DEBUG forest to `SCENES/World/World.tscn`, preserving their existing appearance and interactions wherever feasible. Provide optional production-world testing panels in `DEBUG/UI/` to adjust and toggle those effects. Reuse existing modules instead of designing new weather or lighting simulation features.

Maintain the existing world/player/ground/scenery generation and the first production pass's data ownership. Keep the original `DEBUG/TopDownTest/` scene operational and unchanged.

## Production destinations

| Existing source in `DEBUG/TopDownTest/` | Production system owner | Developer controls |
| --- | --- | --- |
| `DayNight/`: cycle, illumination, projected sun shadows | `ENVIRONMENT/Time/` for authoritative time/clock and `ENVIRONMENT/Lighting/DayNight/`, `Lighting/Sun/`, `Lighting/Shadows/` for rendering | `DEBUG/UI/` day/night panel |
| `Polish/`: colour grade, cloud shade, torch light | `ENVIRONMENT/Lighting/` for colour/light and local torch; atmospheric cloud visuals under `ENVIRONMENT/Atmosphere/` as appropriate | `DEBUG/UI/` lighting/polish panel |
| Shared wind currently controlled from `Weather/` | `ENVIRONMENT/Wind/` authoritative shared wind state and visual integration | `DEBUG/UI/` wind panel |
| `Atmosphere/`: haze, fog-like overlays, pollen, drifting leaves, smoke and embers | `ENVIRONMENT/Atmosphere/` with relevant effects/profile subfolders | `DEBUG/UI/` atmosphere panel |
| `Weather/`: rain, overcast, lightning, ground impact effects | `ENVIRONMENT/Weather/` (`Rain/`, `Storms/` and related components) | `DEBUG/UI/` weather panel |
| `SnowTest/`: flakes, settled snow and accumulation/melting comparisons | `ENVIRONMENT/Weather/Snow/` for precipitation/coverage control and shared effects; world-object/ground-specific material adapters remain near their owning systems where appropriate | `DEBUG/UI/` snow panel |
| `Trees/TreeSway.gdshader`, grass wind materials and snow adapters | Reusable adapters/effect shaders controlled by `ENVIRONMENT/`; canonical Oak/grass artwork and object identities stay in `WORLD/Content/` | Use shared environment controls; no duplicate controls per tree |
| Shared `Surfaces/PlayerSurfaceVisual.cs` for snow/lake interactions | Integrate a reusable player visual adapter under `ENTITIES/Player/Visuals/` **only if needed** for in-scope snow appearance; avoid copying lake/wading dependencies | No player-specific debug panel unless necessary |

**Do not copy an entire DEBUG subsystem to one monolithic production folder.** Distribute its responsibilities by ownership while retaining tested calculations/materials and minimizing rewriting. Inspect existing production folder READMEs and config files before assigning final filenames.

## Developer UI layout

```text
DEBUG/
└── UI/
    ├── README.md
    └── Environment/
        ├── EnvironmentDebugPanel.tscn     # optional panel composition
        ├── EnvironmentDebugPanel.cs       # controls only; not the simulation
        ├── DayNight/                       # optional subpanels if needed
        ├── Lighting/
        ├── Wind/
        ├── Atmosphere/
        ├── Weather/
        └── Snow/
```

The exact subpanel files are up to the implementation; **avoid generating empty folders just for appearance**. Prefer a reasonably sized, organized and collapsible UI over crowding the gameplay viewport. Preserve useful live sliders, toggles, manual lightning testing, time scrub/pause and weather/snow comparison controls from DEBUG where the corresponding production effect exists.

`SCENES/World/World.tscn` should instantiate production environment nodes independently of `DEBUG/UI/`. Developer panels may optionally be instantiated by the world scene for testing and removed or disabled without altering environment simulation, materials or saved configs.

**Do not use `UI/HUD/` or `UI/Menus/` for these controls.** Those belong to actual player-facing interfaces; `DEBUG/UI/` is strictly developer tooling.

## Shared dependency rules

1. **One game clock:** Day/night visuals consume `ENVIRONMENT/Time/`; do not create a separate clock for every effect. Only implement the clock behaviour necessary to reproduce the working test.
2. **One wind source:** Wind feeds tree sway, grass sway, particle drift, rain slant and cloud movement. The DEBUG signed-wind behaviour can be carried across initially; expanding to sophisticated multidirectional wind is **not** required in this copy pass.
3. **Canonical world assets stay in WORLD:** Tree sprites, grass artwork, rocks and ground-surface definitions remain in `WORLD/`. Use existing production tree/rock/grass scenes created in Phase 1; adapt effects to those instances instead of reintroducing DEBUG prop scenes.
4. **Biome properties are data:** Oak Forest may provide optional local appearance/environment settings via its definition or referenced profiles, not a separate copy of all renderers. Other biomes can reuse these effect systems later.
5. **Snow effects cross modules:** Snowfall belongs to WEATHER; a grounded rock cap, tree canopy deposition or ground surface snow must target the corresponding production WORLD materials/objects cleanly. Preserve snow toggles and material restoration; don't assume original DEBUG node paths remain valid.
6. **Avoid conflicting sprite owners:** If snow affects the player's sprite, follow the existing shared surface-visual principle so environment effects don't compete to change transforms/materials. Water immersion/lake functionality remains **out of scope** for this phase.
7. **Environment is independent of debug UI:** DEBUG panels send configuration commands/values to existing production controllers; all effects function with panels removed.

## Suggested Codex extraction sequence

1. Read root `README.md`, `CodePreferences.md`, the original DEBUG subsystem READMEs, `ENVIRONMENT/` READMEs, and Phase 1's `SCENES/World/` scene/scripts. Map actual dependencies and current classes/resources first.
2. Copy/adapt **DayNight + Lighting**, including projected tree/rock/player shadows as compatible with the production content. Keep render wiring modular.
3. Copy/adapt shared **Wind**, then reconnect tree and decorative grass sway without per-instance expensive update loops.
4. Copy/adapt **Atmosphere** effects and their resource/particle configurations.
5. Copy/adapt **Weather** (rain, overcast, lightning and impact effects).
6. Copy/adapt **Snow** after wind, tree materials and ground materials are stable.
7. Copy/adapt **Polish** colour-grading/cloud/torch controls to the correct production owners (or integrate the relevant portions during Lighting/Atmosphere steps without duplication).
8. Create `DEBUG/UI/` controls **for implemented effects only**, wiring directly to production controllers; optionally compose them in one developer panel for `World.tscn`.
9. Validate independently with `World.tscn` and with debug panels removed; update owning folder READMEs and mark Phase 2 status accurately.

If a subsystem can be migrated independently, prefer verifying it before migrating the next. This ordered sequence is **one requested Phase 2 extraction job**, not permission to add unrelated mechanics.

## Phase 2 out of scope

- No new biome, cave, macro world generation, chunk streaming or save system.
- No lake/wading, water-physics gameplay, fire spread/tree destruction or survival/combat additions.
- No new seasons simulation or full meteorological/climate engine.
- No mandatory player-facing options screen for environment controls.
- No rewriting the DEBUG scene or removing its existing controls.
- No broad shader redesign solely for visual novelty; aim to match current working DEBUG appearance.

## Phase 2 acceptance checklist

- [x] Existing production `SCENES/World/World.tscn` still runs with player movement, camera, ground and procedurally scattered Oak Forest scenery.
- [x] Day/night, illumination and projected shadows work on the **production** scene.
- [x] Shared wind drives production tree/grass visuals and applicable particles/weather.
- [x] Atmosphere and colour/polish effects visually work without debug resource references.
- [x] Rain, overcast, ground impacts and lightning are testable.
- [x] Snowfall and settled snow can be enabled/disabled and affect production ground, Oak trees, rocks and grass without breaking base materials.
- [x] Controls are located under `DEBUG/UI/`, work against production environment controllers, and can be removed without disabling those controllers.
- [x] Production environment scripts/scenes/resources contain no dependencies on `DEBUG/TopDownTest/`.
- [x] Original `DEBUG/TopDownTest/` remains intact and runnable.
- [x] Runtime C# build, scene/resource loading and visible rendering are checked; any untested target backend/performance limitations are documented.
- [x] Folder READMEs identify implemented systems and their MasterConfigs/settings.
- [x] **One GitHub commit per implementation job**, consistent with `CodePreferences.md`.

**Phase separation:** The earlier Phase 1 `Keep out of scope` paragraph applies to **Phase 1 only**. This Phase 2 section explicitly authorizes extraction of the listed environment systems and creation of developer UI controls under `DEBUG/UI/`, but does not imply that Phase 2 is already implemented.

## Phase 2 implementation handoff

- Composition: ENVIRONMENT/EnvironmentRuntime.cs, invoked after production scenery. One GameClock, one WindController; no production environmental references to DEBUG/TopDownTest.
- Data: EnvironmentMasterConfig -> default BiomeEnvironmentProfile; WorldBiomeDefinition.Environment optionally overrides component profiles. OakForest/Environment.tres selects its local Atmosphere.tres; other components inherit. Resolution is once per startup and explicitly clones each selected component. No region blending or toolbox integration is claimed.
- Ownership: Lighting is split among DayNight/Sun/Shadows/Colour/LocalLights; haze/cloud/particles live in Atmosphere; precipitation/storm/coverage live in Weather. Surface-specific snow adapters/shaders remain with WORLD content/ground and the player visual adapter with ENTITIES. Canonical sprites/colliders remain unchanged.
- Developer UI: DEBUG/UI/Environment/EnvironmentDebugPanel.tscn; collapsible/scrollable, F9 hide/show. Removing it leaves all systems running; live changes reset on restart.
- Adaptations: content metadata replaces hardcoded tree names/debug groups; map bounds are supplied from production config; lake dependencies and comparison UI are removed from controllers; shared orientation materials replace per-instance wind uniforms to avoid the known software-renderer instance limit. Existing visual equations/noise/limits remain.
- Validation: successful C# build; native runtime assertions for profiles, no-UI operation, wind, time/shadows, snow accumulation/melt/off/removal, player burial/camera, manual lightning/rain and movement. Full production-density 1920×1080 software OpenGL renders inspected at day/night, rain/lightning, partial/full snow and restore. Windows D3D12/target hardware remain untested.
- Limits: one biome profile applied at load; startup caches require future registration for dynamic content/streaming. No new weather scheduling, calendar, season, lake, enemy, weapon or fire mechanics. Original DEBUG is preserved.

Integration note: the concurrent FIRE2 commit contained a C# CS0136 local-name collision in DEBUG/TopDownTest/Fire/Debug/FireTestController.cs. The ignition event delegate was renamed from ignite to onIgnited to allow the full project to compile; fire behaviour and all other original debug files are preserved.
