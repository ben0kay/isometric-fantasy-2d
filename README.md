# Fantasy Survival Sandbox — 2D Top-Down Reboot

**First P3 stability + SO1 pass:** Off-screen chunk wind/wetness/snow updates suspend through shared artwork bounds; sun casters use conservative shadow reach. Chunk effect binding is staged while hidden, partial binding cancels cleanly, and static streamed visuals disable interpolation. The right-side F9 diagnostics expose culling A/B and CPU timings; F7 shows the map. Terrain/systems/debug UI now have clear containers; Actors/Y-sorting stays intact. Target-machine rendering/flicker verification remains open. See [streaming](WORLD/Runtime/Streaming/README.md) and [scene organization](SCENES/World/scene%20organize.md).

## Current production state — 22 October 2026

**Production F6 now runs the P0–P3 finite streamed world by default.** The current macro map is **45,000×33,750 world units**, currently filled by repeated Oak Forest/Pink Meadow regions. Its physical scenery, ground, collisions and local navigation load in chunks around the player; the old **3840×1728** adjacent-biome scene remains an optional **compatibility preview** when `WorldMasterConfig.Streaming.Enabled=false`. The independent F5 DEBUG/TopDownTest is preserved. This is a playable **technical foundation**, not a finished survival sandbox.

| System/pass | Implemented now | Still missing / important limitation |
| --- | --- | --- |
| **P0/P1 macro map** | Seeded finite, queryable, connected biome regions; any number of eligible biome Resources; weighted target diameters, complete land coverage, hashes and detached blueprint manifest | No oceans/continents, true height, rivers or climate fields; full map is currently assigned land |
| **P2 feature planner** | Required/preferred polygon reservations, protected start, seeded priorities/permissions/clearance, F7 overlays and detached *value-only* manifest feature snapshots | Reservations do not yet make villages, roads, water, volcanoes or caves |
| **P3 chunk streaming** | 512-unit chunks, active radius 2, prefetch 3, unload 4, stable coordinate-keyed scenery density, cross-chunk spacing, ground/biome materials, chunk-local environment/light/fire/occlusion registration, local navigation | Loading data and some lifecycle/nav steps remain synchronous; no guaranteed FPS; **graphical/performance verification still pending** |
| **P3 session world changes** | Removed objects and fire/burn progress survive chunk unload/reload **during this running scene** | **No disk save, Continue, persistent player profile, or offscreen fire simulation**; starting another world session loses edits |
| **Generic content/actors** | One [shared tree scene](WORLD/Content/Vegetation/Trees/README.md) with Oak/Pink Blossom Resources; one [shared entity scene](ENTITIES/Shared/README.md), common hit/health/death/optional chase, test skeleton and production axe hit registration | No full A-Life, loot/resource harvesting economy, creature populations, Avalonia import/catalog |
| **Environment/feedback** | Shared clock/wind, projected sun/contact and LL0–LL1 native torch shadows, clouds, colour, snow, 3-layer GPU rain and wet leaves, adjustable overcast shadow reduction, pond/wading, fire FIRE1/FIRE2, tree occlusion | Seasons/climate scheduling, lightning-to-tree ignition, wildfire spread, LL2–LL3 reusable light framework |

**P3 acceptance caution:** The user reported grass-only spawn, severe lag and briefly flashing giant trees after the first P3 commit. A follow-up reduced protected-start clearance, hid/uninterpolated uninitialized sun silhouettes, shared foliage shader variants and trimmed unnecessary refreshes. Godot/.NET headless checks passed, but **Windows graphical stability and actual frame times must still be verified**. The soft **3 ms / 12 scenery-roots-per-frame** apply budget does not cover initial 25 active chunks, all synchronous generation, shader compilation or navigation. Read [current P3 handoff](WORLD/Generation_Implementation.md) and [streaming guide](WORLD/Runtime/Streaming/README.md) before changing the streaming architecture or claiming it is performance-complete.

**Run/controls:** F6 runs `SCENES/World/World.tscn` (**streaming ON by default**); **F7** opens macro biome/reservation/chunk statistics; **F9** toggles removable environment controls (including rain, wet foliage, overcast shadows, torch shadows); removable Fire controls ignite/reset trees. **F5** launches the independent DEBUG/TopDownTest scene from `project.godot`. The 1920×1080 viewport is unchanged. New scene loads choose a fresh random seed unless fixed-seed replay is configured.

**P4/P5/P6 remain planned:** P4 = separate player-profile/run/private-world durable saves and recovery; P5 = actual terrain features/landmarks/paths/cave links; P6 = lightweight offscreen A-Life. Future Avalonia authoring and climate/season systems are also **not implemented**. [WORLD/Generation_Implementation.md](WORLD/Generation_Implementation.md) is the current phase handoff; [SCENES/World/scene organize.md](SCENES/World/scene%20organize.md) is a **plan only**, not an already-applied scene tree refactor.

