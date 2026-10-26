# Regional climate — BC3

**Implemented, awaiting native/local validation.** One `RegionalClimateController` is composed by EnvironmentRuntime for the frozen World climate/map. World generation and biome layout do not change; PreviewMap never creates this controller. `RegionalClimateMasterConfig` owns temperature amplitude, phase-transition width, accumulation/melt durations, 16–64 field resolution and camera margin. Optional `WorldBiomeDefinition.WeatherResponse` supplies snow/rain permission, thermal offset/season scale and snow-cover multiplier; null is neutral. No species-name conditions or per-biome managers.

Effective temperature = frozen macro baseline + optional biome offset − seasonal amplitude × cos(year phase). January is coldest, July warmest. Snow/rain eligibility blends across −2..+2°C by default; warm southern lowlands receive rain. If even the summer maximum remains below freezing, settled snow has a permanent floor. Falling snow is independent of settled pack. Seasonal Oak palettes retain their existing mask/Gradient logic; Pink retains its own authored appearance.

## Normal World and F9

SE4 automatically advances the year from the shared clock. Default is a 120-game-day year starting at day 60 (July); tune `ENVIRONMENT/Seasons/DefaultSeasonConfig.tres`. F9 → Seasonal foliage toggles calendar advancement, changes year length or temporarily scrubs the date. Clear manual date override resumes the calendar; the simulation date continues behind a temporary override. Pausing the clock pauses calendar advancement, independently of real-time precipitation effects.

F9 → **Regional climate / precipitation**:

- Enable regional climate uses the frozen world's field. With no frozen climate, old global weather behavior remains.
- Regional precipitation 0–3 supplies one shared forcing. Cold pixels get snow, warm pixels rain, boundary pixels blend; values above 1 retain the existing blizzard layers. No random scheduler is included.
- Manual weather override temporarily bypasses the field and restores existing rain/snow controls. Their runtime snow cover/rate start at zero in climate worlds to avoid surprising snowfall when regional mode is disabled. Manual date/weather overrides are session-only and are not saved; regional precipitation and enabled state are saved.
- Snow's existing master controls its visual cover/emitters. The separate regional mode controls the climate model; disable regional climate to stop its snow-state updates.

## Spatial rendering and lifecycle

The existing EnvironmentRuntime camera callback schedules the regional field; the resolver has no `_Process` or physics loop. A 48×48 default image covers the camera plus a margin and refreshes at most every 0.25s while precipitation/melt is active, when the field moves, or on season/config events. Settled, stationary, dry conditions do not resample/upload. Only changed enable state is sent to the renderer. This adds a small O(1) scheduling check to the existing camera callback, not another per-object callback.

One shared GPU field stores cover and rain/snow eligibility. Ground, rooted foliage and rocks sample it; rain/snow/blizzard particles sample their own world positions. Existing particle budgets/motion/art remain; no CPU particle simulation is added. Warm/cold sides of one camera can therefore differ without switching the whole screen based on the player biome. CPU ground/liquid splash candidates reject snow-only locations. The shared wet-foliage shader masks wetness spatially. Field interpolation is a coarse visual approximation, not centimetre-scale local meteorology.

Viewed coarse macro cells hold mutable snow amount/timestamp; bilinear snow queries avoid chunk seams. Cold precipitation builds pack, warmth melts it, cold dry conditions retain it, and permanent-cold zones keep their floor. New cells start from that floor. No distant gameplay chunk, navigation or scene is loaded. Runtime cells allocate once when first accessed; active updates mutate cached scalars. SAVE captures detached sorted cell records. Number of cells is bounded by the existing macro map (max 262144); traversal gradually increases this small SAVE dictionary.

**Unloaded catch-up approximation:** on access, elapsed simulation seconds are integrated using the current season/weather, capped at one day of real simulation time. Intermediate remote weather/temperature history is not replayed. Offline time does not advance the calendar or snow model. This pass does not implement weather history, ecology, survival heat/cold damage, frozen water or a random weather scheduler.

`project.godot` declares three Shader Globals with disabled defaults. The active World sets one texture/rectangle/enable flag; leaving disables it before releasing texture memory. This assumes one rendered production World at a time; concurrent independent World viewports need separate field bindings before being supported. PreviewMap only shares pure `RegionalClimateMath`, adding map-only Seasonal temperature / Snow eligibility views and date scrubbing. These predict eligibility, not recorded snow depth.

See [SE4/BC3 validation and compatibility](SE4_BC3_Validation.md).
