# World Scene Organization — Codex Design / Refactor Plan

> **Status (2026-10-22):** SO0/SO1 implemented; SO2 ownership implemented and awaiting local Copilot validation; SO3/SO4 remain proposed. Additional GL0–GL3 location/session architecture is proposed below, not authorized code changes. See SO1 implementation notes near the end.
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

**Current status (2026-10-22):** P0–P3 generation/streaming exist and **P4 private campaign checkpoints/Continue have been implemented and automated-tested** (manual in-editor/performance acceptance open; see `SAVE/README.md`). Off-screen A-Life and general populations are future phases. The tree above is a **historical SO0 snapshot**; inspect actual HEAD before moving nodes.

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
6. **A named node is not a save system.** P4 already persists per-profile/run/world campaigns and supported surface edits. Never serialize all live Nodes just because they belong to EntitiesRoot or create a second save coordinator. Future cave/layer persistence needs an explicit versioned P4 extension.

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

## Implemented SO1 + first scenery stability pass — 22 October 2026

SO0 source dependency audit and programmatic runtime hierarchy/baseline checks were performed. Interactive Remote-tree inspection, graphical profiler captures and Windows/D3D12 validation are still outstanding. SO1 containers are implemented; SO2–SO4 remain planned. The original PLAN ONLY banner applies to the remaining proposals, not this recorded implementation.

Exact path migrations:

| Old path under World | Current path under World |
| --- | --- |
| Ground / Liquids / Bounds | TerrainRoot/Ground / TerrainRoot/Liquids / TerrainRoot/Bounds |
| Runtime ground meshes directly under World | TerrainRoot/StreamedGroundRoot/ChunkGround_X_Y |
| EntityPlacement / Environment / Fire | SystemsRoot/EntityPlacement / SystemsRoot/Environment / SystemsRoot/Fire |
| Streaming / Occlusion | SystemsRoot/Streaming / SystemsRoot/Occlusion |
| EnvironmentDebugPanel / FireDebugPanel / MacroMapDebugPanel | DebugUIRoot/(same panel names) |
| Actors, Actors/Player, Actors/Chunk_X_Y, torches, skeleton | Unchanged deliberately for SO1 |

All container transforms are identity. Production WorldScene/streamer and F7/F9/fire paths were updated together; preview fixtures follow genuine production path changes. Transform-sensitive tree fire, rain/snow and atmosphere effects stay with their existing owners. New tests inspect actual instantiated ownership, not just .tscn text.

Fixed-seed, paused-noon headless A/B sample at settled spawn: 49 loaded chunks; cosmetic updates in 16; sun updates fell from 367 casters to 90. Average sun-projection CPU time in that sample was 2.226ms without culling versus 1.392ms with culling. This is a narrow, environment-dependent CPU diagnostic, not a measured graphical FPS improvement; subsequent runs may differ. Loaded identity/counts remain unchanged by the switch.

Runtime binding is now staged while hidden, and cancellation cleans partial adapters. Static chunk/ground/contact shadows disable interpolation; contact shadows also start hidden. Shared artwork/camera bounds and off-screen scheduling live in WORLD/Runtime/Visibility; existing environmental systems retain their ownership. Native local lights, active fire effects, worker-thread generation and hard real-time budgets remain outside this pass. Do not mark the user's flicker or target-machine lag resolved until verified visually.

Validation caveat: one repeated Godot 4.5.1 Mono fixture run logged `Handle is not initialized` from ScriptManagerBridge/DisposablesTracker during process shutdown after all 160 assertions passed. An immediate repeat completed with no errors. This is recorded rather than treated as a proven production fix or ignored; check shutdown on the project's target Godot runtime if it recurs.


## 10. Updated SO1–SO4 handoff and future locations — 22 October 2026

**Read this section before acting on the earlier historical proposals.** SO0/SO1 are implemented as documented above. P4 disk saves, New Campaign, Continue and save/quit have also been implemented and passed automated checks; **P4 manual UI/graphical/profiler acceptance and P3 visual/performance acceptance remain open**. Do not interpret older “P4 future” wording or the original raw node tree as current. Work in small, separately approved commits.

### SO1 — COMPLETE: safe grouping; validate, don't redo

- Existing `TerrainRoot`, `SystemsRoot` and `DebugUIRoot` paths are recorded in the SO1 implementation section. Original `Actors` and its responsibilities were deliberately retained at this stage.
- Codex should **inspect actual HEAD/Remote tree and compare against that record**; only fix an observed missing path, Y-sort issue, or regression. Do **not** repeat SO1 reparenting or add duplicate managers for cleaner names.
- Keep F5 profile → campaign New/Continue and F6 direct disposable World separate. Include save/restore and old preview in SO1 regression checks.

