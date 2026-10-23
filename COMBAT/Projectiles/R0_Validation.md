# R0 — Straight projectile foundation

Implemented 23 October 2026. R0 only: no ranged AI, bow equipment, archer definition, practice encounter, F9 ranged controls, homing, health, piercing or pooling. Existing World scenes/resources and save schema are unchanged.

## Ownership and correctness

`ProjectileProfile` validates finite bounded flight/visual values and snapshots `AttackProfile.Impact` contact values plus the shooter's effective stats once. Positive speed/lifetime/range are required; invalid/null inputs and inactive sources fail before allocation. Collision mask is fixed to current solids + actors (3). Melee timing fields are unused in flight. The profile can author zero damage; zero applied damage emits no impact/reaction.

`ProjectileSpawner` is a scene-local budget/boundary owner, not an always-running global manager. Set a finite world-space Bounds rectangle and inject the existing stream's IsPositionLoaded predicate for streaming use; neither is inferred through global searches. It requires a living source physics body in its own World2D. Default 64 active shots, hard maximum 256; failed spawns do not consume slots. R1 should reuse one owner per appropriate world scope rather than bypassing the cap with a new spawner per shot. No production World node/resource was added in R0.

`Projectile` is one non-blocking Node2D with one reused native ray query and source RID exclusion. It performs one full-segment ray cast per active physics step, including the final partial range/lifetime step. It is a **point projectile**, not a thick shape sweep. Existing actor/scenery bodies obstruct the ray; no feet or melee hurt colliders are changed/duplicated. Drawn shaft and fixed world-up visual elevation do not enlarge its hit geometry. Collider-layer semantics remain AC1/2. Physics bodies remove disabled actors through existing ownership.

The first obstruction retires the arrow. An eligible living target receives one canonical damage call using the fire-time faction/stat/damage snapshot. Friendly/ineligible actors stop arrows without damage; source is excluded. Props/trees stop arrows without invoking IToolHitTarget.ReceiveHit, so arrows do not harvest wood. Directional guard uses incoming arrow bearing rather than the shooter's possibly changed location. Confirmed positive HP loss reuses impact and optional reaction contracts, including lethal feedback and existing reaction/feedback master switches.

Retirement disables processing/hides/releases its budget before lifecycle callbacks. Damage re-entry cannot issue a second hit. Expiry, boundaries, source death/disable/free and spawner disable retire safely. Scene exit removes callbacks and disposes the ray without late impact events. Arrows are transient and have no save component. They are deliberately cancelled when the shooter becomes inactive; retaining ownerless arrows is a possible future policy, not implicit behavior.

## Performance handoff

- Wake: one accepted TryFire creates/configures one arrow and activates its physics callback.
- Sleep: impact, expiry, source loss, disabled ownership or boundary failure stops it immediately and queue-frees it. Empty spawner has no process/physics callback. No timers, sensors, full Entity stacks or world/group scans.
- Active work: at most one ray per arrow per physics step; cached query and immutable values. Godot result dictionaries allocate; spawn/native-object allocations are not pooled. No allocation-free claim.
- Bounds: finite rectangle and at most 65 membership checks (64 subsegments) per step, spaced at most 16px. A >1024px step fails closed rather than doing unbounded work. This may retire a shot during a very large hitch. The membership predicate must be read-only and must not load chunks. Sampling is not exact continuous validation of arbitrarily small holes/corner crossings; inspect the intended streamed-world integration in R1 before claiming boundary acceptance for all trajectories. Endpoint/rectangle failures are conservative, potentially retiring before a closer collision in that same oversized/outside segment.
- Scaling: cap controls live arrows, not an unlimited caller's spawn/retire loop or aggregate caps across unrelated spawners. Profile 20+ arrows on the target PC before adding pooling/batching. No FPS, GC or CPU improvement is claimed.
- Extension seams: immutable flight/contact values, isolated point-ray movement and Finished(reason, contact) lifecycle notification. Later trajectory/impact policy extraction or optional combat-target/ActorHealth attachment does not require full Entity inheritance. No unused strategy framework was added.

## Actual validation

Godot 4.7.2 Mono, .NET 8.0.415, Linux headless at 60 FPS:

| Check | Observed result |
| --- | --- |
| Normal MSBuild restore/build | Exit 0; matching Godot SDK/local package source, no compiler errors |
| Editor import | Exit 0, no ERROR lines |
| R0 fixture | **75 checks**, exit 0: source exclusion, snapshot/once-only damage, multiple directions, high-speed thin wall, no tool hits, frontal/rear guard after shooter moves, range/life, invalid input/mask/bounds, friendly blocking, loaded gap, cap/release, owner death/disable/free, spawner suspension, scene removal and bursts 1/5/20 |
| C1/C2/C3.2 | 10037 /46 /41 checks, exit 0 |
| Material impacts | 29 checks, exit 0 |
| Ownership / streaming | 60 /4425 checks, exit 0 |
| P4 write/read | 44 /39 checks, separate processes, exit 0 |
| C2.5 | Exit 1: existing `Thrust rotated away from aim` assertion; recorded before R0, unchanged tool code. Not a clean full-suite pass |

Initial guard-fixture attempts were invalid because the player's equipped-tool presentation resumed after HP changes and rotated the shield toward the headless mouse. The fixture now disables that input/presentation child while using the real player's receiving/guard implementation. Production guard code was not modified to satisfy the test.

Bursts demonstrate bounded counts and return to zero, not measured target-PC performance. New projectiles are not yet wired into production streaming or saves; those regression passes show the existing World still works, not that an archer encounter is accepted. Visual arrow appearance/Y-sort, target-PC profiler evidence and full real-World streamed lifetime remain R1/R2 acceptance work.

## Reproduce / R1 handoff

From repository root with Godot Mono/.NET installed:

```
dotnet build -p:NuGetAudit=false
godot --headless --path . --editor --import --quit
godot --headless --path . --max-fps 60 res://TESTS/Projectiles/ValidateProjectiles.tscn
```

Expect `R0 PASS: 75 checks` and exit 0. Fixture uses real player/entity receivers and a thin solid tool-receiver probe. No campaign or authored profile is modified.

R1 must bind the spawner to the real World actor/effect Y-sort root and current finite/loaded bounds; keep projectile lifetime separate from individual scenery chunks, and never let a membership check request generation. Build generic ranged intent within the existing movement owner. Explicitly test arrows near retiring chunks, source death, moving targets and mixed actors. Keep the five-melee F6 setup intact unless a practice addition is separately requested.
