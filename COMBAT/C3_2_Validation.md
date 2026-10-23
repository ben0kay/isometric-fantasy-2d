# C3.2 — Shared knockback and stagger

Implemented 23 October 2026 against `0075984`. Targeted headless checks pass; Windows graphical feel/profiling remains user acceptance. AC remains closed for its agreed scope. This does not certify a clean broader world suite.

## Behavior and authoring

| Attack Resource | Push distance | Push window | Stagger | Interrupt committed attack |
| --- | --- | --- | --- | --- |
| `ITEMS/Tools/Axes/PrimaryAttack.tres` | 15px | 0.18s | 0.12s | No |
| `ITEMS/Tools/Axes/SecondaryAttack.tres` | 55px | 0.18s | 0.30s | Yes |
| `ITEMS/Weapons/Swords/Starter/PrimaryAttack.tres` | 10px | 0.18s | 0.10s | No |
| `ITEMS/Weapons/Swords/Starter/SecondaryAttack.tres` | 30px | 0.18s | 0.20s | No |
| `COMBAT/Weapons/DefaultEnemyMelee.tres` | 12px | 0.18s | 0.12s | No |

These are unobstructed targets before resistance; walls/actors and loaded-area guards reduce displacement. Axe damage remains **11/5**; other damage, reach and attack timings also stay unchanged. Edit each Resource's **Hit reaction** Inspector group. `PlayerMasterConfig` and `EntityDefinition` expose **Hit reaction resistance**: separate 0–1 push/stagger resistance and total immunity. Heavy creatures, wildlife, constructs and bosses reuse this data without species-specific code. No production species, boss logic, crowd steering or new scene nodes were added.

Confirmed melee contacts submit locked attack aim after actual HP removal. No extra overlap/ray query, recalculation or generic health-event inference. Zero damage, lethal hits and props skip physical reaction. Reduced but positive shield chip damage can react; a full zero-damage block cannot. Direct generic damage without attack reaction intent retains its previous behavior.

The receiving actor uses its existing movement owner. GroundChase pauses pursuit and refreshes its route after reaction; passive entities can recoil without enabling chase. Player reaction ends dodge/guard, suppresses locomotion/new committed actions, and allows an existing visual-only hop to finish. Non-interrupting hits allow an already committed swing to finish. An explicit interrupt suppresses pending contact and ends swing presentation while preserving the remaining attack time/cooldown.

Latest impact replaces remaining push rather than adding vectors. Stagger refreshes to the longer remaining duration within a **0.6s maximum reaction burst**, then **0.25s reaction immunity** permits recovery; damage is never prevented by that recovery. Death, disabled ownership and scene exit discard reactions. No save fields/schema changes or persistence of reaction timers.

## Ownership and performance

- `COMBAT/Weapons/AttackProfile` snapshots finite, bounded reaction tuning; `MeleeHitQuery` submits one reaction per successful damaging contact. `AttackExecution.Interrupt` retains cooldown.
- `ENTITIES/Shared/Movement/Knockback` is plain state and analytic linear-deceleration integration. It never changes transforms or owns callbacks/timers.
- Entity/GroundChase and PlayerMovement retain the sole collision-aware MoveAndSlide path; pursuit checks navigation bounds and loaded chunks, player movement retains CanMoveTo.
- A confirmed reaction wakes the existing passive motor; it sleeps after reaction plus recovery. Pursuers/player reuse existing physics callbacks. No global scans, additional physics drivers, per-impact allocations or idle queries were added. Target-PC CPU/GC/FPS were **not measured**.

## Automated results

Godot 4.7.2 Mono (`ed1daf0bf`), .NET SDK 8.0.415/runtime 8.0.21, Linux headless. No project/toolchain version changes. Initial container CLI startup errors were bypassed by invoking the installed SDK's MSBuild directly; the final actual MSBuild build succeeded, not merely Roslyn compilation. Initial import lacked SDK PATH/MSBUILD_EXE_PATH; corrected import exited 0 without ERROR lines.

| Check | Result |
| --- | --- |
| Final MSBuild restore/build | Exit 0, no compiler warnings/errors |
| Correctly configured Godot editor import | Exit 0, no ERROR lines |
| C3.2 production-component fixture | Exit 0, **41 checks**, no ERROR lines |
| AC1 collision / AC2 lifecycle | Exit 0, **11 / 17 checks** |
| C1 foundation / C2 melee / C2.5 actions | Exit 0, **10037 / 46 / 44 checks** |
| Campaign write | Exit 0, **44 checks** |
| Separate-process campaign read | **39 assertions pass**, native shutdown exit **-11**; not a clean pass |
| SO2 ownership | **60 assertions pass**, exit 0 with resource/renderer/RID shutdown errors; not a clean pass |
| P3 streaming | Initial runs failed `Active chunks did not finish loading`; final isolated run passed **3146 assertions**, then native shutdown exit **-6**; not a clean pass |

The new fixture covers exact integrated displacement including a large step, clamped/non-finite settings, independent resistance/immunity, diagonal/overwritten direction, bounded repeated impacts/recovery, actual positive/zero/blocked/lethal contacts, enemy-to-player reactions, passive wake/sleep and wall collision, player action priority, suspension/death cleanup, chase resumption and receiving attack interruption without delayed contact. It does not prove rendered feel, larger-crowd performance or every streamed boundary scenario.

The old C2.5 fixture's successive stationary-target attacks correctly lost reach after the new shove. Its disposable target now explicitly opts into reaction immunity so that fixture continues to isolate action/damage assertions; physical displacement is independently checked in C3.2. Its production damage expectations were not weakened. The C3.2 guard fixture sets facing immediately before striking, avoiding the real equipment aim update rotating the guard between setup and assertion.

An unchanged `0075984` worktree was separately compiled with the same Roslyn/Godot source generators (baseline MSBuild startup hit the container process-info fault). Its P3 run passed **3640 assertions**, then native shutdown exit **-11**, reproducing the broad shutdown problem without C3.2. Counts vary with traversal/loading; no timing assertion was relaxed. This comparison does not prove a cause for intermittent loading failures.

Known Camera2D interpolation warnings remain. Broad world shutdown faults and intermittent P3 loading failures were already documented in C2/C2.5/AC4; they remain unresolved. No streaming timeout/assertion relaxation, forced GC or engine workaround was added here.

## Local commands and manual acceptance

From the project root with .NET 8 and Godot Mono on PATH (`godot` may be named differently locally):

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateHitReactions.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateActorCollision.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateActorLifecycle.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidatePlayerActions.tscn
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
godot --headless --max-fps 60 --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
```

Use a disposable Godot user-data directory for save fixtures; do not target real profiles. Expected targeted counts are above; broader known failures must be reported separately from assertion passes.

Open World.tscn and press F6. Compare LMB light shove against MMB stronger prod/interruption on the five debug skeletons. Test diagonals, backs, trees/rocks, other skeletons and loaded-area edges; verify chase resumes, tools retain their pose transitions and HP changes remain 11/5. Check receiving hits during sprint/dodge/hop/guard, rapid hits from several enemies, death/unload/reload and settled idle profiler callbacks. No F9 panel or custom debug drawing was needed; use Godot's Visible Collision Shapes and existing disposable practice setup. C3.3–C3.5 remain separate work.
