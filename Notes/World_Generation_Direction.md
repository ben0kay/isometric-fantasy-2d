# World Generation Direction — Fantasy 2D Sandbox

> **Status:** Future-geography design / provisional preferences. **P0–P4 foundational generation, streaming and saves are already implemented**; remaining proposals are not automatically approved work.
> **Last updated:** 2026-10-21
> **Scope:** The new **fully top-down Godot 2D** fantasy survival sandbox, despite the repository's "isometric" name.
> **Current implementation:** P0/P1 seeded finite macro biome regions, P2 feature reservations, P3 local chunk streaming and P4 private disk saves/Continue exist. Oceans/continents/relief/rivers, real settlements/caves, expandable finite boundaries and offline ecology remain future. Use [phase guide](../WORLD/Generation_Implementation.md) for authoritative status.

## Core decision under consideration

The leading preference is **an expandable finite world**, while keeping the underlying procedural generator capable of supporting large or effectively unbounded coordinates.

The intent is to combine:
- **Initial geographical structure:** Create a large world with oceans, continents, mountain ranges, rivers and distinctive fantasy biomes. Optionally bias major features toward broad areas of the map.
- **Future expansion:** Allow new landmasses/regions to be appended later without rebuilding areas the player has already explored or modified.
- **Procedural terrain:** The overall landscape is procedurally generated rather than hand-authored tile by tile.
- **Optional authored landmarks:** Separately constructed scenes (villages, ruins, dungeons, camps, unique landmarks) can be embedded into suitable generated locations.
- **Efficient exploration:** Generate detailed local terrain in chunks and persist meaningful player changes.

**This is a preference, not a decision to rule out infinite worlds.** A strictly finite world and a fully infinite procedural world are still possible options. The goal of the first tests is to learn which plays and looks best.

### Options evaluated

| Option | Advantage | Main concern |
| --- | --- | --- |
| Strict finite procedural world | Strong control over global geography, continent-scale structure and biome distribution | Less convenient to extend with future regions or new biomes |
| Fully infinite procedural world | Exploration can continue indefinitely | Large-scale coherent geography, planned regional features and versioning can be harder |
| **Expandable finite (current preference)** | Planned initial geography plus future expansion | Requires careful treatment of boundaries, transitions, coordinate space and saved areas |

A useful design choice is to **separate world bounds from generation rules**, so the same terrain/biome pipeline can be reused for all three modes.

## Fantasy-first biome placement — important preference

**Gameplay and visual variety should take priority over strict climate realism.**

The player does **not** want to spend long journeys passing through four neighbouring biomes that all look like minor variations of the same forest or plains. Climate regions can loosely organize the world, but must not force visually repetitive neighbouring areas.

The desired exploration experience is a believable *fantasy* world with noticeably different environments within reasonable travel distances.

Ideas to test:
- A **visual-similarity or contrast score** for biome adjacency; discourage stretches of nearly identical biomes.
- Configurable **minimum/maximum biome footprint**, average traversal time, rarity and frequency.
- Broad regions may have **themes/biases**, but need not contain only climatically similar biomes.
- Allow unusual fantasy biomes to override natural climate assumptions through magic, corruption, volcanic influence, ancient events, etc.
- Favor organic irregular shapes, patches, enclaves and smooth transitions rather than random checkerboard tiles.
- Allow some deliberate **exceptionally large** forests, mountain chains, deserts or other memorable geographical features; do not force every biome to be tiny.
- Avoid repetitive alternating patterns and guarantee that adjacency rules do not produce visually predictable worlds.

Potential creator controls: **biome diversity**, **biome region size**, **climate realism**, **fantasy frequency**, **similar-biome adjacency penalty**, **transition width**, **major feature scale**.

## World creation: procedural with optional broad geographic guidance

The player/developer may eventually use a **World Creator** to choose a seed and preview or guide the broad geography before starting a world.

Potential modes:
1. **Fully procedural:** The seed decides everything within the world-generation rules.
2. **Guided (promising):** Set broad preferences such as "large ocean toward southwest," "mountain range toward northeast," or "volcanic territory in the east" without manually shaping individual chunks.
3. **More constrained presets:** Explicit regional masks, special landmarks or desired geographic anchors when needed.

Regional examples are illustrative, not required map locations. A fresh seed can create a substantially different world even when it uses the same guidance.

A future creator might show overview layers for **land/ocean, elevation, rainfall/moisture, temperature, biome regions and major points of interest**. It should support regeneration and seed comparison before committing to a world.

## Proposed generation stages

Use a layered, deterministic process instead of placing every biome independently at random:

1. **Macro geography:** Continents, oceans, coastlines, islands and broad regional constraints.
2. **Relief:** Mountains, plateaus, valleys and a broad elevation field.
3. **Water:** River systems, lakes, wetlands, shorelines and drainage constraints.
4. **Loose climate fields:** Temperature/moisture and perhaps wind/rain-shadow influences, treated as *soft input* rather than rigid biome zoning.
5. **Biome candidate selection:** Use suitability, theme, rarity, region size and **visual-diversity goals**; allow fantasy exceptions.
6. **Local terrain:** Detailed ground tiles, cliffs, paths, shoreline transitions, and decoration for active chunks.
7. **World population:** Trees, rocks, resources, caves, authored landmarks, settlements and creatures based on reusable content definitions.
8. **Restore persistence:** Apply saved player edits, cleared vegetation, buildings, excavation and other changes over the generated baseline.

