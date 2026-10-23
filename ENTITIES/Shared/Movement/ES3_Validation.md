# ES3 — Engagement positioning and F9 combat tuning

Implemented 23 October 2026. Direct/Spread and the unified F9 drawer are implemented; Windows visual/CPU/GC acceptance is pending. ES2 species presets, Flank/facing policies and the wider ES4 stress pass remain future work.

## What changed

- `AI/EngagementProfile.cs`: optional EntityDefinition override, otherwise MovementMasterConfig.EngagementDefaults. Mode Direct/Spread; preferred distance 32px (clamped by attack reach); extra spacing 10px beyond summed body radii; persistence 1.2s; target movement 16px; reconsider interval 0.5s; blocked timeout 1s; maximum extra route 70px. Optional persistence, movement-threshold, blocked-fallback and detour-limit switches are independent. Numeric values are bounded and nonfinite values use safe defaults.
- `AI/EngagementPositioning.cs`: one plain helper per existing motor. Up to 8 angular candidates; exact walkable-cell and loaded-area checks; bounded route-length checks and direct fallback. Straight-distance lower bounds skip path queries that cannot improve the current candidate. Actual selection also respects the existing GroundChase repath cadence, so the reconsider interval is a minimum, not an exact timer.
- A target-scoped transient ledger holds up to 64 claims (IDs, points, radii only). It reserves approach points without an always-running manager, node references, population scans or a new motor. Each candidate checks at most 64 claims. Full ledger/no suitable candidate means direct pursuit. Empty target entries are removed. Stable claims are retained during attacks/stationary engagement, but cleared by reaction, death, disable, invalid target, out-of-detection, target/navigation reconfiguration, master/Direct switch and unload. Sustained poor movement releases a claim and briefly tries direct pursuit before choosing again.
- GroundChase remains the only enemy MoveAndSlide owner. Eligible attacks are attempted before positional selection; a preferred point never delays an available attack. Final navigation and streaming guards remain authoritative. Separation and engagement have independent masters. No actor layers, collision shapes, authored entity scenes/resources or save fields changed.
- CombatMasterConfig now exposes enemy melee, knockback, stagger and combat-impact switches, all default On. Push/stagger Off independently suppress the existing axis on the next active motor tick and prevent new reactions on that axis. Impact Off blocks new bursts; active particles finish and sleep. Enemy melee Off cancels enemy execution; movement continues.

## F9 organization

Fixed top-left Environment/Combat category buttons share an exclusive ButtonGroup and a scrollable body. Clicking the selected button collapses it; selecting another switches drawers. `DebugCategoryMenu.AddCategory` supports future entries. F9 hides/shows the complete HUD, preserving the selected category and live trial values. Environment includes the former standalone tree-fire buttons; right-side environment diagnostics appear only while that category is open. F7 remains map-only. The old EnvironmentDebugPanel/FireDebugPanel scene paths are retained for compatibility; no .tscn layout/dependency text was changed.

Combat includes independent masters, Direct/Spread, ES3 sliders and optional-policy switches, Reset and player/practice counters. It duplicates shared engagement defaults in memory; species-specific overrides retain their authored values. Original Resource values/references are restored on Reset/unload (Reset preserves the duplicate object used by widget delegates). No asset/campaign saving API is called. Hiding F9 preserves the trial; it does not reset it. These switches are shared within the running process.

No added gameplay process callback exists: existing pursuing motors activate selection, attacks/recoil suspend it, and disabled/passive actors remain dormant. Hidden/inactive drawers and diagnostics disable refresh callbacks; visible widgets refresh at 4 Hz. Resizing is event-driven. Fire scans registered world receivers only on button presses. Combat counters enumerate only the small practice root while visible, not a global population per agent. Explicit C# tracing continues independently while hidden.

## Evidence actually obtained

Godot 4.7.2 Mono / .NET 8.0.415, Linux headless with a 60 FPS limiter:

