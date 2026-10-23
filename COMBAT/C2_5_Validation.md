# C2.5 — implementation, validation and local handoff

Implemented the agreed forward slash/thrust, starter axe/sword selection, separate shield flag, slower directional block overriding sprint, dodge cancelling guard, and visual-only hop. Physical knockback remains C3. No inventory, shield art, additional physics mover, production skeleton rebalance or world/schema change.

Baseline: `19f38f1` (C2 + C2.5 handoff). Concurrent Markdown-only C3 scaffold `733727f` was incorporated without executing C3. One GitHub commit for this job. Target-PC graphical/feel acceptance and a final normal SDK build remain open; the full regression suite is not clean.

## What changed

- `EquippedItemDefinition` under ITEMS/Weapons/Shared owns two independent attack references and texture/grip settings. One existing held sprite/executor renders axe and sword; no per-weapon attack controller. New starter Resources load through optional code defaults, avoiding edits to existing scene/resource text and campaign fingerprints.
- Axe primary: 11 damage, 52 reach, 0.38s + 0.15s cooldown, forward slash. Alternate: 5 damage, 60 reach, 0.28s + 0.45s cooldown, narrow prod. Sword: separate 8/6-damage sweep/thrust profiles with clearly temporary SVG art, prop hits off. Axe prop hits remain on. Timing/reach/presentation is authored data.
- `PlayerActions` gates committed Attack / Block / Dodge / Hop. Requests are rejected, never queued. Attack commits until swing/recovery ends; shared cooldown prevents another attack or item switch. Successful dodge ends guard; RMB must be released/repressed to re-enter it. Guard overrides held sprint with 0.45× walking speed. Release, UI-consumed release, focus loss, pause, death and disabled ownership clear held guard appropriately.
- Shared `DirectionalGuard` / `GuardProfile` lives in COMBAT/Defense. Melee supplies optional `DamageRequest.Origin`; only eligible frontal contacts receive reduction. Order: attacker scaling → flat defense/clamp → 60% guard reduction → one final rounding → HP cap. Unknown/rear/coincident origins remain unguarded. `DamageResult.Blocked` records integer damage actually reduced by guard, not armor-only absorption. No additional HP pool or i-frames.
- Hop affects Sprite.Offset only, independently composing with snow/water Sprite.Position. Equipment follows the lift. Root/collider/camera/Y-sort world coordinate stays unchanged. Default 12-unit height, 0.35s duration + 0.45s extra cooldown; zero/nonpositive height or duration disables activation. No obstacle crossing or damage immunity.
- Existing sprint/dodge, health bars, PNG grip/art, legacy AxeConfig/tool-hit API, navigation and P4 schema remain. Temporary equipment selection resets to axe/shield-off on scene load; it is not persisted inventory. Actual new attack values live in starter attack Resources, not AxeConfig's inherited legacy fields.

## Actual checks

Godot 4.7.2 Mono (`ed1daf0bf`), .NET SDK 8.0.415/runtime 8.0.21, Linux headless. No project/package versions changed.

| Check | Observed result |
| --- | --- |
| Initial normal SDK restore/build/import | Exit 0 before later test/cleanup additions |
| Latest complete source compilation | Roslyn + Godot 4.7.2 source generators, exit 0, no compiler errors/warnings |
| Final normal SDK/MSBuild build | **Not verified:** container startup fails in `DebugUtils` / `Process.ProcessName` before compilation; local build required |
| Godot editor import | Exit 0, no ERROR lines |
| C2.5 real-component fixture | Exit 0, **42 checks**, no ERROR lines after final compilation/import |
| C2 fixture | Exit 0, **46 checks**, no ERROR lines; expected production primary damage now follows the selected starter profile |
| C1 fixture | Exit 0, **10037 checks**, no ERROR lines |
| Practice room launch | Exit 0 with `--quit-after 120`; no ERROR lines; this is a load smoke test, not graphical acceptance |
| P4 write | Exit 0, **43 checks** |
| P4 read, separate process | **39 assertions pass**, then native shutdown exit -11, including retry; not a clean pass |
| Actual pre-C2 disk checkpoints | **39 assertions pass**, then native shutdown exit -11; compatibility data checks pass, process does not exit cleanly |
| SO2 ownership | Exit 0, **60 checks**, no ERROR lines in this run |
| P3 streaming | Initial `Active chunks did not finish loading`; isolated retry **2960 assertions pass**, then native refcount shutdown exit -6; not a clean pass |
| Unmodified baseline P3 comparison | **3145 assertions pass**, then native shutdown exit -11 with leaked resources |
| Visibility/cancellation | `Cancellation left a frontier stuck`, exit -11; same failure was reproduced on unchanged baseline in C2 validation |
| Generic trees / production fire / local lights | All exit 0 without ERROR lines; tree/light suites report 0 failures, fire lifecycle suite passes |