**Four distinct feature statuses:**
- **Implemented in production:** Runs in World.tscn or its production dependencies; check module docs/tests.
- **Implemented in DEBUG:** Disposable isolated prototype, not automatically production code.
- **Scaffolding only:** Directories/READMEs/configs without full working mechanics.
- **Planned / exploratory:** Design intentions, agreed concepts and open alternatives. **Notes do not themselves authorize implementation.**

## Start here — future ChatGPT / Codex / contributors

1. Read **this README** to understand the game and what exists today.
2. Read **[CodePreferences.md](CodePreferences.md)** **before touching code**. Follow the author's folder layout, C# style, comments/regions, MasterConfigs, reusability, simplicity, performance priorities and **one GitHub commit per job**.
3. Read the **README inside the exact feature folder** you are modifying, its `MasterConfig` / `.tres` settings, relevant scene and actual scripts. Some older notes may be superseded by newer documentation or code.
4. Start current production work at [SCENES/World/README.md](SCENES/World/README.md), [WORLD/Runtime/Streaming/README.md](WORLD/Runtime/Streaming/README.md), and the [P0–P6 handoff](WORLD/Generation_Implementation.md). Use [DEBUG/TopDownTest/README.md](DEBUG/TopDownTest/README.md) only for the independent experimental forest; treat older notes as historical when superseded.
5. **Do not silently promote debug experiments to production, reorganize unrelated modules, or implement idea documents on sight.** When promotion is requested, extract reusable parts into their owning production folders deliberately, preserving module boundaries, inspector-tweakable data and removable test wiring. Verify the real code and current scene rather than relying only on summaries.


## Long-term architectural north star — READ BEFORE CODING

**Compiled from the full Notes collection and current feature READMEs.** This is a durable guide for future ChatGPT/Codex sessions so new systems fit the game's eventual scale and authoring model. **It is not a claim these features already exist, and not an implementation order.** For every job first inspect actual code, [CodePreferences](CodePreferences.md), the owning module and current scene wiring. Keep experimental proof-of-concepts separate from approved production work.

### 1. Ultimate goal: Avalonia external content-authoring toolbox (PLANNED)

**End-state vision:** Use an external **Avalonia toolbox** to create/edit a tree species, rock, creature, item, crafting recipe, biome, prop or supported structure **by importing artwork and authoring resource data**, without coding a new C# class or hardcoding a new spawner for each content type. **Reuse shared behaviour and templates; add new C# only when a genuinely new mechanic is needed.** The Avalonia integration/catalog loader is **not yet implemented**; the existing shared tree/entity templates are important stepping stones.

**Desired add-a-tree workflow (future, illustrative):**

1. Toolbox creates a stable unique content ID (for example tree.pine), name and optional artwork variants.
2. Author sets sprite(s), correct anchor/rooted collider, conservative footprint and spacing, scale/mirror rules and optional wind, snow, fire/charred, hit and harvesting profiles.
3. Toolbox saves/imports textures and data Resources in the canonical WORLD/Content/Vegetation/Trees/Pine/ folder (or an intentionally supported content pack), reusing **Shared/Tree.tscn** instead of creating per-species scripts/scenes. Asset/import/resource references must resolve.
4. A **future content registry/catalog** finds and validates IDs, template references, dependencies and schema versions. **A separate conflicting database inside Avalonia is undesirable.**
5. Add this canonical definition as a reference in one or more **biome population resources** with data-controlled compatibility-preview counts/clusters **and P3 density (InstancesPerMillionUnits)**, weights/scale/eligibility. Merely creating a file should not make it spawn in all biomes.
6. The generic, seeded placer instantiates the new tree from data and applies shared wind, snow, fire, collision, shadows and other configured features. **Do not add special-case “if pine” branches to general generator logic.**

**Already implemented for no-code content reuse:** [SceneryDefinition](WORLD/Content/Shared/SceneryDefinition.cs) has Id and generic scenery scene/footprint data (for rocks/grass); [TreeDefinition](WORLD/Content/Vegetation/Trees/Shared/TreeDefinition.cs) supplies Resource-based VisualVariants and one [Shared/Tree.tscn](WORLD/Content/Vegetation/Trees/Shared/Tree.tscn), used by both Oak and Pink Blossom. [WorldBiomeDefinition](WORLD/Biomes/Shared/WorldBiomeDefinition.cs) and [BiomePopulation](WORLD/Biomes/Shared/BiomePopulation.cs) drive macro regions, preview populations and P3 density. [EntityDefinition](ENTITIES/Shared/EntityDefinition.cs) supplies shared sprite/health/optional chase on [Entity.tscn](ENTITIES/Shared/Entity.tscn). **Not yet implemented:** external Avalonia authoring, automatic registry/discovery/validation or content packs; references are still explicitly authored in Resources.

**Do-not-break contracts:** definition ≠ mutable individual instance; stable unique IDs ≠ file names or Godot node names; canonical assets defined once and referenced by many biomes; reusable opt-in profiles/behaviours; no new class per tree/item/enemy where existing archetypes suffice; resource schema and dependency validation; versioning for future saves. The exact Avalonia-to-Godot authoring/serialization design remains open. A PNG alone cannot supply collision, identity and behaviour.

