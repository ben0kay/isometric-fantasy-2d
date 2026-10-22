# Production Implementation — First Playable Oak Forest World

> **Status:** Implementation plan for Codex — **NOT implemented by this note**.
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

- [ ] `SCENES/World/World.tscn` opens and runs.
- [ ] Production player moves with arrow keys, collides and is followed by Camera2D.
- [ ] Oak Forest ground retains its familiar organic grass colour patches and fine surface detail.
- [ ] Oak trees, rocks and grass decorations use reusable production assets and correct anchors/footprints.
- [ ] Deterministic startup scenery placement works without debug paths, hardcoded second biome or lake dependencies.
- [ ] Oak Forest biome and surface settings are data driven and point to canonical production resources.
- [ ] Production files do not depend on `DEBUG/TopDownTest/` paths/classes/resources.
- [ ] All existing debug experiments stay intact and runnable.
- [ ] Updated READMEs clearly distinguish newly implemented production code from remaining design scaffolding.
