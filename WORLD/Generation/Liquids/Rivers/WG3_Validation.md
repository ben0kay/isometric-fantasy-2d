# WG3 implementation and validation — 24 October 2026

**Implemented; target Windows graphical/performance acceptance pending.** River planning lives in `Generation/Liquids/Rivers`, and the shared liquid renderer, interactions and streaming remain under `WORLD/Liquids`. WG4–WG7 are not implemented by this change.

## Changes and contracts

- Added bounded deterministic cost-grid/A* routing, separate river slot seeds, full-footprint biome/reservation/shore-clearance validation, organic lateral bends and width variation. Existing lake geometry and independent P1/P2 RNG are preserved. Unsupported/impossible optional river requests produce warnings after bounded attempts; they do not fade at prohibited biomes or publish disconnected pieces.
- Each river is one simple polygon (≤64 existing shared vertices) plus an immutable ≤24-point source-to-terminal path with copied positive widths. Spring/terminal pools are part of that same polygon. This is a branch-free river between explicit interior pools, not elevation-aware drainage, a connection to an existing lake, flow physics or carved terrain.
- Reused WG2 scalar fields, definition, player immersion, rain, scenery exclusions and nav polygon integration. No river-only runtime/renderer or per-tile/body simulation was added. Shared body queries cache edge bounds so long winding shapes do not tile their empty bounding-box interior; visibility also checks actual corridor/loaded sections.
- F7 explicitly draws the saved centerline because narrow corridors can fall between coarse map samples. This developer overlay does not drive generation or gameplay. `RiverPractice.tscn` starts at a middle bank and R cycles five saved source/river/terminal sections.

## Version/save policy

Lake-only WaterBodyPlan uses **version 1**, preserving its old geometry/hash layout. Plans with rivers use **version 2**; hashes include exact polygon, centerline, widths and supported endpoint kinds. Arrays are cloned for runtime and serialized snapshots. Unknown/corrupt metadata fails validation.

New river worlds use **blueprint schema 6**. The previous executable rejects schema 6 rather than ignoring new metadata during Continue. Lake-only new worlds stay schema 5, and no-water/legacy worlds retain schema 4. Version 2 data is rejected under schema 4/5. Existing schema-4/5 saves are restored without adding rivers or rerunning any planner. New-world settings remain opt-in and do not migrate old campaigns. Authored scene/.tres dependency text and the strict content-fingerprint policy are unchanged.

## Performance handoff

Wake: one new-world macro planning pass; existing chunk publication wakes resident presentation. Stop: planner returns frozen values; chunk unload releases sections and the last resident body. Idle: no new river callbacks or simulation; existing one shared player lookup and bounded visible-water animation/rain handling retain WG2 ownership. Dry/static/offscreen/cancel/unload behavior uses the same shared paths. All full-route metadata is bounded and detached; no all-world scenery scan is required.

Default route grid is 512 world units, independent of default 512-unit streaming chunks and 256-unit macro biome cells. Maximum 65536 route cells; 4 river slots, 32 attempts per slot, 65536 expansions per search and 24 path controls. Exact footprint checks are capped at 4096 macro cells. Grid/row/texture work and initial chunk loading remain synchronous or softly budgeted, not a hard frame-time cap. Different hardware, content density or maximum authoring limits need profiling.

## Automated validation

Completed on this runner:

- `dotnet build --no-restore`, .NET SDK **8.0.415**, unchanged `Godot.NET.Sdk/4.7.2` / `net8.0`: **0 warnings, 0 errors**, exit 0. A plain CLI retry hit the runner’s intermittent System.Diagnostics.Process metadata exception; the final PTY build succeeded. An independent Roslyn/Godot source-generator compatibility compilation also passed with no warnings/errors.
- Linux Godot **4.5.1 Mono**, .NET **8.0.21**: `ValidateRiverPlanning.tscn` **7346 checks passed**, exit 0. Twelve seeds test repeatability, cross-biome routing, all centerline sections inside positive-width corridors, reservations, authored pond separation, lake preservation, Resource reordering, later-slot independence, JSON/hash corruption, incompatible versions, river-vs-lake permissions, zero-suitability endpoints, forbidden barriers, sparse chunk queries and one-biome fallback. The seed/repeat/order section took 2538 ms in one recorded run including repeated macro/planner setup; this is fixture time, not a frame-time/FPS claim.
- `ValidateStreamedRiver.tscn --fixed-fps 60`: **78 checks passed**, exit 0. Real production source/middle/terminal travel and return, water fields, single-body selection, wading, scenery exclusion, bounded ownership, disk checkpoint identity, real Continue with new-world water disabled, and rejection under older blueprint headers. Continue runs in a new scene in the same process after disk round-trip; separate-process restart remains a target check.
- Existing `ValidateStreamedWater.tscn`: **214 checks passed**, exit 0; existing `ValidateWaterPlanning.tscn`: **1415 checks**, exit 0. Covers original pond, legacy schema-4/5 behavior, water seam scalar samples, cancellation/release, rain sleep/wake and disk identity.
- Existing `ValidateFeatureReservations.tscn`: **175 checks**, exit 0; `ValidateMacroGeneration.tscn`: **377085 checks**, exit 0.
- `RiverPractice.tscn --fixed-fps 60 --quit-after 600`: headless smoke passed, exit 0, with no runtime errors or missing-river warning. Existing camera interpolation warning remains.
- Source `git diff --check` passed. No production scene or `.tres` dependency text was edited. Editor import exited 0 after preparing texture metadata but printed runner SDK/MSBuild tooling diagnostics; it is not claimed as a clean Windows editor check. An initial pre-import smoke reported the absent generated Dirt PNG import metadata; the post-import smoke and fixtures above passed. Unrelated editor-created metadata is excluded from the commit.

Windows Godot 4.7.2 Mono remains the target; Linux headless checks cannot establish rendered seam quality or frame rates. Target graphics/profiler and separate-process restart checks remain open.

## Target-PC checks

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Water/ValidateRiverPlanning.tscn
godot --headless --fixed-fps 60 --path . TESTS/Water/ValidateStreamedRiver.tscn
godot --headless --fixed-fps 60 --path . TESTS/Water/ValidateStreamedWater.tscn
godot --headless --path . TESTS/Water/ValidateWaterPlanning.tscn
```

Use your installed Godot Mono executable path. Expect WG3 PLAN / WG3 STREAM / WG2 / WG1 PASS logs and exit 0; investigate native errors even if process exit is 0.

1. Open `TESTS/Water/RiverPractice.tscn` → F6. Allow nearby streaming after the disposable teleport; check variable width, bends, shoreline/wading and no straight chunk seam or duplicate surfaces.
2. R cycles source, middle sections and terminal pool. Verify no zero-width gaps or abrupt biome-boundary disappearance. F7 shows the cyan river route; source is white and terminal is blue. F9 rain on/off should produce water splashes and settle when off.
3. Travel away/return, check field/node/memory counts settle; compare startup and chunk-crossing spikes with WG2. No FPS claim follows from the automated fixtures.
4. For opted-in new campaigns, save/restart/Continue and verify the same river. Existing schema-4/5 campaigns must keep their old geography. Do not insert rivers into valued old saves.

Deferred unless recorded below: target graphical render/shore/zoom inspection, profiler CPU/GPU/allocations/hitches/memory, separate-process restart and broader unrelated suites. No elevation, tributaries, directional currents or underwater behavior is implied.