### 2. Large finite world — P0–P3 IMPLEMENTED; geography and durable persistence NEXT

**Existing runtime:** P0/P1 generates a complete, deterministic, queryable **finite macro biome map** using the configured Resource list. P2 freezes required/preferred polygon **feature reservations** and a protected starting anchor. P3 draws nearby chunk ground and canonical resource-driven scenery, handles load/unload/material/shadow/occlusion/fire lifecycle, and maintains **session-only** changed-object deltas. It is already the **default F6 world**—do not implement a duplicate streamer or revert to the compatibility preview as the primary architecture.

**Existing separation:** coarse biome **regions are NOT streaming chunks**; one biome spans many chunks. The current streamed ground is flat 2D artwork/palette blending, not oceans/relief. Local decoration uses a globally keyed candidate grid and a density per million world units, with deterministic cross-chunk spacing independent of traversal/load order. P2 reservations exclude incompatible objects but **do not create** an actual village, lake or road. Default chunk size is 512; current prototype world bounds are 45,000×33,750. Bounds, region diameters and chunk sizes are separate settings.

**Future conceptual pipeline (not a list of implemented systems):** add continent/ocean landmasks, relief/terrain and rivers; meaningful fantasy biome contrast and region themes; P5 actual reusable settlements, caves, landmarks, routes and entrances; P4 disk-backed deltas and per-profile/run/world saves; P6 abstract offscreen ecology; possible guided World Creator, finite-world expansion and versioned old-world migration. Keep climates a soft influence, allow visually distinctive neighbours, and don't assume infinite worlds or the final world size have been chosen.

**Protect existing contracts:** generation/world IDs, feature reservation ownership, stable candidate/object IDs, cross-chunk seams, biome Resource references, world-specific cache cleanup, shared weather/clock, and reproducible generation under a fixed generation/content version. P3 preserves removal/burn progress only **inside one running session**; no selected player profiles, disk saves, Continue or permanent old-world migration exist. The startup/preload system and some data/nav/shader work are synchronous; the per-frame application budget is **not** a hard frame-time cap. The latest P3 fixes still require in-game Windows performance verification.

**Authoritative detail:** [generation phases / regressions / P4 policy](WORLD/Generation_Implementation.md), [macro generator](WORLD/Generation/World/README.md), [feature reservations](WORLD/Generation/World/Features/README.md), [streaming lifecycle](WORLD/Runtime/Streaming/README.md), [placement priority](WORLD/placement_priority.md), and [SAVE future ownership](SAVE/README.md).

### 3. A-Life-style living world: populations + simulation LOD (DESIRED; NOT IMPLEMENTED)

**Desired experience:** deer flee wolves, predators hunt prey, bears defend territory, creatures react to other creatures, and NPCs/travelers/bandits potentially follow meaningful routines. **The player is one participant in the ecosystem, not everybody's universal target.** “A-Life” is a long-term experience goal; no algorithm or simulation scale is locked.

- **Behaviour types:** passive, neutral, territorial, predatory, hostile and intelligent, affected by hunger, fear, injury, group/pack, territory, sound, light, weather and time of day.
- **Population sources:** residents at dens/nests/camps/settlements; roaming herds/predators/travelers/patrols; rare special events. Avoid endless arbitrary edge-of-camera monster spawning. Killing residents can make an area quieter until gradual recolonization.
- **Simulation levels of detail:** near the player, fully active agents with navigation, perception and interactions; farther away, lighter agent/group/territory/schedule records; far-off or unloaded regions, **cheap aggregate/event/elapsed-time** advancement. Don't simulate thousands of invisible CharacterBody2D nodes every frame.
- **State handoff/persistence:** world/site/actor/group IDs, home areas, territories, schedules and consequences should persist through chunk unload/reload. Prevent agents duplicating or resetting simply because a chunk streamed out.
- **Fair danger:** wildlife and enemies may be active **day and night**; hour changes *how* risk occurs, not a monster-on-at-night switch. Avoid difficulty escalation tied only to days survived. Shelter helps with protection, temperature, recovery and supplies without forbidding night exploration.

**Current reality:** the one test Skeleton Basic uses [shared Entity.tscn](ENTITIES/Shared/Entity.tscn) / EntityDefinition for tool hits, health, death and an optional GroundChase; its local navigation sleeps when distant. This is **not** a resident population, ecology or offscreen A-Life simulation. Build only approved follow-ups on [ENTITIES/Shared](ENTITIES/Shared/README.md) and [CORE/Simulation](CORE/Simulation/README.md).

### 4. Caves: independent cave biome + overhead surface biome + depth (AGREED DESIGN, NOT IMPLEMENTED)

**Important fixed design rule:** underground resources and inhabitants depend on **both** the cave biome at the position and **the surface biome geographically ABOVE THAT SPECIFIC POSITION**, plus depth/geology. A *Ravine Cave* under Oak Forest may contain emeralds; the **same Ravine Cave under Desert** may not, even in one connected network. Do **not** duplicate cave biomes as OakForestRavine or DesertRavine.

