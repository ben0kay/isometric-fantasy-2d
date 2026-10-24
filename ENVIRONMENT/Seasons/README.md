# ENVIRONMENT / Seasons

**SE0 implemented; awaiting local validation.** `EnvironmentRuntime.Seasons` owns one manual, event-driven `SeasonController`. `SeasonMasterConfig` / `DefaultSeasonConfig.tres` supply Inspector defaults through `EnvironmentMasterConfig.Seasons`; initialization copies values into private world state, never edits authored Resources. A missing config safely defaults to disabled / Summer (0.25).

F9 → Environment → **Seasonal foliage** (collapsed initially) exposes Enable, Year position 0–1 and the nearest seasonal-quarter label. Endpoints both represent Spring. Commands work while the day clock is paused and without F9. The controller publishes `Changed` only when state changes; it has no frame callbacks, shader work or tree scans. The existing visible debug panel refresh remains unchanged.

**SE0 does not recolour trees.** No automatic year/calendar, persistence or weather/fire changes. See [SE0 validation and local checks](SE0_Validation.md).

[SE0–SE4 plan](SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md): SE1 binds Oak's existing Avalonia `Sprites/tree_01_fm.png` mask and adds seasonal palettes, retaining automatic detection for unmasked artwork. SE2 completes weather/streaming compatibility; SE3 broadens mask validation. SE4 calendar/gameplay/save integration remains deferred.

State belongs here; generic foliage logic belongs under WORLD/Content/Vegetation/Shared; artwork/masks belong beside species sprites. DEBUG owns only removable controls. Future fire-mask uses are outside this task.
