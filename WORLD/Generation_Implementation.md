# Large Finite World Generation — Codex Implementation Planning

> **Status:** Detailed DESIGN BRIEF / proposed staged implementation plan; **NOT implemented or permission to execute all passes automatically**.
> **Created:** 2026-10-22
> **Scope:** Production fantasy top-down Godot/C# world; preserve current working production and DEBUG prototypes while designing.
> **Purpose:** Discuss this document with Codex, improve/refine it, then implement in small verifiable passes. The author can edit this plan before approving each phase.

## 0. Read these documents and inspect the CURRENT CODE first

- Root README.md and CodePreferences.md (comments, method separators, regions, compact modular C#, shared configs, performance, README ownership, **one commit per job**).
- Notes/World_Generation_Direction.md — geographical structure, strong fantasy biome contrast, finite/expandable world, seeded generation, possible World Creator.
- WORLD/placement_priority.md — required features reserve first, paths, caves, then large scenery, then small decorations. **Treat as authoritative priority guidance.**
- Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md — cave layers geographically aligned with surface; multiple entrances and biome-dependent underground resources.
- Notes/Ideas/World_Hostility_Wildlife_Exploration.md — living ecosystem, danger by day and night, resident/roaming/event populations, lightweight off-screen state.
- SAVE/README.md, SAVE/Profiles/README.md, SAVE/WorldState/README.md, SAVE/Serialization/README.md, SAVE/Migrations/README.md, SAVE/SaveMasterConfig.cs and WORLD/Runtime/Persistence/README.md — **profile-specific save ownership; currently scaffolding only**.
- WORLD/README.md, WORLD/Biomes/README.md, WORLD/Generation/README.md, WORLD/Runtime/README.md and their subfolder READMEs.
- SCENES/World/World.tscn and README, WORLD/Runtime/WorldScene.cs, WORLD/WorldMasterConfig.cs, WORLD/DefaultWorldConfig.tres, WORLD/Generation/Placement/WorldSceneryPlacement.cs.
- WORLD/Biomes/Shared/WorldBiomeDefinition.cs, BiomePopulation.cs, AdjacentBiomeLayout.cs, WORLD/Biomes/Layouts/OakAndMeadow.tres and both current biome resources.

**Existing reality as of drafting:** The production world has **Oak Forest** and **Pink Meadow**, with a fixed curved west/east boundary in AdjacentBiomeLayout. Scenery is placed across the current 3840 x 1728 map once at scene initialization. The world can choose a random seed at scene load. Ground/environment effects, player, pond and a basic skeleton are integrated. None of this is yet a giant macro-biome generator, streamed world or off-screen A-Life system. Current code may have changed: **Codex must re-read HEAD rather than trusting this snapshot**. Preserve working pond, second biome, weather, day/night, snow, wind, lighting, skeleton navigation and player camera.

## 1. Agreed gameplay/world direction

1. **One very large FINITE world for the initial game.** World width/height must be configurable; do not instantiate the entire world or every tree at startup. Architect coordinates/chunks to permit expanding world bounds later, but an actually infinite world is NOT required.
2. **The world must be COMPLETELY assigned using the available eligible biomes.** Initially there are only TWO usable overworld biome definitions: Oak Forest and Pink Meadow. The entire *eligible land* area should be partitioned into multiple naturally shaped regions of those two biome types, not one Oak half and one Meadow half. Regions can repeat many times.
3. **Per-biome minimum/maximum region size** and **relative spawn weight** are editable on the biome definition (or an explicitly referenced generation profile). Each new region selects a size in its allowed range and biome type using eligibility/weighted rules. No hardcoded number of regions, fixed west/east split or need for five biomes.
4. **Strong visual fantasy-biome variety**, not rigid real-world climate realism. Climate/latitude/moisture/elevation may **bias** the probability of biomes, rather than globally disallowing them, unless a specific biome is configured with a hard requirement. Allow unusual adjacency and eventually fantasy exceptions.
5. **Guided major features** may be specified by resources: e.g. required large volcanic region in top-right/bottom-right; required massive ocean in the centre, if such content definitions exist. These **must be planned/reserved FIRST**; random seeds vary their exact organic shapes/locations inside the specified region but **must satisfy the requirement**.
6. **Priority placement matters:** protected macro terrain and required locations are established before optional smaller content, following WORLD/placement_priority.md.
7. **Chunk streaming:** load/generate detailed terrain, collision, scenery and actors only around the player; unload distant chunks. The coarse world layout is queryable anywhere without loading 2D scenes.
8. **A-Life-inspired distant simulation** eventually lets populations and events evolve in unloaded areas. This is NOT a demand to run full character AI or physics everywhere. Use a lower-resolution abstract world simulation, persistent time/state, and reify active agents on loading.
9. **Deterministic and persistent:** a world is created from one world seed; revisits should preserve layout and meaningful player actions. RandomSeedOnLoad is acceptable in a disposable demo but **must not silently generate a new geography whenever a saved world is reloaded**.

These are requirements/preferences to refine with Codex before implementation; proposed classes, numeric values and phases below are recommendations, not mandatory naming.

## 2. Two scales: macro-world versus streamed detail

**Macro world (created once per world seed / save):**
- World dimensions, logical origin, fixed chunk coordinate convention and coordinate-to-cell mapping.
- Coastline / major reserved obstacles, macro elevation/water, climate fields, guaranteed feature masks/footprints.
- A complete region-ID / biome-ID map covering every eligible land cell; region shape and connections, seed identity, reserved POIs/entrances/roads, broad populations and zones.
- Coarse, inexpensive data must answer: Which biome is at (X,Y)? Which macro region? Which planned river/POI/cave entrance? Without loading individual chunks.

**Local detail (created/streamed per nearby chunk):**
- Tiles or procedurally composited surfaces, biome transitions, visible water edges and cliffs.
- Canonical reusable trees, rocks, grass, local resources and smaller features.
- Collision/navigation for currently loaded geometry; sprites, shadows, physics and near-camera actors.
- Restored modifications from a separate persistence delta layer.

**Avoid** generating the random biome region map independently per chunk. That causes discontinuities and inconsistent biome sizes. Plan global/coarse region membership once, then each chunk queries/crops it. Use deterministic boundary noise at the coordinate level.

### Two-biome coverage invariant — non-negotiable

Given only Oak Forest and Pink Meadow enabled and suitable for the baseline world:
- Every valid land sample is assigned exactly one existing biome ID (with an optional transition/blend field at borders).
- Regions have variable sizes and organic outlines, repeated as needed to fill the finite map.
- There are NO empty, null, untyped, or untextured holes between regions.
- No extra "generic fallback biome" silently appears just because a seed fails to place a region.
- If only one biome is enabled, it occupies all eligible land. If NONE are enabled or all valid weights are zero, **fail validation clearly** instead of looping forever or secretly inventing a biome.
- The last/edge regions may be truncated by the finite world boundary. A configured minimum should be interpreted for interior target-size selection, not an impossible guarantee that every clipped sliver meets that minimum. Report small fragments/merges in validation.
- Future ocean/water masks/voids are distinct, explicitly tagged **non-land** where appropriate. The two land biomes fill all *remaining* land; they do not replace open ocean with meadow or forest.

## 3. Biome generation configuration (update existing definitions)

Extend the existing WORLD/Biomes/Shared/WorldBiomeDefinition.cs Resource without replacing canonical Surface / Environment / Populations references.

**Suggested Inspector-editable new fields, grouped under "World generation":**
- EnabledForGeneration (bool; existing biome may still be used in handcrafted/editor tests).
- SpawnWeight (float, nonnegative; **relative** weight per region selection, NOT guaranteed percent land coverage).
- MinRegionSize / MaxRegionSize with explicit units. Recommend **target diameter in world units or coarse map cells**, not local tree counts. Define the units in Inspector and docs; allow large special biomes.
- Optional PreferredClimate/Temperature/Moisture/Elevation ranges or region-affinity profiles, soft vs hard filtering.
- Optional RegionTags (woodland, magical, arid etc.) and later adjacency contrast/rarity modifiers.
- Optional min/max region occurrences only if actually needed; do not add unneeded complex settings in the first pass.
- DisplayName, stable Id, Surface/Environment/Populations remain authoritative.

**Keep these concepts separate:**
- **Region size** = how big an Oak Forest / Pink Meadow patch is on the map.
- **Spawn weight** = how likely that biome is when selecting a new region.
- **Content density** = number of trees/rocks/grass *per eligible area or chunk*, NOT a fixed "80 trees across the entire gigantic world".
- **Guaranteed region** = explicit location/feature reservation; bypasses ordinary random weight.
- **Climate affinity** = local modifier to selection probability; hard exclusion only when requested.

CURRENT BiomePopulation.Count represents a **whole-demo-map count**. A huge streamed world needs a new per-area density or per-chunk spawn model. Migration must be deliberate so Oak Forest remains reasonably dense regardless of map size. Don't multiply 80 oaks by the entire world, and don't have just 80 total oaks spread over millions of pixels.

### World-wide generator controls (master config)

Retain WORLD/WorldMasterConfig.cs / DefaultWorldConfig.tres as the production root composition. Put cohesive detailed generator settings in a referenced Resource (e.g. WorldGenerationMasterConfig + DefaultWorldGenerationConfig.tres in WORLD/Generation/World/) rather than a massive catch-all class.

Candidate knobs:
- WorldBounds or world size + origin; finite/expandable setting, seed override, generator version.
- Coarse macro-cell resolution and chunk world-size (chosen so streaming and biome region sizes have independent scales).
- World creation seed versus fixed-seed replay; world identity.
- Reference to **array/list of allowed WorldBiomeDefinition resources** (Oak, Pink Meadow initially; automatic new definitions later).
- Region-size scaling, boundary irregularity/noise seed, minimum viable patch area, attempts/fallback and optional diversity/adjacency scoring.
- Climate influence intensity, fantasy exceptions and adjacency penalty.
- References to required macro feature / world-layout presets and placement-priority/reservation profile.
- Active chunk radius, preload radius, unload hysteresis, load/create/unload time or node budgets and optional debug visualizers.
- Population simulation tier/rate budgets (future phase; do not prebuild simulation before chunk streaming works).

For visual changes, reuse existing ground shader/profile assets and environment systems; generation should select a region's biome reference rather than instantiate duplicate biome materials and actors across the whole map.

## 4. Macro region algorithm proposal (let Codex refine)

Prefer a **full-coverage partition** approach (seeded sites/region growing, weighted Voronoi + domain warping, or another algorithm that guarantees assignment), NOT random circular biome stamps that leave gaps or become prohibitively slow through repeated failed attempts.

Suggested logical steps:
1. Validate the world bounds, enabled biome list and region size/weight inputs.
2. Decide world seed once; derive stable independent sub-seeds for major features, geography, climate, biome layout, POIs, paths, scenery, entities.
3. Allocate a coarse macro map / sampler for the finite world.
4. Reserve required major features and player starting zone.
5. Build coarse land/water/elevation/climate suitability.
6. Choose eligible biome region seeds with relative weighted selection; apply any hard biome suitability constraints only where opted in. Generate region target sizes within their min/max settings.
7. Expand/partition to cover every eligible land cell, enforcing/smoothing reasonable sizes, avoiding isolated 1-cell fragments where feasible.
8. Domain-warp region boundaries with deterministic continuous noise, respecting non-land barriers, required features, edge limits and connectedness. Preserve organic transitions.
9. Give every region a stable identity, biome ID, bounding data and reserved feature conflicts; generate a coarse overview/occupancy proof.
10. Place/reserve high-priority POIs, caves and roads before generating local chunks.

### Weights / sizes example (illustrative only)

Two enabled definitions:

| Biome | Example relative weight | Example target region diameter |
| --- | ---: | --- |
| Oak Forest | 70 | 5–12 coarse cells |
| Pink Meadow | 30 | 3–7 coarse cells |

A world must be completely composed of BOTH allowed biomes (subject to randomness and sufficiently large area), but 70/30 means **selection bias**, not exact 70/30 coverage. Larger Oak patches also affect final land-area proportions. Coverage is always guaranteed; a particular tiny map cannot guarantee both types appear unless minimum occurrence is set. For the big-map test, validate both biome types occur; optionally support a small guaranteed minimum count when the user wants that.

**Codex must explicitly choose/describe the meaning of region size and test it**. A naïve independently assigned random biome per grid cell would create noisy checkerboards and fail the target.

## 5. Guaranteed macro geography: user-authored directives

The user wants data such as "large volcano region somewhere in bottom-right" or "giant central ocean" to apply **before random biome placement**.

Propose a reusable set of Resource definitions, not hardcoded enum cases:
- WorldLayoutPreset.tres: array of RequiredWorldFeature resources.
- RequiredWorldFeature fields: Id, feature type/definition reference, Enabled, Required vs Preferred, target area/quadrant or normalized anchor point, allowed placement zone/mask, desired extent/min/max size, organic shape seed profile, priority, minimum separation/exclusion, overrides/allowed interactions and failure strategy.
- Normalized coordinates in the **finite world bounds** let TopRight/BottomRight/Centre remain meaningful when map size changes. In Godot +Y goes down; BottomRight is high X/high Y.
- Exact forced location and broad allowed zone are different modes: a volcano may appear **somewhere within the SE quadrant**, not at the identical pixel coordinate every seed.
- Feature shapes can be irregular footprints/masks instead of only rectangular reserved areas.

Important: "Volcanic Region" could become a **guaranteed biome/region directive** when that biome definition is actually available. "Ocean" should be a **geographical water/landmask directive** with coastlines, not require copying an Oak Forest definition. A POI (castle, volcano caldera, unique ruin) is another kind of anchor. Don't implement a fake volcanic or ocean biome if none exists yet: store the capability as an extensible contract and demonstrate with a neutral test reservation/overlay or two existing biomes first.

**Precedence / conflict handling:**
1. Explicit mandatory world features and their reserved influence zones.
2. Basic world land/ocean topology and impassable major terrain.
3. Required starting region and critical routes/approaches.
4. Biome suitability and ordinary weighted regions for all still-unassigned land.
5. Required locations, caves, crossings and road connections (with earlier reservations).
6. Optional decorative content.

If impossible constraints conflict (e.g., giant central ocean entirely covers a required central settlement; two overlapping forced volcanoes), Codex must produce a clear validation error or explicit priority/fallback result. No silent disappearance of a REQUIRED region. Seeds change the look **within the constraints**, not whether the mandatory feature exists.

## 6. Placement priority and a shared reservation/constraint map

**Read and obey WORLD/placement_priority.md.** Generation passes and reservation priorities are related but NOT identical: a future castle or cave entrance can reserve clearance in the macro plan before its actual art is instantiated during local chunk loading.

Recommended reservation/placement workflow:
1. World bounds, landmask, required biome layout, safe/reachable starting area and mandatory macro features.
2. Major geography: ocean, mountains, ravines/cliffs, large lakes, rivers, impassable barriers; reserve footprint and appropriate shoreline/approach bands.
3. Important hand-authored/structured POIs: towns, ruins, strongholds, special camps, temples; reserve footprints/entrances.
4. Cave entrances/exits, including links into persistent underground network registry and approach clearance.
5. Paths/roads/bridges/fords through permitted crossings between relevant POIs and starting routes.
6. Large props: tree trunks, massive boulders, dense obstacle clusters.
7. Small props/resources: bushes, mineral nodes, saplings, grass, twigs, loose items.
8. Local visual polish, natural transitions, shoreline pebbles, leaf litter and ground details.

**Reservation permissions matter:** A travel path excludes blocking roots/rocks but can allow low grass; POI interior excludes random props while admitting authored decor; lake excludes ordinary land props but can permit shoreline plants. Store shape-aware masks/polygons plus clearance, not a single blanket "occupied" boolean or an entire feature's large bounding rectangle.

Reservations must apply consistently **across chunk borders** and be queryable when neighbour chunks are unloaded. Lower-priority optional objects retry with bounded attempts or skip, rather than push aside higher-priority features. Required landmarks must be validated with reachable access/fallback.

**A larger ocean/volcano plan happens first** at macro level even if the actual tiles, animated water and environment effects are spawned much later when a player approaches.

## 7. Chunk streaming: detailed world only near player

At large map sizes do NOT resize the current one giant ground Sprite2D to enormous dimensions, generate every prop globally or maintain one physics/collision/nav body for the entire world.

Recommend:
- Stable integer ChunkCoord (x,y), deterministic conversion from logical world position, plus explicit world origin; handle negative coordinates and map edges correctly.
- Chunk sizes tunable independently from macro biome regions. A biome may span dozens of chunks and cross chunk edges.
- Chunk state machine: Unloaded → Queued → Generating Data → Applying to scene → Loaded → Unloading; cancel obsolete work and apply a per-frame/physics-safe budget.
- Load an active ring around player, prefetch an outer ring in travel direction, unload only beyond a larger hysteresis radius to avoid repeated churn at chunk boundaries. Expose active count and queue size for profiling.
- **Generate pure world data off-thread only if safe**. Godot Node/Sprite/physics creation and scene-tree mutation must occur on the main thread. Keep initial implementation single-threaded/budgeted if that is simpler/safer; parallelism is NOT the acceptance criterion.
- Use stable per-chunk derived seeds keyed by (world seed, generator version, layer, chunk coord, content stream) rather than a single advancing global RNG. Changing a distant grass population should not relocate a guaranteed ruin.
- Generate scenery per available area and biome density; avoid clipping sprites or colliders across chunk ownership seams. Choose a clear owner chunk for objects whose visuals overlap neighbours; render/collision visibility should not pop in too late.
- Keep nearby world and biome look intact: grass/organic shader detail, snow, weather, wind, contact/daylight shadows, ponds, tree/rock scenes, player camera limits, Y sorting and existing skeleton.
- Rework environment cache dependencies that assume all Actors are created at startup: add chunk/object load/unload registration or bounded local discovery, cleanup and no stale references. Likewise local navigation must be built/updated for active obstacles rather than one world-wide grid.
- Spawn player only into valid loaded terrain; transitions into caves/houses should pause/schedule chunk changes appropriately.
- World exterior bounds remain real and block travel at the finite edge. Out-of-bounds/edge chunks are handled explicitly.

**Careful migration:** The existing WORLD/Runtime/WorldScene.cs, ground surface, AdjacentBiomeBlend shader and WORLD/Generation/Placement/WorldSceneryPlacement.cs assume a fixed pair of side-by-side biomes and once-at-start composition. Replace these assumptions through interfaces/region lookups and streaming, **without deleting the two biome definitions or breaking the current production scene**. A temporary compatibility flag/mode while migrating is preferable to a single risky rewrite.

## 8. Persistence: base generation versus saved changes

A world should have a persistent **WorldRecord**:
- WorldId, chosen fixed seed, generator version, world bounds/origin, selected biome/resource definition IDs/versions.
- Frozen macro geography/regions/required anchors/entrance IDs (or sufficient immutable data to reproduce them), and game-world clock.
- Per-chunk deltas: removed/chopped trees, mined ore, placed structures, dropped/looted items, altered terrain, open doors, persistent corpses etc.
- Entity populations/territories and significant state changes.
- Do NOT save an entire duplicated procedural world if immutable baseline can be regenerated; do save everything that would be lost/inconsistent otherwise.

**RandomOnNewWorld**, not "random every scene enter." Fresh world chooses seed once; reloading that save uses the recorded seed. If generator algorithms change, do not silently reshape saved regions/entrances/landmarks. New biome definitions can appear in brand-new worlds; existing saves require explicit migration, untouched ungenerated expansion, or an opt-in upgrade policy. Stable IDs for region, chunk, feature and key entities.

## 8A. Player profiles, unique worlds and isolated save persistence — IMPORTANT

> **User requirement (2026-10-22):** Players will have **profiles with their own unique saving**. The world-generation implementation MUST be designed for that from the beginning. Do not save all players into one global world, and do not accidentally cross-load one profile's objects, world, clock or NPCs into another profile. SAVE currently contains **only scaffolding**; this section specifies the intended integration, not an existing system.

### Ownership model: Profile → Run/Save Slot → World

Distinguish three identities; don't use the character's display name or just the seed as a save-file key:

| Identity | Owns | Important rule |
| --- | --- | --- |
| **ProfileId** (stable unique ID) | Profile display name, settings/metadata, owned save slots/runs, list of characters | Renameable name is **not** the identifier; profile A cannot overwrite profile B |
| **RunId / SaveSlotId** (stable unique ID) | One character/playthrough, its status, last played time, reference to one WorldId; one active save initially is fine | Make the model extensible even if only a single slot per profile is exposed at first |
| **WorldId** (stable unique instance ID) | **That run's** finite map, world seed, generator version, required geographical features, biome map, player changes, chunks, caves, creatures and A-Life state | Two different profiles using the **same seed** still have **independent world instances and edits** |

**Safe default for first implementation:** **One profile → one active run → one private world**. This meets the player's intent now and keeps the structure extensible. Multiple characters per profile, several run slots per profile, and intentionally shared worlds are future design choices, **not requirements**. Do not implement cross-profile sharing unless explicitly requested.

**Creation flow:**
1. Player creates/selects a ProfileId. Creating a profile alone does not need to generate a giant world.
2. "New world/new run" creates a fresh RunId and WorldId, chooses an initial world seed (random or user-entered), snapshots chosen generation settings/layout version and records these IDs together.
3. The macro world plan for **that WorldId** is generated/frozen once. Streaming only instantiates nearby detail.
4. On **Continue**, load the selected run's world manifest and its persisted seed/macro layout BEFORE loading player/chunks; **do not reroll** just because \`RandomSeedOnLoad\` was true for a prior debug session.
5. Save/quit and load must restore that profile's player position, environment layer/cave location and world edits. Switching profiles must completely discard in-memory world/chunk/entity/A-Life caches from the previous run before loading the next.

### Proposed file organization — conceptual, NOT a required format

~~~text
user://saves/
  profiles/
    <ProfileId>/
      profile.meta                         # Display name, profile settings, owned RunIds
      runs/
        <RunId>/
          run.meta                         # Slot metadata, alive/dead, active WorldId
          player.state                     # Stats, inventory, position, equipment, needs
          world/
            manifest.state                 # WorldId, seed, generator/schema version, bounds,
                                           # layout/guided features, clock, creation settings
            regions.state                  # Frozen/coarse macro plan if necessary
            chunks/
              <LayerId>_<ChunkX>_<ChunkY>.delta
            entities.state                 # Persistent named agents / groups / deaths
            lifecycles.state               # Distant populations, territories, event queue
            caves/
              <CaveNetworkId>.state        # Network identity, entrances, changes
~~~

Extensions/filenames/serializer are **illustrative** (JSON, binary or other versioned format may be chosen). Godot \`user://\` is per local installation, and profile folders here are **game save profiles**, not user authentication/security boundaries. Sanitize file names and map IDs to safe canonical paths; no player-entered name should become an unchecked filesystem path.

### The right split between SAVE and WORLD

- **SAVE/Profiles/** — profile index/metadata, profile selection and optional slot/run selection; links ProfileId → RunId(s) → WorldId(s); character/run status and eventual permadeath policy.
- **SAVE/WorldState/** — orchestrates combined saving/loading of the chosen player's world plus state; checks that stored IDs match selected run and profile.
- **SAVE/Serialization/** — formats, schema version, integrity, atomic writes (write temp then replace), backup/recovery and corruption handling.
- **SAVE/Migrations/** — forward compatibility when definitions/generator/schema evolve; never silently reshape an established world.
- **WORLD/Runtime/Persistence/** — owns **world-domain data and deltas**: immutable macro-plan snapshots, chunk edits, cave modifications, entity/world state and correct chunk load/unload restoration. It should not decide which profile is active.
- **ENTITIES/Player/** — produces/restores player-owned stats, inventory, equipment, needs, current world-space position and environment/depth. SAVE coordinates the operation; don't put ad-hoc save files into each gameplay system.
- A central save coordinator identifies the **selected ProfileId + RunId + WorldId** at every save/load boundary. An unloaded chunk's delta is part of that world, never part of a global biome cache.

**Keep generation data separate from mutable save state:** World seed/generator settings + immutable macro layout reconstruct untouched base terrain; each run's modifications, structures, removed props, opened caves, resources, NPCs and A-Life deltas overlay it. Cache keys for persistent state must include WorldId (and layer/chunk/feature ID); **world seed alone is not a unique save identity**.

### Exact state to preserve for each profile/run

- **Player:** Character identity and run ID, health/needs/injuries, equipment/inventory/item instances, progression, current coordinates, heading (if used), active world/cave/floor/layer, survival/time state.
- **World blueprint:** WorldId, fixed chosen seed, generator version, immutable or snapshotted biome-region map, guaranteed ocean/volcano directives, reservations, terrain bounds and origin, placed POI IDs, caves/entrance links. Do not regenerate conflicting features after a reload.
- **Chunk modifications:** Chopped trees, mined rocks/deposits, dropped/removed items, containers and contents, built structures, destroyed/altered terrain, object health/fire state where relevant; save stable object IDs, not fragile scene instance IDs.
- **World simulation:** Game clock, weather/season phase if owned by the world, significant residents/group populations, NPC persistence, migration, deaths, event queue, off-screen tick timestamps. Each profile world has an **independent clock and event history**.
- **Player's location on load:** Preload the correct chunk(s) or cave map and validate a safe arrival point before activating input/physics; don't spawn at default Oak Forest coordinates by mistake. Restore the correct environment/biome.

When a chunk unloads, serialize meaningful current changes to its world's delta state **before freeing live nodes**. When it reloads, generate the same baseline using its fixed keys, overlay saved changes, then materialize eligible entities exactly once. Save an active world as a **consistent snapshot**, coordinating in-flight chunk generation/unload and A-Life so files do not capture conflicting times or duplicate agents.

### Permadeath — explicitly unresolved policy

The game's overall vision mentions permadeath, **but the world-after-death rule has not been chosen**. Keep profile identity separate from character/run identity and from WorldId so later you can choose between:
- Character/run ends but the profile remains; player starts a **new world**.
- Character/run ends but the profile can create a **new character in the same persistent world**.
- A fully locked/archive-only hardcore slot, optional restart modes, etc.

**Do NOT automatically delete an entire profile or its world because a character dies.** Choose and document the rule with the user before implementing permadeath cleanup. Archive/backup policy should respect that choice.

### Safe saving, loading and scaling — required design constraints

- Do not dump every generated scene/node of the giant finite world into one save file. Persist small immutable metadata, chunk-level deltas and coarse simulation records separately; cache frequently used macro layout safely.
- Writes must be scoped to a resolved profile/run/world triple; validate IDs on disk and reject mismatches, corrupt manifests and cross-profile references instead of guessing.
- Atomic writes and recoverable previous snapshots are preferable to partially written saves. Specify save timing (explicit, autosave, sleep, quit, chunk unload) and snapshot consistency.
- Save schema version **and** generator version are different. Keep schema migrations separate from generation-content migrations and the author's intentional layout edits.
- Switching profiles while another world is running must flush or cancel pending saves, streaming jobs, queued spawns and A-Life events (or safely isolate them by world ID) **before** activating the other run. No stale deferred job may write into the wrong world.
- **A-Life while unloaded ≠ permanent simulation of all profiles at the same real-world time.** Initially, only the active run/world advances its in-game simulation clock. Offline catch-up for *inactive profiles* is an optional policy (suggest default OFF), explicitly configured and bounded; save/load timestamps must not make inactive worlds advance by accident.
- Storage/quota, maximum profiles/slots and manual/automatic backups can be configurable in the owning SAVE master config when implemented. WORLD generation master config owns map/stream settings, **not** profile storage policies.

### Acceptance tests to add to Codex's implementation

1. Profile **A** creates Run A / World A at seed 123, chops a tree, builds a test marker, moves to a new chunk, saves.
2. Profile **B** creates Run B / World B at the **same seed 123**, does not chop that tree, has different inventory and world time, saves.
3. Reload A: tree remains gone; marker, position, clock, deterministic macro layout and slot ID match A. Reload B: tree still exists; B's position/inventory/clock and own edits remain untouched.
4. Switch A→B→A repeatedly, including while streamed chunks/AI are active; no A objects, cached A-Life records or delayed writes appear in B.
5. Save while a chunk is unloading and retry after simulated interrupted write; world state remains coherent or a backup restores it, never merges slots.
6. Profile renaming does not rename/reseed/delete its world; world ID mismatch is rejected. Future permadeath behavior remains unimplemented until approved.

**Milestone:** P0 should establish these ownership boundaries on paper, P1 should record a world ID/seed in a test run manifest (or prepare the interface), P3 should honor world-scoped chunk identities, **P4 must test two isolated profiles before being declared complete**, and P6 should restore each world's independent abstract population state.

## 9. A-Life-style off-screen simulation (future staged subsystem)

**Goal:** Nearby AI feels physical/continuous, but important populations in unloaded areas still advance meaningfully and can change world conditions.

Use 3 simulation tiers:
1. **Near player / loaded:** Real CharacterBody2D actors, collision, combat, perception, animations, navigation, sound, detailed interactions.
2. **Middle / loaded-adjacent or abstract:** Lightweight groups/territories with positions, tasks, schedules, path intentions and occasional resolved encounters; no full physics/sprites.
3. **Distant / unloaded:** Coarse regional records and event scheduling. Simulate population counts, health/resources of notable groups, migration, raids, hunting pressure, settlement supply, deaths, territory changes and special discoveries via discrete events/low-frequency ticks.

Key architecture:
- A global simulation/world clock with explicitly modeled in-game time steps. A-Life advances separately from frame time, and may support bounded catch-up after save/load or sleeping.
- Persistent IDs for noteworthy NPCs/packs/groups plus regional population aggregates for generic fauna. Keep player-caused kills and POI captures persistent; prevent instant free respawns.
- Deterministic keyed event streams and causal scheduling, not one random enemy spawn right outside player's camera every few minutes.
- A reification/de-reification bridge: when a chunk loads, materialize agents matching the abstract state; on unload, serialize those agents back to abstract state **once**, avoiding duplicate actors and losing health/inventory/position.
- Spatial transitions/events crossing chunk/region boundaries must be consistent even when neither chunk is loaded.
- Configurable budget (sim regions per tick, max events, max notable agents); event queue/catch-up limits prevent hours of paused gameplay or long saves from freezing the engine.
- Distant outcomes can be approximate rather than frame-perfect; **do not simulate thousands of off-screen Godot NPC nodes**. Define what can and cannot change without the player observing it, and don't allow off-screen events to corrupt player buildings or essential quests without clear rules.

**Deliberate later pass.** Do not mix full A-Life implementation into the first chunk-streaming proof. At most reserve interfaces for regional state and timestamps, then implement real simulation after world generation/persistence/streaming is proven.

## 10. Caves, vertical layers and surface-biome influence

Preserve the agreed model in Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md:
- Caves can be separate 2D scenes/layers; entrances A and B may connect one long underground network across distant surface regions.
- Surface + Cave biome + depth influence ore, enemy, plant and feature eligibility (e.g. emeralds permitted under Oak, not under Desert, even if both use Ravine Cave).
- Use stable world coordinates / logical mapping and an entrance registry, never assume a whole cave inherits the biome of the entrance used.
- Macro plan reserves cave entrance IDs and safe external approach footprints before chunks are generated. Cave interiors/layers may use their own chunk streams later.
- The surface biome sampler must work for underground coordinates **without loading the surface scenery**.

Implementing cave networks themselves is not part of the first world-generation migration pass, but the macro layout and reservation interfaces must not make these future features impossible.

## 11. Folder ownership and suggested modules (PROPOSAL)

Reuse existing folders, no new giant root domain:

~~~text
WORLD/
├── WorldMasterConfig.cs                   # Root composition / existing
├── DefaultWorldConfig.tres                # Root resource / existing
├── Generation_Implementation.md           # THIS planning document
├── placement_priority.md                  # Existing priority rules
├── Biomes/
│   ├── Shared/
│   │   ├── WorldBiomeDefinition.cs        # Extend with size, weight and preferences
│   │   ├── BiomePopulation.cs             # Change counts to area-aware densities
│   │   └── [biome selection/region data]
│   ├── Definitions/
│   │   ├── OakForest/Biome.tres
│   │   └── PinkMeadow/Biome.tres
│   └── Layouts/
│       ├── OakAndMeadow.tres              # Legacy compatibility during migration
│       └── [new weighted macro layout resource]
├── Generation/
│   ├── World/
│   │   ├── [WorldGenerationMasterConfig.cs + default .tres]
│   │   ├── [MacroWorldBuilder / biome-region map]
│   │   ├── [GuidedFeature definitions and layout preset]
│   │   └── [WorldReservationMap]
│   ├── Biomes/                            # Region selection, shapes, climate fit
│   ├── Terrain/                           # Ground geometry and major barriers
│   ├── Water/                             # Coast, lakes and rivers
│   ├── Locations/                         # POIs and cave entrance reservations
│   ├── Caves/                             # Future underground layout
│   └── Placement/                         # Chunk-local props, resources and entities
├── Runtime/
│   ├── WorldScene.cs                      # Runtime coordinator
│   ├── Chunks/                            # Chunk IDs, data, ownership, lifecycle
│   ├── Streaming/                         # Active/prefetch/unload management
│   └── Persistence/                       # Baseline metadata + deltas
└── [future world simulation service]      # Abstract populations / event clock
~~~

Don't add duplicate shaders, another authoritative biome list, or parallel master configs. WORLD owns geographical and biome/population placement; ENTITIES owns animal/enemy species and AI; ENVIRONMENT owns weather/lighting; SAVE/Persistence owns durable save integration. Existing loaded-node composition may move behind a world streaming orchestrator.

## 12. Recommended Codex passes — IMPLEMENT SMALL, TEST EACH

**P0 — Architecture audit + frozen baseline**
- Read all referenced docs/code and confirm current two biomes, content/pond/environment/skeleton dependencies, plus the existing SAVE/Profiles and SAVE/WorldState scaffolding.
- Define ProfileId / RunId / WorldId ownership before new persistent runtime data exists; write down default one-profile/one-run/one-world rule and unresolved permadeath decision.
- Make a concrete data-flow diagram and identify changes required to shaders, scenery placement, world startup, navigation and caches.
- Measure current startup time, peak nodes, active scenery, frame time and memory. Make a reversible test scene/profile.
- **Pass when:** Existing Oak + Pink Meadow world still runs identically.

**P1 — Macro map and biome selection (FIRST playable milestone)**
- Add biome min/max region size, weight and optional climate affinity to the existing definitions; add world generation master resource.
- Create a seeded **finite coarse macro biome map** with fully covered land and repeating organic regions. Only Oak Forest + Pink Meadow exist; no placeholders or mystery biomes.
- Add an optional simple top-down biome-color overview/debug print (regions, counts, min/max size, % land assigned, deterministic hash).
- Give each newly generated world a stable WorldId separate from its seed, scoped to the initiating profile/run; design a minimal saved manifest contract for later persistence.
- Do not move entire production scene to streaming yet if the existing look would break.
- **Pass when:** 10+ seeds have 100% valid land biome coverage and no invalid IDs; same seed/settings yield same map hash; different seeds vary region shapes; new weight/size values affect the macro map; world edges valid; two actual biome definitions referenced.

**P2 — Required geography / guided layout / reservations**
- Introduce required vs preferred features and normalized map regions (top-right, centre etc.).
- Demonstrate a reserved mandatory **region-shaped mask** with currently available content (or pure preview data) before implementing actual volcanos/oceans. When new ocean/volcano content exists, plug it into the same contract.
- Implement permission-aware priority reservations and deterministic feature IDs.
- **Pass when:** a required large region always appears inside its permitted quadrant for multiple seeds and requested size; collisions/conflicts warn/fail predictably; lower-priority candidates cannot overwrite it.

**P3 — Chunk data and nearby streaming**
- Add chunk coordinate/state and active ring; generate local details from frozen macro map and existing scenery/biome resources.
- Ensure chunk state/cache identity includes WorldId and layer so profile-switching or identical-seed worlds cannot share mutable chunks.
- Ensure continuous biome border and no seams or duplicated/missing props at chunk boundaries; migrate ground/shaders with minimal visual regression.
- Chunk unload/reload without memory/node leaks, with the same props and colliders returning at identical locations.
- Fix environment actor caches and pathfinding lifecycle when chunks stream.
- **Pass when:** traverse several biome regions and return, no pop-in inside visible bounds, no full-world node explosion, controls/weather/pond/snow remain sound.

**P4 — Per-profile persistent world saves and deterministic edits**
- Implement the chosen ProfileId → RunId/SaveSlotId → WorldId association through SAVE/Profiles and SAVE/WorldState rather than global singleton save files.
- Persist world seed/region plan, chosen macro features and chunk deltas for a minimal changed object (e.g. a removed tree/test marker), along with player coordinates, current layer, representative inventory and world clock.
- Validate save/reload, exit/re-enter chunks, no resurrection of modified props, no duplicate object identities. Use atomic replacement, integrity validation and recovery handling.
- Lock generator version and stable ID strategy before expanding feature count; existing saves do not silently regenerate terrain.
- **Pass when:** profile A and B can have **the same seed but distinct world IDs and distinct edits, positions and clocks**; A→B→A switching cannot mix chunks/data; quit/reload preserves the correct run; changing random-new-world settings never replaces a saved world seed. Include interrupted-save recovery testing.

**P5 — Priority content placement + POI/paths/caves scaffolding**
- Expand macro reservation and local placement passes to real large obstacles, required POIs, cave-entrance locations and roads, as those assets arrive.
- Required-feature footprints/paths remain reachable while optional grass/trees respect clearance.
- **Pass when:** seeded POI and entrance remain stable/accessible across chunk unload/reload and do not overlap water/trees.

**P6 — Abstract off-screen A-Life (separate substantial feature)**
- Regional records + clock/tick queue, a simple migrating animal group or wandering patrol off-screen.
- Scope all abstract entities, groups, event queues and clocks to the active WorldId/RunId; inactive player profiles should not advance automatically unless the user explicitly enables bounded offline simulation.
- Spawn/dissolve concrete actors at chunk activation, carry health/position/group identity through unload/load.
- Add deterministic catch-up and performance budget counters; only then expand to factions, ecology, settlements and long-term events.
- **Pass when:** player leaves a region, off-screen state meaningfully changes, returns to a consistent encounter, and no duplicate/deleted NPCs or runaway sim costs occur.

**Strong suggestion:** Codex implement **P0–P1 first**, review results with the user, then P2–P3. Do NOT attempt every phase in one massive refactor. Change only the explicitly approved pass; one commit per pass/job.

## 13. Required QA / acceptance checklist

### Biome and geography correctness
- [ ] Exactly the two configured land biome definitions fill 100% of eligible land at the start. One biome also fills 100% if it is the only enabled one.
- [ ] Region minimum/maximum size and relative weights are editable and demonstrably affect results; "weight" never falsely claims an exact land-area percentage.
- [ ] Biome boundaries are organic/connected enough for meaningful regions, without small checkerboard fragments or visibly repeated straight seams.
- [ ] Multi-seed stress tests terminate within bounded time even with awkward region-size/weight settings.
- [ ] Guided required feature respects its target zone, scale and priority in multiple seeds; impossible constraints are reported, not ignored.
- [ ] Critical spawn/POIs remain accessible and terrain/props follow WORLD/placement_priority.md.
- [ ] Climate weighting is optional/tunable and doesn't block fantasy-biome variety by default.

### Streaming and stability
- [ ] Same world seed + generator version produces identical macro layout and local detail/IDs, independent of chunk load order or travel path.
- [ ] Only a bounded ring of loaded chunks has active scene nodes, collision and expensive AI.
- [ ] Travelling across chunk seams preserves visuals, physics, Y sorting and natural biome transitions.
- [ ] Unload/reload does not regenerate a new pond, move a tree, duplicate a creature, leak event handlers/shadows or leave orphaned collider/nav references.
- [ ] Scene and environment initialization no longer assume "all scenery exists in Actors at startup".
- [ ] Existing production Oak/Pink art, lighting, wind, weather, rain, snow, pond and player/skeleton stay functional.
- [ ] Avoid loading all chunks to compute a world overview; no giant all-world Sprite2D or full-world AStarGrid2D.
- [ ] Log/visualize seed, chunk coordinates, region ID, biome ID, active chunk count, generation queue and time spent per pass.

### Per-profile save isolation (P4 is NOT complete without these)
- [ ] Each player profile has a stable ProfileId, its own save slot/run, and a separate WorldId; display names and identical seeds cannot merge saves.
- [ ] World manifest, macro biomes, placed required features and persisted chunk modifications are loaded using the selected profile/run/world IDs.
- [ ] Player stats/inventory/equipment, position, cave/layer, game time and meaningful world changes round-trip after quit/reload.
- [ ] Profile A and B with **the same seed** preserve different tree removals/buildings and independent A-Life/world clocks.
- [ ] Switching active profiles clears or segregates pending chunk loads, cached nodes, delayed saves and simulation jobs.
- [ ] Interrupted/corrupt saves are detected/recoverable; slot IDs are validated, file paths safe, and writes atomic.
- [ ] Permadeath world-retention/deletion policy is confirmed before any cleanup rule is implemented.

### Persistence and A-Life (when their passes begin)
- [ ] Saved world identity, seed, generator version and required layout are persistent.
- [ ] User edits are restored without overwriting generated baseline and work across chunk boundaries.
- [ ] Off-screen actors/populations advance at controlled tick/event budgets; they do not run full Godot AI/physics while unloaded.
- [ ] An entity is materialized only once, and its abstract state round-trips with health/position consistently.
- [ ] Game-time jump/sleep/save-load catch-up is bounded, testable and won't freeze the game.

### Validation discipline
- [ ] Run C# compilation and Godot scene/import checks; confirm no broken paths, duplicate GlobalClass names, .tres or shader loading errors.
- [ ] Test fixed seed, random new seed, 1 biome, 2 biomes, small/large bounds, corner cases of min/max size and impossible forced directives.
- [ ] Compare startup, CPU frame time and memory against measured baseline. Set an explicit test hardware/performance budget rather than inventing guaranteed FPS.
- [ ] Update actual owner READMEs/config docs after every implemented pass; mark planned vs implemented truthfully.
- [ ] Preserve DEBUG prototypes and do not remove the existing production world/biomes as a shortcut.
- [ ] One GitHub commit per implementation job, per CodePreferences.md.

## 14. Questions to resolve *with Codex* before P1/P2

1. **World extent and scale:** What are the initial world dimensions in world units, and how many seconds/minutes should crossing one typical biome take? Pick testing presets (small/medium/huge) before locking region sizes.
I'm updating this. My response: small = 3 min. med = 5 min. huge = 8 min (regular walking)
2. **Biome size unit:** Is Min/Max a *diameter* in world units, a number of macro cells, or an approximate area? Define one meaning and keep it stable for UI/resources. --- whichever is easiest? approximate area in diameter and the biome will pick between
3. **Macro representation:** Raster grid, vector regions/sites with warped boundaries, or hybrid? Show why the choice guarantees full coverage and scales to the selected huge world. --- not sure what this means
4. **Ocean/volcano priority:** Are they landmask + geological features, mandatory biome instances, or POIs? Use typed feature definitions rather than conflating them. --- geological features, but big ones. some multi biome sized. ocean could be like equivalent of 10 biomes stacked together. things could exist only in the ocean.
5. **Climate bias:** Which rough fields are worth adding first (moisture/temp/elevation), and which should be deferred? Avoid prematurely forcing realism. -- maybe i'd prefer biomes of certain close temps to be relatively close
6. **Persistence policy:** Freeze *all* macro biomes at creation or calculate immutable per-region seeds? Decide versioning and how future expansion works without changing already committed regions. -- what?
7. **Chunk sizing:** What is the target loaded radius for the current 1920x1080 camera/zoom? Test against actual canopy and shader extent. -- somewhere slightly outside this boundary
8. **Gameplay time and A-Life:** What tick size/region resolution and major events matter initially? Which entities remain named/persistent? Avoid designing a universe-wide detailed simulator before a small proof. -- what
9. **Authoring:** Which Resource(s) should define major geographical directives? Can a future Avalonia World Creator edit them without duplicating the canonical data? --- what
10. **Backward compatibility:** How will the existing two-biome authored boundary and current saved/test seeds behave during migration? Have an explicit rollback/test mode. --- migration? what u mean. they are already in the production folders?

### Profile persistence decisions (please resolve before implementing SAVE/P4)

- **Default assumed:** Separate private world per active profile/run; no shared world state between profiles. Confirm whether each profile should eventually contain multiple selectable save slots/characters.
- **Permadeath:** Decide whether character death preserves the world's terrain/buildings, creates a brand-new world, or archives the run. Keep this switchable by design; don't wipe anything yet.
- **Clock behavior:** Should an inactive profile's A-Life/weather/season clock remain paused (recommended initially) or progress while not being played? If offline progression is enabled later, bound the catch-up.
- **Autosave:** Choose manual/autosave interval and recovery/backup policy. Avoid adding a save every frame or per object change.
- **Loading:** Verify that each selected profile resumes in its saved overworld/cave layer and coordinates, without regenerating a different world seed.

## 15. Recommended first instruction to Codex

> Read WORLD/Generation_Implementation.md, WORLD/placement_priority.md, Notes/World_Generation_Direction.md, relevant WORLD/ and biome READMEs, CodePreferences.md, and inspect the latest production scene and scripts. **Plan only P0/P1 first:** introduce a configurable large finite world macro layout with organic, fully covering, repeated Oak Forest/Pink Meadow regions. Extend their existing biome Resources with min/max region size and selection weight. Preserve all art/biome definition ownership and do NOT build the full A-Life, ocean, volcano or massive streamed world in one unreviewed pass. Propose the precise schema, algorithm, performance budget, migration points and tests; flag better approaches or conflicting requirements before implementation.

---

**Key invariant to remember:** With only Oak Forest and Pink Meadow enabled, **all available land is covered by those two biome types in randomly sized, organically shaped regions**. Required oceans/volcanoes/major destinations get first claim when such features are configured, and nearby chunks are the only places with fully active scenery and detailed AI. Distant regions retain a persistent abstract state for a later A-Life system.