| Check | Result |
| --- | --- |
| Compilation | Matching Roslyn compiler + Godot source generator/project references: exit 0 |
| Normal build | dotnet CLI/MSBuild failed before compilation in process-information/DebugUtils startup; no normal-build success claimed |
| Editor import | Initial attempt had GodotTools/MSBuild environment exception; rerun with MSBUILD_EXE_PATH set exited0 without ERROR lines |
| ValidateEngagement |42 checks, exit 0: distinct five-agent claims, persistence, moving target, blocked/direct fallback, full lifecycle cleanup, independent masters, actual attack priority, push/stagger and particle switches |
| ValidateDebugHud |16 checks, exit 0: exclusive Environment/Combat/future categories, fire hosting, resizing, F9 state, hidden refresh sleep, slider/toggle binding, original-default protection and unload restoration |
| Existing separation |64 checks, exit 0; real1/2/5/10 chasers in a narrow lane |
| Existing combat | Hit reactions41, melee46, impact feedback29 checks; exit 0 |
| World regression | Ownership60, streaming4387 checks; exit 0 |
| Fire / tracing | Production fire lifecycle fixture and runtime trace fixture pass, exit 0; hidden diagnostics do not stop trace capture |

These are focused results, not a complete-suite pass. P4 save fixtures and the known C2.5 thrust assertion were not rerun. No save-schema migration/bypass was introduced.

### Fixed-seed World comparison

Updated `TESTS/Movement/SeparationWorldProbe.gd`: seed 213876, normal five skeletons, 600 physics steps. Player survives through in-memory MaxHealth 10000/ReactionImmune overrides. Both runs keep ES1 separation enabled; `-- --engagement-off` selects the ES3 baseline. Both exited0.

| Metric | ES3 Off | ES3 Spread |
| --- | --- | --- |
| Final target distances(px) |25.2,40.7,25.3,25.1,25.8 |32.7,32.1,32.0,32.3,25.7 |
| Close pairs below 24px in final 5s |834/3000(27.8%) |300/3000(10.0%) |
| Minimum sampled centre distance |18.00162px |18.00216px |
| Confirmed melee contacts per skeleton |6,5,5,5,5 |6,6,5,5,5 |

This demonstrates less tight packing on this seed while all five still make contact; it does not prove smooth animation, full tactical surrounding or corridor immunity. No sampled feet penetration occurred. Whole-engine cached physics monitor means ranged roughly 2.8–3.9ms across these runs, with higher readings in the enabled run; these are not independent per-tick samples, isolated ES3 CPU cost or a Windows FPS benchmark. No CPU/GC speedup is claimed. Candidate paths/arrays allocate only at bounded selection times; larger crowds need profiling before raising caps.

## Reproduce / local acceptance

From repo root, with matching Godot Mono/.NET installed:

```
dotnet build -p:NuGetAudit=false
godot --headless --path . --editor --import --quit
godot --headless --path . --max-fps 60 res://TESTS/Movement/ValidateEngagement.tscn
godot --headless --path . --max-fps 60 res://TESTS/DebugUI/ValidateDebugHud.tscn
godot --headless --path . --max-fps 60 --script res://TESTS/Movement/SeparationWorldProbe.gd -- --engagement-off
godot --headless --path . --max-fps 60 --script res://TESTS/Movement/SeparationWorldProbe.gd
```

F6 Windows: toggle Environment/Combat, collapse both, F9 hide/show; verify fire buttons, scrolling/resizing and unfocused movement keys. In Combat compare Direct/Spread and separation independently, alter sliders, toggle each optional policy, Reset, leave/restart. Check stationary/moving player, trees/corners/narrow gaps, LMB/MMB recoil, shield, death and loaded-chunk boundaries. Profile 5/10/100 active actors for selection paths, claim occupancy, allocation/CPU spikes and sustained blocked fallback. Authored species overrides require testing separately; tactical flank/facing behavior remains unimplemented.
