# Fantasy Survival Sandbox — 2D Top-Down Reboot

**Production foundation added:** [SCENES/World/World.tscn](SCENES/World/World.tscn) is an independent playable Oak Forest world (F6): player/collision/camera, organic grass ground and data-driven oak/rock/grass placement. A fresh seed is chosen/logged each load; fixed-seed replay is configurable. DEBUG stays intact and remains the F5 default. Production environmental visuals are now extracted as described below; survival/gameplay systems remain planned. See [run/configuration guide](SCENES/World/README.md). Earlier scaffolding-only descriptions below are superseded for these specific modules.

**Phase 2 environment implemented:** The production world now includes day/night and cached sun/contact shadows, shared wind/vegetation sway, haze/particles, cloud shade, colour grade/torch, rain/splashes/lightning and snow on ground/trees/rocks/grass. Optional collapsible controls live in `DEBUG/UI/Environment/` (**F9**). Biomes can supply typed local profile overrides; shared renderers remain in their owning production folders. See [environment documentation](ENVIRONMENT/README.md). No lake, combat, fire gameplay, seasons or climate engine is promoted.

> **Project snapshot — 21 October 2026:** An actively expanding, **runnable 2D debug sandbox**, not yet a production survival game. The earlier 3D/isometric approach is retired. The debug forest now demonstrates player movement, procedural scenery placement, two adjacent biomes, environmental visuals, water/snow comparisons and an axe/tree-hit experiment. Most gameplay, survival, simulation, persistence and world-generation ambitions below are **design or scaffolding**, not implemented features.

**Three distinct statuses used in this README:**
- **Implemented in DEBUG:** Experimental scenes, shaders, controls and C# scripts under `DEBUG/`. They are disposable, may have limitations, and are **not production systems**.
- **Production scaffolding:** Folder structures, READMEs and MasterConfigs exist, but this does not imply working mechanics.
- **Planned / exploratory:** Gameplay vision, design notes and alternatives under consideration. A note is **not authorization** to implement it.

## Start here — future ChatGPT / Codex / contributors

1. Read **this README** to understand the game and what exists today.
2. Read **[CodePreferences.md](CodePreferences.md)** **before touching code**. Follow the author's folder layout, C# style, comments/regions, MasterConfigs, reusability, simplicity, performance priorities and **one GitHub commit per job**.
3. Read the **README inside the exact feature folder** you are modifying, its `MasterConfig` / `.tres` settings, relevant scene and actual scripts. Some older notes may be superseded by newer documentation or code.
4. For the working demo, start with **[DEBUG/TopDownTest/README.md](DEBUG/TopDownTest/README.md)**. For long-term ideas, use the [design notes](#design-notes--decision-status) rather than assuming everything in `Notes/` is obsolete or approved.
5. **Do not silently promote debug experiments to production, reorganize unrelated modules, or implement idea documents on sight.** When promotion is requested, extract reusable parts into their owning production folders deliberately, preserving module boundaries, inspector-tweakable data and removable test wiring. Verify the real code and current scene rather than relying only on summaries.

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
- Aim for atmospheric but efficient dynamic sun/day-night light, projected tree shadows, wind, rain, snow, cloud shade, mist, particles, water, seasons and environmental reactions. Current implementations are **comparisons in DEBUG**, not a unified production renderer.
- Art variants in the repository root (including charred oak sprites) are **assets/reference material**, not proof that a complete fire-spread, tree-burning or dead-tree transformation mechanic exists.

## Gameplay pillars — intended, not implemented

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

**Scope and limitations:** These modules are experiment-sized and independently documented for adjustment/removal. `DEBUG/TopDownTest/` must **not** be confused with production `WORLD/` or `ENVIRONMENT/`. Scenery is scattered once on a fixed test map, not streamed in world chunks. Shader-based snow, liquid immersion, weather and lighting are visual approximations; there is no complete fire system, seasons simulation, food/inventory, NPC AI, saving, farming, buildings or survival loop. Some rendering checks in feature notes used software OpenGL and reduced scene density; **Windows D3D12 behaviour and target-machine performance still require local testing**.

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
- [SYSTEMS/](SYSTEMS/README.md): reusable interaction, gathering/harvesting, crafting, farming, building and skills/effects.
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
- [Player building ideas](Notes/Ideas/Playerbuildings.md): building-related brainstorming; check its status before use.
- [Snow test design](Notes/Snow_Accumulation_Codex_Test.txt): earlier experiment specification; current behaviour is best checked against the **SnowTest README and code**.
- [DEBUG/TopDownTest](DEBUG/TopDownTest/README.md): the **implementation index** for runnable experiments and their config paths.

## Scope and next steps

**Immediate milestone:** Turn the promising visual/debug sandbox into a small, believable, playable survival slice **only after specific systems are approved for extraction**.

1. Continue testing and selecting the art style, sprite sizes, top-down sorting/occlusion and visual effects; assess target hardware and confirm which DEBUG results to keep.
2. Define a minimal **production** player/interaction foundation by adapting useful debug movement and interaction work rather than blindly copying debug scripts.
3. Add a basic gathering loop (e.g. tree harvesting with health/damage and item drops), a small inventory and a simple crafting/building objective. **Current axe wobble is only hit feedback.**
4. Prototype one passive wildlife species and one daytime neutral/territorial/hostile creature, with basic avoidance or combat, then gradually introduce survival/shelter consequences.
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
