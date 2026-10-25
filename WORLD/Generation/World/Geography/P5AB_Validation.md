# P5a/P5b — geography before biome assignment

Implemented; target Windows Godot 4.7.2 visual/performance acceptance pending. P5c/P5d and WG4 remain separate planned work. Baseline inspected/tested: `763f732` (including the latest inventory/UI and dirt-variant changes). This job changes no authored production scene/.tres dependency text or content-fingerprint rules.

## Result and ownership

- `WorldGenerationPipeline` composes P2 required reservations → `WaterBodyPlanner.BuildGeography` → immutable `WorldGeographyPlan` → geography-constrained `MacroWorldBuilder`. Water generation reads no biome collection/map. River shapes, slot IDs, shared water fields, navigation and scenery exclusions stay canonical. The old biome-first water entry point remains for baseline checks.
- One temporary byte/cell records conservative lake/river overlaps at the existing macro resolution, including narrow edge crossings, partial cells and mixed footprints. Hard Hydrology flags restrict biome assignment; positive generation weights and soft factors `0.1+0.9*suitability` affect selection/growth. Null/default profiles remain permissive; zero suitability is not a hard ban. Impossible eligible sets report cell/type context without erasing water.
- Connected growth seeds compatible isolated components explicitly, subject to MaximumRegions. Every cell remains assigned to a nonempty four-connected region. If no hydrology restriction/preference applies, original region cells are retained; new hashes carry generator version 2.
- Blueprint schema 8 / geography contract 1 / macro generator 2 identifies the new pipeline. Water records keep version 1 (lake/empty) or 3 (smooth river). Existing schema 4–7 / macro version 1 hashes and frozen outcomes are preserved. Continue restores frozen maps/water, with no current profile re-evaluation and no insertion into old campaigns. Geography masks are not redundantly serialized; the query wrapper uses saved canonical water.
- All current macro cells still have an underlying land biome; this is not an ocean/non-land mask implementation. Authored fixed pond stays a compatibility landmark and placement exclusion, outside the generated-water biome mask. `RequireMultipleRiverBiomes` is legacy-test-only; geography-first rivers may cross one or several compatible biome types. Future major ocean/volcano/POI providers must extend the same plan deliberately.

## Checks performed

Runner: Linux Godot 4.5.1 Mono / .NET 8.0.21, .NET SDK 8.0.415 compiler. Direct Roslyn compilation with Godot source generators passed without warnings/errors; the authored project SDK remains 4.7.2. Normal `dotnet build` aborted before compilation with the runner's `System.Diagnostics.Process` metadata exception. Editor asset import exited 0 but printed SDK/MSBuild tooling errors; this is asset preparation, not a clean target-editor build.

| Check | Result |
| --- | --- |
| ValidateGeographyFirst | **141769 checks**, exit 0: six full seeded worlds/repeats/order changes, no-biome water inputs, immutable water under changed profiles, complete connected constrained coverage, separate/mixed lake/river rules, narrow cell masks, zero weights/preferences, one-biome fallback, bounded region failure, ownership/version/hash rejection, schema 4–8 disk round-trips |
| ValidateStreamedRiver --fixed-fps 60 | **2870 checks**, exit 0: real source/middle/terminal traversal, chunk field agreement, exclusions/wading/lifetime, schema-8 disk checkpoint and real new/legacy schema-7 Continue with new-world water disabled and both biome hydrology profiles now forbidden |
| ValidateStreamedWater --fixed-fps 60 | **223 checks**, exit 0 on isolated rerun: fields/seams/cancellation, nav/scenery/wading/rain, new checkpoint and real schema-4/5/8 Continue. Fixture now waits for save/input initialization before freeing a restored scene. Initial concurrent run hit a native `gchandle.is_released()`/finalizer crash and pending save-initialization exception; this runner instability is not claimed fixed in production. |
| ValidateMacroGeneration | **377085 checks**, exit 0; original v1 hash behavior retained |
| ValidateWaterPlanning | **1415 checks**, exit 0; legacy lake planner baseline retained |
| GeographyFirstPractice --fixed-fps 60 --quit-after 240 | Exit 0, no startup/runtime errors. Existing camera interpolation warning only. This is a headless smoke, not rendered acceptance. |

Original legacy `ValidateRiverPlanning`: **94885 checks**, exit 0. `ValidateFeatureReservations`: **175 checks**, exit 0. `git diff --check` passed. Local Markdown links are checked before commit.

**Broader baseline issue:** `ValidateChunkStreaming` exits 1 at `New Autumn chunk has missing/neutral seasonal atlas`. The same assertion reproduces on the unmodified `763f732` baseline compiled with the same runner and imported assets. No seasonal code fix is included. Therefore this is not a clean full-suite acceptance claim.

## Performance handoff

Wake: once when a new world is created; Continue only restores existing immutable data. Stop: pipeline returns; temporary mask/rule tables and growth queues are released. Idle: no new Nodes, process/physics callbacks, timers, subscriptions or shader writes. Persistent geography is bounded frozen water plus a small wrapper. Existing shared runtime still owns visible animation/rain and chunk prepare/publish/cancel/release.

Mask memory is one byte per validated macro cell (about 23 KB at default 176×132), plus four preference tables per enabled biome and the existing bounded growth queue. Rasterization visits body bounding cells with cached edge rejection; generation cost still scales with configured cells, water geometry and regions. Fallback sites count toward MaximumRegions. Planning/startup remain synchronous, and stream batching remains a soft budget. No measured FPS/latency improvement is claimed.

## Target-PC handoff

From repository root using the installed Godot .NET executable:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Generation/ValidateGeographyFirst.tscn
godot --headless --path . TESTS/Water/ValidateStreamedRiver.tscn --fixed-fps 60
godot --headless --path . TESTS/Water/ValidateStreamedWater.tscn --fixed-fps 60
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . TESTS/Water/ValidateRiverPlanning.tscn
godot --headless --path . TESTS/Generation/ValidateFeatureReservations.tscn
```

Expect PASS banners and exit 0; investigate engine errors separately from assertion counts. The broader streaming seasonal assertion above remains a tracked baseline failure.

Open `TESTS/Generation/GeographyFirstPractice.tscn`, F6, then R through five stations and F7 for the map. Only the first cloned biome permits water: confirm the other biome stays on dry cells while the river remains continuous. Inspect biome/shore transitions, field seams and scenery/nav at bends. Existing RiverPractice/WaterPlanningPractice still work; normal World defaults subsequently enabled three lakes and one river; see [the production-default follow-up](../../Liquids/WORLD_WATER_DEFAULTS.md).

Profile startup separately from standing still, walking across chunks, leaving water and returning. Check memory/allocations, row/nav binding hitches, dormant water behavior and bounded resident counts. Verify a new campaign through separate-process quit/Continue and retain an older campaign's exact world/map/water identity. User files must not be reset or migrated for testing. Graphical and target performance acceptance remain pending.
