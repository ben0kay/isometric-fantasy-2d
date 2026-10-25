# Item foundation and UI3 groundwork — 24 October 2026

**Implemented; native headless checks passed; Windows visual/profiling acceptance pending.** One job commit titled `feat: add data-driven item and inventory practice foundation`. Resolve its SHA with `git log -1 --format=%H -- SYSTEMS/Inventory/ITEM_FOUNDATION_VALIDATION.md`.

## Scope

Adapted the older isometric-new-2d project's useful separation: authored item definition/catalog, runtime stacks, hotbar references and UI observation. Kept existing fantasy folder ownership: raw logs under ITEMS/Resources, current axe/sword folders untouched except adding wrapper item Resources, and shared storage in SYSTEMS/Inventory. Flat catalog avoids unnecessary category catalog files at this scale.

ItemDefinition includes ID, name, description, icon, mass, stack limit and optional EquippedItemDefinition. Registered examples: starteraxe (2 kg, stack 1), startersword (1.5 kg, stack 1), wooden_log (1 kg, stack 40). Weights/artwork are provisional. Existing held profiles/artwork/attack definitions are reused. No fake shield definition.

The disposable InventoryPractice grants axe/sword/12 logs. Hotbar 1–9 selects actual equipment or empty hands through central PlayerInput. Busy attacks/hops reject selection without queuing it. Losing the active weapon interrupts pending contacts but preserves remaining recovery/cooldown; settled empty hands sleeps the equipment render callback. Icons, quantities, tooltips and weight read real storage. Worn-equipment spaces remain previews, not false equipped-state displays. Hidden inventory refreshes defer until open.

Normal World/campaign inventory and numeric controls remain unchanged. No campaign item state/save schema/content migration, tree rewards, physical pickups, transfers between containers, inventory drag/drop/rebinding UI, crafting, encumbrance or Avalonia application/loader. UI3 is groundwork/partial, not complete or accepted.

## Validation performed

- Direct Roslyn build with .NET SDK 8.0.415, net8 reference assemblies and Godot 4.5.1 assemblies/source generators: exit 0, no compiler errors.
- Available Godot 4.5.1 asset import completed. Editor SDK discovery still reports missing SDK/Microsoft.Build; project Godot.NET.Sdk 4.7.2 is unchanged. Standard build on target engine remains pending.
- `TESTS/Inventory/ValidateItemInventory.tscn`: **54 checks passed**, exit 0. Duplicate/invalid definition rejection; canonical identity; all-or-nothing capacity failure; overflow/negative quantity rejection; stacking, merging, moves, weight; reference invalidation; actual 1–9/I/Esc routing, weapon selection/attack/busy rejection/material empty hands, sprint/release with hotbar enabled, live UI counts, lost-weapon cooldown and two fixture lifecycles.
- Existing `TESTS/GameplayUI/ValidateGameplayUI.tscn`: **45 checks passed**, exit 0, confirming normal HUD placeholders/input/layout behavior.
- Existing `TESTS/Combat/ValidateMeleeExecution.tscn`: **C2 PASS: 51 checks**, exit 0.
- `git diff --check`: passed.

Headless checks establish behavior and source compatibility with available tooling; no rendered Windows screenshot, full campaign Continue regression or measured FPS/GC performance was obtained.

## Local commands / acceptance

Use the project's Godot .NET version compatible with its existing 4.7.2 SDK. Substitute actual executable path for godot.

```powershell
dotnet build
godot --headless --path . --editor --import
godot --headless --path . res://TESTS/Inventory/ValidateItemInventory.tscn
godot --headless --path . res://TESTS/GameplayUI/ValidateGameplayUI.tscn
godot --headless --path . res://TESTS/Combat/ValidateMeleeExecution.tscn
```

F6 InventoryPractice: inspect icons/counts/tooltips at 1080p and smaller windows, open inventory while moving/guarding, select axe/sword/log/empty slots, swing and try switching during recovery, add logs then reopen, check no duplicate state after restart. Worn equipment remains labelled preview. F6 normal World and a campaign Continue: old 1/2/3 behavior and empty inventory remain; no test supplies should appear there.

Profile never-open/open/closed/reopened/scene unload: no new model/UI _Process or _PhysicsProcess loops, no idle polling/material writes; only successful model events, selection and viewport resize wake UI work. Equipment's existing active attack physics is preserved and sleeps on completion. Actual timings remain unmeasured.

## Next separately authorized work

Finish UI3's production player adapter and versioned durable inventory/hotbar/equipment state before exposing inventory in campaigns. Decide unique equipment-instance identity, merge/rebind UX and temporary shield migration. Then implement data-driven tree harvest yields referencing wooden_log, followed by physical drops/containers if desired. Future Avalonia edits canonical Resources/catalog and invokes Godot validation; no separate database or gameplay folder scans.
