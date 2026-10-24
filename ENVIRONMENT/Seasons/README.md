# ENVIRONMENT / Seasons

**SE0/SE1 and SE1b implemented. User reported SE1 working; SE1b awaits local validation.** `EnvironmentRuntime.Seasons` owns one manual, event-driven `SeasonController`. `SeasonMasterConfig` / `DefaultSeasonConfig.tres` supply Inspector defaults through `EnvironmentMasterConfig.Seasons`; initialization copies values into private world state, never edits authored Resources. A missing config safely defaults to disabled / Summer (0.25).

F9 → Environment → **Seasonal foliage** (collapsed initially) exposes Enable, a month/date-labelled day slider, colour strength 0–100%, and early/late variation ±0–45 days (default ±14). YearPosition now maps January 1=0/1, early July=0.5; defaults remain disabled / Summer. Northern Hemisphere season names describe a non-leap manual preview, not an automatic calendar. Commands work with the clock paused and without F9.

Each tree receives one stable timing group from its world instance ID (preview fallback: content/rooted position). One 16×2 state texture per species contains shifted palettes and tone controls; season commands update it, with no per-tree loop or material rebuild. Zero timing variation synchronizes trees. The controller/adapter have no frame callbacks.

**SE1 recolours Oak** using its optional Avalonia mask and Gradient palette. Summer is unchanged; other stops supply fresh green/yellow/orange/red/brown and pale blue-green winter. Optional tone controls preserve detail while lifting winter brightness and reducing saturation. Missing masks retain automatic detection. Pink Blossom stays unchanged. Rain/snow use original pixel identity or the mask. No calendar or preview persistence. See [SE1 checks, F6 instructions and campaign limitation](SE1_Validation.md); [SE0 history](SE0_Validation.md).

[SE0–SE4 plan](SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md): SE1 binds Oak's existing Avalonia `Sprites/tree_01_fm.png` mask and adds seasonal palettes, retaining automatic detection for unmasked artwork. SE2 completes integration/performance validation and older-campaign compatibility; SE3 broadens mask validation. SE4 calendar/gameplay/save integration remains deferred.

State belongs here; generic foliage logic belongs under WORLD/Content/Vegetation/Shared; artwork/masks belong beside species sprites. DEBUG owns only removable controls. Future fire-mask uses are outside this task.

Current follow-up: [SE1b implementation, tests and local checks](SE1b_Validation.md).
