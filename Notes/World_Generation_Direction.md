# World Generation Direction — Fantasy 2D Sandbox

## Author clarification — 26 October 2026

**Ocean placement:** “middle/central ocean” below means a broad preferred middle area, not a required exact centre, symmetry, or fixed central anchor. Allow seeded offsets and irregular extent through normalized Inspector guidance. Keep ocean presence/scale separate from preferred location; centrality must not become a hard constraint merely from earlier wording.

**North / Mid / South starts:** the player chooses a broad area preference. Resolve a deterministic accessible land anchor within that area after major geography and major POI footprints/access reservations are known, before protecting the start and placing local scenery. Add configurable minimum distance from major POI footprints (world units), optional per-POI/category overrides, water/shore setback and obstacle/access clearance. Measure clearance from the actual footprint, not just its centre. Exclude dangerous/required POIs and protected approaches as configured; preserve ordinary biome content around the spawn. Use bounded candidates and deterministic tie-breaking. If no candidate satisfies the requested area and clearances, report the failed constraint; never silently reduce clearance, change area, erase a POI or relocate an ocean. Freeze the chosen preference, exact anchor and relevant policy/version in SAVE; Continue restores saved position. BC0 settles these contracts, BC1 reconciles ocean/start ownership, BC5 implements the selector and resolver. Future POI providers must supply planned footprints without loading distant scenes.

**Minimal content now:** Snowy Tundra and Desert are ground-only placeholder definitions registered in the normal finite World collection, alongside Oak and Pink. No new scenery/entity/deposit spawning or regional weather is included. They currently place by global weights; BC2 will add climate affinity. Their artwork and populations can be expanded later. BC0–BC7 remain planned.


> **Status:** **Agreed future world-layout direction**, not a claim of implementation. P0–P4, P5a/P5b, P5c ocean/non-land and BP0/BP1 climate baseline Resources exist; **large climate zones, biome climate affinity and region-aware snow do not**. Future phases require approval.
> **Last updated:** 2026-10-26
> **Scope:** The new **fully top-down Godot 2D** fantasy survival sandbox, despite the repository's "isometric" name.
> **Current implementation:** P0/P1 seeded finite macro biome regions, P2 feature reservations, P3 local chunk streaming and P4 private disk saves/Continue exist. WG lake/river geometry and streaming also exist; P5a/P5b now plans major water before constrained biome assignment. Oceans/continents/relief, real settlements/caves, expandable finite boundaries and offline ecology remain future. Use [phase guide](../WORLD/Generation_Implementation.md) for authoritative status.

## Core world decision — large finite, optionally expandable later

The **initial world is large and finite** (current production preset 142,304 × 106,728 units). Future expansion is optional; do not build an infinite world by default.

The intent is to combine:
- **Initial geographical structure:** Create a large world with oceans, continents, rivers, distinctive fantasy biomes and **large climate/theme provinces**. Broad guidance may bias geography and entire climate clusters toward different parts of the map.
- **Future expansion:** Allow new landmasses/regions to be appended later without rebuilding areas the player has already explored or modified.
- **Procedural terrain:** The overall landscape is procedurally generated rather than hand-authored tile by tile.
- **Optional authored landmarks:** Separately constructed scenes (villages, ruins, dungeons, camps, unique landmarks) can be embedded into suitable generated locations.
- **Efficient exploration:** Generate detailed local terrain in chunks and persist meaningful player changes.

**Finite world is the current decision.** The options table above is retained as historical trade-offs, not three equally active implementation choices. Seeded generation still varies detail inside chosen broad constraints.

### Options evaluated

| Option | Advantage | Main concern |
| --- | --- | --- |
| Strict finite procedural world | Strong control over global geography, continent-scale structure and biome distribution | Less convenient to extend with future regions or new biomes |
| Fully infinite procedural world | Exploration can continue indefinitely | Large-scale coherent geography, planned regional features and versioning can be harder |
| **Expandable finite (current preference)** | Planned initial geography plus future expansion | Requires careful treatment of boundaries, transitions, coordinate space and saved areas |