Separate 2D cave scenes/layers can remain tied to logical surface X/Y and depth. Connected cave systems may pass beneath multiple surface biomes and exit via multiple entrances (even in distant regions); use stable, persistent portal/connection IDs. The exit must return the player to that specific location, **not always the entrance originally used**. Determine the overhead biome from coarse geography **without rendering/loading the surface scene**. Exact coordinate transforms, depths, portal generation and save format are open. Read the [full cave note](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md).

### 5. Survival, buildings, ecosystems and other future mechanics (MOSTLY IDEAS)

- **Food:** per-item-type freshness, spoilage and nutritional roles; *separate harvested batches keep independent age* even if stacked. Storage conditions and temperature influence decay. Preservation/smoking/drying/salting, cooking and balanced nutrition should matter without needless micromanagement. Advance while chunks are unloaded via elapsed time/on-access work, not per-food frame processing.
- **Inventory and equipment:** weight **and** volume, bags/slots/container limitations, encumbrance; separate canonical item type from mutable instance durability, condition, freshness and timestamp. Clothing insulation/wetness and tool wear/repair support longer survival.
- **Health and survival:** hunger, thirst, **long-term fatigue versus short-term stamina**, temperature/exposure, illness/injuries, sleep and shelter; status effects should interact through shared modifiers, not disconnected meter sprawl.
- **Buildings:** modular floor tiles and **tile-edge walls**, rooms, doors, windows, furniture. Outdoors a completed house shows a **full roof**; entering hides/fades its roof to reveal the playable interior, restoring it when leaving. Possible hybrid automatically assembled roofs, multi-floor building ID + floor/depth, basements, stairs, shelter heat and long-term repair/persistence. **32×32 tiles are provisional, not locked.**
- **Fire and world interactions:** generic ignition/lifecycle/effects in [SYSTEMS/WorldInteractions/Fire](SYSTEMS/WorldInteractions/Fire/README.md); ENVIRONMENT supplies wind/rain/lightning/light; WORLD owns species art. Production FIRE1/FIRE2 and **P3 in-session burn deltas** exist; lightning ignition, spread, damage, extinguishing, offline burning and **disk-persistent** burnt objects remain future.
- **Living consequences:** harvesting/regrowth, water quality, muddy/snowy terrain movement, noise/scent, wildlife displacement, shelter and building wear. Keep common mechanics reusable and data-driven.
- **Setting:** medieval/high fantasy now (possibly prehistoric elements). A later sci-fi/alien/robotic world is **optional**, not permission to introduce it into the starting medieval scene.

**Design priority:** believable interconnected systems that create choices, not maximum complexity or survival chores.

### 6. Folder ownership — future coders must NOT create parallel homes

| Canonical owner | What belongs there |
| --- | --- |
| [WORLD/Content](WORLD/Content/README.md) | Canonical scenery, tree, rock, structure, assets and species variants (one definition reused across biomes). |
| [WORLD/Biomes](WORLD/Biomes/README.md) | Overworld Definitions/ and Shared/ types, content references and profiles. Cave biomes separate. |
| [WORLD/Generation](WORLD/Generation/README.md) | Generic deterministic generation/placement/landmark algorithms, not species-specific spawners. |
| [WORLD/Runtime](WORLD/Runtime/README.md), [SAVE](SAVE/README.md) | **P3 built:** chunk stream and memory-only changed-instance state. **P4 planned:** per-profile/run/world durable disk saves, migration and Continue. |
| [ENVIRONMENT](ENVIRONMENT/README.md) | **One authoritative wind/clock**; daylight, weather, rain, snow, lightning and atmosphere. Not AI or generic burn state. |
| [ENTITIES](ENTITIES/README.md) | Production Player plus [shared data-driven Entity scene](ENTITIES/Shared/README.md) and basic optional chase; richer NPC/population/AI remains future. |
| [SYSTEMS](SYSTEMS/README.md) | Harvesting, crafting, building, farming, cooking, interaction, effects; fire lives under **WorldInteractions/Fire**. |
| [ITEMS](ITEMS/README.md) | Canonical item/material/food/tool/armour definitions (instance state belongs to inventory/runtime). |
| [CORE](CORE/README.md) | Shared IDs, future content registry, events/contracts and slow simulation scheduling where justified. |
| [COMBAT](COMBAT/README.md) | Reusable hit/damage/defense rather than another per-enemy or player-only damage engine. |
| [SCENES](SCENES/README.md) | Composition like World.tscn; [scene root regrouping](SCENES/World/scene%20organize.md) is a **plan only**, not an implemented refactor. |
| [DEBUG](DEBUG/README.md) / [UI](UI/README.md) / [FEEDBACK](FEEDBACK/README.md) / [AUDIO](AUDIO/README.md) | Isolated experiments/developer panels / future player UI / **implemented foliage occlusion** and future screen feedback / audio. Production runs without developer UI. |

**Cross-system example:** biome **references** canonical Oak; generic placement spawns an instance; Fire/Harvesting changes that tree; ENVIRONMENT gives wind/weather; ITEMS defines its dropped logs; SAVE will eventually record that this exact tree was removed/burnt. Never copy oak art into a biome, build a second wind simulator for fire, write every species directly into WorldScene or make production depend on DEBUG/TopDownTest assets/code.

