# AC4 — Actor collision regression validation

**23 October 2026.** Tested production revision `cbf7156` (includes AC1 `bbdca85`, AC2 `4ded2d0`, and the later weapon-motion change), plus the save-fixture correction described below. No production gameplay code changed during AC4.

## Results

Linux headless Godot **4.7.2.stable.mono.official.ed1daf0bf**, .NET SDK **8.0.415**, net8.0/runtime 8.0.21. Matching tools were located under `/tmp/c2-tools`; earlier AC1/AC2 notes that tools were absent from PATH describe those earlier attempts, not the current availability. The standard dotnet CLI encountered the already documented container process-information startup exception; direct MSBuild invocation succeeded.

| Check | Result |
| --- | --- |
| C# build/restore via direct MSBuild | Exit 0, no compiler warnings/errors |
| Godot editor import with toolchain environment | Exit 0, no ERROR/Exception lines |
| AC1 physical collision | Exit 0, **11 checks**, no ERROR lines |
| AC2 actor lifecycle / real movement into wall | Exit 0, **17 checks**, no ERROR lines |
| C1 health/stats/damage | Exit 0, **10037 checks**, no ERROR lines |
| C2 melee/contact/tree/AI | Exit 0, **46 checks**, no ERROR lines |
| C2.5 actions and newer tool motion | Exit 0, **44 checks**, no ERROR lines |
| SO2 World ownership | **60 assertions pass**, exit 0, then renderer/RID/resource shutdown errors; **not a clean pass** |
| P3 streaming | **3084 assertions pass**, then native C# binding shutdown abort, exit **-6**; **not a clean pass** |
| P4 save/write and separate-process read | Write exit 0, **44 checks**, no ERROR lines. Read **39 assertions pass**, then shutdown exit **-11** with resource/renderer errors; not a clean read pass. |

SO2/P3 shutdown signatures match the previously documented failures reproduced on unchanged earlier baselines in `COMBAT/C2_Validation.md`: `RenderingServer::get_singleton() is null`, dummy texture RID/resource leaks, and `csharp_lang && !csharp_lang->script_bindings.is_empty()` with unsafe resource references. A serial read-only retry also printed 39 passing assertions and exited -11 (without ERROR lines), so the read is still not clean. No fresh baseline reproduction was performed in AC4; these are consistent with the documented baseline signatures, not proven harmless by a PASS message. No production cleanup workaround, GC forcing, skipped assertion or renderer patch was added.

Early runs before asset import completed were invalidated due to missing imported textures. The first import lacked the SDK environment; the corrected final import and post-import fixture runs supersede it. An initial save write failed the historical-layout assertion below; its subsequent read is not final evidence. Final result rows refer to corrected sequential runs.

## Save fixture correction

The fixture unconditionally required the historical SO1→SO2 layout bridge even after AC1/AC2 intentionally changed scene-content text. The existing production gate correctly refused that fingerprint. The fixture now checks the same exact-scene eligibility: on later layouts it asserts refusal, then exercises **schema-1 HP restoration with the current compatible content fingerprint**, followed by a schema-2 write. On the exact historical layout the bridge test is retained. This separates health schema compatibility from content/layout compatibility without changing production migration rules or claiming old scene-content saves are now accepted.

The save fixture retains profile/world identity, changed-object, clock, HP/death, backup/recovery and separate-process restore checks, including the single authored skeleton under TestActorsRoot. Four F6 extras remain absent from campaign capture. All tests use `XDG_DATA_HOME=/tmp/ac4-tests` plus the fixture's dedicated `user://p4-validation` store. Real profile/campaign files were not used.

## Reproduction

From project root, with these installed tools (ordinary local Godot Mono/.NET installations can use their own executable paths):

```sh
export DOTNET_ROOT=/tmp/c2-tools/dotnet
export PATH="$DOTNET_ROOT:$PATH"
export MSBUILD_EXE_PATH="$DOTNET_ROOT/sdk/8.0.415/MSBuild.dll"
export GODOT_SILENCE_ROOT_WARNING=1
export XDG_DATA_HOME=/tmp/ac4-tests
```

Direct build command used:

```sh
/tmp/c2-tools/dotnet/dotnet /tmp/c2-tools/dotnet/sdk/8.0.415/MSBuild.dll isometric-fantasy-2d.csproj -restore -t:Build -p:RestoreConfigFile=/tmp/c2-tools/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false -m:1 -v:minimal -nologo
```

The local NuGet.Config points to the shipped GodotSharp/Tools/nupkgs source. Import command:

```sh
/tmp/c2-tools/godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64 --headless --path . --editor --import --quit
```

For each fixture below run that same Godot Mono executable with `--headless --max-fps 60 --path .`, followed by the listed scene. The FPS limiter stabilizes render-frame-based test pacing; it is **not** a performance measurement.

```text
res://TESTS/Combat/ValidateActorCollision.tscn
res://TESTS/Combat/ValidateActorLifecycle.tscn
res://TESTS/Combat/ValidateCombatFoundation.tscn
res://TESTS/Combat/ValidateMeleeExecution.tscn
res://TESTS/Combat/ValidatePlayerActions.tscn
res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
res://TESTS/Generation/ValidateChunkStreaming.tscn
res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Write and read must be separate processes, in that order. Exit 0 alone is insufficient: inspect PASS counts and ERROR/Exception output. Logs were captured as `/tmp/ac4-*.log` during this run; key results and limitations are preserved here.

## User acceptance and remaining scope

The user reported AC2 looks okay for now: skeletons group together without visible overlap and no major issues observed in **open-world terrain**. This is initial manual open-terrain acceptance, not a claim that every lifecycle/boundary test was manually exercised.

**AC3 corridor/bottleneck checks are deferred** because the user currently has no corridors. No crowd-avoidance work was justified by observed open-terrain behavior. Larger crowds, narrow passages and target-PC CPU/GPU/GC profiling remain unmeasured. Headless tests do not establish rendered smoothness or FPS.

AC4 has been executed with the limitations above. Targeted actor/combat checks pass; broader world shutdown faults remain open, so **the complete regression suite is not clean**. C3.2 remains a separately authorized implementation phase; this record does not implement knockback or silently close the outstanding engine/performance work.
