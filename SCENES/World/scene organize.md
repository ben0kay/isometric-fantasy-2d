# World Scene Organization — Codex Design / Refactor Plan

> **Status:** PLAN ONLY. No scene reparenting or gameplay changes are authorized by this note.
> **Date:** 2026-10-22
> **Location:** This document intentionally lives beside `SCENES/World/World.tscn`.
> **Goal:** Make Godot's **Remote Scene Tree** understandable at a glance, give spawned objects clear ownership/lifecycle, and prepare for entities, effects, large-world chunk streaming, profile saves and future A-Life **without breaking existing production gameplay or performance**.
> **Codex:** Read root `README.md`, `CodePreferences.md`, this file, `SCENES/World/README.md`, `WORLD/Generation_Implementation.md`, `WORLD/Runtime/Streaming/README.md`, and all affected production scene/scripts **at current HEAD**. Implement only a specifically approved pass. One GitHub commit per job.

## 1. Important terminology

- Godot `World.tscn` already has **one actual scene root**: `World` (`WorldScene : Node2D`).
- Names like `EntitiesRoot`, `EffectsRoot`, `TerrainRoot`, `SystemsRoot`, `ChunksRoot`, `DebugUIRoot` mean **ordinary child container nodes**, not extra top-level scene roots, autoload singletons or magical built-in managers.
- A container organizes **ownership and runtime hierarchy**. It does **not** automatically discover, count, register, stream, optimize or simulate the objects below it.
- An optional manager/registry can coordinate currently active objects via **explicit spawn/despawn registration** and stable IDs; Godot groups provide lightweight category lookup. Don't create a full manager for each category just to count children.
- **Node grouping alone does not increase FPS.** Benefits are clear debugging, bounded lifecycle cleanup, safer chunk unloading, separating responsibilities and making profiling easier.

## 2. Actual production snapshot — MUST NOT assume old preview-only architecture

As reviewed at writing time, `SCENES/World/World.tscn` contains:

~~~text
World (Node2D, WorldScene.cs)
├── Ground (Sprite2D, GroundSurface.cs)          # legacy preview surface
├── Liquids (WorldPond.tscn)                    # shared authored pond
├── Actors (Node2D, WorldSceneryPlacement.cs)   # YSortEnabled = true; MIXED responsibilities
│   └── Player (Player.tscn)
├── EntityPlacement (Node, WorldEntityPlacement.cs)
├── Environment (Node2D, EnvironmentRuntime.cs)
├── EnvironmentDebugPanel (CanvasLayer, F9)
├── Fire (Node, FireRuntime.cs)
├── FireDebugPanel (CanvasLayer)
└── MacroMapDebugPanel (CanvasLayer, F7)
~~~

**And at runtime** `WorldScene._Ready()` adds:
- `Bounds` — `StaticBody2D` holding finite-world boundary walls.
- `Streaming` — `WorldChunkStream`; **P3 streamed world is already implemented and enabled by default**. This is NOT a task to build a new streaming system.
- `Occlusion` — `OcclusionController` for player-behind-tree fading.
- Inside `Actors`: single test skeleton `Entity` from `ENTITIES/Shared/Entity.tscn`, original torch and up to two preview torches, **plus** many `Chunk_X_Y` nodes containing generated trees/rocks/grass. Each chunk also owns a separate streamed **ground MeshInstance2D currently added directly under World**, not under `Actors`.
- Inside `Environment`: one world clock, shared wind/weather/day-night, effects, sun-shadow adapters, snow, torch light controllers, etc. It already registers/unregisters **chunk-local** adapters when chunks load/unload.
- Legacy/compatibility preview still exists by disabling `WorldMasterConfig.Streaming.Enabled`; then `WorldSceneryPlacement` generates directly under `Actors` and `Ground` is active.

