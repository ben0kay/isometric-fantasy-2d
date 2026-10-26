# Finite world streaming — P3

> **BC4 — 26 October 2026:** BC4 changes global macro planning/default bounds only; nearby detail remains the same 512-unit chunk window. Default is 1006240×754680 with 2048-unit macro cells and on-demand coordinate border refinement. Continue restores saved bounds; no entire world scenery path is preloaded. [Current handoff](../../Generation/Authoring/BC4_Validation.md).


The production World now streams the P1 biome map and respects P2 reservations. F6 starts at the protected macro starting anchor. F7 retains the map/reservation view and adds live chunk/count status. The default finite bounds remain 45000×33750; a new scene session still chooses one random seed. P4 private campaign disk saves/Continue are **implemented** under SAVE. F6 direct sessions remain disposable.

## Data and scheduling

`WorldMasterConfig.Streaming` references an inline `WorldStreamingConfig` in DefaultWorldConfig.tres. Set Enabled=false (or Streaming=null) for the original 3840×1728 compatibility preview. The new stream uses Generation.Biomes as its authoritative biome collection; the adjacent Oak/Pink layout is not required to select streamed content. Adding biome Resources requires no biome-name branches.

Default chunk size is 512 world units, active radius 2, prefetch radius 3 and unload radius 4 (Chebyshev squares). The initial **25 active chunks** are materialized before player control. Later loads select nearest missing prefetch chunks, generate one data result, and apply at most 12 scenery roots per frame under a soft 3 ms application budget. A chunk becomes visible/physical only when complete and registered. There is one pending application, not an unbounded generation queue. Obsolete partial chunks cancel; at most two distant loaded chunks unload per frame. Hysteresis avoids repeated unload/reload at edges. Stale work cannot mutate another world's stream instance.

The application budget does not include a preemptible worker job: data generation, lifecycle binding, initial startup and local nav rebuilds remain synchronous bounded steps. Profile their actual cost before raising radii/density. NodesPerFrame counts scenery roots, not every child in an authored scene. No GPU FPS guarantee is made.

Input pauses at a streaming frontier until the necessary active square is loaded. Active extent is validated against the maximum configured camera view, conservative art extent and sun-shadow length. If you enlarge artwork or reduce chunk size/radius, increase active/prefetch/unload radii together. Ring values are bounded (unload <=6); invalid settings report errors. Window/zoom capabilities beyond startup assumptions require revalidation/restart. Camera and four finite boundary walls use the macro bounds.

## Placement and ownership

`ChunkSceneryData` uses a global 96-unit candidate lattice with coordinate-keyed RNG, independent of travel/load order and chunk size. Every candidate is owned by the floor-derived chunk containing its root; art/colliders may extend beyond it. Neighbour candidates are sampled through a conservative halo on both sides of chunk borders. Stable priority thinning gives solid scenery precedence over decorative grass, then an integer rank; overlapping losers are rejected. It is intentionally conservative, not a maximal packing algorithm.

Populations explicitly author `InstancesPerMillionUnits` (density per million square world units). Expected candidates are area-based; spacing, biome edges, water and reservations reduce accepted counts. Total density must fit candidate-grid capacity; footprints must fit the bounded neighbour halo. Preview Count/ClusterSize/ClusterRadius remain **preview-only**, clearly grouped in the Inspector. Shared scale/mirror/variant settings apply in both modes. The initial streaming scatter is spatially jittered rather than the preview's cluster generator.

IDs are `surface/<candidate-x>/<candidate-y>/<content-id>`, scoped to the manifest WorldId. Chunk cache ownership includes WorldId/surface/chunk coordinates. No globally shared mutable seed cache exists. Resources stay canonical and unmodified. P2 masks are queried before placing scenery; low grass can occupy permitted start/path space while solid trunks cannot. The existing single authored pond remains a fixed lightweight world landmark and rejects overlapping candidates; it is not randomly regenerated per chunk.

## Visuals, physics and current state

Each complete chunk owns its actors and one clipped ground quad. StreamedGroundPalette builds small shared coarse appearance maps plus a 32-pixel-tile atlas from biome surface textures; colour/patch/detail fields and world-space noise are shared across chunk seams. Macro cell sampling blends palette boundaries. AvoidBiomeTransition scenery gets conservative biome/visual margin checks. The detailed terrain renderer is still flat top-down ground, not coasts/cliffs or height generation.

