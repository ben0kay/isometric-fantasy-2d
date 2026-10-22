# SO2 validation — 22 October 2026

**Automated validation passed.** Codex ran the tests after the user withdrew the Copilot-testing delegation. Tested production implementation: `64d44226b7894b8f1284f710a8a2b0cc2a116c0b`. The follow-up changes extend the migration checkpoint test, record these results and add engine-generated UID sidecars; no production-code fixes were necessary.

## Environment and build

Linux x86_64 container; **Godot 4.7.2.stable.mono.official.ed1daf0bf**, **.NET SDK 8.0.415**, runtime 8.0.21, MSBuild 17.11.48.46605. The authored `Godot.NET.Sdk/4.7.2` and `net8.0` project settings were preserved. Godot's bundled 4.7.2 NuGet packages supplied the offline restore; no project/package version downgrade occurred.

The normal SDK command initially hit a container process-information error. Direct SDK MSBuild invocation succeeded. The first editor imports exited 0 but logged SDK-discovery/MSBuild integration errors, so those runs were **not** counted as clean imports. Supplying PATH, DOTNET_ROOT, MSBUILD_EXE_PATH and MSBuildSDKsPath resolved the container configuration: the final editor import exited 0 with no errors. This was an environment adjustment, not a project code fix.

Final build command (from repository root; paths identify the temporary validation tool installation):

```bash
DOTNET_ROOT=/tmp/so2-tools/dotnet \
MSBuildSDKsPath=/tmp/so2-tools/dotnet/sdk/8.0.415/Sdks \
/tmp/so2-tools/dotnet/dotnet /tmp/so2-tools/dotnet/sdk/8.0.415/MSBuild.dll \
  isometric-fantasy-2d.csproj -restore -t:Build \
  -p:RestoreConfigFile=/tmp/so2-tools/NuGet.Config -p:NuGetAudit=false \
  -p:UseSharedCompilation=false -m:1 -v:normal -nologo
```

Result: **exit 0, build succeeded, 0 warnings, 0 errors**. The temporary NuGet configuration pointed only to the downloaded engine's `GodotSharp/Tools/nupkgs` directory. Final import used the same installed engine with `--headless --editor --path . --import`, PATH including the SDK directory, and `MSBUILD_EXE_PATH=/tmp/so2-tools/dotnet/sdk/8.0.415/MSBuild.dll`; exit 0, no errors. No engine executable/runtime/archive or generated build cache is committed.

## Runtime results

Each entry below ran in a separate engine process and exited **0**. Runtime logs contained no ERROR/Exception/crash messages. Tests did not run simultaneously against the same save directory.

| Test | Observed result |
| --- | --- |
| SceneOrganization/ValidateWorldOwnership.tscn | SO2 OWNERSHIP PASS: 60 checks |
| Generation/ValidateMacroGeneration.tscn | P1 PASS: 377085 checks |
| Generation/ValidateFeatureReservations.tscn | P2 PASS: 175 checks |
| Generation/ValidateChunkStreaming.tscn | P3 PASS: 4425 checks |
| Generation/ValidateSceneryVisibility.tscn | SCENERY STABILITY PASS: 167 checks |
| Generation/ValidateSnowSleeping.tscn | SNOW SLEEP PASS: 23 checks |
| Generation/ValidateRainSleeping.tscn | RAIN SLEEP PASS: 22 checks |
| Generation/ValidateSpatialScheduling.tscn | SPATIAL SCHEDULING PASS: 32 checks |
| Generation/ValidateSunCaching.tscn | SUN CACHE PASS: 20 checks |
| Trees/validate_generic_trees.gd | GENERIC TREE CHECKS: 0 failures |
| Fire/validate_production_fire.gd | PRODUCTION FIRE CHECKS PASSED |
| Lighting/validate_local_lights.gd | LOCAL LIGHT CHECKS: 0 failures |
| Generation/measure_preview.gd | P0 PREVIEW BASELINE PASS; both seeded signatures 1135839835, 352 scenery props |
| Save/ValidateCampaignSaves.tscn -- --p4-write | P4 PASS: 39 checks |
| Save/ValidateCampaignSaves.tscn -- --p4-read | P4 PASS: 33 checks |

Invocation prefix: `/tmp/so2-tools/godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64 --headless --path .`, with `DOTNET_ROOT` set. Scene paths are `res://TESTS/<table path>`; GDScript fixtures use `--script TESTS/<table path>`. These are the same cases listed in SO2_Copilot_Handoff.md. The existing P4 fixture gained two end-to-end assertions: actually commit the verified legacy-layout campaign, then read it from storage and check its current fingerprint, unchanged WorldId/map and retained removed prop. Its initial 37-check run passed; after the stronger assertions, write/read were repeated and passed with **39/33**.

## Real old-revision disk checkpoint proof

An isolated worktree at **`a206a2580cc14262e17f1171bd9b2e2b1869b981`** (SO1 layout, before SO2) was built with the same SDK/packages. Its original P4 write fixture ran under a dedicated data directory: **exit 0, P4 PASS: 31 checks**. A new SO2 process then read those actual disk checkpoints without running SO2 write over them: **exit 0, P4 PASS: 33 checks**. The read phase covered A→B→A, independent clocks/edits, player position, test-actor health/death, exact map/features and world/stream identities.

All automated data used `/tmp/so2-tools/user-data` or `/tmp/so2-tools/legacy-user-data` as XDG_DATA_HOME; P4 tests additionally use their existing `p4-validation` storage override and test-only profile IDs. No user's real local profiles/campaigns were accessed. This verifies the compatibility bridge against real earlier files, in addition to the constructed legacy-fingerprint fixture.

## Warnings and remaining acceptance

- Godot's existing Camera2D physics-interpolation override warning appeared in applicable fixtures.
- Compatibility-preview fixtures warned that some `grass.tuft` objects could not fit. Bounded placement and the assertions passed.
- No runtime shutdown error or crash occurred in these runs.

**Not verified:** rendered Y-sort overlap/seams, graphical menu/save/quit interactions, Windows rendering, actual GPU frame pacing, or target-PC large-save performance. Headless results are not proof of FPS or visual equivalence. Preview/scenery diagnostic timing values include startup/environment effects and are not promoted to a performance guarantee. Complete the manual checklist in SO2_Copilot_Handoff.md before full SO2 visual acceptance.

Generated `.uid` sidecars for `WorldActorSources.cs` and `ValidateWorldOwnership.cs` are included to preserve script identities. Build/import assets and validation save data remain untracked/ignored, outside the deliverable.