**Current design problem:** `Actors` is simultaneously (1) a Y-sorted draw/physics container, (2) the legacy scenery generator because `WorldSceneryPlacement.cs` is attached to it, (3) the parent of player/skeleton/torches, and (4) the parent of streamed scenery chunks. Many scripts explicitly depend on this shape. A simple editor drag-and-drop of these nodes will break the scene.

**Current status:** Macro geography, priority reservations and active streaming are in place. Player-profile disk persistence, off-screen A-Life and general entity populations remain later phases. Do not claim those are built.

## 3. Proposed target tree — ORGANIZATIONAL DESIGN, not a direct immediate rename

Favor **a few clear top-level domains**, and only one necessary ownership layer per use case. Names can be refined by Codex, but explain the exact ownership before editing:

~~~text
World (WorldScene.cs)
│
├── TerrainRoot (Node2D)                          # terrain-domain visuals, not gameplay AI
│   ├── Ground (GroundSurface.cs)                 # legacy preview; retain compatibility
│   ├── StreamedGroundRoot (Node2D)               # ONLY currently loaded chunk ground meshes
│   │   ├── ChunkGround_-1_0
│   │   └── ChunkGround_0_0
│   └── Liquids (WorldPond.tscn)                  # authored pond / future water composition
│
├── WorldActorsRoot (Node2D, YSortEnabled=true)   # common visual/Y-sort coordinate space
│   ├── PersistentEntitiesRoot (Node2D)           # player + long-lived currently loaded entities
│   │   ├── Player
│   │   └── Entity [the current test skeleton]    # dynamically created; see lifetime notes
│   ├── PersistentPropsRoot (Node2D)              # spawn torches / fixed test props when needed
│   │   ├── Torch
│   │   └── PreviewTorch1/2
│   ├── ChunkActorsRoot (Node2D)
│   │   ├── Chunk_X_Y (Node2D, YSortEnabled=true)
│   │   │   ├── SceneryRoot (Node2D)              # live trees, rocks, grass
│   │   │   └── EntitiesRoot (Node2D)             # future chunk-owned creatures / NPCs
│   │   └── Chunk_X2_Y2 (...)
│   └── PreviewSceneryRoot (Node2D)               # ONLY compatibility-mode legacy props
│
├── EffectsRoot (Node2D)                          # optional shared world-space transient FX
│   └── [registered standalone effects only; see exceptions below]
│
├── SystemsRoot (Node)                            # lightweight coordinators; avoid duplicate managers
│   ├── EntityPlacement [existing single test spawner]
│   ├── SceneryPlacement [existing legacy placement controller, if separated]
│   ├── Streaming [existing WorldChunkStream]
│   ├── Environment [existing EnvironmentRuntime + its child modules]
│   ├── Fire [existing FireRuntime]
│   └── Occlusion [existing OcclusionController]
│
├── Bounds (StaticBody2D)                         # existing walls; may be placed with terrain safely
│
└── DebugUIRoot (Node or CanvasLayer owner)
    ├── EnvironmentDebugPanel (F9)
    ├── FireDebugPanel
    └── MacroMapDebugPanel (F7)
~~~

**This is a conceptual final arrangement, not a command to create all folders/nodes immediately.** Prioritize working ownership over cosmetic naming. Godot `Node2D` container origins should normally remain at (0,0), with no hidden offset/scale/rotation; preserve each object's **global** transform during reparenting.

### Important practical exceptions