Chunk wind/wetness/shadow/snow adapters register after materialization and restore/release on unload. One authoritative wind, clock and weather remain shared. Rain artwork exclusions and atmosphere tree sources refresh only on lifecycle changes; snow cover/pollen/haze are bounded around the camera. Fire subscriptions and occlusion entries unregister before node release. Removed casters also release projected/contact shadows. Existing torches, pond/wading and single skeleton remain available; no new encounter population system is added.

Navigation reuses one active-area grid and refreshes after lifecycle changes at most twice per second, never over the complete map. The existing skeleton keeps its identity and sleeps outside the local active bounds. Biome environment overrides currently resolve from the starting biome; **automatic per-region weather/profile blending is not implemented**. Ground/content already follow all region lookups.

Removed generated props and burn progress survive unload/reload **within this scene session**, using touched-instance deltas. Burning pauses while unloaded; no off-screen fire simulation is claimed. P4 now persists supported player/run/clock and changed chunk state through private profile campaigns; inventory is not yet a completed gameplay system and automatic content migrations remain future. Restarting a **disposable F6** scene still creates a temporary world and loses edits.

## Validation

See TESTS/Generation/ValidateChunkStreaming.tscn. The headless integration passed 4227 checks: negative floor coordinates, reverse-order generation, neighbour seam separation, four distant areas/return, bounded loaded chunks, stable home IDs/positions, burnt-state restoration, a removed prop tombstone, both-biome membership, finite edge chunks and world exit cleanup. At rest each tested destination reached 49 loaded chunks; observed scene nodes varied ~3342–4695 with biome density rather than growing each trip. Existing P1 (377085 checks), P2 (175 checks) and explicit legacy-preview baseline also passed. C# was compiled with .NET 8 Roslyn/Godot source generators, using Godot 4.5.1 .NET for runtime checks; the repo SDK declaration is unchanged. Native graphical Windows/D3D12, visual frame times and target-hardware memory still need local verification.

The runtime blueprint also records local generator version, chunk size and candidate-cell size (schema 3). P4 SAVE now stores compatible blueprint/checkpoint identity; runtime blueprint metadata is not a second independent disk-save format.

Concurrent layered-rain and shared-enemy changes were retained. New foliage joins the world wetness level, its shader keeps receiving shared wind, and the generic enemy continues using the same bounded navigation instance.

Compatibility fixtures for generic trees, fire and local lights explicitly select preview mode and also pass after the streaming change. Their tests keep their original authored-count/position expectations; the P3 suite tests the new runtime separately.

## Rendering fixes after P3

The protected start now uses a 320×320 footprint plus 64-unit clearance (DefaultWorldLayout.tres), leaving nearby trees/rocks visible while keeping arrival space clear. This changes new-session feature outcomes, not biome identity.

Sun shadows start hidden until their first projection and disable physics interpolation because their transforms update in the render process. Completed chunk actors reset interpolation before publication. This prevents uninitialized full-resolution silhouettes and interpolation from identity transforms. Wetness and snow share generated Shader resources across chunks through world-owned caches; material parameters and cleanup remain owned by each adapter. Disabled snow no longer updates unused foliage uniforms. Actor-source refreshes coalesce once per frame, and prefetch ordering is cached until the player changes chunk.

Regression checks cover nearby spawn trees, hidden initial shadows, interpolation settings and shared foliage shader identity across distant traversal. Headless checks cannot establish graphical frame rates or prove the Windows rendering symptoms resolved; verify those in the running game. Streaming bounds scenery memory, but shader compilation, loaded scenery effects and synchronous lifecycle/navigation work still have frame costs.

## First stability pass + SO1 — 22 October 2026

Static chunk roots and ground now explicitly disable interpolation; generated contact shadows start hidden and disable interpolation too. This closes additional interpolation/publication paths; a target-renderer visual check is still required before claiming the reported flicker is resolved.

