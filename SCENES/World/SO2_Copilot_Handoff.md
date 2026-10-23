# SO2 ownership refactor — implementation/testing handoff

Status: automated validation PASSED by Codex after the user requested testing here instead of Copilot. See [SO2_Validation.md](SO2_Validation.md) for actual results and environment details. Manual graphical/performance acceptance remains open. The delegation instructions below are retained for future local retesting.

## Audited path/destination changes (recorded before editing the scene)

| SO1 path under World | SO2 path |
| --- | --- |
| Actors (WorldSceneryPlacement script + live actors) | WorldActorsRoot (plain Y-sorted container); controller → SystemsRoot/SceneryPlacement |
| Actors/Player; authored skeleton under Actors | WorldActorsRoot/PersistentEntitiesRoot/Player; skeleton in the same persistent entity root |
| Actors/Torch; Actors/PreviewTorch1/2 | WorldActorsRoot/PersistentPropsRoot/(same names) |
| Actors/Chunk_X_Y | WorldActorsRoot/ChunkActorsRoot/Chunk_X_Y |
| Preview props directly below Actors | WorldActorsRoot/PreviewSceneryRoot/(props) |
| TerrainRoot/StreamedGroundRoot/ChunkGround_X_Y | Unchanged; explicitly passed to streamer |

WorldScene caches typed roots/services once before child Ready callbacks. All actor container transforms stay identity and every intermediate actor container enables Y sorting. Individual chunk props remain immediate chunk children, preserving existing adapter ownership/cancellation. No extra per-chunk SceneryRoot/EntitiesRoot until there is a concrete need. No EffectsRoot, entity registry, GameSession, caves, duplicate streamer or new frame-processing controller is introduced.

Dependency audit: WorldScene composition; stream actor/ground destinations and navigation refresh; preview AddChild destination; entity spawn versus collision-source root; environment torch destination; initial wind/wetness/snow/shadow/atmosphere/artwork discovery; explicit snow player reference; fire debug ownership/player; campaign player references; C#/GDScript fixtures and benchmarks with immediate-child assumptions. Fire tree-child effects, chunk adapter roots, Z/layers, stable IDs and generation algorithms remain unchanged.

P4 compatibility must explicitly recognize the previous SO1 source scene fingerprint using its frozen reference text while hashing the CURRENT dependencies; no seed/blueprint/delta regeneration, reset or broad bypass. Subsequent checkpoints use the current fingerprint. Resource changes must still fail. Existing durable schema stays unchanged.

## Implemented changes — automated validation passed; local graphical retest checklist retained

WorldScene now exposes cached typed `Player`, `ActorsRoot`, `EntitiesRoot`, `PropsRoot`, `ChunkActorsRoot`, `PreviewSceneryRoot`, `StreamedGroundRoot`, `SceneryPlacement`, `EntityPlacement`, `Environment` and `Fire`. The legacy placer is a plain Node controller with an explicit output destination. Entity spawning has a separate persistent destination and navigation-source root. The existing streamer receives explicit actor/ground/navigation destinations; IDs, placement/density, chunk dimensions/rings/budgets and hidden binding/unload lifecycle are unchanged.

`WORLD/Runtime/WorldActorSources.cs` enumerates initial sources through **marked ownership containers only**, skipping chunk roots. Initial sun/wind/wetness/snow/artwork/atmosphere adapters use it; streamed sources still register per chunk/prop. It is not a frame-loop scene scan or registry. Snow receives the player explicitly. Smoke/embers convert the torch's global origin into renderer space. Tree fire effects remain tree children; lighting/weather/clock/save ownership remains singular.

`SAVE/Migrations/References/SO1WorldScene.txt` is frozen migration input, **not a playable alternate scene**. A legacy fingerprint is accepted only when (1) the live scene source matches the exact SO2 source hash in SaveCompatibility, and (2) substituting the original SO1 scene text while hashing CURRENT dependencies reproduces the saved fingerprint. A different resource/settings tree or arbitrary later scene edit still fails. No files are rewritten merely by checking Continue; the next successful checkpoint records the verified current fingerprint. The existing backup retains the prior checkpoint. No save schema, world ID, seed, feature, chunk or entity data is regenerated.

### Changed files to inspect

- `SCENES/World/World.tscn`, `WORLD/Runtime/WorldScene.cs`, `WORLD/Runtime/WorldActorSources.cs`.
- `WORLD/Generation/Placement/{WorldSceneryPlacement,WorldEntityPlacement}.cs`, `WORLD/Runtime/Streaming/WorldChunkStream.cs`.
- `ENVIRONMENT/EnvironmentRuntime.cs`, `Atmosphere/WorldAtmosphere.cs`, `Wind/Visuals/VegetationWind.cs`, `Lighting/Shadows/SunShadowRenderer.cs`, `Weather/Snow/WorldSnow.cs`.
- WORLD vegetation wetness/snow and rock snow adapters; `WORLD/Runtime/Visibility/SceneryArtworkIndex.cs`.
- `DEBUG/UI/Fire/WorldFireDebugPanel.cs`, `SAVE/WorldState/{CampaignSession,CampaignSaveController}.cs`, `SAVE/Migrations/SaveCompatibility.cs` and frozen migration reference.
- Updated C#/GDScript fixture/benchmark paths; new `TESTS/SceneOrganization/ValidateWorldOwnership.{cs,tscn}`; P4 write fixture now includes legacy-layout restoration assertions. No test assertion was removed to hide a failure.

## Copilot instructions and commands

