# SE4 / BC3 — delivery and local handoff

**Implementation complete, awaiting native/local validation.** Work is on `dev`; no BC4 size change, BC5 region selector or BC6 full atlas tooling.

## Delivered

- GameClock publishes actual elapsed game-day fractions; SetHour only scrubs hour and never advances the date. SeasonController binds once, advances fractional calendar days and updates palettes/climate only at simulation-day boundaries. No independent calendar frame callback. Defaults: automatic foliage/calendar enabled, 120-day year, starting day 60. F9 preserves manual seasonal palette strength and stable tree timing controls.
- One world-position resolver combines frozen regional baseline, calendar offset and optional biome weather response. Same pure math serves PreviewMap/F7 selected-date temperature and snow-eligibility layers.
- Bounded camera texture masks actual rain/snow particles, blizzard/spindrift, settled ground/foliage/rock cover and wetness. Snow cover is independent from precipitation. Existing rain/liquid splash sampling rejects snow-only candidates; zero rain/snow rates free their existing emitter owners. Existing budgets and shared wind stay intact.
- Viewed macro-cell snow builds/melts locally, with dormant settled/dry state and explicit on-access unloaded catch-up approximation (current conditions, no intermediate weather history). No full-world or per-tree simulation.

## SAVE policy

Campaign checkpoint/index schema **4** extends SavedClock with a validated SavedCalendar and optional SavedRegionalClimate. Blueprint stays **11**, macro generator **4**; no geography is regenerated. Saved calendar owns exact absolute fractional day, year length, auto mode and foliage settings. Snow owns simulated elapsed seconds, forcing, regional enable and visited-cell amounts/timestamps. Save worker validations bound counts, keys, ranges and times. Capture only creates detached DTOs; runtime cell data mutates in place.

Campaign schemas **1–3** remain recognized. If calendar/snow data is absent, initialization uses the current explicit default calendar and permanent-snow floor; there is no inferred offline elapsed time or invented remote snow history. Schema-4 captures require calendar data and, for frozen-climate worlds, climate state. Session date/weather previews do not replace saved calendar state. Content fingerprints remain strict: changed environment Resources/shaders may refuse older campaigns. No blanket compatibility bypass or claim that all old authored content migrates. Existing files remain untouched on rejection; use F6/new campaign for this pass.

## Checks performed

Source/diff review, Resource references/IDs and shader include/global contract checks; whitespace checks. Godot/.NET unavailable here: **no native build/import, shader compilation, rendered boundary check, disk Continue or profiler pass is claimed.** A focused local fixture was added; existing hand-authored SAVE fixtures were brought forward for schema 4, without running full suites.

## Short local validation

1. `dotnet build`, then Godot .NET import. Shader Globals must load from project.godot before included shaders compile.
2. Optional focused fixture: `godot --headless --path . res://TESTS/Climate/ValidateSeasonalClimate.tscn`. Expected `SE4 BC3 CLIMATE PASS`, exit 0. Checks clock/day wrap, hour scrub, pause, session override clearing, calendar JSON restore, north permanent pack, temperate seasonal phase, south rain, mixed boundary and malformed snow timestamps.
3. F6 normal World: F9 Seasonal foliage shows date/year and 120-day year. Shorten seconds/day to observe date/foliage advancement; pause clock and verify date stops. Date slider changes the seasonal preview; Clear resumes calendar. No per-tree frame/calendar callbacks.
4. PreviewMap: Seasonal temperature / Snow eligibility and map-only date slider. January/July should move the temperate freezing line; very cold north remains eligible through summer. These views should not instantiate any World/weather/chunks.
5. World: set Regional precipitation 1 (or up to 3 for storm). Warm area → rain, cold → snow. Inspect an actual boundary within one camera, including wet leaves, trees/rocks, particle atlas animation and liquid/ground splashes. Observe accumulation, then dry cold retention and warm melt. Disable precipitation: GPU emitters should disappear; dry settled field sampling/uploads should stop. Manual weather override restores the previous controls.
6. New Campaign → advance calendar/accumulate pack → save → Continue. Confirm exact calendar fraction and visited snow values, regional forcing, inventory/HP and frozen map identities. Resume must clear session previews. Corrupt/missing schema-4 calendar or invalid snow keys/timestamps should reject without modifying source files.
7. Target-PC profiler, if desired: default 48×48 field at 4Hz while active, zero field sampling while stationary/dry/settled, no far chunk loads, existing bounded particle counts. Camera/texture border behavior and multi-layer particle atlas rendering specifically need Godot/Windows acceptance.

Future follow-up: true historical remote snow catch-up and weather scheduling, richer biome winter palettes, survival consumers and independent multiple-World viewport bindings are intentionally outside this scoped foundation.