### 7. Scope guardrail and implementation status matrix

Before writing code identify (a) **implemented vs debug vs scaffold vs planned**; (b) the **existing correct owner**; (c) **new data or truly new shared behaviour**; (d) whether content IDs, seed determinism, streaming/persistence, offscreen simulations, cave eligibility or Avalonia authorability are affected. **Do not implement all these goals automatically.** Use small, testable passes; preserve the production world and independent DEBUG test; follow [CodePreferences](CodePreferences.md) on modular C#, README/MasterConfigs, method comments/regions, performance and **one GitHub commit per job**.

| Mechanic area | Actual status |
| --- | --- |
| **P0/P1:** finite seeded multi-region macro map, biome Resources, F7 overview/manifest | **Implemented** |
| **P2:** priority feature/start reservations and permission queries | **Implemented; reservation footprints only** |
| **P3:** finite local streaming, density-based scenery, active chunk environment/navigation and session deltas | **Implemented; recent rendering fixes committed, graphical/FPS acceptance pending** |
| Old 3840×1728 Oak/Pink world | **Optional fallback, not the default** |
| Shared Tree and Entity templates, test skeleton and axe hits, FIRE1/FIRE2, player occlusion | **Production foundations implemented** |
| Shared wind/clock, sun/local shadows (LL0–LL1), layered rain/wet foliage, adjustable overcast, snow/pond | **Production effects implemented** |
| **P4:** separate profile/run/private-world saves, durable chunk deltas/Continue/migrations | **Not implemented** |
| **P5:** real terrain features, towns/POIs/routes/cave entrances | **Not implemented** |
| **P6:** resident wildlife/NPC populations and tiered offscreen A-Life | **Not implemented** |
| Avalonia toolbox/import and content registry; player inventory/needs/crafting/building | **Future goals/plans** |

**Read these original sources for detail:** [World generation](Notes/World_Generation_Direction.md) · [Wildlife/hostility](Notes/Ideas/World_Hostility_Wildlife_Exploration.md) · [Underground geography](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md) · [Player buildings](Notes/Ideas/Playerbuildings.md) · [Survival/mechanic ideas](Notes/Ideas/FutureMechanicIdeas.md) · [Production implementation](Notes/Production_Implementation.md) · [Snow experiment](Notes/Snow_Accumulation_Codex_Test.txt). Notes/work.md is empty; it is not an active roadmap.

## Run production or the preserved DEBUG scene

- Open in **Godot .NET** and build the C# solution. With `SCENES/World/World.tscn` selected, **F6 runs the default P3 finite streamed world**. **F7** shows biome regions, feature reservations and live chunk counts. **F9** toggles the optional environment panel; removable Fire controls ignite/reset trees.
- **F5** still starts `DEBUG/TopDownTest/TopDownVisualTest.tscn`, the independent older 2D test. Its trees/rocks scatter once on a bounded map; this **does not** describe F6 streamed production. See [DEBUG scene documentation](DEBUG/TopDownTest/README.md).
- Arrow keys move; mouse wheel adjusts zoom; mouse aim/left-click uses the production axe on applicable hit targets (including the test skeleton). No full harvesting/loot/skill combat exists. Viewport is **1920×1080**.
- **Streaming on:** current **45,000×33,750** macro world, with chunk scenery only around the player. **Streaming off:** old **3840×1728** adjacent Oak Forest/Pink Meadow compatibility preview. Edit `WorldMasterConfig.Streaming.Enabled` via `WORLD/DefaultWorldConfig.tres` to switch, then restart.
- Generated scenery/chunks will not be hand-placed in the editor tree. The F6 scene uses runtime registration to add/remove effects with streamed chunks; F9/Fire debug panels can be removed without disabling the underlying systems.

## Vision

A **fully 2D, top-down medieval / high-fantasy survival sandbox**: an ordinary person trying to live as long as possible in a dangerous, living world. Exploration, farming, crafting, building, hunting, mining, combat and discovery should each be meaningful ways to spend time. The eventual world should feel expansive and interactive, with wildlife, settlements, ruins, caves and fantastical threats. Inspirations include the breadth of **Skyrim**, the survival tension of **Project Zomboid**, and the top-down, content-friendly approach of **Necesse**.

**Survival-first direction:** Permadeath and a new run after death are currently intended. There need not be a conventional mandatory win condition. World persistence, progression and difficulty balance are open decisions.

**Initial setting:** Medieval/high fantasy, potentially with prehistoric influences. A separate alien/sci-fi world or mode is **only a possible future direction**, not an instruction to introduce aliens, robots or dinosaurs into the current prototype.

## Visual direction

