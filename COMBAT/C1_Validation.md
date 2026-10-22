# C0/C1 validation — 2026-10-22

Scope: C0 audit plus C1 shared health, four cached effective stats, damage results, existing actor/tool adapters and player checkpoint HP. Baseline `3f4dabe21cf3659409464941d7ba2a1d16cd1461`. One implementation commit for this job. C2–C5, SO3 and enemy attacks are outside scope.

## Results actually run by Codex

Linux headless Godot 4.7.2 Mono (`ed1daf0bf`), .NET SDK 8.0.415 / runtime 8.0.21. All final runs exit 0:

| Check | Result |
| --- | --- |
| .NET restore/build, editor import | Successful; no compiler warnings/errors, no import errors |
| C1 contracts + production actor scene fixture | 10037 assertions (includes 10000 unchanged cache reads) |
| P4 write / separate-process read | 43 / 39 checks |
| Actual pre-SO2 schema-1 disk checkpoint read | 39 checks; old identity/geography/edits retained and player defaults full |
| SO2 world ownership | 60 checks |
| P1 macro generation | 377085 checks |
| P2 reservations | 175 checks |
| P3 chunk streaming | 4398 checks; bounded streaming, count can vary with frame timing |
| Scenery stability | 167 checks |
| Generic trees | 0 failures |
| Production fire | Lifecycle/anchors/collider/wind/snow/shadows/reset/UI/cap passed |

The C1 fixture tests damage rounding/scaling/defense/overkill/zero/invalid inputs, one death transition, healing/explicit restoration, cache reuse/no-op updates, shared-profile invalidation, modifier isolation/removal/order/extreme products/clamps, real Entity legacy hits and feedback sleeping, blocked-hit inactivity, player movement scaling and death/restore shutdown. P4 writes damaged player A (87 HP) and dead player B (0), then restores both in new processes. Its existing legacy migration path additionally tests schema-1 missing HP and schema-2 capture, alongside corrupt/recovery/storage rejection cases.

Expected existing warnings: Camera2D physics interpolation override; bounded grass placement on preview/scenery tests. Final combat/save runs have no ERROR lines. During fixture development an extra duplicate world load produced one dummy-renderer texture exit leak; forcing managed GC to investigate caused a Godot finalizer crash. That experimental GC call was removed. Schema-1 assertions now share the existing legacy migration scene instead of spawning a redundant world; the final build and both P4 runs pass without either error. No production GC forcing or workaround was added.

## Reproduce locally

Use Godot **Mono** 4.7.2 and .NET 8 SDK. Build `dotnet build ./isometric-fantasy-2d.csproj`, then `Godot --headless --editor --path . --import`. Substitute the installed console executable for `Godot`.

```text
Godot --headless --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
Godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
Godot --headless --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
Godot --headless --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
Godot --headless --path . res://TESTS/Generation/ValidateMacroGeneration.tscn
Godot --headless --path . res://TESTS/Generation/ValidateFeatureReservations.tscn
Godot --headless --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
Godot --headless --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn
Godot --headless --path . --script TESTS/Trees/validate_generic_trees.gd
Godot --headless --path . --script TESTS/Fire/validate_production_fire.gd
```

P4 fixture explicitly sets `CampaignSession.TestStorageRoot` to `user://p4-validation`; it never intentionally targets real saves. Codex additionally isolated XDG_DATA_HOME for all tests. To prove original checkpoints independently, use a separate baseline checkout to write P4 fixtures into an isolated user-data directory, then use this checkout's read phase with the same directory, without rerunning write first. Real schema-1 files used here were generated previously by the pre-SO2 `a206a25` checkout.

## Performance and manual handoff

No new combat Node, process callback, physics query, group scan or timer. HP changes only on explicit calls. Stats rebuild lazily after modifier edits/profile signals; unchanged reads return the cached struct. Four sorted modifier traversals happen only on rebuild, scaling with modifiers on that actor. Resource listeners detach on scene exit. Existing chase/player physics and equipped-tool aim continue their original work; attack speed snapshots once at swing start. Death stops player movement/input/tool; Entity hit flash wakes on real damage and sleeps when settled, with its existing bounded death fade/free.

Automated checks establish callback/cache state, not graphical FPS, allocation measurements or combat feel. On the target PC, play F6 and a campaign: verify unchanged default walking/axe/tree feedback, skeleton damage/flash/death, save/quit/Continue/profile switching and chunk unload/reload. Profile idle actors and active hits under representative load; confirm no added idle combat callbacks and feedback settles. C1 has no enemy attacks, player health HUD or death/retry screen; player receiving/death is exercised by the fixture until C2 connects attacks. Never treat the foundation as a polished encounter.