1. **Don't automatically reparent every effect to EffectsRoot.** BurningEffect currently lives under its burning tree and uses the tree/receiver reference; torch smoke/embers and rain/snow live inside ENVIRONMENT modules; some effects need parent transforms and must die with their owner. EffectsRoot is appropriate for truly **standalone world-space transient effects** such as impact particles or pooled explosions, with an explicit owner if needed.
2. **Don't auto-move all NPCs into a single global list.** Chunk-bound ordinary creatures should belong to their chunk; persistent test/unique entities can live outside disposable chunks until A-Life materialization exists. A global live-entity registry can track references across both paths when needed.
3. **Godot Y sorting:** `WorldActorsRoot`, child containers and chunks must be reviewed together; nested `YSortEnabled`, `ZIndex`, CanvasItem hierarchy and sibling order can alter how the player draws in front of/behind nearby trunks, buildings, props and NPCs, including **across chunk seams**. Do not assume moving the tree under `SceneryRoot` leaves sorting identical. Validate at actual intersecting sprites and the chunk boundary.
4. **Godot drawing order:** Terrain ground and water, sun silhouettes/contact shadows, atmospheric overlays, ColourGrade CanvasLayer and particle renderers have intentional Z/layer behavior. Moving their CanvasItem ancestors can affect draw order, local coordinates or modulate inheritance. Test instead of blindly moving every visual node.
5. **The current preview / streaming switch must keep working.** `PreviewSceneryRoot` can be omitted or folded into an existing destination if it complicates the refactor; it should not spawn twice or leak props across modes.
6. **A named node is not a save system.** `WorldId`, streamed chunk ownership and session deltas already exist, but durable per-profile saves are an independent follow-up. Never serialize all live Node objects just because they belong to EntitiesRoot.

## 4. CRITICAL code dependencies that must be updated together

This is **not a complete automated grep**; Codex must search the latest repository/tests for all old paths and parent assumptions, then list its actual replacements **before changing the .tscn**.

| Existing component | Current coupling | What refactor must handle |
| --- | --- | --- |
| `WORLD/Runtime/WorldScene.cs` | Paths `"Ground"`, `"Actors/Player"`, `"Actors"`, `"Liquids"`, `"EntityPlacement"`, `"Environment"`, `"Fire"`; `AddBounds()` adds directly to `World` | Update paths or centralize cached typed references; initialization order stays **macro → stream/props → creature → environment → fire/pond → occlusion/bind** |
| `WORLD/Generation/Placement/WorldSceneryPlacement.cs` | A **scene Node2D placement controller** that calls `AddChild(prop)` on itself; used for preview, and as the `actors` passed into stream | Separate generator's *logic* from the destination **only if required**, or provide a compatible output-container reference; no duplicate legacy/streamed scenery |
| `WORLD/Runtime/Streaming/WorldChunkStream.cs` | Stores `_actors`; `Begin()` adds chunk actor nodes under `_actors`; separate ground meshes are added via `_world.AddChild(chunk.Ground)`; code fetches `_world.GetNodeOrNull("EntityPlacement")` and `_world.GetNode("Environment")` | Feed explicit `ChunkActorsRoot` + `StreamedGroundRoot` destinations; retain stable chunk ID, session removal/burn data, backpressure, hiding and unload order. Adapt `ApplyOne`, `BindChunk`, `Release`, preview path and nav updates. Use **existing** stream, not parallel streamer |
| `WORLD/Generation/Placement/WorldEntityPlacement.cs` | `Populate(actors,...)` calls `actors.AddChild(instance)`; static nav `Build(actors,...)` scans collision descendants; `RefreshNavigation` uses bounds | Spawn into intended PersistentEntitiesRoot; accept separate **collision/nav-source root** covering streamed obstacles, with correct world/local transforms. Don't build navigation only from entity children |
| `ENVIRONMENT/EnvironmentRuntime.cs` | `Initialize(world, actors)` attaches original/preview torches to `actors`, binds wind/wetness/shadows/snow/rain/atmosphere, and `RegisterChunk(chunk)` / `UnregisterChunk(chunk)` | Keep **one** clock/wind/weather/day-night; point torch instantiation at persistent prop destination; distinguish total world actor source from streamed chunks and register exactly once |
| `ENVIRONMENT/Atmosphere/WorldAtmosphere.cs` | Holds `Actors`, scans `Actors.FindChildren(...)` on chunk lifecycle; draws particles using `tree.Position` and `_torch.Position` | If nesting changes, particle coordinates must be converted via **GlobalPosition/ToLocal**; otherwise falling leaves/smoke may drift to wrong location even if the nodes exist. Preserve bounded refresh scheduling |
| `SYSTEMS/WorldInteractions/Fire/Simulation/FireRuntime.cs` | Registers live `Burnable` descendants of the passed `actors` root; spawned `BurningEffect` is tree-child | Maintain ancestor checks and per-chunk register/unregister/event cleanup; do not move burning FX to global root without auditing transforms/lifetime |
| `DEBUG/UI/Fire/WorldFireDebugPanel.cs` | `GetParent().GetNode("Actors")` then `_actors.GetNode("Player")`; scans `world_burnables` and checks `_actors.IsAncestorOf(...)` | A DebugUIRoot move breaks **both** parent/path lookups. Update explicitly, keep Fire buttons working in both streaming and preview modes |
| `DEBUG/UI/Environment/EnvironmentDebugPanel.cs` | Exported `EnvironmentPath="../Environment"`; F9 runs against a shared profile | Repoint to actual environment node after reparenting and retain initialization timing and hotkey |
| `DEBUG/UI/Generation/MacroMapDebugPanel.cs` | Assumes `GetParent() is WorldScene`; F7 reads World.MacroMap/Stream | If grouped under DebugUIRoot, give a safe exported WorldPath or resolved ancestor; keep panel removable and live counts correct |
| `ENVIRONMENT/Lighting/Shadows/SunShadowRenderer.cs` and `ENVIRONMENT/Weather/` / `Wind/` / `Snow/` | Initial actor scans and streamed chunk adapter lifetimes; projected shadow transforms rely on source and shadow coordinate spaces | Re-check positions across hierarchy and ensure registration/unregistration and shader/caster cleanup remain correct; **one authoritative environment**, no duplicated lighting |
| `TESTS/Generation/`, `TESTS/Lighting/`, `TESTS/Trees/`, `TESTS/Fire/` | Some fixtures explicitly reference `Actors` or `World` child paths, and assert item counts/sorting | Update the minimum expected paths in tests; avoid hiding broken production logic behind only test changes |