- **Godot 2D world and coordinates**: square-grid/top-down presentation with front/angled-view sprites, in the broad projection family of Stardew Valley or Necesse. **Not** diamond-grid isometric and **not** 3D terrain with billboard sprites, despite the repository name.
- Richer, more mature, semi-realistic-feeling **pixel art** rather than bright/cartoon styling. **Drova: Forsaken Kin** and **Stoneshard** are visual references for atmosphere, foliage, grounded palettes, stonework and readable characters.
- Sprite/tile size, final resolution and animation workflow **are not locked**. Reuse modular art, variation, lighting and shader effects instead of requiring an enormous bespoke animation library.
- Aim for atmospheric but efficient dynamic sun/day-night light, projected tree shadows, wind, rain, snow, cloud shade, mist, particles, water, seasons and environmental reactions. Many visual effects now also run in production; seasons and several gameplay consequences remain future work.
- Loose art at repository root is not automatically usable content. Production FIRE1/FIRE2 include charred oak transformation, but spread/damage/persistence remain future work.

## Gameplay pillars — intended direction (most mechanics still planned)

1. **Survive:** Food/water, fatigue/sleep, stamina, temperature/exposure, injury, illness and shelter should create decisions, not exhausting chores.
2. **Live off the land:** Foraging, hunting, fishing, farming, harvesting wood, cooking, food preservation and potentially livestock.
3. **Craft and build:** Tools, weapons, armour, storage, workstations, shelters, houses, farms and defenses; resources and equipment should matter.
4. **Explore:** Visually and mechanically distinct forests, grasslands, mountains, rivers, swamps, snowfields, villages, ruins, dungeons and dangerous wilderness.
5. **Go underground:** Caves, mining, ores, multi-depth locations and underground creatures; cave/interior scenes may be separate 2D maps.
6. **Fight, observe or evade:** Wildlife, bandits and fantasy creatures with diverse behaviour, rather than every entity aggressively pursuing the player.
7. **Live in a changing world:** Longer-term possibilities include settlements, NPC schedules, ecosystems, seasonal conditions, changing populations and encounters.

## World, hostility and exploration direction

The **current F6 world already generates finite coarse biome geography (P0/P1), reserves important locations (P2) and streams local ground/scenery (P3)**. The expandable finite world preference and possible infinite mode remain longer-term design choices. Mountains, oceans, rivers, actual villages/caves/routes, offline A-Life and permanent save files have not been implemented. The old startup scatter is preserved **only** in DEBUG and optional compatibility preview; see [current phase handoff](WORLD/Generation_Implementation.md).

- **Biome diversity matters more than strict climate realism.** Explore substantial visual contrasts at reasonable travel distances while retaining occasional huge forests or other memorable regions. Transition shapes should feel organic, not checkerboard-like.
- **Danger exists during day *and* night.** Time of day changes activity, visibility, weather/exposure and encounters; don't make daylight perfectly safe or trigger all danger only after sunset.
- **Shelter is valuable but not obligatory.** Temperature, rain, fatigue, recovery, supplies and security should motivate shelter; prepared players can continue exploring at night.
- **Wildlife dispositions should vary:** passive, neutral, territorial, predatory, hostile and intelligent. Creatures may respond to other creatures and their environment, not just the player.
- **Regions provide reasons to travel:** distinctive threats, resources, wildlife, caves, knowledge, trade and events. Prefer resident populations, roaming activity and rare events over arbitrary player-targeted spawns.
- **Long-term realism must remain playable and performant:** Simulate nearby actors in detail; distant ecology could use aggregate state and bounded updates. Avoid multiplying micromanagement meters.

These are **future ecology, survival and major-terrain preferences**, not finished creature populations or generated oceans/mountains. The P0–P3 biome/streaming foundation above is real production code.

## Preserved DEBUG prototypes — not production streaming

This **F5-only experiment** lives in [DEBUG/TopDownTest/](DEBUG/TopDownTest/README.md). The **F6 production scene** uses P0–P3 streaming; entries below describe DEBUG proofs of concept, not the current production world.