### SO2 — NEXT CONDITIONAL PASS: explicit ownership and destination APIs

- Audit `WorldSceneryPlacement` (generator/controller mixed with `Actors`), `WorldChunkStream`, `WorldEntityPlacement`, navigation/collision source roots, F7/F9/fire/debug paths and all environment/scenery adapter subscriptions **at current HEAD**.
- Prefer **typed explicit destinations** for `PersistentEntitiesRoot`, `PersistentPropsRoot`, `ChunkActorsRoot`, optional `PreviewSceneryRoot` and `StreamedGroundRoot`, rather than hardcoded parent walking; add only roots that eliminate a real ownership ambiguity. Preserve shared Y-sort space, global/local transforms, physics, occlusion, shadows and chunk seam draw order.
- Separate the legacy scenery placer from live actor parenting only when tests justify it. Keep **one** streamer, one existing player, one environment clock and one P4 save coordinator. Chunk-owned scenery/entities die with chunks; player and persistent authored actors must not be accidentally unloaded.
- P4 integration: continue after real save/reload must restore unchanged chunks and altered/burnt/removed props, test actor and player. Switching campaigns must clean runtime subscriptions, pending stream work and any refs; world IDs must never leak across profiles.
- **Pass gate:** source dependency/old→new paths recorded, build and headless regression suites green, F5/F6/F7/F9, fire/torch/pond/player, stable frame timings and manual Y-sort/stream seam checks. Keep SO2 independent of GameSession or cave implementation.

### SO3 — OPTIONAL: lightweight registration, not mandatory managers

- If needed, add **one location-scoped active-entity registry** with explicit registration on spawn/materialize and unregister-before-free; stable keys should include location/layer and world identity where necessary. Do not enumerate `GetNodesInGroup` or descendants each frame.
- Chunk-owned living entities and persistent players/unique actors should use the same lookup contract but retain their correct owning parents. The registry indexes **currently materialized** actors; later P6 A-Life owns detached offscreen records, not permanent Nodes.
- Optional event-maintained counts for chunk actors, entities and transient FX; test unload, despawn, save/quit and profile switch cleanup before adding UI. No species-specific registries, extra F-key panels or new `_Process` loops just to label nodes.
- **Pass gate:** zero stale refs/duplicate registrations after repeated chunk load/unload, menu transition and test-skeleton death. Codex may recommend **skipping SO3** if Godot groups and existing event hooks suffice.

### SO4 — Shared-location compatibility contract (DESIGN FIRST)

- The proposed **GameSession** coordinates an *active* campaign, existing P4 coordinator, global game time and transitions. An **active location** (surface/cave/interior) owns detailed terrain, streaming, local actors/effects and environment-specific presentation. Only nearby parts of one playable location should be materialized by default; inactive overworld/cave cosmetic/physics loops must sleep.
- Prefer **composition and small contracts** over inherited `Simulation.tscn` containing all surface weather/sun/vegetation. Shared movement, combat, fire, local torches and interactions should work in either environment; surface rain/snow/sun remain optional, cave darkness/tunnels/biomes separate. Do not create duplicate clocks, save coordinators, fire managers or full parallel streamers.
- Define `LocationId`, location type, logical depth/layer, local/world coordinates, entry/return portal IDs and deterministic destination; support **multiple entrances to one cave network**. Cave resource rules must consider the underground biome AND the **surface biome above each underground coordinate**, never just the entry biome (see `Notes/Ideas/Underground_Biome_Influence_And_Cave_Connections.md`).
- P4 currently supports only the `surface` layer and rejects unknown layers: design a **versioned schema/compatibility extension** for active location, return destination, cave modifications and layer state; never silently modify existing saves or introduce a second profile registry. Address what happens to an in-flight checkpoint during transitions and how re-entry restores correct chunks/actors.
- Existing `World.tscn` should remain canonical for F6 and current P3/P4 tests until any wrapper is demonstrably equivalent. Examine whether `GameSession.tscn` should own `ActiveLocationRoot` or whether a lighter session node suffices; **do not assume a new global autoload or scene inheritance is required**. Keep menu worlds out of memory while on menus.
- SO4 output is a design/risks/test brief, **not** a full cave generator, P5 landmark placer or P6 A-Life implementation.

### Proposed follow-on GL0–GL3 (SEPARATE APPROVAL from SO phases)