Read CodePreferences.md, the main README, scene organization plan and this handoff. Pull the single commit titled **Separate SO2 actor ownership from world controllers** and record its SHA with `git log -1 --format=%H`. Initially **do not modify source**. Build/run checks and return exact commands, versions, exit codes, PASS banners and full relevant errors. Stop at compilation failure; do not silently change SDK/engine versions or claim deferred tests passed.

From the repository root, use the user's known installed .NET console executable (confirm it still exists):

```powershell
$godot = 'Q:\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
git status --short
git log -1 --format=%H
dotnet --version
& $godot --version
dotnet build .\isometric-fantasy-2d.csproj
& $godot --headless --editor --path . --import
& $godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateMacroGeneration.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateFeatureReservations.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateSnowSleeping.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateRainSleeping.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateSpatialScheduling.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateSunCaching.tscn
& $godot --headless --path . --script TESTS/Trees/validate_generic_trees.gd
& $godot --headless --path . --script TESTS/Fire/validate_production_fire.gd
& $godot --headless --path . --script TESTS/Lighting/validate_local_lights.gd
& $godot --headless --path . --script TESTS/Generation/measure_preview.gd
& $godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
& $godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Capture `$LASTEXITCODE` immediately after **each** command; stop/report unexpected failures rather than running blindly. Expected: build/import exit 0; `SO2 OWNERSHIP PASS`, each suite's PASS/zero-failures banner and exit 0. P4 write/read are distinct processes, in order. Existing Camera2D interpolation overrides and bounded grass-placement warnings may recur; report them rather than counting assertions alone. A shutdown crash/error still needs reporting even if assertions passed. Do not use `--quit-after` to force a passing exit.

The new SO2 fixture checks real streamed/preview hierarchy, persistent player/skeleton/torch destinations, empty generator, no old Actors alias, identity transforms and nested YSort flags, initial-source isolation, leaf/artwork caches, player sun/snow wiring, 25 initial actor/ground chunks and teardown. Existing streaming/visibility suites exercise removal/burn/unload/cancellation and adapter counts; P4 checks profile isolation/restart and the narrow layout bridge. New counts may differ from older handoff totals: report actual banners, never edit expected output to manufacture a pass.

### Optional real previous-revision save proof

If isolated `user://p4-validation` from the previously tested SO1 commit already exists, **run SO2 `--p4-read` before overwriting it with `--p4-write`**. That proves an actual old disk checkpoint can load through the bridge. Otherwise create an isolated detached worktree at baseline `a206a25`, build/import/run its P4 write phase there, then build/import/run SO2 read in the current checkout. Use the same engine and application user-data identity. Do not overwrite real `user://profiles.cfg`/`user://saves`, delete user files, stash/reset their work, or alter tracked definitions merely to test migration. New P4 write also exercises a constructed legacy-fingerprint checkpoint and ensures the next snapshot adopts the current fingerprint.

## Ben's required graphical/performance checks

1. F5 select an existing campaign from before SO2 → Continue. Check position/hour, burnt/removed trees and skeleton state. F8 → Esc save/main menu → Continue; restart; switch A→B→A. Old files must not be reset/reseeded. Starting a new campaign still works. An incompatible resource change must show an error and preserve files.
2. F6 disposable World: Remote tree shows the documented roots. Player/skeleton stay in PersistentEntitiesRoot, torches in PersistentPropsRoot; terrain ground stays separate. Walk through/behind trees, rocks, skeleton and torches, **including across a chunk seam**. Compare player/trunk/prop overlap at several Y positions and zooms; nested YSort flags alone do not prove visual equivalence.
3. F7 map, F9 environment/diagnostics and Fire buttons work. Rain/wet leaves, snow/player burial, daylight/contact shadows, torch shadows, pond/wading, axe hits and leaf/smoke origins remain correct. Burn and remove props, travel away/back, and repeat save/restore.
4. Disable streaming in a temporary runtime/test resource copy; the preview has one generated population, normal collisions/shadows/snow/wind, and no streamed actor/ground roots. Restore authored settings; do not commit local test tweaks. F5 campaign saves intentionally require streaming.
5. Capture a fixed-seed baseline versus SO2 at spawn and after chunk travel using F9/Godot profiler. Compare CPU/GPU frame pacing, startup, node counts and memory. Expected structural overhead is a few identity containers plus one separate controller; no new process/physics loop or per-frame discovery exists. Source discovery scales with persistent/preview sources at startup and direct chunk props at binding only. No FPS improvement or zero-regression guarantee has been measured here.

## Validation/rollback record

Codex subsequently ran the build, clean editor import, all 15 listed runtime checks, and a real SO1-write → SO2-read checkpoint proof. All passed; exact counts, warnings, resolved tool-environment issues and the strengthened P4 fixture are recorded in [SO2_Validation.md](SO2_Validation.md). Graphical tests and target-PC profiling remain outstanding. Large-save profiling and P3 rendering acceptance are not declared complete.

If SO2 fails, report the failure before fixes. To inspect rollback, use an isolated worktree at `a206a25`; do not destructively reset Ben's checkout. An SO2 checkpoint uses the current content fingerprint, so rolling code back to SO1 may intentionally reject that checkpoint; its prior `.bak` is retained. Back up a disposable campaign before any manual rollback experiment, and never manually edit checksum envelopes to bypass validation.

Return: commit SHA, clean/dirty state including generated `.uid` sidecars, Godot/.NET versions, command-by-command results, warnings/crashes, manual checks performed and measured performance. Mark SO2 accepted only after local automated and visual seam/save checks pass.