| Debug area | Current experiment / capabilities |
| --- | --- |
| [Base demo](DEBUG/TopDownTest/README.md) | 2D placeholder player, arrow movement, foot collisions, bounded Camera2D, actor Y sorting and a larger authored ground/path scene. |
| [Generation](DEBUG/TopDownTest/Generation/README.md), [Trees](DEBUG/TopDownTest/Trees/README.md), [Rocks](DEBUG/TopDownTest/Rocks/README.md) | Repeatable seeded **startup** scatter, ground-footprint collisions, varied scales/mirroring, tree/rock artwork and projected sun shadows. Latest documented map includes **Oak Forest / Pink Meadow**, with up to **80 oaks**, **36 pink blossom trees** and **50 rocks**; settings and placement constraints affect actual results. |
| [Biomes](DEBUG/TopDownTest/Biomes/README.md), [Ground](DEBUG/TopDownTest/Ground/README.md) | Two adjacent biome profiles, curved/softened ground palette transition, sampled-noise organic grass colour/detail and separated rock/gravel ground transitions. Not a full biome/world generator. |
| [Grass](DEBUG/TopDownTest/Grass/README.md), [Trees](DEBUG/TopDownTest/Trees/README.md) | Decorative scattered grass tufts, tree foliage sway, species-specific green/pink artwork, shared wind input and tree shadow behaviour. |
| [DayNight](DEBUG/TopDownTest/DayNight/README.md), [Polish](DEBUG/TopDownTest/Polish/README.md) | Accelerated/adjustable day-night sunlight, moving projected shadows, colour grading, cloud shade and torch/flicker comparisons. |
| [Atmosphere](DEBUG/TopDownTest/Atmosphere/README.md), [Weather](DEBUG/TopDownTest/Weather/README.md) | Haze, pollen/leaves, smoke/embers, signed shared wind, GPU rain, ground splashes, overcast and lightning tests (including manual controls). |
| [SnowTest](DEBUG/TopDownTest/SnowTest/README.md) | Removable snow test: gradual ground/foliage accumulation and melting, GPU snowfall, snow on green/pink foliage, grass and rocks, plus shallow visual player sinking in snow. Not real snow-depth physics or seasons. |
| [Liquids](DEBUG/TopDownTest/Liquids/README.md), [Surfaces](DEBUG/TopDownTest/Surfaces/README.md) | Organic debug lake with reusable liquid-type definitions, colour/depth, ripples/rain impacts, wading and player sprite immersion. Shared surface-visual helper coordinates water and snow visual offsets; camera/collider stay stable. |
| [Weapons](DEBUG/TopDownTest/Weapons/README.md), [Trees/HitFeedback](DEBUG/TopDownTest/Trees/HitFeedback/README.md) | Permanently equipped mouse-aimed debug axe; nearby targeted hit triggers tree wobble and leaf/petal burst. **No harvesting, tree health, resource drops or general combat system yet.** |

**Scope and limitations:** These modules are experiment-sized and independently documented for adjustment/removal. `DEBUG/TopDownTest/` must **not** be confused with production `WORLD/` or `ENVIRONMENT/`. Scenery is scattered once on a fixed test map, not streamed in world chunks. Shader-based snow, liquid immersion, weather and lighting are visual approximations; FIRE1/FIRE2 visuals exist, but there is no complete fire ecology/spread, seasons simulation, full inventory, A-Life ecology, saving, farming, building or survival loop. Some rendering checks in feature notes used software OpenGL and reduced scene density; **Windows D3D12 behaviour and target-machine performance still require local testing**.

## Architecture and coding conventions

**Read [CodePreferences.md](CodePreferences.md) for the full author preferences; the summary here does not replace it.**

- **Godot + C#**, modular folders with a clear owner per feature and a short README inside each major folder.
- Prefer **data-driven, reusable content**: species, biomes, item definitions, resources, recipes, effects, buildings and encounters should use common systems + assets rather than bespoke scripts for each instance.
- Keep rules separate from art and shaders. Reuse helpers/shared components where appropriate, while maintaining clear boundaries (e.g. weather owns wind; environment visuals do not own gameplay terrain generation).
- Maintain **MasterConfig / editable `.tres` Resources** for frequently adjusted values; organize inspector-exported settings into comprehensible groups.
- Prefer simple, readable, modestly vertically compact C# with **top-of-file script explanations**, comments **above methods**, conspicuous `=========================================================` separators, and `#region` organization where helpful.
- Prefer reusable helpers, contained scene nodes and modular test scenes rather than sprawling scene trees or duplicated per-entity logic.
- **Performance-first but measured:** profile bottlenecks, favor caching/shared shader variants/bounded effects, and avoid expensive per-frame work for massive populations, scenery or inventory changes.
- When a requested feature already exists in DEBUG, **inspect it before reimplementing**; extract only the approved/reusable parts to production. Preserve working experiments until asked to remove or replace them.
- Keep documentation aligned with what **actually runs**, with explicit **implemented vs planned** status. Respect the preference for **one GitHub commit per job**.

## Production folder foundations

These folders combine implemented production foundations and remaining scaffolding; see the current world/environment status above:

- [WORLD/](WORLD/README.md): canonical content/biomes/terrain/liquids; **built P0–P3 generation/reservation/streaming**; future mountains/oceans/landmarks/caves/expansion.
- [ENVIRONMENT/](ENVIRONMENT/README.md): shared clock/wind, weather, snow, layered rain, overcast controls, sun/local torch shadows, atmosphere. Live per-region weather/season scheduling remains future.
- [ENTITIES/Player/](ENTITIES/Player/README.md): production player/input/camera and axe; most inventory/survival remain plans. [ENTITIES/Shared/](ENTITIES/Shared/README.md): implemented generic Entity scene/definition, hit/health/death and optional chase.
- [CORE/](CORE/README.md): shared IDs, registries, contracts/events and eventual simulation infrastructure.
- [ITEMS/](ITEMS/README.md): central item/content definitions, tools, food, clothing and materials.
- [SYSTEMS/](SYSTEMS/README.md): reusable interaction, harvesting, crafting, farming, building, skills/effects and [WorldInteractions/Fire](SYSTEMS/WorldInteractions/Fire/README.md).
- [COMBAT/](COMBAT/README.md): common combat/damage resolution, distinct from player controls.
- [SAVE/](SAVE/README.md): **P4 planned** private profile/run/world identity, durable serialization and migrations; P3 session deltas are in WORLD/Runtime, not saved files.
- [UI/](UI/README.md): player-facing screens/HUD planning; [FEEDBACK/](FEEDBACK/README.md): production occlusion fade and future shared screen feedback.
- [AUDIO/](AUDIO/README.md): ambience, music and shared sound effects.
- [DEBUG/](DEBUG/README.md): temporary experiments, plus retired/historical visual tests.

