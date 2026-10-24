# Fantasy Survival Sandbox — 2D Top-Down Reboot

A Godot C# **2D top-down medieval/high-fantasy survival sandbox** built around modular, data-driven systems, deterministic streamed worlds and low idle cost. The repo name says "isometric" for historical reasons; the active game is **not** diamond-isometric or 3D.

**Read [CodePreferences.md](CodePreferences.md) before changing code.** This README is the project overview, architectural north star and navigation hub; feature READMEs and validation records own implementation detail and test evidence.

## Performance is an architectural requirement

Design every new mechanic for **low idle cost and large-world scalability from the start**, not as a later optimization pass. **Dormant systems should remain dormant:** activate effects through events/signals or explicit state changes, stop unnecessary `_Process`/`_PhysicsProcess` callbacks, simulation, redraws and GPU updates when disabled or settled, and ensure F9/gameplay events can reliably wake them again. Rain/snow at zero intensity and healthy trees with no fire are examples—not exceptions.

Prefer **shared controllers, reusable materials, pooled effects, cached/spatial queries, and chunk-local registration** over per-object polling, full-world scans and repeated unchanged shader writes. Off-screen cosmetics may sleep, but persistent world state and legitimately continuous effects (clock, active wind, sunlight) must remain correct. Preserve visual quality and modular Resource-driven authoring; avoid optimizing by simply freezing visible animations.

**Every implementation should answer:** *What activates it? What stops it? What does it cost while nothing is happening?* Validate idle, active, shutdown, reactivation and streaming behavior with Godot profiling; distinguish measured improvements from assumptions. Follow the full mandatory guidance in [CodePreferences.md](CodePreferences.md). See [Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/performance_refactor.md](Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/performance_refactor.md) for the staged environment audit and Codex handoff; cross-check [world scene organization](SCENES/World/scene%20organize.md) before changing scene ownership.

## Current production state — 23 October 2026

| Track | Implemented now | Still pending |
| --- | --- | --- |
| **P0–P2 world generation** | Finite seeded macro biomes, protected spawn and feature reservations | P5 real terrain/locations/paths/caves/major geography |
| **P3 streaming** | Default 45,000×33,750 world, 512-unit nearby chunks, local scenery/navigation, lifecycle-registered effects | Windows graphical stability, frame pacing, memory and hitches |
| **P4 private campaigns** | Boot profiles, disk saves, New Campaign/Continue, F8, Esc, autosave; **earlier user functional checks accepted** | Large-save profiling and newer SO2/C1 graphical regression |
| **SO0–SO2 scene layout** | Explicit persistent entity/prop, chunk and preview roots, shared Y sorting; automated checks passed | Windows rendered overlap/seams; SO3/SO4 still proposed |
| **Combat C0–C2.5 + C3.2** | Shared HP/stats/damage and player/enemy melee execution; arc/reach/faction/obstruction checks, schema-2 HP saves, existing shared health bars; dual attacks/starter switching, directional block, visual hop, shared knockback/stagger and material impacts | C3.4–C5 feedback/telegraphs/encounter feel; broader baseline test failures and target-PC acceptance |
| **Fire/environment** | Production FIRE1/FIRE2, rain/wet foliage, snow, manual Oak seasonal preview, wind/clock, daylight/torch shadows, occlusion, atmosphere and pond | Fire spread/lightning ignition, seasonal calendar/SE2 acceptance, LL2/LL3, climate simulation |

**Validation by revision (not competing totals):** P4 at `ce6767d1`: **31 write / 33 read**; SO2 at `64d44226`: **39/33**, 60 ownership assertions and real legacy-disk restoration; newer [C1](COMBAT/C1_Validation.md): **43/39** save checks, including schema-2 player HP. See [P4 handoff](SAVE/P4_Copilot_Handoff.md), [SO2 evidence](SCENES/World/SO2_Validation.md) and [SAVE status](SAVE/README.md). The user accepted original P4 save/restore/profile functionality; SO2/C1 visual and target-PC performance checks remain separate. Headless success does not demonstrate FPS.