Environment adapter binding yields between wind, wetness, sun and snow stages and between receiver registrations. Runtime chunks remain hidden until registration finishes. Cancelling a partially bound chunk disposes its enumerator and releases whichever adapters/subscriptions were created without discarding saved burn deltas. Each adapter stage, data generation, navigation rebuild and unload remains individually synchronous: the 3ms budget is still soft, not an absolute maximum. Navigation waits for remaining frame budget, ignores prefetch-only lifecycle changes, skips rebuilding for the sleeping test skeleton and rejects wholly off-grid polygons before rasterization.

Shared off-screen cosmetic scheduling is documented in ../Visibility/README.md. F7 now exposes a reversible culling comparison checkbox plus process/physics CPU monitor readings, last/peak stream work, last/peak generation, last item application, total binding time for the last completed runtime chunk, navigation/unload timings, sun time/caster counts and active cosmetic-chunk counts. Values are diagnostic; CPU monitors are not GPU or end-to-end frame-time measurements. Peaks include warm-up and remain for the scene session. Capture F7 after standing still, walking across a chunk boundary and returning; compare culling enabled/disabled on the same fixed seed/camera position.

SO1 keeps Actors/Player, chunk roots, torches and the skeleton in their existing Y-sorting hierarchy. Ground/pond/bounds and streamed meshes now belong to TerrainRoot; controllers belong to SystemsRoot; removable panels belong to DebugUIRoot. WorldScene, streamer, F7/F9/fire wiring and affected fixtures use the new explicit paths. There is no second streamer or scenery manager.

