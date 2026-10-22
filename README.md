# Fantasy Survival Sandbox — 2D Top-Down Reboot

**World generation P0–P2 implemented:** F6 production World, then **F7** for a seeded finite biome-map overview with protected starting space and an irregular required southeast test reservation. Biome collections, weights, target region diameters and required/preferred feature footprints are resource-driven. The larger map is still a blueprint; the playable Oak/Pink preview remains unchanged. Chunk streaming, durable profile saves and A-Life are planned. See [generation guide](WORLD/Generation/World/README.md) and [P2 feature guide](WORLD/Generation/World/Features/README.md).

**Generic production trees:** Oak and Pink Blossom now use one shared tree template, configured entirely by species/variant Resources. Both support axe feedback, wind, snow, shadows and fire; blossoms need no charred artwork to burn. Future species need .tres data/art/biome references only. See [tree authoring guide](WORLD/Content/Vegetation/Trees/README.md).

**Production fire (FIRE1/FIRE2):** Oaks in World.tscn now support manual ignition, charred transformation, canopy-shaped GPU flames/smoke/embers and shared-wind firelight. Top-centre controls in `DEBUG/UI/Fire/` are removable. See [production fire](SYSTEMS/WorldInteractions/Fire/README.md). Spread, damage and lightning ignition remain future work.

**Two-biome production preview:** World.tscn now contains Oak Forest and the copied Pink Meadow, with canonical pink-blossom content, pink ground and a curved blended transition. The map is modestly widened to **3840×1728**. Walk east from spawn. Seeded placement and shared environmental visuals work on both sides; large-world generation/sizing remains future planning.

**Production foundation added:** [SCENES/World/World.tscn](SCENES/World/World.tscn) is an independent playable Oak Forest world (F6): player/collision/camera, organic grass ground and data-driven oak/rock/grass placement. A fresh seed is chosen/logged each load; fixed-seed replay is configurable. DEBUG stays intact and remains the F5 default. Production environmental visuals are now extracted as described below; survival/gameplay systems remain planned. See [run/configuration guide](SCENES/World/README.md). Earlier scaffolding-only descriptions below are superseded for these specific modules.

**Phase 2 environment implemented:** The production world now includes day/night and cached sun/contact shadows, shared wind/vegetation sway, haze/particles, cloud shade, colour grade/torch, rain/splashes/lightning and snow on ground/trees/rocks/grass. Optional collapsible controls live in `DEBUG/UI/Environment/` (**F9**). Biomes can supply typed local profile overrides; shared renderers remain in their owning production folders. See [environment documentation](ENVIRONMENT/README.md). See the newer liquid, tool and fire module documentation for subsequent promotions; seasons and climate simulation remain planned.

> **Project snapshot — 22 October 2026:** An independently runnable **production world preview** now exists alongside the preserved **DEBUG/TopDownTest**. The former 3D/isometric direction is retired. Production includes two-biome seeded scenery, player/camera, environmental visuals, a pond/wading, FIRE1/FIRE2 oak burning and an initial Skeleton Basic test. **This is not yet a complete survival game**: huge procedural maps, chunk streaming, world persistence, A-Life populations, Avalonia authoring and much of survival/building remain future goals. Verify actual source/feature READMEs for the latest status.

**Four distinct feature statuses:**
- **Implemented in production:** Runs in World.tscn or its production dependencies; check module docs/tests.
- **Implemented in DEBUG:** Disposable isolated prototype, not automatically production code.
- **Scaffolding only:** Directories/READMEs/configs without full working mechanics.
- **Planned / exploratory:** Design intentions, agreed concepts and open alternatives. **Notes do not themselves authorize implementation.**

## Start here — future ChatGPT / Codex / contributors