**Latest C2 validation:** build/import, 46 melee checks, 10037 C1 checks, P4 43/39 and pre-C2 disk restoration pass. World ownership/streaming assertions encounter existing native shutdown errors; scenery cancellation also fails on the unmodified baseline. These are not clean full-suite passes; see [C2 evidence](COMBAT/C2_Validation.md).

**Run/controls:** **F5** launches `UI/Menus/Boot/BootScreen.tscn` (profile → Main Menu → New Campaign / Continue). Open `SCENES/World/World.tscn` and press **F6** for a **disposable** streamed-world test session without campaign persistence. **F7** macro map/reservations, **F9** exclusive Environment/Combat debug drawers and diagnostics, **F8** save active campaign, **Esc** campaign save/leave menu. `WorldMasterConfig.Streaming.Enabled=false` selects the old 3840×1728 compatibility preview. The independent [DEBUG/TopDownTest](DEBUG/TopDownTest/README.md) starts by opening its scene and pressing **F6**, not F5.

**Performance acceptance:** P3 startup and some generation/binding/nav remain synchronous; a nominal 3ms scenery-apply budget is not a hard frame-time limit. Earlier grass-only/giant shadow bugs had fixes and headless regressions but still require target-PC rendering verification. See [streaming evidence](WORLD/Runtime/Streaming/README.md) and the [archived performance handoff](Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/performance_refactor.md).

## Start here — future ChatGPT / Codex / contributors

1. Read **this README** to understand the game and what exists today.
2. Read **[CodePreferences.md](CodePreferences.md)** **before touching code**. Follow the author's folder layout, C# style, comments/regions, MasterConfigs, reusability, simplicity, performance priorities and **one GitHub commit per job**.
3. Read the **README inside the exact feature folder** you are modifying, its `MasterConfig` / `.tres` settings, relevant scene and actual scripts. Some older notes may be superseded by newer documentation or code.
4. Start current production work at [SCENES/World/README.md](SCENES/World/README.md), [WORLD/Runtime/Streaming/README.md](WORLD/Runtime/Streaming/README.md), and the [P0–P6 handoff](WORLD/Generation_Implementation.md). Use [DEBUG/TopDownTest/README.md](DEBUG/TopDownTest/README.md) only for the independent experimental forest; treat older notes as historical when superseded.
5. **Do not silently promote debug experiments to production, reorganize unrelated modules, or implement idea documents on sight.** When promotion is requested, extract reusable parts into their owning production folders deliberately, preserving module boundaries, inspector-tweakable data and removable test wiring. Verify the real code and current scene rather than relying only on summaries.



## AI assistant workflow — roles and handoffs

**Codex is the primary implementation developer** for this Godot C# project, especially major features, multi-file integrations, structural changes, performance-sensitive work and phase implementation. The author uses other AI assistants deliberately to conserve Codex/ChatGPT Work usage; **being able to read GitHub does not grant an assistant permission to implement a plan or change code unprompted**. Each job's explicit user instructions decide who edits, tests and commits.

| Assistant | Intended responsibility |
| --- | --- |
| **Codex / ChatGPT Work coding sessions — primary developer** | Own substantial implementation and architectural changes; inspect current code, integrate with existing production systems, update affected docs and make **one GitHub commit per job** when authorized. |
| **Regular ChatGPT chats — planning and smaller tasks** | Discuss designs, break work into focused phases, clarify architecture, read code/docs, review diffs, diagnose specific issues and handle limited, explicitly assigned small edits. Avoid inventing APIs or asserting untested fixes. |
| **Claude — optional second opinion / supporting assistant** | Independently review designs, C# changes, performance risks and bugs; may handle small, isolated tasks if assigned. Its advice is not authoritative without checking the actual repository and tests. |
| **GitHub Copilot in VS Code — local tester and small-fix assistant** | Pull/review the designated commit; run .NET/Godot builds, headless tests and local diagnostics; capture precise commands, versions, exit codes, failures and performance evidence. **Test first without modifying code**, unless the user authorizes fixes. |
| **User — final acceptance** | Decide scope and approvals; inspect graphics, gameplay, Godot profiler results and Windows performance; approve a phase only after the relevant checks, not merely because code was pushed. |

