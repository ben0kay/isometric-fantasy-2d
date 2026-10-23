# AC2 — Lifecycle and World practice handoff

> Follow-up: [AC4 validation](AC4_Validation.md) supersedes the toolchain/testing availability below. AC1/AC2 targeted headless checks passed; broad world shutdown and graphical/performance limits remain.

**Implemented from `bbdca8518e967e2c5b235fe63a8c6c9b16a7ae96`, 23 October 2026. Awaiting local validation.** AC1 runtime acceptance is also still open. This change does not implement C3.2 or claim collision/crowd/FPS acceptance.

## Behavior and ownership

- Player death disables the feet collider, resets actions and stops movement. Explicit health restoration reenables blocking. Entity death retains existing collider shutdown and death fade.
- Both actor scenes explicitly use `disable_mode=0` (Remove). Disabling an inherited owner removes its bodies from physics; restoring the owner restores living collision shapes. Health-disabled shapes remain disabled.
- GroundChase suspension cancels its attack, zeros velocity and clears its route/repath state. Its existing single mover checks navigation bounds and loaded destination chunks. Player walk/sprint/dodge retain the existing streaming guard and MoveAndSlide; hop remains visual only.
- `WorldActorsRoot/PersistentEntitiesRoot/DebugActorsRoot` owns the original test skeleton and four additional F6 practice skeletons. The player stays outside it. Y-sort/ownership metadata allows existing startup source registration to traverse it; markers are containers with no render/physics registration.
- Select DebugActorsRoot and uncheck **Enabled** before F6 to omit all skeletons. Remote Inspector toggles hide/suspend existing skeletons and remove blocking. Reenable resumes surviving actors; if startup was disabled, reopen World after enabling to populate. Do not use visibility alone. Four Marker2D positions are offsets from player start; bounded placement searches nearby walkable positions at least 48 units apart and 64 from the player, warning on failure.
- The canonical Entity scene/definition is reused. Placement retains one shared navigation grid and tracks at most five test entities for the existing streamer refresh. Four extras run only in disposable F6 sessions. Campaign New/Continue retains the original single test-entity save contract; extras are not captured or recreated in campaigns. Disabling the root also disables original test placement.
- Scene text changes alter the campaign fingerprint. Old campaigns are preserved and may refuse Continue; no migration, version/schema bypass, or destructive save operation was added.

## Validation actually performed

| Check | Result |
| --- | --- |
| `git diff --check` | Exit 0 |
| Changed scene/resource reference existence and four-marker inventory | Passed, exit 0 |
| Whole-repository reference scan | Found existing missing references in DEBUG/VisualTests/NaturalTerrainTest.tscn and CAMERA/DefaultCameraConfig.tres; these files were untouched. Scoped scan above passed. |
| `dotnet build isometric-fantasy-2d.csproj` | Exit 127: dotnet not installed/on PATH |
| `godot --headless --path . --editor --import --quit` | Exit 127: godot not installed/on PATH |
| Combat/physics/streaming/save fixtures and manual profiling | Not run: engine/toolchain unavailable |

No build or runtime success is implied by source checks. The prior C2 documented native-shutdown/cancellation baseline faults must be distinguished from any new failures after running the engine.

## Local validation commands

Use Godot 4.7.2 Mono and .NET 8 from the project root. Replace `godot` with the installed Mono executable.

```sh
dotnet build isometric-fantasy-2d.csproj
godot --headless --path . --editor --import --quit
godot --headless --path . res://TESTS/Combat/ValidateActorCollision.tscn
godot --headless --path . res://TESTS/Combat/ValidateActorLifecycle.tscn
godot --headless --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
godot --headless --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --path . res://TESTS/Combat/ValidatePlayerActions.tscn
godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Save fixture uses its existing test storage; follow SAVE/P4_Copilot_Handoff.md prerequisites. Run write/read as separate processes and retain exact commands, exit codes and logs. The added lifecycle fixture expects `AC2 LIFECYCLE PASS: 17 checks`; AC1 expects 11 checks. Other totals may differ as ownership assertions evolve. Updated existing save/streaming/ownership tests now address TestActorsRoot; F6 checks expect five actors, campaigns keep one/zero according to saved state.

## Manual acceptance still required

F6 World: confirm five nearby skeletons, unchanged LMB/MMB/tree contact, feet sliding/Y-sort, walking/sprinting/dodging into enemies/trees/rocks/torches, visual hop retaining collision, player/enemy death and no invisible blocking. Toggle DebugActorsRoot.Enabled during active pursuit/wind-up and reenable: no delayed contact or stale motion. Start once with Enabled=false: no test skeletons. Check fixed seeds and obstructed marker positions; failure to place four must be investigated, not counted as success.

Travel far enough for enemies/chunks to suspend/unload, return and verify no collider ghosts, duplicated actors or stale attacks. Check loaded-area/world boundaries and large physics steps. The added endpoint guards are not evidence that footprint/boundary/large-step tests passed. Check normal and disabled-root New Campaign/Continue using disposable profiles, original skeleton HP/death persistence, and preserved refusal for incompatible old fingerprints.

Profile idle, five active skeletons, root disabled, reactivation and world exit. DebugActorRoot has no frame callbacks. Shared placement search runs only at startup; existing streamed navigation refresh handles a bounded five-element list. Enemy movement adds a rectangle check and one loaded-chunk lookup during movement, with no added overlap scans or timers. Five enabled chasers still cost more than one; no measured performance claim. AC3 crowd assessment and AC4 final regression acceptance remain open.
