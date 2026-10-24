# WG0 audit and WG1 implementation — 24 October 2026

WG0 is complete. WG1 code is implemented; automated checks are recorded below. Windows graphical/performance acceptance remains open. WG2–WG7 are still planned.

## WG0: confirmed contracts and decisions

| Owner inspected | Findings / decision |
| --- | --- |
| `WORLD/Liquids/Regions/{WorldPond,WorldLiquidRegion}.cs`, `WorldPond.tscn`, `Pond.tscn` | The production pond exists. Region builds a local 96-point harmonic polygon and 384×256 signed-distance texture once. World-space distance queries use that polygon. Surface animation, player wading and rain splashes already exist. This is an overlay, not terrain excavation. Preserve the pond and its DEBUG ancestor. |
| `WORLD/Generation/World/{MacroWorldMap,WorldGenerationMasterConfig}.cs` | P1 supplies frozen seed/bounds/coarse cell-to-biome ownership. Hydrology must query whole intersecting cells, not only lake centres. The macro grid does not define shoreline resolution. No elevation/drainage service exists. |
| `World/Features/{WorldFeaturePlan,WorldReservationMap}.cs` | P2 runs first; protected start blocks Water. Existing disk/reservation queries can conservatively reject a complete enclosing disk plus shore separation. Keep required features fixed. |
| `WORLD/Runtime/WorldScene.cs`, streaming and placement | Existing WorldScene knows the fixed pond before stream initialization. Plan water after P2/P1, before streaming, but defer all generated-water scene materialization, scenery exclusions and navigation changes to WG2. Current scenery supports the fixed liquid region, not a generated-body collection. |
| `WORLD/Runtime/Persistence/{WorldBlueprintManifest,WorldBlueprintRestore}.cs`, `SAVE/WorldState/{CampaignStore,CampaignSession}.cs`, `SAVE/Migrations/SaveCompatibility.cs` | Runtime blueprint schema 4 is already frozen and stored as a checked content-addressed blob. Add optional `WaterPlanSnapshot`; missing data means empty forever on Continue. Do not regenerate from current authoring settings. Validate water version/seed/IDs/bounds/simple geometry/hash before accepting a checkpoint. No existing scene/resource dependency text changes are needed, preserving existing content fingerprints. |
| Ground surfaces and `DEBUG/UI/Generation/MacroMapDebugPanel.cs` | Ground masks remain unchanged until WG2/WG4. F7 may show planned polygons without implying playable water. Do not use F9 to mutate saved geography. |
| `ENTITIES/Player/Input/PlayerInput.cs` | RMB currently emits shared utility intent for hop/guard. WG7's proposed afloat binding cannot blindly replace it. Future water-state-aware routing must be designed when WG7 is authorized; no input changes in WG1. No climate/underwater service is introduced. |

Minimum body contract: stable slot ID scoped by owning blueprint WorldId, liquid ID, immutable world-space polygon and bounds; plan version, seed and exact geometry hash. Renderers and definition Resources remain separate. Depth, flow and underwater destination are deferred to their owning phases.

Decisions: preserve the fixed pond; conservatively exclude its complete bank rectangle plus separation. Hard freshwater permissions apply to every touched biome cell, including enclosed forbidden islands. Soft suitability is the mean of intersecting cells' weights, used as candidate acceptance probability; it is not area coverage. Null biome Hydrology means allowed with weight 1. Null/disabled generation Water means no generated bodies. Lakes use 32 angular samples and bounded low-frequency radial harmonics, giving positive-radius simple organic outlines. Initial diameter 600–1200 world units (about 1.2–2.3 default 512-unit chunks); one requested lake by default when explicitly enabled. No terrain is carved.

Performance contract: generate once at new-world startup, restore frozen values on Continue, hold at most 32 immutable bodies (64 vertices max on restore). At most 128 attempts per body and 4096 coarse cells per candidate. No Nodes, timers, per-frame callbacks, GPU fields or per-chunk regeneration. Bounded AABB-first list queries are sufficient for 32 bodies; introduce an index only if a later approved scale needs it. F7 builds its coarse image once. Source edits do not mutate a live plan. No global cache or unload subscriptions exist; data dies with WorldScene. The unchanged fixed pond retains its existing processing; this work makes no idle-performance claim about that older system.

## WG1 authoring and test route