The environment disconnected briefly, then recovered. Later normal MSBuild launches hit the container's already-observed process-information error. To continue meaningful validation, all current sources were compiled with the installed SDK's `Roslyn/bincore/csc.dll`, .NET 8 reference assemblies, exact GodotSharp 4.7.2 API, Godot.SourceGenerators 4.7.2 and generated project analyzer configuration. Existing SDK-generated runtime/dependency files were retained; no compiler substitute, package downgrade or project edit was used. This verifies compilation and permits actual runtime tests, but is explicitly **not** a successful final `dotnet build` invocation.

Known Camera2D physics-interpolation and bounded grass-placement warnings remain. Broad shutdown failures are not called passes merely because their assertions print PASS. The initial P3 loading assertion did not recur on isolated retry; this does not establish a cause or a fix. No streamer changes, forced GC, relaxed assertions or rendering workarounds were added. Core action tests run separately after final compilation; older pre-rebuild fixture results are superseded.

## Local commands

Pull this commit, use Godot **Mono** 4.7.2 / .NET 8 SDK, then from repository root (replace `Godot` with the installed console executable):

```text
dotnet build ./isometric-fantasy-2d.csproj
Godot --headless --editor --path . --import
Godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidatePlayerActions.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Combat/ValidateCombatFoundation.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Combat/PlayerActionsPractice.tscn --quit-after 120
Godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-write
Godot --headless --max-fps 60 --path . res://TESTS/Save/ValidateCampaignSaves.tscn -- --p4-read
Godot --headless --max-fps 60 --path . res://TESTS/SceneOrganization/ValidateWorldOwnership.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateChunkStreaming.tscn
Godot --headless --max-fps 60 --path . res://TESTS/Generation/ValidateSceneryVisibility.tscn
Godot --headless --max-fps 60 --path . --script TESTS/Trees/validate_generic_trees.gd
Godot --headless --max-fps 60 --path . --script TESTS/Fire/validate_production_fire.gd
Godot --headless --max-fps 60 --path . --script TESTS/Lighting/validate_local_lights.gd
```

Inspect process exit and ERROR lines, not only assertion totals. The 60 FPS cap is repeatable test pacing, not a performance measurement. P4 fixture targets its isolated `user://p4-validation`; Codex additionally isolated XDG_DATA_HOME. Actual old data came from the separate unchanged `3984472` checkout used in C2 validation, read without overwriting it first. Existing .tscn/.tres content and production skeleton MaxHealth=3 are unchanged by C2.5, and no migration gate/schema/version was bypassed.

## Manual feel/performance handoff

Open `TESTS/Combat/PlayerActionsPractice.tscn`, press F6. One disposable 50-HP skeleton uses the production Entity; an explicit reset button restores the target/player. Defense is optionally Inspector-tunable on the test root (default zero). No saves. LMB forward slash, MMB straight thrust, 1 axe / 2 temporary sword / 3 toggle shield. Start axe/shield off. RMB visual hop without shield; hold for guard with shield. Right Shift sprint/double-tap held-direction dodge remains. A blue arc is a debug guard marker, not finished shield art.

Check slash/thrust art/contact alignment at several aim angles, both weapon grips, front/rear block, held sprint slowdown/release, successful dodge cancelling block, rejected utility during committed attack, release over UI/pause/focus loss, no double damage/cooldown bypass, hop collisions and snow/water composition, bar placement while hopping, and exact offset restoration. Then F6 World and F5 campaign New/Continue: original trees, enemy attacks, F7/F9, streaming retirement/re-entry and profile/save/quit flow. The 3-HP production skeleton will die to a starter axe hit; use practice for the 11/5 balance trial.

No new always-running actor/FX Node or full-world scan. The existing equipment aim render callback remains; attack physics sleeps outside active swing/cooldown. Hop math/offset writes run only while hopping in the existing player motor (one cooldown scalar ticks there); zero-height hop never activates. Guard has no callback/query: release events set state, and incoming contacts test direction. Marker redraws on visibility changes; facing uses the existing pose transform. Definitions load once, modifiers/stats remain cached, and input/window subscriptions detach on exit. Profile idle, repeated attacks, multiple actors and chunk transitions on target hardware; no rendered FPS/GPU/allocation improvement is claimed.

Knockback remains C3: use existing confirmed Contact + source Origin/Aim, add authored `AttackProfile.KnockbackDistance` / snapshotted AttackValues only when implementing motor-coordinated impulse. Suggested future axe 15/55 values are **not active**. C3 notes, damage numbers, parry, stamina, inventory and production death/retry UI were not implemented here.