1. Read **this README** to understand the game and what exists today.
2. Read **[CodePreferences.md](CodePreferences.md)** **before touching code**. Follow the author's folder layout, C# style, comments/regions, MasterConfigs, reusability, simplicity, performance priorities and **one GitHub commit per job**.
3. Read the **README inside the exact feature folder** you are modifying, its `MasterConfig` / `.tres` settings, relevant scene and actual scripts. Some older notes may be superseded by newer documentation or code.
4. For the working demo, start with **[DEBUG/TopDownTest/README.md](DEBUG/TopDownTest/README.md)**. For long-term ideas, use the [design notes](#design-notes--decision-status) rather than assuming everything in `Notes/` is obsolete or approved.
5. **Do not silently promote debug experiments to production, reorganize unrelated modules, or implement idea documents on sight.** When promotion is requested, extract reusable parts into their owning production folders deliberately, preserving module boundaries, inspector-tweakable data and removable test wiring. Verify the real code and current scene rather than relying only on summaries.


## Long-term architectural north star — READ BEFORE CODING

**Compiled from the full Notes collection and current feature READMEs.** This is a durable guide for future ChatGPT/Codex sessions so new systems fit the game's eventual scale and authoring model. **It is not a claim these features already exist, and not an implementation order.** For every job first inspect actual code, [CodePreferences](CodePreferences.md), the owning module and current scene wiring. Keep experimental proof-of-concepts separate from approved production work.

### 1. Ultimate goal: Avalonia external content-authoring toolbox (PLANNED)

**End-state vision:** Use an external **Avalonia toolbox** to create/edit a tree species, rock, creature, item, crafting recipe, biome, prop or supported structure **by importing artwork and authoring resource data**, without coding a new C# class or hardcoding a new spawner for each content type. **Reuse shared behaviour and templates; add new C# only when a genuinely new mechanic is needed.** The Avalonia integration/catalog loader is **not yet implemented**.

**Desired add-a-tree workflow (future, illustrative):**

1. Toolbox creates a stable unique content ID (for example tree.pine), name and optional artwork variants.
2. Author sets sprite(s), correct anchor/rooted collider, conservative footprint and spacing, scale/mirror rules and optional wind, snow, fire/charred, hit and harvesting profiles.
3. Toolbox saves/imports valid Godot-compatible Resource/scene assets and textures under the canonical WORLD/Content/Vegetation/Trees/Pine/ folder or deliberately supported external content pack. Asset/import/resource references must resolve.
4. A **future content registry/catalog** finds and validates IDs, template references, dependencies and schema versions. **A separate conflicting database inside Avalonia is undesirable.**
5. Add this canonical definition as a reference in one or more **biome population resources** with data-controlled counts/weights/cluster/eligibility. Merely creating a file should not make it spawn in all biomes.
6. The generic, seeded placer instantiates the new tree from data and applies shared wind, snow, fire, collision, shadows and other configured features. **Do not add special-case “if pine” branches to general generator logic.**

**Existing starting point:** [SceneryDefinition](WORLD/Content/Shared/SceneryDefinition.cs) has stable Id, scene Variants, footprint and environmental metadata. [TreeDefinition](WORLD/Content/Vegetation/Trees/Shared/TreeDefinition.cs) is a shared subtype. [WorldBiomeDefinition](WORLD/Biomes/Shared/WorldBiomeDefinition.cs) holds Surface, Environment and an array of [BiomePopulation](WORLD/Biomes/Shared/BiomePopulation.cs) references. Oak and Pink Blossom are canonical species in production. **Missing:** the actual toolbox workflow, content discovery/registry, content-pack support, validation and automatic import. Current populations are explicitly referenced; new content cannot yet be assumed to work with zero wiring.

**Do-not-break contracts:** definition ≠ mutable individual instance; stable unique IDs ≠ file names or Godot node names; canonical assets defined once and referenced by many biomes; reusable opt-in profiles/behaviours; no new class per tree/item/enemy where existing archetypes suffice; resource schema and dependency validation; versioning for future saves. The exact Avalonia-to-Godot authoring/serialization design remains open. A PNG alone cannot supply collision, identity and behaviour.

### 2. Procedural massive world, chunk streaming and save persistence (PLANNED)

**Leading but not final choice:** a seeded, **expandable finite** medieval-fantasy world, with coordinate/generator design that could support infinite worlds later. Don't lock generation to today's fixed dimensions or assume the final world is one enormous pre-populated World.tscn.

**Intended conceptual pipeline:**
1. Macro geography: continents, coasts, oceans/islands, ranges, relief, rivers and lakes. Future World Creator may offer seed previews and broad geographic guidance, not mandatory hand-authoring of tiles.
2. Fantasy-first biome assignment: organic boundaries, rarity/footprint, unusual enclaves, strong **visual contrasts** within reasonable travel distances and occasional immense forests/deserts. Climate is a **soft input**, not rigid realism that produces six similar adjacent biomes.
3. Deterministic local **chunk detail**: terrain, shoreline, vegetation, ore, resources, inhabitants and authored reusable location templates (villages, ruins, landmarks, camps, dungeons).
4. Stream nearby chunks in/out within CPU, memory and frame budgets, rather than generating/rendering/simulating every chunk at world startup. World bounds, biome regions and streaming chunks are different things.
5. Regenerate untouched baseline content from seed, then layer **persistent changes**: chopped/burned trees, mined ore, buildings, modified terrain, containers, creatures and explored landmarks. Previously changed areas shouldn't reset when revisited.

**Future engineering requirements:** stable world/chunk/object IDs; separate deterministic RNG streams (terrain/biomes/vegetation/landmarks) to minimize unrelated content reshuffling; generator and content **versions**; a deliberate migration policy when new biomes are added months later. New worlds can use updated rules; old explored or modified areas should normally remain stable, while unexplored regions/expansion borders need an explicit policy to prevent seams. Query coarse geography without instantiating distant scenery; bound chunk load/unload work and support cancellation/streaming budgets. **Same seed does not guarantee the same world if definitions/order or generator versions change.**

**Current reality:** [WorldSceneryPlacement](WORLD/Generation/Placement/README.md) scatters once on the bounded **3840×1728 Oak Forest/Pink Meadow** preview. It is **not** macro geography, real chunk streaming or persistence. [WORLD/Runtime/Chunks](WORLD/Runtime/Chunks/README.md), [Streaming](WORLD/Runtime/Streaming/README.md), [Persistence](WORLD/Runtime/Persistence/README.md) and [SAVE](SAVE/README.md) are planning foundations.

### 3. A-Life-style living world: populations + simulation LOD (DESIRED; NOT IMPLEMENTED)

**Desired experience:** deer flee wolves, predators hunt prey, bears defend territory, creatures react to other creatures, and NPCs/travelers/bandits potentially follow meaningful routines. **The player is one participant in the ecosystem, not everybody's universal target.** “A-Life” is a long-term experience goal; no algorithm or simulation scale is locked.

- **Behaviour types:** passive, neutral, territorial, predatory, hostile and intelligent, affected by hunger, fear, injury, group/pack, territory, sound, light, weather and time of day.
- **Population sources:** residents at dens/nests/camps/settlements; roaming herds/predators/travelers/patrols; rare special events. Avoid endless arbitrary edge-of-camera monster spawning. Killing residents can make an area quieter until gradual recolonization.
- **Simulation levels of detail:** near the player, fully active agents with navigation, perception and interactions; farther away, lighter agent/group/territory/schedule records; far-off or unloaded regions, **cheap aggregate/event/elapsed-time** advancement. Don't simulate thousands of invisible CharacterBody2D nodes every frame.
- **State handoff/persistence:** world/site/actor/group IDs, home areas, territories, schedules and consequences should persist through chunk unload/reload. Prevent agents duplicating or resetting simply because a chunk streamed out.
- **Fair danger:** wildlife and enemies may be active **day and night**; hour changes *how* risk occurs, not a monster-on-at-night switch. Avoid difficulty escalation tied only to days survived. Shelter helps with protection, temperature, recovery and supplies without forbidding night exploration.

**Current reality:** Skeleton Basic is an initial production chase/navigation/health test, **not A-Life or a population ecology**. Build reusable behaviours in ENTITIES/Shared and introduce [CORE/Simulation](CORE/Simulation/README.md) budgeting only in focused, authorized stages.

### 4. Caves: independent cave biome + overhead surface biome + depth (AGREED DESIGN, NOT IMPLEMENTED)

**Important fixed design rule:** underground resources and inhabitants depend on **both** the cave biome at the position and **the surface biome geographically ABOVE THAT SPECIFIC POSITION**, plus depth/geology. A *Ravine Cave* under Oak Forest may contain emeralds; the **same Ravine Cave under Desert** may not, even in one connected network. Do **not** duplicate cave biomes as OakForestRavine or DesertRavine.

Separate 2D cave scenes/layers can remain tied to logical surface X/Y and depth. Connected cave systems may pass beneath multiple surface biomes and exit via multiple entrances (even in distant regions); use stable, persistent portal/connection IDs. The exit must return the player to that specific location, **not always the entrance originally used**. Determine the overhead biome from coarse geography **without rendering/loading the surface scene**. Exact coordinate transforms, depths, portal generation and save format are open. Read the [full cave note](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md).

### 5. Survival, buildings, ecosystems and other future mechanics (MOSTLY IDEAS)

- **Food:** per-item-type freshness, spoilage and nutritional roles; *separate harvested batches keep independent age* even if stacked. Storage conditions and temperature influence decay. Preservation/smoking/drying/salting, cooking and balanced nutrition should matter without needless micromanagement. Advance while chunks are unloaded via elapsed time/on-access work, not per-food frame processing.
- **Inventory and equipment:** weight **and** volume, bags/slots/container limitations, encumbrance; separate canonical item type from mutable instance durability, condition, freshness and timestamp. Clothing insulation/wetness and tool wear/repair support longer survival.
- **Health and survival:** hunger, thirst, **long-term fatigue versus short-term stamina**, temperature/exposure, illness/injuries, sleep and shelter; status effects should interact through shared modifiers, not disconnected meter sprawl.
- **Buildings:** modular floor tiles and **tile-edge walls**, rooms, doors, windows, furniture. Outdoors a completed house shows a **full roof**; entering hides/fades its roof to reveal the playable interior, restoring it when leaving. Possible hybrid automatically assembled roofs, multi-floor building ID + floor/depth, basements, stairs, shelter heat and long-term repair/persistence. **32×32 tiles are provisional, not locked.**
- **Fire and world interactions:** generic ignition/lifecycle/effects in [SYSTEMS/WorldInteractions/Fire](SYSTEMS/WorldInteractions/Fire/README.md); ENVIRONMENT supplies wind, rain, lightning and light; WORLD owns charred oak/structure artwork. Production FIRE1/FIRE2 work, but lightning ignition, heat/fuel, spread, damage, collapse/extinguishing and persistent burnt objects are future scopes.
- **Living consequences:** harvesting/regrowth, water quality, muddy/snowy terrain movement, noise/scent, wildlife displacement, shelter and building wear. Keep common mechanics reusable and data-driven.
- **Setting:** medieval/high fantasy now (possibly prehistoric elements). A later sci-fi/alien/robotic world is **optional**, not permission to introduce it into the starting medieval scene.

**Design priority:** believable interconnected systems that create choices, not maximum complexity or survival chores.

### 6. Folder ownership — future coders must NOT create parallel homes

| Canonical owner | What belongs there |
| --- | --- |
| [WORLD/Content](WORLD/Content/README.md) | Canonical scenery, tree, rock, structure, assets and species variants (one definition reused across biomes). |
| [WORLD/Biomes](WORLD/Biomes/README.md) | Overworld Definitions/ and Shared/ types, content references and profiles. Cave biomes separate. |
| [WORLD/Generation](WORLD/Generation/README.md) | Generic deterministic generation/placement/landmark algorithms, not species-specific spawners. |
| [WORLD/Runtime](WORLD/Runtime/README.md), [SAVE](SAVE/README.md) | Chunk lifecycle/streaming/change deltas; coordinated saves, run/profile versioning and serialization. |
| [ENVIRONMENT](ENVIRONMENT/README.md) | **One authoritative wind/clock**; daylight, weather, rain, snow, lightning and atmosphere. Not AI or generic burn state. |
| [ENTITIES](ENTITIES/README.md) | Player, species, AI/perception/navigation, factions, health/movement and population behaviours. |
| [SYSTEMS](SYSTEMS/README.md) | Harvesting, crafting, building, farming, cooking, interaction, effects; fire lives under **WorldInteractions/Fire**. |
| [ITEMS](ITEMS/README.md) | Canonical item/material/food/tool/armour definitions (instance state belongs to inventory/runtime). |
| [CORE](CORE/README.md) | Shared IDs, future content registry, events/contracts and slow simulation scheduling where justified. |
| [COMBAT](COMBAT/README.md) | Reusable hit/damage/defense rather than another per-enemy or player-only damage engine. |
| [SCENES](SCENES/README.md) | Composition like World.tscn, not the home for all gameplay source files. |
| [DEBUG](DEBUG/README.md) / [UI](UI/README.md) / [AUDIO](AUDIO/README.md) | Disposable prototypes and removable developer panels / player UI / reusable audio. Production must run with debug UI removed. |

**Cross-system example:** biome **references** canonical Oak; generic placement spawns an instance; Fire/Harvesting changes that tree; ENVIRONMENT gives wind/weather; ITEMS defines its dropped logs; SAVE will eventually record that this exact tree was removed/burnt. Never copy oak art into a biome, build a second wind simulator for fire, write every species directly into WorldScene or make production depend on DEBUG/TopDownTest assets/code.

### 7. Scope guardrail and implementation status matrix

Before writing code identify (a) **implemented vs debug vs scaffold vs planned**; (b) the **existing correct owner**; (c) **new data or truly new shared behaviour**; (d) whether content IDs, seed determinism, streaming/persistence, offscreen simulations, cave eligibility or Avalonia authorability are affected. **Do not implement all these goals automatically.** Use small, testable passes; preserve the production world and independent DEBUG test; follow [CodePreferences](CodePreferences.md) on modular C#, README/MasterConfigs, method comments/regions, performance and **one GitHub commit per job**.

| Mechanic area | Status |
| --- | --- |
| Production two-biome preview, seeded startup scatter, player/environment/pond, oak FIRE1/FIRE2, early Skeleton Basic | **Implemented preview** |
| Massive macro world, optional World Creator, finite expansion/infinite choice, real chunk streaming | **P1 coarse finite biome map implemented; streaming/major geography still planned** |
| Save system, per-chunk deltas, save migrations and old-world updates | **Planned; not built** |
| A-Life resident populations, roaming ecology, dynamic territories, offscreen simulation tiers | **Desired future feature; not built** |
| Avalonia authoring/import, content registry/catalog, no-code content packs | **Ultimate end goal; not built** |
| Multi-entrance caves and per-position surface + underground biome rules | **Agreed design; not built** |
| Full food/needs/inventory/encumbrance, crafting/building/roofs/floors | **Mostly future ideas/scaffolds** |

**Read these original sources for detail:** [World generation](Notes/World_Generation_Direction.md) · [Wildlife/hostility](Notes/Ideas/World_Hostility_Wildlife_Exploration.md) · [Underground geography](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md) · [Player buildings](Notes/Ideas/Playerbuildings.md) · [Survival/mechanic ideas](Notes/Ideas/FutureMechanicIdeas.md) · [Production implementation](Notes/Production_Implementation.md) · [Snow experiment](Notes/Snow_Accumulation_Codex_Test.txt). Notes/work.md is empty; it is not an active roadmap.

## Run the current debug scene

- Open this repository in **Godot .NET** (Godot 4, C# project) and build the C# solution.
- Press **F5**: `project.godot` currently launches `res://DEBUG/TopDownTest/TopDownVisualTest.tscn`. Alternatively, open that scene and press **F6**.
- **Arrow keys** move a simple placeholder explorer. The **Camera2D follows**, with collisions against grounded props/map bounds and Y sorting. **Mouse aim + left-click** swings the debug axe (outside UI panels); a close aimed hit makes a tree wobble and shed leaves/petals.
- Use the debug on-screen panels for daylight, wind, rain, lightning, surface polish, atmosphere, grass, snow and liquid comparisons. See the test README for precise controls, defaults and removal instructions.
- Scene/window viewport is configured at **1920 × 1080**. The *latest documented* adjacent-biome test map is **3840 × 1728 world pixels**, replacing older 2560 × 1728 references in some detailed notes.
- **Important:** Many trees/rocks are **scattered at runtime once** and therefore will not appear as hand-placed objects in the editor. The ground, clearing, path and map footprint remain authored in the scene; this is **not chunk-based terrain generation**.

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

The current **provisional world-generation preference** is a large, **seeded and expandable finite world**, with the possibility of supporting infinite generation later. Its major geography may be created at a coarse scale (landmasses, mountains, water, themed regions); nearby detailed areas could eventually be generated/loaded as deterministic chunks. Meaningful player changes would need persistence. Handcrafted villages, ruins, cave entrances and landmarks could be embedded in generated terrain. **None of this macro/chunk/save pipeline is implemented by the current scatter demo.**

- **Biome diversity matters more than strict climate realism.** Explore substantial visual contrasts at reasonable travel distances while retaining occasional huge forests or other memorable regions. Transition shapes should feel organic, not checkerboard-like.
- **Danger exists during day *and* night.** Time of day changes activity, visibility, weather/exposure and encounters; don't make daylight perfectly safe or trigger all danger only after sunset.
- **Shelter is valuable but not obligatory.** Temperature, rain, fatigue, recovery, supplies and security should motivate shelter; prepared players can continue exploring at night.
- **Wildlife dispositions should vary:** passive, neutral, territorial, predatory, hostile and intelligent. Creatures may respond to other creatures and their environment, not just the player.
- **Regions provide reasons to travel:** distinctive threats, resources, wildlife, caves, knowledge, trade and events. Prefer resident populations, roaming activity and rare events over arbitrary player-targeted spawns.
- **Long-term realism must remain playable and performant:** Simulate nearby actors in detail; distant ecology could use aggregate state and bounded updates. Avoid multiplying micromanagement meters.

These are **design preferences and ideas**, not finished AI, population, shelter, survival or world-generation mechanics. See the source notes below for alternatives and unresolved decisions.

## Currently implemented — contained DEBUG prototypes

The active demo lives in **[DEBUG/TopDownTest/](DEBUG/TopDownTest/README.md)**. Its test README and component docs are the detailed sources of truth; the root README only records broad capabilities.

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

- [WORLD/](WORLD/README.md): reusable scenery/content, biomes, terrain, water, caves, landmarks, generation and future runtime world management.
- [ENVIRONMENT/](ENVIRONMENT/README.md): shared time/calendar, lighting, weather, wind, seasons and atmosphere. Biomes may reference local environment profiles; species-specific art stays with WORLD content.
- [ENTITIES/Player/](ENTITIES/Player/README.md): modular player input, movement, visuals, equipment, inventory, stats, interactions, combat and intended survival components.
- [CORE/](CORE/README.md): shared IDs, registries, contracts/events and eventual simulation infrastructure.
- [ITEMS/](ITEMS/README.md): central item/content definitions, tools, food, clothing and materials.
- [SYSTEMS/](SYSTEMS/README.md): reusable interaction, harvesting, crafting, farming, building, skills/effects and [WorldInteractions/Fire](SYSTEMS/WorldInteractions/Fire/README.md).
- [COMBAT/](COMBAT/README.md): common combat/damage resolution, distinct from player controls.
- [SAVE/](SAVE/README.md): future save/profile lifecycle and coordination with generated world state.
- [UI/](UI/README.md): player-facing interfaces and HUD.
- [AUDIO/](AUDIO/README.md): ambience, music and shared sound effects.
- [DEBUG/](DEBUG/README.md): temporary experiments, plus retired/historical visual tests.

## Design notes & decision status

`Notes/` mixes **current exploration**, future mechanic ideas and older references. Read **each note's own status/date**; none automatically supersede running code or constitutes an implementation order.

- [World generation direction](Notes/World_Generation_Direction.md): finite vs infinite, expandable-world preference, seed-based macro geography, chunking, persistence, fantasy biome contrast, caves and authored landmarks. **Provisional.**
- [Hostility, wildlife, shelter and exploration](Notes/Ideas/World_Hostility_Wildlife_Exploration.md): daytime/nighttime danger, creature dispositions, realistic ecosystem feel, reasons to shelter/travel, optional sci-fi direction. **Design ideas only.**
- [Future mechanic ideas](Notes/Ideas/FutureMechanicIdeas.md): food spoilage/nutrition, preservation, inventory and load, clothing/temperature, survival, fire, structures and more. **Do not implement solely from this note.**
- [Player building ideas](Notes/Ideas/Playerbuildings.md): modular rooms, exterior full roofs, roof fade and optional multi-floor construction. Ideas only.
- [Underground cave connections](Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md): agreed overhead surface biome + underground cave biome + depth model, multi-exit geography. Not built.
- [Production implementation](Notes/Production_Implementation.md): implemented extraction, validation and limitations; check newer code for changes.
- [Snow test design](Notes/Snow_Accumulation_Codex_Test.txt): earlier experiment specification; current behaviour is best checked against the **SnowTest README and code**.
- [DEBUG/TopDownTest](DEBUG/TopDownTest/README.md): the **implementation index** for runnable experiments and their config paths.

## Scope and next steps

**Near-term direction:** Evolve the existing production preview into a believable survival slice while protecting the long-term architecture above. Approve each implementation pass separately.

1. Continue testing and selecting the art style, sprite sizes, top-down sorting/occlusion and visual effects; assess target hardware and confirm which DEBUG results to keep.
2. Extend the **existing production** player/interaction/harvesting foundation as requested, reusing proven DEBUG mechanics rather than rebuilding it.
3. Add a basic gathering loop (e.g. tree harvesting with health/damage and item drops), a small inventory and a simple crafting/building objective. **Current axe wobble is only hit feedback.**
4. Build on Skeleton Basic with passive and neutral/territorial wildlife when requested, testing shared behaviour/population models before global A-Life.
5. Separately prototype terrain/chunk generation, contrasting biome boundaries, a cave/landmark and saved world changes *before* committing to a huge streaming world.
6. Extend systems iteratively, data-first and with performance checks rather than attempting all envisioned survival mechanics together.

These are **suggested priorities**, not an automatically authorized implementation ticket list.

## Decisions still open

- Final art resolution, sprite scale/animation pipeline and the right balance of pixel-art detail and shaders.
- Exact size/bounds of the world, expandable finite vs infinite approach, biome sizes/adjacency, generated-vs-authored balance and save/versioning of visited areas.
- Height depiction, cave representation, world-creator options, settlement/NPC simulation and population density.
- Combat, magic, survival depth, progression, permadeath persistence rules and difficulty.
- Whether any future sci-fi setting is a separate world/project or ever shares content with the medieval game.

## Repository history and transition

The repository name still contains **`isometric`** for historical reasons, but **the active direction is a fully 2D top-down game**. The former 3D/isometric architecture is no longer the game's main path. Some legacy resources/scenes/`.uid` remnants may reference removed scripts; they are not evidence of implemented modern systems. Retired material under `DEBUG/VisualTests/` and older notes should be checked carefully before reuse.

**General rule for future sessions:** Distinguish **game vision**, **current debug proof-of-concepts**, and **approved production code** at every step. Do not infer a mechanic is finished because its folder, README, art asset or config exists.