Open `TESTS/Water/WaterPlanningPractice.tscn`, press F6, then F7. Blue patches show three planned lakes at seed 42. Ground remains grass and the existing fixed pond remains the only playable water. This fixture clones settings, does not change authored Resources and creates no campaign automatically.

For an intentionally new authored world, create a `WaterGenerationMasterConfig` Resource in `DefaultWorldGeneration.tres → Optional water planning → Water`, enable it and set count/diameters/separation. Production defaults remain null/off. Configure an optional `BiomeHydrologyProfile` on each biome to change permissions/weights. Existing Oak/Pink use the safe null fallback; no redundant species resources were added. Changing authored `.tres` dependencies still triggers the existing strict campaign fingerprint policy; use F6/new campaign for experiments. There is no new fingerprint bypass.

WG1 snapshots store placed outcomes, not recipes or mutable Resources. Unavailable optional placements warn/skip after bounded attempts. Each stable `lake.NNN` slot gets its own explicit integer RNG stream. Resource order and added later slots leave earlier footprints unchanged; earlier conflicts or relevant permission/geometry changes may legitimately alter a new world's result. Hydrology does not change P1/P2 hashes. Existing worlds with no optional snapshot keep null/empty generated-water state.

## Automated validation

See `TESTS/Water/README.md` for commands. Completed checks on this runner:

- .NET SDK 10.0.402 targeting the unchanged `net8.0` / `Godot.NET.Sdk/4.7.2` project: `dotnet build` passed, **0 warnings / 0 errors**.
- Godot **4.5.1 Mono**, .NET 10 runtime with `DOTNET_ROLL_FORWARD=Major`: headless editor import completed. The first invocation lacked dotnet on PATH and printed editor SDK/MSBuild diagnostics; rerunning with the SDK directory on PATH exited 0 with no errors/warnings. The older editor temporarily rewrote the SDK declaration to its own version; this automatic edit and its backup were removed, and the original 4.7.2 project declaration was retained.
- `ValidateWaterPlanning.tscn`: **1415 checks passed**, exit 0. Includes actual isolated CampaignStore disk write/load, frozen water validation, corrupted-water rejection and legacy null-water disk round-trip. Twelve-seed repeat/order/JSON/extension section measured 632 ms total including repeated macro builds; this is test-suite elapsed time, not gameplay frame time.
- Existing `ValidateFeatureReservations.tscn`: **175 checks passed**, exit 0.
- Existing `ValidateMacroGeneration.tscn`: **377085 checks passed**, exit 0.
- `WaterPlanningPractice.tscn --quit-after 120`: headless production-world smoke exited 0; existing camera interpolation warning only. Headless does not inspect F7 appearance.
- `git diff --check`: passed. Existing production `.tres` and `.tscn` text is untouched, preserving the existing dependency fingerprint inputs.

Deferred: target Windows Godot 4.7.2 graphical F6/F7 check, profiler measurements, and full existing campaign process-restart regression. Focused disk round trips passed here; they are not a claim that the entire P4 suite or Windows renderer was tested. Target Windows Godot 4.7.2 Mono remains the authoritative renderer/performance check; Linux headless tests cannot prove graphical FPS.

## WG2 handoff

Use `WorldScene.WaterPlan`, `WaterBodyPlan.Query(bounds, destination)` and `PlannedWaterBody.CopyPolygon/SignedDistance`. Query appends to a caller-owned list; caller clears/reuses it. IDs belong to `Blueprint.WorldId`, never seed alone. Resolve the liquid ID against canonical definitions once during binding. Do not build a manager per biome or reroll shapes per chunk. The initial bounded plan uses a list, not a world-wide tree scan.

`WorldLiquidRegion._Ready` currently always creates its own fixed shape and fixed-resolution distance field. WG2 must add an explicit supplied-geometry setup path and handle world/local coordinates, field scale/resolution, cropping, shared materials, cross-chunk seams and lifecycle cleanup. Existing wading references one Region; multi-body selection must reuse shared distance queries and switch cleanly. Scenery and nav must consume the same authoritative polygon plus appropriate shore margins. Keep far-away physics/rendering unloaded. Do not merely duplicate the fixed pond scene at each chunk.

Optional water snapshots are metadata-only in WG1, so blueprint schema 4 is retained. Before WG2 adds actual placement/terrain consequences, introduce an explicitly incompatible/versioned generated-water contract or migration policy so older executables cannot silently ignore playable water. No old-world water insertion is authorized. Banks/carving remain WG4; river path/flow/depth remain their own phases.