This is a conceptual ordering; implementation can combine or rearrange stages as needed.

### Two scales of generation

**Macro world:** Generate or determine a coarse overview once per world/region (or deterministically as regions are added). Keep it light enough to preview.

**Local chunks:** Generate detailed tile/scenery content as needed near the player. Avoid generating every tile/tree/creature for the full world at startup. Stream and unload distant chunks.

Use **stable IDs, deterministic seeds and separate random streams** (e.g., terrain, biome, vegetation, landmarks) to minimize unrelated content reshuffling when definitions change.

## Adding new biomes months later

New content should not require completely remaking an established save.

- **Brand-new saves:** The new biome participates normally in generation.
- **Existing saves / previously unvisited and uncommitted regions:** Updated rules may be used, subject to world-generation versioning and how macro geography has been fixed.
- **Visited, generated, or player-modified terrain:** Preserve existing terrain and constructions by default.
- **Expansion regions:** Offer a natural place for new biomes, civilizations and geological features to appear.
- **Borders:** Handle old/new terrain with generated transition zones, compatible masks, or intentionally separated landmasses/ocean. Avoid obvious seams.

**Important:** Whether unexplored chunks in an *existing* region can change after updates requires a deliberate persistence/versioning policy; don't promise seamless upgrades just by changing the seed.

## Surface height and caves

The game stays **fully top-down 2D**, not an isometric or 3D terrain renderer.

Preferred starting visual approach:
- Mostly flat, walkable terrain.
- Optional shader/texture-based gentle hill shading purely for atmosphere.
- Selective logical elevation levels for plateaus, cliffs, passes, stairs, ramps and bridges if experiments prove they read well in top-down art.
- No requirement to simulate physically smooth traversable 3D slopes.

Caves can exist as **separate procedural 2D maps or depth layers**, linked by entrances, stairs, holes or mine shafts. Potential for multiple underground depths and different cave biomes. Only active cave/overworld areas need full simulation.

## Handcrafted scene integration

The world remains mostly procedural, but **landmark templates** can be hand-authored once and placed by rules: villages, ruins, shrines, caves, fortresses, camps, bridges, etc.

The placer should consider:
- Slope/height and valid ground type.
- Biome and nearby water/roads.
- Required empty footprint and collision clearance.
- Frequency/rarity and minimum separation.
- Terrain blending, entrances, road links, and deterministic orientation/variants.
- Persistence of player modifications to a placed landmark.

This is different from handcrafted world terrain: author reusable *locations*, not an entire giant map.

## First test: keep it fully in DEBUG

Current disposable forest/visual tests under `DEBUG/TopDownTest/` are **not** the production world generator. Existing `WORLD/` folders are scaffolding for future promotion.

Before making a large production system, prototype:
1. **Overview generator** with changeable seed: land/ocean, major mountains, biomes, optional geographic bias.
2. **Biome-variety comparison:** realism-weighted versus fantasy-diversity-weighted; measure how often visually similar regions cluster.
3. **Detailed local chunk demonstration** of 3–5 visibly different neighbouring biomes with organic transitions.
4. **Optional elevation test:** flat ground versus shader-shaded hills versus stepped cliffs/plateaus.
5. **Landmark and cave test:** insert an authored village/ruin or cave entrance into generated terrain.
6. **Persistence/expansion proof:** regenerate the same seed consistently, preserve a player modification, and connect a newly added region without seams.

All test code and assets remain isolated in DEBUG until explicitly selected for use under `WORLD/`.

## Open questions

- Does the player prefer a very large fixed map, a gradually expandable world, or truly unbounded exploration after testing?
- Should the world feel like one major continent, several continents, or many islands?
- How far should the player typically travel before encountering a substantially different *visual* biome?
- How large should rare mega-biomes be?
- How much explicit location control should the World Creator expose?
- Should macro biome maps be generated and frozen at world creation, or extended region-by-region?
- What should happen to *unvisited* chunks in older saved worlds after generator/content updates?
- Do actual elevation levels improve the artwork, or is mostly flat ground preferable?

## Related existing folders

- `DEBUG/TopDownTest/` — contained 2D art, scenery, movement and atmosphere tests.
- `WORLD/Generation/` — future generation algorithms.
- `WORLD/Biomes/` — future biome profiles and spawn references.
- `WORLD/Terrain/` — future ground/cliff content and transitions.
- `WORLD/Caves/` — future cave-region definitions.
- `WORLD/Locations/` — future reusable handcrafted points of interest.
- `WORLD/Runtime/` — future chunk streaming and persistence.

**Summary:** Provisional choice is an **expandable finite, seeded procedural fantasy world** with optional guided geography, strong biome contrast, procedural chunks, reusable authored landmarks, and multi-layer caves. Preserve the option for genuinely infinite world bounds. Prioritize **interesting exploration and distinct visuals**, not realistic climate gradients.
