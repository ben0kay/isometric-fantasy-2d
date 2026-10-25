# WORLD — World Content and Generation

**Current production:** [World.tscn](../SCENES/World/README.md) runs P0/P1 finite seeded macro regions, P2 reservations and P3 nearby chunk streaming **by default**. P4 durable profile/run/world campaigns with Continue are implemented through SAVE. F7 shows the macro map; F8 saves in a campaign. The old adjacent Oak/Pink layout is a compatibility preview only when `WorldMasterConfig.Streaming.Enabled=false`. See [phase status](Generation_Implementation.md), [streaming](Runtime/Streaming/README.md) and [SAVE](../SAVE/README.md). P5 landmarks/terrain and P6 offscreen A-Life remain future.

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

- **Definition (implemented):** shared [TreeDefinition Resources](Content/Vegetation/Trees/README.md) configure Oak and Pink Blossom art, variants, collisions, fire/hit opt-ins and presentation using one `Shared/Tree.tscn`. Harvesting inventory is future.
- **Biome (implemented):** [biome Resources](Biomes/README.md) reference canonical content and configure preview populations or P3 streamed densities without species-specific generator paths.
- **Generator (implemented):** seeded [placement/streaming](Runtime/Streaming/README.md) uses stable coordinate IDs, cross-chunk spacing and P2 masks; P4 persists supported removed/burnt delta states.

**Do not copy Oak assets into biome folders. Do not create one spawn resource per item per biome.** The intended model is **one biome definition/profile** (or a small set of category profiles if genuinely necessary) containing collections of content references, editable in Godot now and a future Avalonia editor later. Stable IDs for content and biomes are strongly recommended.

Sprite variation belongs to the **content type** (Oak has its own variants; Pine has different variants), while biome selection controls **which types** appear and with what placement distribution. Random horizontal mirroring and modest scaling can increase visual variety; keep sprite-root anchors consistent and provide per-variant collision overrides where needed. Avoid changing a regenerated tree's appearance between visits by using seeded randomness / persisted generated state.

## Shared vs biome-exclusive content

All reusable trees, plants, rocks, props, ores and structures live in `Content/` **once**, regardless of the biome that first uses them. A magical tree can start in just one biome; adding it to another biome later should require only a reference in that second biome's settings. There are no `Shared/` and `BiomeSpecific/` asset duplicates.

## World-specific boundaries

- `Content/Structures/` contains **reusable buildings and pieces**, whether used **above ground or in caves**. `Locations/` contains assembled places: a village, an abandoned mine or a ruined underground temple.
- `Terrain/` and `Water/` hold the *domain assets and definitions*; `Generation/Terrain/` and `Generation/Water/` hold the *algorithms* that place/shape them.
- `Caves/` is for underground world-region concepts, entrances and features; a reusable stalagmite/rock can still be defined once under `Content/Rocks/`.
- `WORLD` biome population definitions may **reference** wildlife/enemies from `ENTITIES/`; their AI, sprites and gameplay definitions should remain in `ENTITIES/`.
- **Time, day/night, wind, weather, season progression and reusable visual atmospheric effects** belong to [ENVIRONMENT/](../ENVIRONMENT/README.md) (production clock, wind, lighting, atmosphere and weather implemented; live per-region blending and seasons remain future). A biome may eventually have an optional `Atmosphere.tres` or climate-profile reference to override local fog/lighting/weather tendencies; reusable shaders/effects remain centralized in ENVIRONMENT. Environmental conditions may affect trees and the world without moving tree-specific seasonal art out of WORLD/Content.
- `DEBUG/TopDownTest/` remains disposable and independent. It is **not** silently promoted to production code by these folders.

## Workflow

1. Create one species/object folder and its definition + variants under `Content/`.
2. Add that object's reference to one or more biome profiles under `Biomes/`.
3. Let the reusable generation/placement logic choose and place it.
4. Browse/edit the same definitions with a future Avalonia toolbox, rather than maintaining a second database.

## Conventions

- Keep one `README.md` per major folder (and short leaf-folder guides where navigation benefits).
- `WorldMasterConfig.cs` and `DefaultWorldConfig.tres` are **live** production Generation, Streaming and preview configuration; avoid duplicate per-asset master config scripts.
- Prefer centralized, data-driven definitions and shared reusable logic over hard-coded species-specific spawners.
- Folder layout is intentional scaffolding, **not a requirement to build every subsystem immediately**.

H4 integration: real World trees use the shared harvest profile. WorldHarvestRuntime owns Player inventory/pickup binding; WorldChunkStream restores work and felled tombstones, attaches harvesting on materialization, and activates only loaded landing-chunk drop nodes. Logical offscreen drops remain saved without Nodes. [Controls, ownership and checks](../SYSTEMS/Harvesting/H4_World_Validation.md).
