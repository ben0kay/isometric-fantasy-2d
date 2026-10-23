# Player equipment

`EquippedTool` adapts `PlayerInput.ToolUseRequested` into shared C2 `AttackExecution` requests and renders the existing axe sweep/facing. Successful requests wake its attack physics callback, which sleeps after recovery/cooldown or cancellation. Its pre-existing continuous mouse-facing render callback remains. Death cancels/hides the tool; disabled scene ownership cancels pending contact; exit unsubscribes and disposes native query objects.

Tool scene/art/config stay in ITEMS/Tools/Axes. `AxeConfig` inherits shared `AttackProfile`; there is no inventory/slot system. Contact geometry, target policy and actor damage live under COMBAT, while tree feedback stays with WORLD tree helpers. The old HitQuery child/API is retained for compatibility callers/tests and is not the production swing executor.

**Planned:** [C2.5](../../../COMBAT/C2_5_PlayerActions.md) generalizes this adapter for LMB/MMB profiles, starter axe/sword switching and an independently testable shield flag; no inventory required.
