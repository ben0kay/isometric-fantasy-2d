# WORLD — World Content and Generation

> **Status:** Foundational folder layout only. No world generators, biomes, new content objects, or runtime features are implemented by this package.

`WORLD` owns the **physical world**: reusable scenery and structures, biome profiles, terrain, water, caves, discoverable locations, generation rules, and runtime world state. It does **not** own the player, wildlife or enemy implementations (those belong in `ENTITIES/`).

## Where things go

| Folder | Purpose |
| --- | --- |
| [Content](Content/README.md) | One canonical home for each reusable world object/species and all its visual variations. |
| [Biomes](Biomes/README.md) | Per-biome settings and references to world content and entity species, **not copies of assets**. |
| [Terrain](Terrain/README.md) | Ground tile art, cliff pieces and transitions. |
| [Water](Water/README.md) | River/lake/shoreline content and shape rules. |
| [Caves](Caves/README.md) | Underground biome concepts, entrances and cave features. |
| [Locations](Locations/README.md) | Whole points of interest built from reusable components (settlements, camps, dungeons, landmarks). |
| [Generation](Generation/README.md) | Algorithms that assemble terrain, biomes, water, caves, locations and natural object placements. |
| [Runtime](Runtime/README.md) | Chunk streaming and persisted player changes to generated areas. |

## The central rule: definition → biome → generator

- **Definition:** `Content/Vegetation/Trees/Oak/Oak.tres` will eventually describe the Oak species, with e.g. `Sprites/Oak_01.png` through `Oak_16.png`, harvesting/collision data, visual weights, and mirror/scale permissions.
- **Biome:** `Biomes/TemperateForest/Biome.tres` will eventually reference that **one Oak definition** and specify how often/where oak trees are allowed to appear. A second biome can reference the same tree with different weights.
- **Generator:** `Generation/Placement/` eventually makes a deterministic placement decision, chooses an allowed content definition and variant, and records/resolves the same result when a chunk reloads.

**Do not copy Oak assets into biome folders. Do not create one spawn resource per item per biome.** The intended model is **one biome definition/profile** (or a small set of category profiles if genuinely necessary) containing collections of content references, editable in Godot now and a future Avalonia editor later. Stable IDs for content and biomes are strongly recommended.

Sprite variation belongs to the **content type** (Oak has its own variants; Pine has different variants), while biome selection controls **which types** appear and with what placement distribution. Random horizontal mirroring and modest scaling can increase visual variety; keep sprite-root anchors consistent and provide per-variant collision overrides where needed. Avoid changing a regenerated tree's appearance between visits by using seeded randomness / persisted generated state.

## Shared vs biome-exclusive content

All reusable trees, plants, rocks, props, ores and structures live in `Content/` **once**, regardless of the biome that first uses them. A magical tree can start in just one biome; adding it to another biome later should require only a reference in that second biome's settings. There are no `Shared/` and `BiomeSpecific/` asset duplicates.

## World-specific boundaries

- `Content/Structures/` contains **reusable buildings and pieces**, whether used **above ground or in caves**. `Locations/` contains assembled places: a village, an abandoned mine or a ruined underground temple.
- `Terrain/` and `Water/` hold the *domain assets and definitions*; `Generation/Terrain/` and `Generation/Water/` hold the *algorithms* that place/shape them.
- `Caves/` is for underground world-region concepts, entrances and features; a reusable stalagmite/rock can still be defined once under `Content/Rocks/`.
- `WORLD` biome population definitions may **reference** wildlife/enemies from `ENTITIES/`; their AI, sprites and gameplay definitions should remain in `ENTITIES/`.
- The future **time, day/night, wind, weather and seasonal simulation** is better considered for a separate top-level `ENVIRONMENT/` module. Environmental systems can query or affect biomes and content without being owned by each biome. **No ENVIRONMENT folder or code is created in this pass.**
- `DEBUG/TopDownTest/` remains disposable and independent. It is **not** silently promoted to production code by these folders.

## Workflow

1. Create one species/object folder and its definition + variants under `Content/`.
2. Add that object's reference to one or more biome profiles under `Biomes/`.
3. Let the reusable generation/placement logic choose and place it.
4. Browse/edit the same definitions with a future Avalonia toolbox, rather than maintaining a second database.

## Conventions

- Keep one `README.md` per major folder (and short leaf-folder guides where navigation benefits).
- Keep the top-level `WorldMasterConfig.cs`/resource as an **empty future settings placeholder** for now; avoid per-asset master-config scripts.
- Prefer centralized, data-driven definitions and shared reusable logic over hard-coded species-specific spawners.
- Folder layout is intentional scaffolding, **not a requirement to build every subsystem immediately**.
