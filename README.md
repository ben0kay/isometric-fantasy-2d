# Fantasy Survival Sandbox — New Direction

> **Status (October 2026):** Fresh-start planning stage. The previous fixed-camera 3D/isometric fantasy prototype has been retired as the project's active direction. The small 2D debug visual test is implemented (runtime validation pending); the wider vision below remains planned. Existing notes under `Notes/` are retained as historical reference and are not automatically current requirements.

**Run the debug test:** Build C# in Godot .NET, then F5. Arrow keys move the static explorer around a 2D forest clearing. Everything for this disposable test lives in [DEBUG/TopDownTest/](DEBUG/TopDownTest/README.md); open TopDownVisualTest.tscn and press F6 to run it directly.

## Vision

A **2D, top-down medieval fantasy survival sandbox**: an ordinary person trying to live as long as possible in a dangerous, living fantasy world. Exploration, farming, crafting, building, hunting, mining, combat, and discovery are all viable ways to spend time. The world should feel expansive and interactive, with wildlife, settlements, ruins, caves, and fantastical threats. Inspired by the breadth of Skyrim, the survival tension of Project Zomboid, and the content-friendly top-down structure of games such as Necesse.

**Survival-first:** Permadeath and starting a new run after death are the current intended core loop. No traditional mandatory win condition. Exact progression and world-persistence rules remain open.

## Visual direction

- **Fully 2D gameplay and world coordinates**, using Godot 2D rather than the old Camera3D/Node3D terrain architecture.
- **Top-down square-grid world with front/angled-view sprites**, similar in projection to Stardew Valley or Necesse; **not** diamond-grid isometric, and not 3D terrain with sprite billboards.
- **More mature, richly atmospheric pixel art** than Stardew Valley's bright/cartoon style. Look to **Drova: Forsaken Kin** and **Stoneshard** for detailed medieval/dark-fantasy art, grounded colours, foliage, stonework, and readable creatures.
- Pixel art is a **production-friendly starting point**, not a commitment to bland or extremely low-resolution graphics. Trial tile and sprite dimensions before locking a pipeline (e.g. 32px or 48px tiles, taller character sprites).
- Dynamic day/night lighting, torchlight, weather, fog, shadows, and environmental effects should provide atmosphere without requiring highly elaborate frame-by-frame animation.
- Favour reusable modular assets, tile variation, palette discipline, and selective detail over a huge bespoke art burden.

## Gameplay pillars

1. **Survive:** hunger, thirst, fatigue, temperature, injuries, illness, and shelter are potential systems; tune for meaningful choices rather than chores.
2. **Live off the land:** forage, hunt, fish, farm, harvest wood, cook, preserve food, and potentially domesticate livestock.
3. **Craft and build:** tools, weapons, armour, storage, workshops, shelters, farms, houses, and defensive structures.
4. **Explore:** forests, grasslands, mountains, rivers, marshes, snowy regions, villages, abandoned structures, ruins, and dangerous wilderness.
5. **Go underground:** mine ores, find cave entrances, traverse procedural cave/dungeon maps, discover resources and hostile creatures; multiple depths/floors are possible.
6. **Fight or evade:** skeletons, undead, bandits, beasts, goblins, wolves, bears, and other fantasy creatures; combat and stealth/avoidance should both matter.
7. **Inhabit a world:** wildlife behaviour, settlements, NPCs, merchants, schedules, and emergent encounters are longer-term possibilities.

## World design

Aim for large, diverse, explorable environments with **data-driven biomes, encounters, resources, and points of interest**. Deterministic generation, chunk loading, and persistence of meaningful player changes are desirable, but implementation details are undecided. Support authored landmarks alongside procedural surrounding content where useful. Caves and interiors can be separate 2D maps/scenes; terrain height can be represented visually/logically with cliffs, ramps, stairs, bridges, and layers rather than true 3D slopes.

## Architecture principles

- **Godot with C#**; clean, modular, readable folder organization.
- **Data-driven content first:** adding a crop, animal, ore, item, recipe, enemy, biome, or building part should generally be data + artwork, not a new bespoke script.
- Shared entity capabilities and behaviours: e.g. health, inventory, harvesting, growth, AI, combat, interactions, containers, crafting stations.
- Keep gameplay rules separate from replaceable visuals; avoid hardwiring behaviour to animation frames or specific sprites.
- Use inspector-editable Resources / MasterConfigs where appropriate; maintain clear READMEs and code organization.
- Performance matters for many entities, vegetation, lighting, and large worlds. Profile rather than assume.

## Production folder foundations

- [WORLD/](WORLD/README.md): reusable scenery/content, biome definitions, terrain, water, caves, locations, generation and runtime world management.
- [ENVIRONMENT/](ENVIRONMENT/README.md): time/calendar, lighting, weather, wind, seasonal progression and shared atmospheric effects. Biomes reference optional local environmental profiles; tree-specific seasonal sprites remain with their WORLD content definitions.
- [DEBUG/TopDownTest/](DEBUG/TopDownTest/README.md): disposable visual tests, including the current day/night, projected shadow and wind experiment. These are **not yet promoted** to ENVIRONMENT production systems.

Both WORLD and ENVIRONMENT are currently **planning/scaffolding only**; no production features are implied by these folders.

## Scope and next steps

**Do not treat the ideas above as implemented.** This is a reboot; prioritize a tiny playable vertical slice before expanding content:

1. Confirm the art reference and camera framing with a simple **2D forest test scene**.
2. Add basic 2D player movement, collisions, camera, and one hostile creature.
3. Test terrain tiles, trees, sorting/occlusion, cave entrance and one cave interior.
4. Prototype gathering, a minimal inventory, and one crafting/building loop.
5. Add survival and procedural systems incrementally once the basic feel is proven.

## Decisions still open

- Exact tile/sprite resolution, animation complexity, and final pixel-art treatment.
- World size, generated-vs-authored balance, settlement density, NPC simulation.
- Survival realism and difficulty, combat mechanics, magic, and progression.
- Save rules across permadeath and the extent of world persistence.

## Repository transition

The old **3D/isometric** README was superseded by this document. Obsolete 3D camera, 3D player configuration, and 3D natural-terrain debug script files have been removed. Generic empty master configuration scaffolding can remain for reuse. Some old scenes, resources, or `.uid` files may still reference removed scripts; these are **legacy leftovers**, not evidence that the new 2D game is implemented. Review them before attempting to run the project. Preserve `Notes/` as requested.