### Rules shared by every assistant

1. **Read before acting:** Start with this README and mandatory [CodePreferences.md](CodePreferences.md), then the owning feature README, related `MasterConfig` / `.tres` Resources, actual scripts/scenes and the relevant phase handoff. Current code and user instructions take precedence over historical notes. Clearly distinguish **production, DEBUG, scaffolding and planned** work.
2. **Respect module ownership:** Reuse canonical `WORLD/Content`, `WORLD/Biomes`, `WORLD/Generation`, `WORLD/Runtime/Streaming`, `SAVE`, `ENVIRONMENT`, `ENTITIES`, `SYSTEMS` and shared `CORE` contracts. Don't build duplicate managers, species-specific generation paths, global caches or DEBUG dependencies in production. Prefer shared scenes, Resource-driven definitions, inspector-friendly configs and small modular C# files.
3. **Performance is non-negotiable:** For each mechanic document **what wakes it, what makes it sleep, its idle cost and scaling under chunk streaming**. Prefer event-driven activation, shared/batched work, pooling, cached/spatial/chunk-local queries and explicit unload cleanup over per-object polling or unnecessary per-frame work. Preserve deterministic generation, save isolation and visible animation. Measure performance rather than claiming it.
4. **Keep work scoped and coordinated:** One assistant should be the active code editor for a given task/branch at a time; others can review or test. Do not overwrite unrelated work, quietly broaden scope or merge conflicting edits. Use **one commit per job** as specified in CodePreferences; record its SHA and touched files.
5. **Separate implementation from validation:** Codex normally performs appropriate validation, but **when the user requests a local-testing handoff**, it may implement and push without consuming time on local build/runtime/performance checks. Follow [CodePreferences' optional testing policy](CodePreferences.md) and the [P4 Copilot handoff example](SAVE/P4_Copilot_Handoff.md). Record exact checks run vs deferred; give Copilot commands, expected outcomes and manual visual/profiler checks. A passing headless test is not proof of visual smoothness or FPS, and **a commit is not an accepted feature**.
6. **Return actionable handoffs:** State changed files, commit SHA, behaviour, remaining limitations, performance implications, failed/deferred checks and reproduction steps. Copilot/Claude/regular ChatGPT should hand major architectural fixes back to Codex unless the user explicitly assigns the change elsewhere.

This is the preferred **collaboration workflow**, not a restriction against assigning a different assistant a clearly scoped task. All assistants follow the same coding conventions, architecture and performance standards.


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

### 2. Large finite world — P0–P4 IMPLEMENTED; major geography NEXT

**Existing runtime:** P0/P1 generates a complete, deterministic, queryable **finite macro biome map** using the configured Resource list. P2 freezes required/preferred polygon **feature reservations** and a protected starting anchor. P3 draws nearby chunk ground and canonical resource-driven scenery, handles load/unload/material/shadow/occlusion/fire lifecycle, and maintains **session-only** changed-object deltas. It is already the **default F6 world**—do not implement a duplicate streamer or revert to the compatibility preview as the primary architecture.

**Existing separation:** coarse biome **regions are NOT streaming chunks**; one biome spans many chunks. The current streamed ground is flat 2D artwork/palette blending, not oceans/relief. Local decoration uses a globally keyed candidate grid and a density per million world units, with deterministic cross-chunk spacing independent of traversal/load order. P2 reservations exclude incompatible objects but **do not create** an actual village, lake or road. Default chunk size is 512; current prototype world bounds are 45,000×33,750. Bounds, region diameters and chunk sizes are separate settings.

**Future conceptual pipeline (not a list of implemented systems):** add continent/ocean landmasks, relief/terrain and rivers; meaningful fantasy biome contrast and region themes; P5 actual reusable settlements, caves, landmarks, routes and entrances; P4 private campaign disk saves (implemented); P6 abstract offscreen ecology; possible guided World Creator, finite-world expansion and versioned old-world migration. Keep climates a soft influence, allow visually distinctive neighbours, and don't assume infinite worlds or the final world size have been chosen.

**Protect existing contracts:** generation/world IDs, feature reservation ownership, stable candidate/object IDs, cross-chunk seams, biome Resource references, world-specific cache cleanup, shared weather/clock, and reproducible generation under a fixed generation/content version. P3 maintains session deltas; P4 now persists supported edits, player state, clock and world identity through private profiles and Continue. Automatic content migrations and permanent old-world migration do not exist. The startup/preload system and some data/nav/shader work are synchronous; the per-frame application budget is **not** a hard frame-time cap. The latest P3 fixes still require in-game Windows performance verification.

**Authoritative detail:** [generation phases / regressions / P4 policy](WORLD/Generation_Implementation.md), [macro generator](WORLD/Generation/World/README.md), [feature reservations](WORLD/Generation/World/Features/README.md), [streaming lifecycle](WORLD/Runtime/Streaming/README.md), [placement priority](WORLD/placement_priority.md), and [SAVE implementation](SAVE/README.md).

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
- **Fire and world interactions:** generic ignition/lifecycle/effects in [SYSTEMS/WorldInteractions/Fire](SYSTEMS/WorldInteractions/Fire/README.md); ENVIRONMENT supplies wind/rain/lightning/light; WORLD owns species art. Production FIRE1/FIRE2 and **P4 persistent campaign burn deltas** exist; lightning ignition, spread, damage, extinguishing, offline burning and **offline** burning remain future. Supported burn/removal deltas already persist in P4 campaigns.
- **Living consequences:** harvesting/regrowth, water quality, muddy/snowy terrain movement, noise/scent, wildlife displacement, shelter and building wear. Keep common mechanics reusable and data-driven.
- **Setting:** medieval/high fantasy now (possibly prehistoric elements). A later sci-fi/alien/robotic world is **optional**, not permission to introduce it into the starting medieval scene.

**Design priority:** believable interconnected systems that create choices, not maximum complexity or survival chores.

### 6. Folder ownership — future coders must NOT create parallel homes

| Canonical owner | What belongs there |
| --- | --- |
| [WORLD/Content](WORLD/Content/README.md) | Canonical scenery, tree, rock, structure, assets and species variants (one definition reused across biomes). |
| [WORLD/Biomes](WORLD/Biomes/README.md) | Overworld Definitions/ and Shared/ types, content references and profiles. Cave biomes separate. |
| [WORLD/Generation](WORLD/Generation/README.md) | Generic deterministic generation/placement/landmark algorithms, not species-specific spawners. |
| [WORLD/Runtime](WORLD/Runtime/README.md), [SAVE](SAVE/README.md) | **P3 built:** chunk streaming and session changed-instance state. **P4 implemented:** per-profile/run/world durable checkpoints and Continue (original functional checks user-accepted; automatic content migrations not implemented). |
| [ENVIRONMENT](ENVIRONMENT/README.md) | **One authoritative wind/clock**; daylight, weather, rain, snow, lightning and atmosphere. Not AI or generic burn state. |
| [ENTITIES](ENTITIES/README.md) | Production Player plus [shared data-driven Entity scene](ENTITIES/Shared/README.md) and basic optional chase; richer NPC/population/AI remains future. |
| [SYSTEMS](SYSTEMS/README.md) | Harvesting, crafting, building, farming, cooking, interaction, effects; fire lives under **WorldInteractions/Fire**. |
| [ITEMS](ITEMS/README.md) | Canonical item/material/food/tool/armour definitions (instance state belongs to inventory/runtime). |
| [CORE](CORE/README.md) | Shared IDs, future content registry, events/contracts and slow simulation scheduling where justified. |
| [COMBAT](COMBAT/README.md) | Reusable hit/damage/defense rather than another per-enemy or player-only damage engine. |
| [SCENES](SCENES/README.md) | Composition like World.tscn; [scene-root organization](SCENES/World/scene%20organize.md) has SO0–SO2 implemented; SO3/SO4 and GL proposals remain planned. |
| [DEBUG](DEBUG/README.md) / [UI](UI/README.md) / [FEEDBACK](FEEDBACK/README.md) / [AUDIO](AUDIO/README.md) | Isolated experiments/developer panels / future player UI / **implemented foliage occlusion** and future screen feedback / audio. Production runs without developer UI. |

**Cross-system example:** biome **references** canonical Oak; generic placement spawns an instance; Fire/Harvesting changes that tree; ENVIRONMENT gives wind/weather; ITEMS defines its dropped logs; P4 SAVE records supported removal/burn states for that exact tree. Never copy oak art into a biome, build a second wind simulator for fire, write every species directly into WorldScene or make production depend on DEBUG/TopDownTest assets/code.

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
| **P4:** private profile/run/world saves, durable chunk deltas and Continue | **Implemented; earlier P4 functional checks user-accepted; latest C1 automated save checks passed. SO2/C1 graphical/performance acceptance remains open; automatic content migrations not implemented** |
| **P5:** real terrain features, towns/POIs/routes/cave entrances | **Not implemented** |
| **P6:** resident wildlife/NPC populations and tiered offscreen A-Life | **Not implemented** |
| Avalonia toolbox/import and content registry; player inventory/needs/crafting/building | **Future goals/plans** |

**Read these original sources for detail:** [World generation](Notes/World_Generation_Direction.md) · [Wildlife/hostility](Notes/Ideas/World_Hostility_Wildlife_Exploration.md) · [Underground geography](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md) · [Player buildings](Notes/Ideas/Playerbuildings.md) · [Survival/mechanic ideas](Notes/Ideas/FutureMechanicIdeas.md) · [Archived production implementation](Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/Production_Implementation.md) · [Snow experiment](Notes/Snow_Accumulation_Codex_Test.txt). Completed historical implementation plans are filed under Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/.

## Vision

A **fully 2D, top-down medieval / high-fantasy survival sandbox**: an ordinary person trying to live as long as possible in a dangerous, living world. Exploration, farming, crafting, building, hunting, mining, combat and discovery should each be meaningful ways to spend time. The eventual world should feel expansive and interactive, with wildlife, settlements, ruins, caves and fantastical threats. Inspirations include the breadth of **Skyrim**, the survival tension of **Project Zomboid**, and the top-down, content-friendly approach of **Necesse**.

**Survival-first direction:** Permadeath and a new run after death are currently intended. There need not be a conventional mandatory win condition. World persistence, progression and difficulty balance are open decisions.

**Initial setting:** Medieval/high fantasy, potentially with prehistoric influences. A separate alien/sci-fi world or mode is **only a possible future direction**, not an instruction to introduce aliens, robots or dinosaurs into the current prototype.

## Visual direction

- **Godot 2D world and coordinates**: square-grid/top-down presentation with front/angled-view sprites, in the broad projection family of Stardew Valley or Necesse. **Not** diamond-grid isometric and **not** 3D terrain with billboard sprites, despite the repository name.
- Richer, more mature, semi-realistic-feeling **pixel art** rather than bright/cartoon styling. **Drova: Forsaken Kin** and **Stoneshard** are visual references for atmosphere, foliage, grounded palettes, stonework and readable characters.
- Sprite/tile size, final resolution and animation workflow **are not locked**. Reuse modular art, variation, lighting and shader effects instead of requiring an enormous bespoke animation library.
- Aim for atmospheric but efficient dynamic sun/day-night light, projected tree shadows, wind, rain, snow, cloud shade, mist, particles, water, seasons and environmental reactions. Many visual effects now also run in production; seasons and several gameplay consequences remain future work.
- Former loose root sprites have been reorganized: [Pine artwork](WORLD/Content/Vegetation/Trees/Pine/README.md) is staged but not yet configured as a species; [unused source artwork](Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/Unused_Sprite_Source/README.md) is archived and excluded from Godot imports. Production FIRE1/FIRE2 use canonical charred art and P4 saves supported burn state. Fire spread, survival damage and offscreen combustion remain future work.

## Gameplay pillars — intended direction (most mechanics still planned)

1. **Survive:** Food/water, fatigue/sleep, stamina, temperature/exposure, injury, illness and shelter should create decisions, not exhausting chores.
2. **Live off the land:** Foraging, hunting, fishing, farming, harvesting wood, cooking, food preservation and potentially livestock.
3. **Craft and build:** Tools, weapons, armour, storage, workstations, shelters, houses, farms and defenses; resources and equipment should matter.
4. **Explore:** Visually and mechanically distinct forests, grasslands, mountains, rivers, swamps, snowfields, villages, ruins, dungeons and dangerous wilderness.
5. **Go underground:** Caves, mining, ores, multi-depth locations and underground creatures; cave/interior scenes may be separate 2D maps.
6. **Fight, observe or evade:** Wildlife, bandits and fantasy creatures with diverse behaviour, rather than every entity aggressively pursuing the player.
7. **Live in a changing world:** Longer-term possibilities include settlements, NPC schedules, ecosystems, seasonal conditions, changing populations and encounters.

## Plans, owners and remaining work

- [World P0–P6 phases](WORLD/Generation_Implementation.md): P0–P4 implemented, P5/P6 planned; [WORLD](WORLD/README.md), [streaming](WORLD/Runtime/Streaming/README.md), [generation](WORLD/Generation/World/README.md), [SAVE](SAVE/README.md).
- [Scene SO/GL plan](SCENES/World/scene%20organize.md): SO0–SO2 implemented; SO3/SO4 and GL0–GL3 future; [SO2 visual checklist](SCENES/World/SO2_Copilot_Handoff.md).
- [Combat C0–C5](COMBAT/COMBAT_IMPLEMENTATION.md): C0–C2.5 and C3.2 implemented, C3.3–C5 future; [C2.5 controls/tests](COMBAT/C2_5_Validation.md), [C2 tests and limitations](COMBAT/C2_Validation.md), [C1/save tests](COMBAT/C1_Validation.md).
- Canonical production owners: [ENTITIES](ENTITIES/README.md), [ENVIRONMENT](ENVIRONMENT/README.md), [SYSTEMS](SYSTEMS/README.md), [COMBAT](COMBAT/README.md), [FEEDBACK](FEEDBACK/README.md), [CORE](CORE/README.md), [SCENES](SCENES/README.md), [UI](UI/README.md), [DEBUG](DEBUG/README.md).
- Future vision: [world direction](Notes/World_Generation_Direction.md), [wildlife](Notes/Ideas/World_Hostility_Wildlife_Exploration.md), [caves](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md), [survival](Notes/Ideas/FutureMechanicIdeas.md), [building](Notes/Ideas/Playerbuildings.md). Completed old implementation plans are indexed in the [Notes archive](Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/README.md).

**Next checks:** Windows Godot P3/SO2 stream seams, Y-sort, FPS/GC spikes, P4/C1 Continue and large-save performance. C2 core combat/save checks pass; broader world-test shutdown/cancellation failures also reproduce on the unchanged baseline ([details](COMBAT/C2_Validation.md)). C3.3–C5 remain separately scoped. P5/P6, SO3/SO4, GL, Avalonia and new survival/population systems are not automatically authorized. One GitHub commit per assigned job; no accidental DEBUG-to-production promotion.


**C2.5 controls:** LMB forward slash, MMB thrust; 1 axe / 2 temporary sword / 3 shield toggle. RMB hops without a shield, holds directional guard with one; guard slows movement and overrides sprint, dodge cancels it. No hop immunity or obstacle crossing. Skeleton Basic now has **50 HP** in the regular World too; [disposable practice](TESTS/Combat/PlayerActionsPractice.tscn) offers a resettable target. This authored-content change can make older campaigns fail the existing content fingerprint check; use F6 World or a new campaign for this balance trial. See [validation/build limitations and manual handoff](COMBAT/C2_5_Validation.md).

**C3.2:** Shared collision-safe knockback/stagger now uses the existing player/entity motors. Axe LMB lightly pushes (15px / 0.12s stagger); MMB prods harder (55px / 0.30s, interrupts committed attacks). Damage stays 11/5. Sword/enemy profiles reuse the same path; shared definitions expose independent push/stagger resistance and immunity. No new species scripts or scene-tree nodes. [Validation, settings and Windows playtest](COMBAT/C3_2_Validation.md).

**C3.3a+b:** Shared bounded particle slots/profiles now drive tree-hit leaves; tree colour/burn suppression and ambient leaves retain their owners. Wobble stops notifying shadows independently of leaf lifetime. [Results and visual/performance handoff](COMBAT/C3_3ab_Validation.md); combat material effects remain C3.3c/d.

**C3.3c+d:** Skeleton bone fragments and player flesh droplets now use shared configurable material profiles on confirmed damaging contacts. Zero damage/misses do not emit; killing blows do. Lazy canvases cap particles and sleep when settled. Use F6 or a new campaign after changed material Resource references; no save-schema migration/bypass. [Results and acceptance](COMBAT/C3_3cd_Validation.md). C3.4 block feedback and C3.5 camera/audio remain future work.

**ES3 / combat tuning:** Shared Direct/Spread engagement positioning now reserves stable reachable approach points with bounded fallback; real attacks and recoil retain priority. F9 switches between Environment (including tree-fire commands) and Combat (independent movement/combat masters and ES3 sliders). See [implementation, evidence and remaining visual acceptance](ENTITIES/Shared/Movement/ES3_Validation.md).

**R0 projectiles:** Shared straight arrows now have swept point collision, immutable contact/stat snapshots, directional guard, bounded scene-local spawning and transient cleanup. R1 now adds the shared archer/practice room; mixed encounters remain R2. [R0 results and integration](COMBAT/Projectiles/R0_Validation.md).

**R1 archer:** Open `TESTS/Projectiles/ArcherPractice.tscn` and press F6 for a playable bow skeleton, cover and reset. Enemy bow definition/art live under `Skeletons/Archer/Equipment/Bow/`, separate from player ITEMS. Shared ranged intent uses the existing motor and R0 arrows; default World still has five melee skeletons. [Settings, validation and remaining visual checks](COMBAT/Projectiles/R1_Validation.md).

**GS2/GS3 terrain:** streamed ground now reads explicit stacks and supports two optional organic overlays plus different-art biome transitions. Oak/Pink remain unchanged by default; disposable `TESTS/Terrain/GroundLayersPractice.tscn` demonstrates sand/soil. Software-GL formula/seam checks passed; Godot/.NET, Windows visual parity and target profiling remain pending. [Evidence and local commands](WORLD/Terrain/Ground/Surfaces/GS2_GS3_Validation.md). GS4 F9 controls remain future.

**GS4 terrain controls:** F9 now includes Ground Surfaces beside Environment/Combat. Base variation/detail, optional-layer master, A/B switches, global coverage/opacity multipliers and reset are session-only. GroundLayersPractice.tscn reuses the drawer for testing configured overlays. [Combined GS0–GS4 local checklist](WORLD/Terrain/Ground/Surfaces/GS4_Validation.md); engine/Windows/performance acceptance remains pending.

**Ground-layer clarification:** Base Ground now supplies one base colour/texture; AccompanyingPatches is its own optional Resource/layer for existing grass flavour; the two extra slots remain available for later dirt/rock. Oak/Pink preserve their enabled appearance. [Structure, save compatibility and validation](WORLD/Terrain/Ground/Surfaces/ACCOMPANYING_PATCH_LAYER.md). Previous World/practice checks were reported passed by the user; this extraction still needs a local check.

**GS5 Oak previews:** optional Dirt/Rock extra layers are configured but default OFF. F9 Ground Surfaces enables each and adjusts coverage/opacity; base detail and accompanying grass now have strength sliders too. Reset restores the original grass look. [Try it / validation](WORLD/Terrain/Ground/Surfaces/GS5_Validation.md); software-GL checks passed, Godot/Windows acceptance remains pending.

**Optional surface revision:** Oak Dirt/Rock now use texture-free dust/basalt recipes adapted from the old isometric project, with broad low-warp blending. The rejected SVG overlay textures are removed; base grass and F9 controls remain unchanged. [Implementation and validation](WORLD/Terrain/Ground/Surfaces/PROCEDURAL_GROUND_RECIPES.md). Software-GL recipe/parity/seam checks passed; Godot/Windows acceptance remains pending.

**SE0/SE1 seasonal foliage:** F9 → Environment → Seasonal foliage now recolours Oak using its optional Avalonia mask and authored Gradient; Summer/disabled returns original artwork, Pink stays pink, and unmasked sprites retain automatic detection. Rain/snow select original foliage independently of autumn colour. Software-GL checks passed; Godot/Windows acceptance and SE2 save compatibility remain pending. Use F6/new campaign because Oak resource changes affect the strict content fingerprint. [Plan and local validation](ENVIRONMENT/Seasons/README.md).

**SE1b seasonal tuning:** F9 now shows month/date/season, colour strength and stable early/late tree timing (default ±14 days). Oak transitions through autumn into pale blue-green winter foliage; snow remains separate. January=0/1, Summer≈0.5. Shared 16×2 species atlases avoid per-tree seasonal loops/materials. [Checks and local acceptance](ENVIRONMENT/Seasons/SE1b_Validation.md); software-GL passed, Godot/Windows validation pending.

**GS5 Dirt PNG:** imported grayscale Dirt01_gs.png now renders at native 256×256 with biome-owned dirt tint. Existing masks, F9 controls and Oak grass remain unchanged; Rock stays Basalt. [Authoring, checks and local validation](WORLD/Terrain/Ground/Surfaces/GS5_PNG_DIRT.md).

**SE2 seasonal integration:** exact older seasonal visual resource fingerprints can now compose with the existing terrain compatibility bridges; schema and manual preview persistence stay unchanged. Active fire shader caches release on completion/unload; missing charred sprites remain supported with original-art fallback. Source/GL checks passed; [native tests, disk Continue and Windows profiling remain pending](ENVIRONMENT/Seasons/SE2_Validation.md).

**SE3 foliage masks:** cached metadata diagnostics, explicit PNG/import audit and an authoring guide now cover future masks. Actual Oak passed 18 software-GL alignment cases across wind/mirroring/scales. Native build/transform/Windows acceptance remains pending. [Mask workflow and validation](ENVIRONMENT/Seasons/SE3_Validation.md). Wind continues to sway rows by height; it does not classify leaves for motion.

**WG0/WG1 water:** audited liquid/stream/save contracts and added opt-in deterministic lake footprint planning, biome hydrology profiles, frozen save metadata and F7 blue plan previews. Normal world settings remain off; `TESTS/Water/WaterPlanningPractice.tscn` (F6→F7) enables three disposable planned lakes. Generated rendering/exclusions/navigation are WG2. [Settings, validation and handoff](WORLD/Generation/Liquids/WG0_WG1_Validation.md).

**WG2 streamed lakes:** opted-in new worlds render saved lake shapes through chunk-cropped water fields, shared player wading/rain, scenery exclusions and local ground-enemy navigation. New playable-water campaigns use blueprint schema 5; schema-4 saves keep their prior geography. `TESTS/Water/WaterPlanningPractice.tscn` (F6) starts beside a lake; L cycles lakes and F7 shows footprints. Normal World generation remains opt-in. [Implementation, checks and Windows acceptance](WORLD/Liquids/WG2_Validation.md).

**WG3 rivers:** opt-in deterministic spring-to-terminal-basin rivers now route across eligible biomes with organic bends/variable widths, reusing WG2 streamed visuals/wading/scenery/nav. River worlds use schema 6; schema-4/5 geography and lake-only hashes remain unchanged. `TESTS/Water/RiverPractice.tscn` (F6) starts beside a river; R cycles sections and F7 shows its cyan route. [Settings, limits and validation](WORLD/Generation/Liquids/Rivers/WG3_Validation.md).

**UI1/UI2 gameplay UI:** World now has a configurable nine-slot visual hotbar and non-pausing backpack/equipment placeholder overlay. I toggles; Esc closes inventory before the campaign menu. Player movement/new combat intents are suppressed while open; enemies/weather continue. Existing 1/2/3 starter combat keys remain when closed; actual inventory data/hotbar binding is UI3. [Validation and Windows checklist](UI/HUD/Gameplay/UI1_UI2_Validation.md).