1. **GL0 — Contract and migration audit, no runtime changes:** map `WorldScene`, `MenuNavigation`, `CampaignSession`, `CampaignSnapshot`, `CampaignStore`, save coordinator and streaming callbacks; decide session/active-location ownership, clock source, portal identity, coordinate transform and versioning. Provide alternatives/tradeoffs and risks.
2. **GL1 — Minimal session wrapper:** introduce `GameSession`/location host *only if it meaningfully simplifies transitions*, wrap existing surface location with near-zero behavioural changes, maintain F5 New/Continue, F6 direct tests, P4 recovery and direct preview. No duplicated world node, clock or save controller.
3. **GL2 — Small cave test location:** a separate lightweight `Cave.tscn` sharing player/torch/fire/interaction building blocks; cave-specific darkness and simple static geometry only. Prove clean entry/exit lifecycle and transform/Y-sort behaviour before procedural underground generation.
4. **GL3 — Two-way portal and versioned save proof:** two surface entrances can refer to the same cave ID; exiting B returns to B, not A. Save/quit while inside, continue into the correct cave/depth, restore changes and return to the surface; guard content/version mismatches, stale async tasks and corrupt-save recovery.

**GL work belongs in a separate approved implementation handoff**, e.g. `SCENES/GameSession/LOCATION_IMPLEMENTATION.md`, when ready. P5 can meanwhile reserve deterministic cave entrance sites/portal IDs; do not imply that a reserved entrance already loads playable caves.

### Codex review requests: improve the design before coding

1. **Critique the wrapper:** Would a persistent `GameSession` scene, a plain coordinator, or another small host integrate best with Godot 4.7.2 scene changes and the existing P4 one-shot `CampaignSession`? Show the smallest migration preserving current startup/direct tests.
2. **Avoid expensive abstraction:** Which actual systems support a shared interface today? Which are tied to overworld `WorldScene`, `EnvironmentRuntime`, chunk ground/nav or CanvasLayers? Prefer adapters/configuration over a giant abstract base scene.
3. **Streaming strategy:** Can the existing `WorldChunkStream` contract support distinct location providers without fragile rewrites, or should cave streaming be a later specialised implementation sharing core chunk services? Provide a measured comparison before selecting.
4. **Save compatibility:** Propose safe layer/location keys, portal return state and checkpoint transaction boundaries. Clearly identify content fingerprint/schema migration and recovery tests; never silently corrupt/delete user campaigns.
5. **Coordinates and biomes:** Propose stable horizontal mapping between cave and surface, multiple entrances, and surface-biome lookup without loading the overworld. Document alternatives for non-1:1 cave distances.
6. **Performance/lifecycle:** Inventory callbacks, GPU effects, background generation tasks, collision/navigation caches, input and subscriptions that must stop at exit. Require one active location, bounded loads, dormant inactive systems, and no per-frame full-scene scans.
7. **Validation and scope:** Give an SO2 then GL0–GL3 implementation order with exact rollback steps, real Windows renderer visual/perf checks and automated cross-profile/reload tests. Identify any SO3 features that should be postponed entirely.

**Decision gate:** Codex should return a concise architecture review and proposed diffs first. No GL0–GL3 or broad save schema change is automatically authorized by this note. One small tested commit per approved implementation job.


## SO2 implementation handoff — 22 October 2026

SO2 is implemented, **awaiting local compilation/runtime/graphical/performance validation by GitHub Copilot** at the user's request. See [SO2_Copilot_Handoff.md](SO2_Copilot_Handoff.md) for the before/after paths, audited dependencies, changed files, exact commands and acceptance/rollback requirements. This supersedes earlier SO2-next/planned wording; SO3/SO4 and GL0–GL3 remain unimplemented and separately scoped.

Actual hierarchy: WorldActorsRoot → PersistentEntitiesRoot (player/skeleton), PersistentPropsRoot (torches), ChunkActorsRoot (existing chunk roots), PreviewSceneryRoot (preview props). Ground stays under TerrainRoot/StreamedGroundRoot. SystemsRoot/SceneryPlacement is a plain Node controller, no longer the parent of runtime actors. WorldScene caches typed roots/services once; initial environment discovery crosses only marked ownership containers and skips chunks. All intermediate actor roots enable Y sorting and have identity transforms. No additional per-chunk roots, EffectsRoot, entity registry, duplicate streamer/clock/save system or process loops were added.

P4 remains functionally accepted by the user on its prior revision, but SO2 save compatibility must be reverified. The exact previous source scene is frozen as migration input; only the known SO2 scene plus matching current dependency fingerprints permits the layout-only transition. Saved IDs, macro/features and edits do not regenerate. New checkpoints use the verified current fingerprint; unknown content/schema changes still fail with files preserved.

Validation so far: source/dependency review and diff whitespace checks only. No Godot execution/build/benchmark occurred here. In particular nested Y sorting, seam overlaps, source adapter counts, preview mode, P4 old/new save restoration and Windows frame pacing are **pending**. No performance gain or visual equivalence is claimed until measured/inspected.