A useful design choice is to **separate world bounds from generation rules**, so the same terrain/biome pipeline can be reused for all three modes.

## Fantasy-first placement — coherent regional climate with intentional magic

**Core preference (26 October): cluster compatible biomes into large, connected climate/thematic provinces before selecting individual biome regions.** The finished map should not be Oak, then alpine snow, then purple fungi, then Pine, repeating from equally likely biome rolls at every regional site. A broad chilly highland might include different snowy forests, tundra and frosty meadows nearby; a remote enchanted territory might contain blue grass, giant mushrooms and purple fauna; dry/volcanic provinces may collect elsewhere. These are *illustrative*, not fixed map coordinates or authored biomes that already exist.

- **Geography first:** macro oceans, future relief/volcanoes and major water help shape broad temperature/moisture provinces. Optional normalized world-layout anchors or masks can steer a cold region toward one side of the *finite* map and an enchanted region toward another. Random seeds still create organic outlines and variation.
- **Biomes inside provinces:** a future biome **climate affinity/compatibility profile** weights site selection among biome types suitable for that zone, with explicit optional hard constraints, min/max region sizes and rare fantasy overrides. Preserve visual differences *within* a province and permit deliberate spectacular transitions, without a random checkerboard or rigid real-world latitude bands.
- **Full coverage with limited content:** right now only Oak Forest and Pink Meadow exist. A seed with no authored tundra or purple fungi must **not fabricate them**, turn empty land into an unnamed biome or violate P5c ocean non-land. Optional climate guidance should stay permissive until more real definitions exist. Impossible *required* constraints should fail with an explanation, not loop forever.
- **Climate vs weather:** world-region climate is a coarse place-dependent baseline; current temperature also depends on time/season and events. **The world/shared weather system determines effective local snowfall**, while a biome Resource supplies eligibility, snow retention and appearance/colour overrides (magical cyan grass may keep colour under frost). A temperate Oak baseline of 15°C does not prohibit snow in a cold winter. Do not add per-biome rain/snow/clock managers.
- **Spatial accuracy:** a camera can see both snowy highlands and unsnowed hot ground. Future rendering must use localized coverage/precipitation masks or chunk/region parameters and existing shared GPU emitters. It cannot merely switch the whole global snow controller based on the player's current biome.
- **Performance and persistence:** generate/freeze small indexed zone fields once per new-world seed; query and blend at world coordinates or changed chunk/camera windows, **not per object/flake or across the whole map per frame**. Save/version the climate map with the frozen world blueprint. Continue must preserve older campaigns and reject incompatible generator/layout changes safely.

**What exists today:** P0/P1 still choose biomes primarily by generation weight/diameter, constrained by P5 geography/hydrology; **climate-based grouping has not been added**. BP0/BP1's optional `BiomeClimateProfile` and Oak baseline 15°C/65% RH exist but have **no runtime consumers**. SE0–SE3 are manual foliage preview, B0/B1 severe-snow visuals and current rain/snow operate globally. No automatic real calendar or local snow suitability exists yet.

### World Author vision: centred ocean, latitudinal climate, selectable spawn and giant finite area

**Desired authorable example:** `Ocean = Middle`, `Cold climate = North`, `Hot climate = South`, naturally variable temperate middle/east/west land, and optional special magical land elsewhere. Far north could stay snow-covered through summer; lower northern Oak/Pine zones experience long winters and spring melt; temperate centre has recognisable seasons; farther south sees cooler winter but little snow. These are **climate tendencies**, not uniformly striped biome assignments or automatic snow generation; coast/relief and magical exceptions can bend temperature. Oak is the same canonical biome definition, with season/weather response decided by effective local conditions.