**Do not create a fake legacy Actors alias node solely to conceal missing reference changes.** Explicit destination nodes, typed references/setters and tested paths are safer. Also do not rename canonical content scripts/resources just to match the scene organization names.

## 5. How EntitiesRoot should actually keep track of creatures

**Hierarchy ownership, lightweight active-index and persistent simulation are different responsibilities:**

- **Parent/owner:** Where the live `Node2D` is attached, when it loads/unloads, and whether it is part of a disposable chunk. The player's scene lives outside unloadable chunks.
- **Entity type / query:** Use Godot **groups**, shared Entity base configuration, IDs and/or entity definitions. E.g. `world_entities`, `enemy`, `wildlife` (define actual names centrally, avoid scanning the entire tree per frame). A skeleton, wolf, deer and future animal all use the same registration contract.
- **Optional world-scoped ActiveEntityRegistry** (introduce only when there are enough entities to warrant it): supports `Register(instance, id, chunk?)`, `Unregister(id)`, count/lookup and safe event subscriptions. On chunk complete, register any new creature; on unload, unregister **before** `QueueFree`. Avoid keeping dead/stale strong references.
- **Future A-Life:** unloaded entities are **not** Node children; they're compact saved records/regions keyed to `WorldId`, logical layer, stable entity ID and world time. A-Life may materialize an entity into a loaded chunk and dematerialize it later. Prevent duplicate live entity and abstract-record representations.
- **Profile saves later:** each run/world's registry and records stay isolated; changing profiles clears the active scene and manager caches without mixing WorldIds.
- **Entity count display:** `EntitiesRoot.GetChildCount()` only counts its *immediate* children; it cannot count all streamed creatures across chunks. Provide explicit `ActiveEntityCount` via a registry or event-maintained counter when needed; don't call `GetNodesInGroup` or `FindChildren` every frame for an F4 overlay.