Validation for this pass: compiled with the bundled Godot 4.5.1 SDK/source generators and .NET 8 (the project's authored 4.7.2 SDK setting is preserved). P1/P2/P3, compatibility preview, generic tree, fire and local-light headless fixtures passed. ValidateSceneryVisibility passed 160 checks and exercises culling A/B, long-shadow influence, restored wind/snow, partial adapter cancellation, hierarchy ownership, static interpolation and preview mode. Headless CPU measurements are recorded in the scene organization handoff; Windows/D3D12 frame pacing, shader compilation and visible flicker remain local graphical checks.

UI follow-up: live performance/streaming counters, the cosmetic-culling comparison and world/biome/reservation text now live in the right-side F9 diagnostics column. F7 is map-only. The diagnostics remain visible when the map is closed, and stop refreshing while hidden.

Tracing follow-up: right-side F9 diagnostics can now capture production C# callbacks with nested exclusive timings. Streaming data/ground construction, instantiation, effect binding, release, navigation and source-cache refreshes are separated. Worst slow-frame reports preserve matching frame data instead of combining unrelated last-value counters. This is instrumentation to locate remaining target-machine stalls; no new performance fix is claimed. See CORE/Diagnostics/README.md.

## Measured rain/atmosphere refresh hitch fix — 22 October 2026

Two user captures isolated the same cause:

| Frame | Interval / measured callbacks | Rain source refresh | Atmosphere source refresh | GC 0/1/2 |
| --- | --- | --- | --- | --- |
| 4323 | 51.99 / 46.67ms | 33.97ms | 6.81ms | 1/1/0 |
| 23175 | 56.43 / 52.43ms | 36.69ms | 10.81ms | 1/1/0 |

The accompanying recordings show rain disabled during traversal, but both caches were still globally rebuilt after lifecycle changes. Those refreshes explain about 87–91% of measured callback time in these frames. Collections coincided with them; their individual pause contribution was not measured.

Streaming now registers rain and atmosphere sources per prop inside the existing yielded receiver-binding steps, and unregisters on explicit removal, chunk release and partial-registration cancellation. EnvironmentRuntime no longer has a dirty flag that scans all actors after chunk changes. Rain uses spatially indexed cached rectangles; atmosphere uses cached tree references/positions/colours and constant-time indexed removal. Fire appearance changes refresh only their owning prop. Unrelated chunks are untouched. The public shared lifecycle supports future canonical scenery templates without species branches.

Compiled with Godot 4.5.1 source generators/.NET 8 while preserving the project's SDK setting. Headless P3 (4425 checks), visibility/source lifecycle (167 checks), runtime trace, generic tree and production fire fixtures passed. New assertions cover unchanged stationary registrations, complete cancellation/unload source counts, negative cell boundaries, idempotent registration and removal of ghost exclusions. No target-machine FPS or fixed hitch duration is claimed until the same route is captured again. Steady-state sun/atmosphere drawing costs remain separate follow-up work.

Rain activation contract: zero intensity now stops precipitation callbacks/GPU/splash pools, and dry wetness helpers sleep after drying. Streaming scenery registers shared ground-query data with EnvironmentRuntime.GroundQueries (SceneryArtworkIndex), independently of rain; there is no rain source-refresh task during dry traversal. Manual lightning queries that shared data without enabling rain. Per-prop material/configuration setup remains part of chunk binding; the existing soft budget still applies. Production rain defaults to zero. See ENVIRONMENT/Weather/Rain/README.md for details.

Validation: the dry-state suite passed 22 checks; streamed traversal/session deltas, visibility/source lifecycle (167 checks), generic trees, production fire and F7/F9 UI checks passed with the new dry default. One concurrent visibility-suite run passed its assertions but crashed in Godot 4.5.1 Mono shutdown with unsafe-reference messages; an isolated repeat passed and exited cleanly. This recurring local Mono shutdown issue remains recorded, not claimed fixed. Target Godot 4.7.2/Windows visual and performance checks remain necessary.

## P4 campaign integration — implemented, original functional checks accepted

Menu-launched campaigns restore frozen macro/features and import SavedChunk deltas before initial chunk materialization. CaptureChanges snapshots loaded removed/burnt props plus retained unloaded edits at save boundaries; healthy visited trees do not accumulate records. SAVE owns the worker/checkpoint/menu lifecycle. Stable candidate IDs and cross-chunk placement remain unchanged. F6 direct sessions remain disposable; fire still pauses while unloaded. See SAVE/README.md and SAVE/P4_Copilot_Handoff.md for exact local test commands. Historical statement about the initial P4 implementation handoff: Copilot later verified commit `ce6767d1` (31/33 write/read checks) and the user accepted the earlier P4 functional flow. SO2/C1 automated regressions also passed. Large-save profiling remains open.

## SO2 destinations — implemented and automated-tested
The existing stream receives ChunkActorsRoot, StreamedGroundRoot, the full actor navigation-source root and EntityPlacement explicitly. Chunk props remain immediate children of the same hidden/publication-controlled chunk node; binding/cancellation/unload and saved deltas are unchanged. Player/skeleton and authored torches now have persistent sibling roots. No additional streamed SceneryRoot/EntitiesRoot layers or streamer are introduced. Regression paths follow genuine hierarchy changes. See SCENES/World/SO2_Copilot_Handoff.md.

SO2 validation update: Codex completed the Godot 4.7.2/.NET 8 build/import and relevant headless regressions, including real old-layout disk checkpoint restoration. All passed; P4 write/read now pass 39/33 checks after adding committed-migration assertions. See SCENES/World/SO2_Validation.md. Earlier automated-pending wording is historical; rendered seam/Y-sort and target-PC performance checks remain open.

GS2/GS3 ground: same single quad per chunk and world-owned shared material; direct stack compilation adds optional two-slot overlays and distinct-art boundary blending without changing streaming lifecycle. Production layers are empty. See ../../Terrain/Ground/Surfaces/GS2_GS3_Validation.md for measured software checks, bounded memory/GPU-work accounting and pending target tests.

GS4: the existing shared ground material owns scene-local GroundSurfacePreview overrides. No additional chunk lifecycle, per-frame callbacks or map rebuilding. See ../../Terrain/Ground/Surfaces/GS4_Validation.md for F9 behavior and pending local checks.

## WG2 generated liquids

Opted-in schema-5 worlds use the same chunk prepare/publish/release scheduler for cropped water surfaces. Row-sliced fields stay hidden until the ground/scenery chunk publishes; cancellation and unload release their native resources. Full frozen lake geometry participates in candidate rejection, and local nav receives shared polygons. No water is inserted into schema-4 campaigns. [Lifecycle, limits and validation](../../Liquids/WG2_Validation.md).

H4 integration: real World trees use the shared harvest profile. WorldHarvestRuntime owns Player inventory/pickup binding; WorldChunkStream restores work and felled tombstones, attaches harvesting on materialization, and activates only loaded landing-chunk drop nodes. Logical offscreen drops remain saved without Nodes. [Controls, ownership and checks](../../../SYSTEMS/Harvesting/H4_World_Validation.md).
