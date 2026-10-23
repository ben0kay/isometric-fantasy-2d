# C3.3c+d — Material impact profiles and confirmed-hit wiring

Implemented against `6bf6559` on 23 October 2026. Bone/Flesh profiles and confirmed-hit presentation are implemented; **Windows visual/performance acceptance remains pending**. Stone/metal/wood splinters, trails, C3.4 guard feedback and C3.5 camera/audio remain separate work.

## What changed

- Shared `HitFeedbackProfile` references one reusable ParticleProfile plus colour, count and visual offset. Skeleton Basic references shared Bone; DefaultPlayerConfig references shared Flesh. Future species choose profiles through EntityDefinition, with no species-specific controller or particle script. Null/disabled/zero-count profiles skip new bursts.
- AttackContact now retains the source actor, selected closest hurt point and locked aim, alongside existing Target/Damage fields. MeleeHitQuery stores geometry before damage/death/recoil and notifies ICombatImpactTarget once, after actual HP removal. No duplicate query or damage calculation.
- Positive applied damage emits a material burst, including chip damage and killing blows. Zero damage, armour absorption, full zero-damage guard, miss/ineligible/dead targets emit none. Knockback immunity alone does not prevent visual damage feedback. Generic direct damage, healing and save restoration never infer hits.
- The first eligible hit creates one cached HitFeedback child. Its 32 reused slots cap bursts at 12; repeat strikes replace slots instead of allocating Nodes. Existing red flash, damage numbers, HP/defense, attacks, knockback and tree hit leaves remain independent.
- Shared ParticleEmitter.Draw optionally maps world-space particle transforms back into the consumer's canvas. Actor translation/rotation/scale cannot drag/stretch emitted fragments; normal actor canvas/Y sorting is retained. The existing default local-space tree draw path is unchanged. A whole burst inherits actor sorting/visibility; individual fragments are not separately Y-sorted.
- VisualOffset lifts effects from the real hurt point (default 14px upward), bounded to ±64. Bone uses eight pale angular fragments, Flesh six smaller red droplets; lifetimes ~0.15–0.30s with bounded spread, acceleration/spin/fade. Default Entity death fade is long enough for the short final burst. Immediate retirement/shorter authored fade can truncate it; no orphan effect survives the owner/chunk.

## Performance and lifetime

Never-hit/profile-disabled actors allocate no impact canvas. First eligible hit allocates one Node/RNG/helper and lazy fixed slot array; subsequent hits reuse them. Only active slots wake the canvas's render callback; the final expiry/clear redraw removes commands, then processing sleeps. Actor death permits its short final burst; suspension/unload clears transient particles and stops processing. No per-particle Nodes, physics bodies, timers, global manager/scans, additional hit queries or health subscriptions. Per-frame work scales with recently struck actors, at most 32 slots each; there is no global cross-world particle budget.

No target-PC CPU/GPU/GC/frame-time measurements were made. Headless cap/idle checks verify behavior, not a measured performance improvement.

## Campaign compatibility

The production SkeletonBasic.tres and DefaultPlayerConfig.tres now reference shared material Resources. These authored-content changes affect the conservative dependency fingerprint: **use F6 World or a new campaign**. Existing incompatible campaigns are refused with files preserved. No save-schema change, migration, fingerprint bypass or persistence of FX state was added. Separate-process tests below use disposable user data, never real profiles.

## Automated evidence

Godot 4.7.2 Mono (`ed1daf0bf`), .NET SDK 8.0.415/runtime 8.0.21, Linux headless. Official matching tools/local bundled Godot NuGet packages were restored; no project/toolchain version changes. Final SDK MSBuild build succeeded without compiler warnings/errors; import exited 0 without ERROR lines.

| Check | Result |
| --- | --- |
| SDK restore/build; editor import | Exit 0 |
| New actual-contact/material fixture | Exit 0, **29 checks** |
| Existing C3.3a+b particle/tree adapter fixture | Exit 0, **45 checks** |
| Generic trees; production fire | Exit 0; zero tree failures and fire lifecycle suite passes |
| C1 / C2 / C3.2 | Exit 0, **10037 / 46 / 41 checks** |
| C2.5 animation fixture | Exit 1, `Thrust rotated away from aim`; previously reproduced on unchanged baseline in C3.3a+b, not fixed or relaxed here |
| SO2 ownership | Exit 0, **60 assertions**, no ERROR lines in this run |
| P3 streaming | Exit 0, **4425 assertions**, no ERROR lines in this run |
| Campaign write | Exit 0, **44 checks** |
| Final fresh isolated save pair, normal effects enabled | Exit 0, **44 write / 39 read**; no ERROR lines |
| Initial campaign read and retries | Native exit **-6** before suite completion, `gchandle.is_released()` and off-thread `propagate_notification`; no read PASS claimed for those runs |

The 29-check fixture covers material differences, lazy allocation, actual closest point/source/aim, positive/zero/armour/miss/chip/full-block outcomes, generic damage/restoration exclusion, renderer reuse/caps, expiry, suspension/reactivation, player Flesh, one burst per swing, killing blows and retirement cleanup. Active drawing also runs while the test moves/scales the actor. Rendered appearance/occlusion and arbitrary transforms still require manual checks.

Read-failure investigation: an unchanged `6bf6559` worktree produced a clean **44-write / 39-read** pair in this session. Its normal MSBuild launches hit the existing container process-info startup error; baseline sources were compiled using the installed SDK's Roslyn plus exact Godot source generators/API and corrected baseline analyzer paths. A diagnostic current build with burst counts temporarily set to zero also produced a clean 44/39 pair; original profiles were restored afterward. Temporary renderer tracing then confirmed that **no impact canvas was created before the failing original read crashed**, so no particle-emission cause was established. Tracing was removed and the final build contains none. A fresh isolated write/read pair with the final normal profiles/build then passed **44/39** and exited cleanly; the original test-data read failures were not reproduced on that new pair. Native read faults are unresolved; prior engine failures are not proof that these failures are harmless. No forced GC, relaxed assertions or engine/streaming workaround was introduced.

## Local commands and manual acceptance

With matching Godot Mono and .NET on PATH, from the project root:

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --max-fps 60 --path . res://TESTS/Feedback/ValidateHitFeedback.tscn
godot --headless --max-fps 60 --path . res://TESTS/Feedback/ValidateParticles.tscn
godot --headless --max-fps 60 --path . --script res://TESTS/Trees/validate_generic_trees.gd
godot --headless --max-fps 60 --path . --script res://TESTS/Fire/validate_production_fire.gd
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidatePlayerActions.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateHitReactions.tscn
godot --headless --max-fps 60 --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
```

Use isolated Godot user data for save fixtures. In F6 World hit skeletons from all directions with LMB/MMB, let enemies hit the player, test shield chip/rear/full absorption, rapid strikes and killing blows. Confirm correct pale bone/red Flesh origins, short gravity/spin/fade, no duplicate damage/flash/numbers, particles staying behind during recoil, camera zoom/Y sorting/occlusion and scaled/mirrored roots. Disable/unload an actor mid-burst, reactivate, and inspect settled/many-actor callbacks/draws/allocations in the profiler. Visual acceptance and performance remain user testing.