**First organization pass needs ZERO new entity manager/AI systems.** The existing generic `ENTITIES/Shared/Entity.tscn` and `WorldEntityPlacement` should continue to work. A registry is a **later, separate, testable pass**.

## 6. What EffectsRoot should and should not do

- Good candidates: world-space one-shot hit sparks, damage numbers, brief splashes, dropped particle bursts, pooled attack impacts, generic VFX whose origin/lifetime is independent of the parent.
- Bad immediate candidates: `BurningEffect` bound to a tree, the player's equipped effects, the current procedural WorldAtmosphere drawing, per-chunk weather adapters, or torch-local flame/PointLight2D components.
- If using a future EffectsManager, give it a capped spawn API and update/removal budget, optionally object pooling for expensive recurring scene types; avoid a new `_Process` on every effect when the engine's GPU particles or timers can handle it.
- World-space effects under `EffectsRoot` must receive correct **global transforms** and be removed on world exit. Chunk-local transient effects may instead be children of their chunk so unloading cancels them automatically.
- Nodes named `EffectsRoot` add organization only; don't promise performance gains until a real measured system enforces limits.

## 7. Safe, incremental Codex plan

### SO0 — Audit, print exact current Remote Scene Tree, establish a baseline (no behavior changes)

- Confirm `World.tscn` and all _runtime-added_ children after F6 (preview and P3 streaming modes), using Godot's **Remote** scene tree, not only the Local editor tree.
- Search all hardcoded `NodePath`, `GetParent`, `GetNode`, `AddChild`, `IsAncestorOf`, YSort, CanvasLayer and transform assumptions (including tests, chunk/scenery generators, foliage/snow/fire/light sources).
- Record node-count hierarchy, frame time, major startup steps and Godot profiler snapshots at spawn, across biome borders and after chunk travel.
- Decide whether the nice names can be achieved with simple containers versus a deeper responsibility split. Show exact old→new node paths, runtime-parent destinations and expected cleanup order **before editing**.

### SO1 — Safe scene-tree containers / hotkey migration

- Create only the useful common-purpose roots; don't automatically create dozens of empty containers.
- Reparent persistent World scene children in **small groups**, update WorldScene and dependent debug panels. Keep original root `World` and canonical script/Resource ownership.
- Confirm `F6`, `F7` and `F9`, FireDebugPanel, player movement/camera, world bounds, pond/wading, torches/local occluders and shared sun/weather still work.
- Optional: at this stage keep existing `Actors` (with script) as a temporary compatibility container **by design**, not as an unrelated second manager. Move player/props/chunks only when their destination APIs have been adapted.
- **Pass:** The Remote scene tree has meaningful top-level sections; no missing-node errors or regressions; preview and streamed modes both work.

### SO2 — Separate dynamic actor/scenery ownership from controllers

- Untangle `WorldSceneryPlacement` (which currently lives ON Actors) from container responsibilities so `EntitiesRoot`, `PreviewSceneryRoot` and `ChunkActorsRoot` can be used without forcing runtime content under a generator service.
- Route player and existing skeleton to persistent non-disposable ownership; original + preview torches into appropriate persistent props.
- Use **the current WorldChunkStream** for generated chunk scenery/ground; assign distinct child roots only if nested Y sorting/physics behaves exactly as required. Do not duplicate streaming or recompute biome data.
- Register/unregister current wind/wetness/snow/sun-shadow/fire/occlusion integrations when chunk roots change; maintain current session delta and stable IDs.
- **Pass:** Crossing boundaries, removing a tree, burning one, returning to its chunk, walking around rocks and switching preview/stream modes all work without duplicates or visual glitches.

### SO3 — Optional lightweight registries and runtime overview (only if useful)