**Scale and on-demand design:** the first larger-target preset is **50× today's total AREA** (roughly 1,006,000 × 755,000 world units, ≈ 7.07× per axis). Current `WorldGenerationPipeline` **already builds entire coarse geography/biome plan on new world creation** while `WorldChunkStream` loads detailed scenery in nearby 512-unit chunks only. The future hybrid will preserve a cheap global seeded/author-directed geographical skeleton (needed for one coherent central ocean) and compute finer region/biome/tile detail only when requested, deterministically and consistently with SAVE and a read-only F7 preview. A map-only full-world F7 preview may take time and display progress, but it must never load all playable chunks. Use independent coarse climate/biome/streaming spatial scales and proven memory/region budgets, not merely enlarged array caps.

**Spawn choice:** optional North / Temperate Middle / South New Campaign choice. The canonical seed and World Author guide a **bounded safe land** anchor, respecting the central ocean and protected start reservations. Then nearby chunks spawn directly around that anchor; no need to visit or simulate the whole path from the old centre. Persist that exact location across Continue.

[Full World Author plan](../WORLD/Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) · [BC phase breakdown](../WORLD/Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md). **These features are NOT implemented.**

Future World Creator controls could include region temperature/humidity trends, broad map-area guides, zone sizes/adjacency, terrain/humidity influences, magical exceptions, transition width and biome weights **within eligible zones**. Climate realism is a configurable artistic preference; **coherent geography is the goal**.

## World creation: procedural with optional broad geographic guidance

The player/developer may eventually use a **World Creator** to choose a seed and preview or guide the broad geography before starting a world.

Potential modes:
1. **Fully procedural:** The seed decides everything within the world-generation rules.
2. **Guided (promising):** Set broad preferences such as "large ocean toward southwest," "mountain range toward northeast," or "volcanic territory in the east" without manually shaping individual chunks.
3. **More constrained presets:** Explicit regional masks, special landmarks or desired geographic anchors when needed.

Regional examples are illustrative, not required map locations. A fresh seed can create a substantially different world even when it uses the same guidance.

A future creator might show overview layers for **land/ocean, elevation, rainfall/moisture, temperature, biome regions and major points of interest**. It should support regeneration and seed comparison before committing to a world.

## Proposed generation stages

The agreed direction is **constraints → major geography → major POIs/access → biomes → local detail/population**. Follow the authoritative continuation and P5a–P5e in [Generation_Implementation.md](../WORLD/Generation_Implementation.md); older conceptual ordering is superseded.

1. Fix bounds/seed and required constraints, including protected start and fixed landmark reservations.
2. Plan major geography: giant ocean/land topology, volcanic regions, mountains, major lakes and rivers. Coarse relief/climate fields can support this without requiring realistic climate simulation.
3. **Plan broad coherent climate/thematic provinces**, conditioned by geography and optional user-guided regions (future, not implemented).
4. Select/reserve important destinations such as temples, settlements and ruins, preserving their climate/theme/access requirements.
5. Assign compatible biomes **inside/near suitable climate regions**, retaining seed/size/weight variability and deliberate fantasy exceptions.
6. Finalize local paths/terrain and stream POI scenes, ground, shores, scenery/resources and entities near the player.
7. On Continue, restore the frozen geography/climate/biome blueprint and saved edits; never rerun new-world generation over an old campaign.

Planning a temple early does not instantiate its entire scene globally. Oceans and volcanic geology are features that may span many biome regions. Smaller biome-dependent water details can be added later without moving established major geography. P5a/P5b implements the major-water/biome dependency inversion. Ocean/volcano/POI providers remain planned; full ordering awaits those separately authorized phases.

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

## Historical DEBUG prototype checklist — superseded by the production WORLD generator

These were early prototyping ideas, **not current instructions to implement world generation under DEBUG**. P0–P5c production ownership is WORLD/Generation + streamed World.tscn; use new tests only when needed.

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

**Summary (26 October 2026):** Build one **large finite, seeded fantasy world** with optional guided geography and **contiguous cold/temperate/hot/magical climate provinces**; place compatible biome types within those territories, while supporting rare magical anomalies and meaningful visual variety. Weather/snow eventually resolves from the **shared world climate + biome-authored response**, not independent biome simulators. Neither climate zoning nor spatial snow behavior exists yet. Keep optional future finite expansion distinct from infinite generation.
