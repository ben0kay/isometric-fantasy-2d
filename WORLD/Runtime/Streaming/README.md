# Finite world streaming — P3

The production World now streams the P1 biome map and respects P2 reservations. F6 starts at the protected macro starting anchor. F7 retains the map/reservation view and adds live chunk/count status. The default finite bounds remain 45000×33750; a new scene session still chooses one random seed. Disk saves/Continue remain P4.

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

Removed generated props and burn progress survive unload/reload **within this scene session**, using touched-instance deltas. Burning pauses while unloaded; no off-screen fire simulation is claimed. Full run/clock/inventory saving, generator/content migration policy and disk-backed deltas belong to P4. Restarting the scene still starts a new temporary world and loses these session edits.

## Validation

See TESTS/Generation/ValidateChunkStreaming.tscn. The headless integration passed 4227 checks: negative floor coordinates, reverse-order generation, neighbour seam separation, four distant areas/return, bounded loaded chunks, stable home IDs/positions, burnt-state restoration, a removed prop tombstone, both-biome membership, finite edge chunks and world exit cleanup. At rest each tested destination reached 49 loaded chunks; observed scene nodes varied ~3342–4695 with biome density rather than growing each trip. Existing P1 (377085 checks), P2 (175 checks) and explicit legacy-preview baseline also passed. C# was compiled with .NET 8 Roslyn/Godot source generators, using Godot 4.5.1 .NET for runtime checks; the repo SDK declaration is unchanged. Native graphical Windows/D3D12, visual frame times and target-hardware memory still need local verification.

The runtime blueprint also records local generator version, chunk size and candidate-cell size (schema 3). This is metadata for later SAVE integration, not a shipped disk-save format.

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