- Expose simple counters for active entities, active chunk roots, rendered scenery, local effects and world/system managers, without a per-frame full-tree scan.
- If multiple creature types now require shared lookup, add one minimal **world-scoped** registry, not a manager for each species. Use explicit spawn/unload registration. Test skeleton chase/target/health and world exit cleanup.
- If requested, a removable developer-only debug overview can show category counts. **The Remote tree itself is already sufficient for basic organization**, so do not invent a new F-key panel by default.

### SO4 — Future compatibility review (do not implement SAVE/A-Life here)

- Document how world-scoped IDs, per-profile save restoration and future A-Life de-/materialization plug into the chosen roots.
- Validate migration across overworld and future cave/indoor layers; ensure actor references don't hardcode one overworld root.
- Keep generators and profiles data-driven; do not invent per-species scene-manager scripts.

**Recommended first Codex scope:** **SO0 → SO1**, review the Remote tree and actual behavior, **then** consider SO2. No massive one-shot scene rewrite.

## 8. Acceptance tests / don't-break checklist

- [ ] Main `SCENES/World/World.tscn` compiles and F6 starts without missing `GetNode` paths, null references or C# scene import issues; F5 DEBUG default remains unchanged.
- [ ] F7 macro map/stream count, F9 environment controls, FireDebugPanel buttons, local-light shadow toggles still function.
- [ ] Player, generic skeleton chase, axe/tree hits, tree visibility fade, local torch shadows and ocean/pond visual/wading stay correct.
- [ ] One authoritative world clock, wind, weather, sun renderer and fire simulation; no duplicate manager spawned by a container node.
- [ ] Physics collisions, parent transforms, player camera, sunlight projection, Y sort and foliage shadows visually match baseline—including **trees beside chunk seams**.
- [ ] Streaming active/prefetch/unload rings, 25 initial active chunks (default config), budgets, ground meshes, chunk IDs and session tree-removal/burn deltas retain working lifecycle.
- [ ] After travelling across multiple regions and back, old chunks, lights, particles, fire subscriptions, navigation adapters and occlusion references are disposed/registered exactly once.
- [ ] Disabled streaming still renders the original Oak/Pink bounded preview with generated scenery.
- [ ] World root is still the actual `WorldScene`, while grouping nodes are ordinary descendants; visible hierarchy in the **Remote** tab matches the documented design.
- [ ] Root grouping itself introduces no new per-frame actor/full-tree scan or unnecessary process loops; compare frame time/peak nodes against baseline.
- [ ] Existing `TESTS/Generation/ValidateChunkStreaming.tscn`, macro/reservation, lighting, generic tree and fire tests are re-run and updated only for genuine path changes.
- [ ] Update `SCENES/World/README.md`, related module READMEs and this note with **actual** implementation status after Codex completes a pass. One GitHub commit per implementation job.

## 9. Suggested prompt for Codex

> Read `SCENES/World/scene organize.md`, `SCENES/World/World.tscn`, `WORLD/Runtime/WorldScene.cs`, `WORLD/Runtime/Streaming/WorldChunkStream.cs`, `ENVIRONMENT/EnvironmentRuntime.cs`, `WORLD/Generation/Placement/WorldSceneryPlacement.cs`, `WORLD/Generation/Placement/WorldEntityPlacement.cs`, and `CodePreferences.md`. Inspect latest HEAD and the actual Remote scene hierarchy. I want readable container nodes such as TerrainRoot, WorldActorsRoot (with persistent actors and already-streamed chunk actors), EffectsRoot, SystemsRoot and DebugUIRoot. **Do SO0/SO1 only to start**: identify all node-path/parent/YSort/coordinate dependencies, then implement a small safe regrouping that preserves P3 chunk streaming, fallback preview mode, all lighting/weather/fire, camera and F7/F9. Avoid creating a new streaming/AI/save system or moving transform-sensitive effects unnecessarily. Run Godot/.NET checks and update notes honestly.
