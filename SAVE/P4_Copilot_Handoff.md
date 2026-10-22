# P4 local testing handoff — VS Code Copilot

**Status: implemented, awaiting local validation. No C# compilation, Godot execution, recovery tests or performance tests were run by Codex for this pass, at the user's request.** Source review and diff checks are not runtime evidence. Use the single commit titled `Implement P4 private campaign checkpoints and local test handoff`; record its SHA with `git log -1 --format=%H` after pulling. Do not test an unrelated later revision without recording that change.

## Instructions to Copilot

Read CodePreferences.md, SAVE/README.md, WORLD/Generation_Implementation.md and TESTS/Save/README.md. Initially do not modify code. Run the checks below and report commands, exit codes, pass/fail banners, relevant complete errors and engine/.NET versions. Stop at build failure and return the errors; do not download a different SDK, downgrade the project or silently skip a suite. Ask Ben before fixes. Do not claim graphical smoothness from headless output.

## Changed code to inspect

- `SAVE/WorldState/{CampaignSnapshot,CampaignStore,CampaignSession,CampaignSaveController}.cs`: detached records, storage/recovery, menu launch, worker/timer/exit lifecycle.
- `SAVE/Serialization/CheckpointFiles.cs`, `SAVE/Migrations/SaveCompatibility.cs`, `SAVE/SaveMasterConfig.cs`, `SAVE/Profiles/ProfileStore.cs`: safe files, compatibility, settings and existing registry recovery.
- `WORLD/Runtime/WorldScene.cs`, `WORLD/Runtime/Streaming/WorldChunkStream.cs`, `WORLD/Runtime/Persistence/{WorldBlueprintManifest,WorldBlueprintRestore}.cs`, `WORLD/Generation/World/Features/WorldReservationMap.cs`: exact baseline restoration and pre-materialization chunk deltas.
- `UI/Menus/Main/MainMenu.cs`, `WORLD/Generation/Placement/WorldEntityPlacement.cs`, `ENTITIES/Shared/Entity.cs`: menu integration and authored test-actor restoration.
- `TESTS/Save/ValidateCampaignSaves.{cs,tscn}` and updated owner/root/phase READMEs.

## Prerequisites and revision

Run in the fantasy repository root with the working tree checked for uncommitted user work. Pull the pushed commit using the user's normal Git workflow. Have the project's Godot .NET version (currently Godot.NET.Sdk 4.7.2) and .NET 8 available. `$godot` must point to the installed **.NET console executable**, not standard/non-Mono Godot or a different project's executable. If Godot is on PATH:

```powershell
git status --short
git log -1 --format=%H
dotnet --info
$godot = (Get-Command godot -ErrorAction Stop).Source
& $godot --version
```

If it is not on PATH, set `$godot` to the actual installed `.NET ..._console.exe` path. Locate it with Ben rather than inventing a path. Keep existing `user://profiles.cfg` and `user://saves` intact. The automated P4 fixture uses `user://p4-validation` with a dedicated in-process store override, and never writes the real profile registry/campaign directory.

## Build and import

```powershell
dotnet build .\isometric-fantasy-2d.csproj
& $godot --headless --editor --path . --import
```

Capture `$LASTEXITCODE` immediately after each native command. Expected: build succeeds with no new errors; imports resolve all scenes/resources/scripts. Preserve authored SDK settings. Then run the P4 fixture in **two separate processes**, in this order:

```powershell
& $godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
& $godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Expected: exit code 0 and `P4 PASS: ... checks` from **both** processes, with no unhandled C#/Godot errors. Do not use `--quit-after` to terminate tests early or treat a forced timeout as a pass. A hung fixture must be reported and stopped; the write phase intentionally traverses out/back for several hundred frames. Existing engine shutdown warnings/crashes must be recorded, even if assertions passed.

The fixture checks same-seed separate worlds, A/B clock and edits, removed/burnt trees after stream unload/reload, test skeleton death/health, exact saved macro/feature hashes, A→B→A after a real process restart, bounded chunks, safe paths, schema/content/layer/position rejection, duplicate edits, ignored temp writes, corrupt checkpoint backup recovery, saving after recovery, and corrupt active-pointer recovery. File-corruption cases operate only on newly generated test directories.

## Relevant regression fixtures

Run these once after the build/P4 checks, recording each outcome:

```powershell
& $godot --headless --path . res://TESTS/Generation/ValidateMacroGeneration.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateFeatureReservations.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
& $godot --headless --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn
```

Expected: each suite's PASS banner and exit 0. Inspect TESTS/Fire/README.md and TESTS/Trees/README.md for their current invocation if tree/fire assertions fail; do not blindly run every unrelated benchmark. The original macro and feature manifest creation APIs remain schema 2 for their fixtures; runtime streamed campaign manifests are schema 4.

## Ben's graphical checks — required before acceptance

1. F5 existing Boot: verify existing profiles still appear. Select/create a disposable test profile. New Campaign enters the streamed world; first checkpoint succeeds. Existing main menu button styling remains intact.
2. Walk away from spawn; set a distinct paused hour in F9; ignite/burn a tree. F8 reports saved. Esc → Save & Main Menu → Continue restores location, hour and burn progress. Close/reopen the application and repeat Continue.
3. Change profile and create another campaign. Its world/clock/edit history must be independent. Return to the first profile and verify again. Automated fixtures provide the **same-seed** portion of this check.
4. Click New Campaign on an existing profile. Cancel preserves the current campaign. Confirm makes a new world only after its first successful checkpoint; prior run files remain. There is no archive selection UI yet.
5. Test Esc Resume, Save & Main Menu, Save & Quit, window X, F8 twice during a save and closing during a save. Normal exit waits for a fresh final checkpoint. On failure, the game must stay open with a readable error and allow retry. Do not corrupt real profile saves for testing.
6. Set SAVE/DefaultSaveConfig.tres AutosaveSeconds temporarily to a short interval, check an autosave and repeat after Esc pause/resume. Set 0 and confirm no autosave timer activity. Restore the config and do not commit test-only tweaks. Save policy is intentionally outside the content fingerprint.
7. F6 directly running World must remain disposable with no campaign save UI or campaign writes; compatibility preview still works. Controls, pond, tree visuals, lighting, rain/snow, Y sorting and local navigation should retain existing behaviour.
8. Profile idle, autosave and travelling saves using F9 traces/Godot profiler. Save controller should have no `_Process`/`_PhysicsProcess`; capture runs only on requests/timer. Measure main-thread snapshot time, frame spikes, allocation and memory with many edited chunks. Record hardware/renderer and data size; no FPS improvement has been claimed. Existing P3 Windows visual acceptance remains separate and open.

## Scope and deliberate limitations

Player location/surface layer and clock round-trip; finished inventory/needs/equipment/caves/A-Life systems do not yet exist and were not invented for a save test. Weather/debug sliders reset. Burning still pauses offscreen. No inactive-profile catch-up, permadeath deletion, slot/archive UI, content migration or automatic blob/archive garbage collection. Source-project resource fingerprinting is conservative; scene/resource changes may block Continue until an explicit migration is designed. RandomSeedOnLoad/FixedSeed changes alone must not reroll or reject an existing campaign. Exported-build compatibility has not been established.

## Report back to Codex

- Commit SHA, Godot version, .NET version, Windows renderer/hardware.
- Exact command + exit code + PASS/error output for build/import, P4 write/read and each regression suite.
- Manual checks completed, failures and reproduction steps.
- Performance observations with measured snapshot/frame timings and edited-data size.
- Any changes Copilot made, if separately authorized; never label this pass accepted solely because it was pushed.