## Design notes & decision status

`Notes/` mixes **current exploration**, future mechanic ideas and older references. Read **each note's own status/date**; none automatically supersede running code or constitutes an implementation order.

- [World generation direction](Notes/World_Generation_Direction.md): original proposals for finite/infinite, biomes, geography/landmarks. **P0–P3 are now coded**; see [current generation handoff](WORLD/Generation_Implementation.md).
- [Hostility, wildlife, shelter and exploration](Notes/Ideas/World_Hostility_Wildlife_Exploration.md): daytime/nighttime danger, creature dispositions, realistic ecosystem feel, reasons to shelter/travel, optional sci-fi direction. **Design ideas only.**
- [Future mechanic ideas](Notes/Ideas/FutureMechanicIdeas.md): food spoilage/nutrition, preservation, inventory and load, clothing/temperature, survival, fire, structures and more. **Do not implement solely from this note.**
- [Player building ideas](Notes/Ideas/Playerbuildings.md): modular rooms, exterior full roofs, roof fade and optional multi-floor construction. Ideas only.
- [Underground cave connections](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md): agreed overhead surface biome + underground cave biome + depth model, multi-exit geography. Not built.
- [Production implementation](Notes/Production_Implementation.md): early production extraction history; old preview-only text is superseded by newer code.
- [WORLD/Generation_Implementation.md](WORLD/Generation_Implementation.md): **authoritative P0–P6 status**, P3 reported regressions/fixes, test/acceptance caveats and P4 save rules.
- [Scene organization proposal](SCENES/World/scene%20organize.md): **plan only**, not actual World.tscn structure.
- [Snow test design](Notes/Snow_Accumulation_Codex_Test.txt): earlier experiment specification; current behaviour is best checked against the **SnowTest README and code**.
- [DEBUG/TopDownTest](DEBUG/TopDownTest/README.md): the **implementation index** for runnable experiments and their config paths.

## Scope and next steps

**Immediate need:** Verify P3 fixes on the user's actual Windows renderer before declaring streaming smooth or proceeding to the next larger phase. Do not create another streamer, ignore the compatibility preview, or treat headless checks as graphical FPS evidence. **These are suggested priorities, not automatic Codex work.**

1. **P3 visual/performance check:** test trees/rocks near protected spawn, absence of giant flashing tree silhouettes, idle and crossing-chunk FPS/CPU/GPU/memory, 25% zoom, biome transitions and weather/snow/fire. Profile synchronous startup, generation, binding and local navigation if lag continues; don't blindly raise load radii.
2. **P4 only when approved:** implement **separate profile → run → private world** durable saves, touched chunk deltas, player/time state, versioning, atomic recovery and identical-seed profile isolation. Confirm permadeath, inactive-clock, autosave and slot policies first.
3. **P5 only when approved:** use P2 reservations for actual major terrain, landmark scenes, roads, caves and entrances; preserve P3 streaming and data-driven placement.
4. **P6 only when approved:** begin with a focused shared-Entity wildlife/population test, then lightweight offscreen state; **not** thousands of distant AI/physics nodes.
5. **Future survival/content work:** item/resource harvest drops, inventory, building/shelter, needs, and a gradual Avalonia authoring pipeline for canonical tree/entity/biome Resources.
6. **Independent optional refactors:** LL2/LL3 lights, live regional environment profiles and [scene organization](SCENES/World/scene%20organize.md) remain separately scoped plans. Preserve running F5/F6 scenes and all P3 lifecycle registrations.

Follow the [current P0–P6 handoff](WORLD/Generation_Implementation.md) for phase boundaries and acceptance tests.

## Decisions still open

- Final art resolution, sprite scale/animation pipeline and the right balance of pixel-art detail and shaders.
- Exact size/bounds of the world, expandable finite vs infinite approach, biome sizes/adjacency, generated-vs-authored balance and save/versioning of visited areas.
- Height depiction, cave representation, world-creator options, settlement/NPC simulation and population density.
- Combat, magic, survival depth, progression, permadeath persistence rules and difficulty.
- Whether any future sci-fi setting is a separate world/project or ever shares content with the medieval game.

## Repository history and transition

The repository name still contains **`isometric`** for historical reasons, but **the active direction is a fully 2D top-down game**. The former 3D/isometric architecture is no longer the game's main path. Some legacy resources/scenes/`.uid` remnants may reference removed scripts; they are not evidence of implemented modern systems. Retired material under `DEBUG/VisualTests/` and older notes should be checked carefully before reuse.

**General rule for future sessions:** Distinguish **game vision**, **current debug proof-of-concepts**, and **approved production code** at every step. Do not infer a mechanic is finished because its folder, README, art asset or config exists.
