# ES1 — Local steering implementation and validation

23 October 2026. ES1 code is implemented; visual crowd-spacing acceptance is pending. ES2 species presets, ES3 engagement slots and extensive ES4 stress/profiling are not implemented.

## Settings and runtime

`DefaultMovementConfig.tres` → **Separation → Enabled** is the global switch, loaded as a shared Resource by GroundChase. False skips querying/calculation and clears cache on the next active motor tick. Existing chase remains authoritative. Optional EntityDefinition.Separation profiles provide Enabled, Clearance (default10px beyond summed feet radii) and Strength (default0.35, maximum0.5). No authored entity scene/definition references or save fields changed; transient steering is unsaved.

One plain helper per existing motor; scratch arrays, shape and query are allocated only on first eligible moving sample. Active pursuit samples its own World2D at 0.2s intervals with instance phase offsets. Actor mask2, bodies only; self and chase target excluded. Filter dead/disabled/queued/incompatible bodies. Stable nearest-eight insertion with instance-ID ties; 33 returned candidates signal saturation and discard steering rather than accepting arbitrary truncation. Search shape radius64px. Result collections and query-exclusion arrays allocate at sample time; this is not allocation-free.

Influence uses actual cached feet geometry and scales. Close-body falloff, persistent collinear side choice, 0.1s smoothing, no backward force, bounded adjustment and final normal speed cap preserve route intent. Attack proximity fades influence; no separation movement of stationary actors. Existing attack/stop/reaction gates clear steering before returning. Existing final navigation/loaded-chunk guards and sole MoveAndSlide remain. Poor forward progress temporarily suppresses adjustment to retry the route. Death/disable/reconfigure/unload clear state; unload disposes native query objects. Passive neighbors influence movers without new callbacks. Master checking occurs only inside an already-active pursuit callback; disabled/passive entities gain no polling callback.

## Checks actually run

- Headless Godot4.7.2 editor import: exit0, no ERROR lines.
- MSBuild attempted twice: aborted before compilation in existing `Microsoft.Build.Shared.Debugging.DebugUtils` / ProcessName startup failure. Matching .NET8.0.415 Roslyn compiler with Godot4.7.2 source generator and project references compiled successfully (exit0). This does not establish a successful normal MSBuild invocation.
- `TESTS/Movement/ValidateSeparation.tscn`: **64 checks**, exit0. Disabled zero-query path; per-profile opt-out; motor master Off/On; query cadence/cap/saturation; target exclusion; passive/dead/disabled neighbors; coincident fallback; forward/speed bounds; unload; recoil priority/resume; death/suspension/sleep. Real 1/2/5/10 chasers traverse a narrow lane with a passive blocker, remain within walls, progress and issue melee contacts.
- Existing hit reactions **41**, melee execution **46**, world ownership **60**, chunk streaming **4425** checks: all exited0 in this run. No broad-suite clean-pass claim; P4 saves and C2.5 were not rerun for this slice.
- Fixed-seed213876 five-skeleton real World, stationary invulnerable/reaction-immune player, 600 physics steps, master Off then On in separate processes: both exited0. Same target distances at2/5/10s as ES0, same **834/3000** final5s close-pair samples below24px. Minimum centre spacing Off18.00077px / On18.00175px; no sampled penetration. **No meaningful improvement in final packing was measured.** This open approach does not exercise steering until entities reach attack positions, where steering intentionally yields. Lane fixtures verify local avoidance functionality, not a demonstrated open-world visual improvement. No performance speedup is claimed.

## Reproduce and acceptance

With matching Godot Mono/.NET installed, run `dotnet build -p:NuGetAudit=false`, then:

```
godot --headless --path . --max-fps 60 res://TESTS/Movement/ValidateSeparation.tscn
godot --headless --path . --max-fps 60 --script res://TESTS/Movement/SeparationWorldProbe.gd -- --separation-off
godot --headless --path . --max-fps 60 --script res://TESTS/Movement/SeparationWorldProbe.gd
```

Probe settings are in-memory only; no campaign opened/saved. Expect64 checks and process exit0 for the focused fixture. Probe logs distances/pair samples; its cached whole-engine monitor readings are not independent per-tick profiling or Windows FPS.

Windows F6: compare master Off/On while moving/stopping, in trees/corners and a narrow gap; watch jitter, side flips, stalls and attack starvation. Verify LMB/MMB recoil, death, disabled debug root, chunk boundaries and restart. Profile query counts, saturation, allocations and CPU at5/10/100 moving actors before adopting a larger crowd index. Final attack-position distribution belongs to optional ES3; do not increase repulsion until it starves melee merely to improve a packing metric. Headless local steering is verified; the plan's substantial five-skeleton visual improvement criterion remains unproven.
