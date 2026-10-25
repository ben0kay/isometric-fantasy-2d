# H0–H3 — shared drops, harvesting and tree fall validation

24 October 2026. H0 source audit complete; H1/H2/H3 implemented in isolated fixtures, **awaiting Windows visual/performance acceptance**. H4 persistence/streaming/production enablement and H5 expansion remain planned. One job commit: `feat: implement H0-H3 harvesting and physical drop practice`; obtain SHA with `git log -1 --format=%H -- SYSTEMS/Harvesting/H0_H3_Validation.md`.

## Implemented behavior / files

SYSTEMS/Drops adds canonical reward Resources, deterministic compiled plans, shared scatter settings, bounded logical journal/coordinator and dormant-settled pickup scene. SYSTEMS/Harvesting adds tool-work/contact contracts, Resource profile and event-only per-source work/completion. Optional fields on ItemDefinition separate world appearance from inventory artwork; EquippedItemDefinition provides tool capability/work. AttackExecution snapshots that work; MeleeHitQuery delivers it only on confirmed nearest-tool contact, preserving legacy visual hits and actor damage/reaction paths.

PlayerInput's named player_interact (E) is enabled only when PlayerPickupInteraction is explicitly bound to a supplied storage/drop scope. E collects one nearest settled pickup, subject to radius, line of sight and all-or-nothing pack acceptance. Inventory-open input suppression still applies. UI uses the existing storage events; no item authority is added to views.

Shared Tree supports optional HarvestProfile via explicit EnableHarvesting, which rejects WorldScene ancestry until H4. The fixture duplicates Oak/Blossom definitions and axe data; production Resource contents stay unchanged. Four valid primary axe chops reserve 3–5 logs and remove standing collision/tool/fire eligibility. A TreeFall sibling copies the exact rooted/mirrored texture transform, accelerates to ±90°, releases the reserved rewards, then fades/frees. Source teardown uses existing lifecycle cleanup; wind/wobble/fire metadata is not copied onto the proxy. Interrupted proxies release their reserved batch once without rerolling. No stump/regrowth or falling damage.

Normal World, saved campaigns and their content fingerprint are unchanged; the fixture uses a fixed scope/source identity and no disk IO. It is not an H4 save system. Progress, records and inventory disappear at fixture teardown.

## Checks performed

- Direct C# Roslyn build: .NET SDK 8.0.415/net8 references + Godot 4.5.1 assemblies/source generators; exit 0, no compiler errors. Project Godot.NET.Sdk 4.7.2 unchanged.
- Godot 4.5.1 asset import completed; editor reports existing missing SDK/Microsoft.Build discovery errors. This is not a passing standard target-engine editor build.
- HarvestingPractice headless startup/teardown smoke: exit 0, no errors.
- `TESTS/Harvesting/ValidateHarvesting.tscn`: **67 checks passed**, exit 0. Includes production WorldScene gate, in-flight work snapshot, actual fourth-swing felling, wrong-tool/prod rejection, snapshot/deterministic rewards, failed completion retry, no duplicate reservation/release/grants, airborne/full/mismatched/dead/out-of-range/behind-wall collection failures, blocked/solid landing and cap atomicity, grounded hop/sleep, actual E/I routing, mirrored transform/direction, fire precedence and interrupted fall release. Confirms default production Oak/axe and content fingerprint unchanged.
- Existing `TESTS/Combat/ValidateMeleeExecution.tscn`: **51 checks passed**, exit 0.
- Existing `TESTS/Inventory/ValidateItemInventory.tscn`: **54 checks passed**, exit 0.
- Existing `TESTS/GameplayUI/ValidateGameplayUI.tscn`: **45 checks passed**, exit 0.
- Existing generic tree GDScript integration: **0 failures**, exit 0.
- Existing production fire GDScript integration: **passed**, exit 0 (normal/mirrored anchors, lifecycle, collider, wind, snow transitions, shadows, reset and effect caps).
- `git diff --check`: passed.

Headless checks establish behavior and native compatibility with available tooling. No rendered Windows visual acceptance, measured FPS/GC result or new disk/streaming persistence claim. Existing camera physics-interpolation warnings remain.

## Local commands

Use the project's target Godot .NET installation (SDK 4.7.2); substitute executable path for godot.

```powershell
dotnet build
godot --headless --path . --editor --import
godot --headless --path . res://TESTS/Harvesting/ValidateHarvesting.tscn
godot --headless --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
godot --headless --path . res://TESTS/Inventory/ValidateItemInventory.tscn
godot --headless --path . res://TESTS/GameplayUI/ValidateGameplayUI.tscn
godot --headless --path . --script TESTS/Trees/validate_generic_trees.gd
godot --headless --path . --script TESTS/Fire/validate_production_fire.gd
```

## Manual acceptance

1. F6 HarvestingPractice. Confirm LMB axe ×4, sword/MMB no chop work, existing nonfinal leaves/wobble, right/left fall from impact, rooted mirrored art, no shader clipping, log quantity/angle/hop/shadow and E collection into actual pack counts/weight. Tune root embedded profile/shared scatter Resource rather than adding per-species scripts.
2. Move away during the fall/flight and return; logs remain in this bounded room. Repeated hits/releases/E cannot multiply rewards; a full pack leaves items; open inventory blocks collection/attacks. No auto-pickup or expiry is implemented.
3. Check 1080p and smaller windows, actor sorting, tooltips, shadow readability and room instructions. TreeFall is intentionally a temporary texture/modulate copy without standing wind/fire/snow/season shader animation; full live-environment falling composition is not validated here.
4. Profile healthy trees, active chopping/fall, airborne pickups, many settled pickups, open/closed inventory and room restart. No new idle tree/harvest/drop/collector frame callbacks or item physics bodies. Only active fall/drop presentation ticks. Coordinator queries run only on reserve/collect events; spatial buckets/budgets bound work. Actual frame cost remains unmeasured.
5. Normal F6 World and existing campaign Continue retain standing trees and previous controls; no H3 supplies/rewards appear. H4 remains required before production harvesting, inventory saves and chunk-owned drops.

## H4 handoff

See H0_Audit and the plan. Stable production world/candidate IDs must replace fixture IDs. Persist source progress/felled + chosen remaining drops + inventory consistently, version legacy saves explicitly, and use existing P3/P4 ownership. Stream by settled location; offscreen records must not keep Nodes alive. Saving mid-fall may restore the already-committed logical outcome as settled. Do not bypass fingerprints or infer drops from tombstones. Current bounded disposable journal is an interface starting point, not a durable store or second streamer.
