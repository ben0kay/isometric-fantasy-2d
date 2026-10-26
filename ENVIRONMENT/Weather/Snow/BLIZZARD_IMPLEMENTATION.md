# Blizzard implementation plan — B0–B4

**Status: B0 implemented — awaiting native/local validation; B1 implemented — awaiting local visual validation; B2–B4 planned.**

**User-approved control revision:** extend the existing Snowfall intensity slider to 0–3, preserving 0–1 ordinary snowfall. Storm target = clamp((rate−1)/2, 0, 1). Settled coverage remains independent. There is no second severity slider or separate preview ON/OFF: existing Snow/Falling snow masters gate the storm. B0 supplies state/transitions; B1 now supplies layered flakes and ground spindrift, with B2 lighting/haze still planned. This revision supersedes older separate-severity control suggestions below. Rain can later use the same control convention without changing current rain in B0. [B0 implementation and validation](B0_Validation.md).
**Recommended first implementation pass: B0 → B1 → B2**, followed by visual feedback before B3/B4.
**Scope:** production Godot C# top-down world, not the independent `DEBUG/TopDownTest` sandbox.
**Intent:** intense snowstorms/near-whiteouts with heavy sideways snowfall, snow-covered ground, icy overcast daylight, broad blowing haze, reduced distant visibility and optional frost/snow on the camera. Reuse existing shared systems; leave the normal snow/rain presentation unchanged at storm intensity zero.

Read the repository root `README.md`, `CodePreferences.md` and this Snow `README.md` before implementing. Use one GitHub commit per assigned pass, update phase checkboxes and validation evidence after each pass.

## Confirmed starting point — do not accidentally rebuild these systems

| Existing owner | What it already does | Planned blizzard interaction |
| --- | --- | --- |
| `ENVIRONMENT/Weather/Snow/WorldSnow.cs` + `SnowProfile.cs` | Independent enable, settled coverage, auto accumulate/melt, dormancy, chunk-aware vegetation/rock/ground adapters | Keep settled **coverage** separate from falling snow intensity and storm severity |
| `ENVIRONMENT/Weather/Snow/WorldSnowfall.cs` | One camera-aware GPU snowfall emitter; **1,300 particles** maximum; follows shared signed wind | Extend/replace its presentation with bounded near/mid/far snow layers, not an additional always-on CPU system |
| `ENVIRONMENT/Wind/Simulation/WindController.cs` | One signed -3..+3 global wind source | Read it; gust variation for particles should not silently overwrite global wind/player F9 edits |
| `ENVIRONMENT/Weather/Rain/RainVisualLayers.cs` | Three depth-separated GPU rain layers with a combined particle budget | Use as a design reference; do not copy rain controllers or duplicate rain particle work |
| `ENVIRONMENT/Lighting/DayNight/WorldDayNight.cs` | Ambient CanvasModulate, rain overcast tint, projected shadows and lightning | Compose blizzard overcast with existing daylight/rain without stacking multiple ambient modulators |
| `ENVIRONMENT/Atmosphere/WorldAtmosphere.cs` + `Fog/GroundHaze.gdshader` | Shared viewport-bounded world haze, per-biome AtmosphereProfile, world wind drift | Blend temporary icy density/size/colour overlays without mutating the original biome Resource |
| `ENVIRONMENT/Lighting/Colour/WorldColourGrade.cs` | Fullscreen contrast/saturation/vignette under CanvasLayer 10 | Prefer an additive configurable storm grade or a coordinated single world post-effect; avoid duplicate full-screen screen-texture passes |
| `DEBUG/UI/Environment/EnvironmentDebugPanel.cs` | F9 Environment drawer with Snow, Rain, Wind, Atmosphere and Lighting controls | Add a **Blizzard** subsection and session-only preview controls |
| `ENVIRONMENT/EnvironmentRuntime.cs` + `Profiles/BiomeEnvironmentProfile.cs` | Compose runtime world environment and clone authored profile Resources for each biome | Inject storm ownership and runtime profile safely without new scene-tree clutter |

**Important layering detail:** gameplay HUD is also CanvasLayer 10 (`UI/HUD/Gameplay/GameplayHUD.tscn`), and F9 debug is CanvasLayer 20. A screen overlay must have deliberate layering so it affects the **world view**, not player HUD text, menus, or F9 controls. Inspect actual draw order before choosing its canvas layer.

## Gameplay/visual contract

- Two authoring controls: **Snow coverage** (0–1 settled surface) and **Snowfall intensity** (0–3). Internally normal snowfall clamps to 0–1 and resolved blizzard severity maps the 1–3 portion to 0–1. Coverage never triggers blizzards. Existing settings ≤1 retain ordinary appearance.
- At storm intensity **0**, the world must behave/render like today's ordinary snow. No extra storm shaders, GPU emitters, screen overlays, ticks, or modified profiles may remain active.
- At low severity: gently stronger flakes/wind. At high severity: much denser diagonal snowfall, fast near-camera flecks, moving near-ground spindrift, larger icy-grey haze patches, overcast daylight, and irregular distance visibility falloff.
- At maximum: dramatic whiteout, but **player, immediate nearby threats, telegraphs, torch lighting and readable HUD must remain identifiable**. Do not replace the world with a flat white or black circle.
- Weather follows a smoothed transition in real seconds, not the accelerated in-game clock; severe gusts should ebb naturally. Camera zoom/window resizing must not break screen composition or cull flakes incorrectly.
- Weather is initially presentation only: no player cold/damage, movement penalty, AI sight reduction, or snow collision changes. Those belong to a separate approved gameplay pass.
- A strong rainstorm / torrential-rain mode is a **future sibling** reusing the storm presentation interfaces, not simultaneous automatic blizzard + rain stacking.

## Proposed architecture and knobs

Use one small, event-driven **runtime blizzard state/controller**. Suggested files in this Snow folder: `BlizzardProfile.cs`, `BlizzardProfile.tres`, and `WorldBlizzard.cs` (names can change if existing conventions prefer otherwise). `BlizzardProfile` owns Inspector-friendly, grouped, bounded knobs for severity, transitions, layers, gust cadence/strength, cold-overcast tint, haze boost, visibility radius/falloff, foreground flakes and frost opacity. Prefer a single shared intensity signal or changed-only settings dispatch; do not let snow, haze, grade and lighting all independently poll the entire world.

Wire the profile into the existing `BiomeEnvironmentProfile` default/override resolution and `EnvironmentRuntime`, if appropriate; clone authoring Resources before F9 edits. Preserve backwards compatibility for biomes/resources that do not declare blizzard data by resolving to an **off** profile. `WorldSnow` remains responsible for actual settled cover and its normal lifecycle; `WorldBlizzard` owns *presentation coordination*, not snow ground simulation. Consumers should receive additive/temporary values via explicit APIs, rather than rewriting `SnowProfile.Coverage`, `WeatherProfile.RainIntensity`, `AtmosphereProfile.HazeDensity`, or `WindMasterConfig.Strength`.

Suggested severity curves (starting visual targets, **not hard-coded requirements**):
- 0.00: normal snow only / blizzard helpers asleep.
- 0.25: light windblown increase, little additional haze.
- 0.50: snowstorm, more directional flakes and cloudy tint.
- 0.75: severe blizzard, low distance visibility, larger haze, some screen flurries.
- 1.00: whiteout, strongest gusts, restrained icy edge-frost and near-camera effects.
Keep each layer independently enableable in F9/Resources so visual problems can be isolated.

## Phases

### B0 — Storm state, composition and F9 controls [x] (implemented; validation pending)

**Goal:** an easy, reversible way to turn a blizzard on for testing without hardcoding meteorology or changing existing snow semantics.

- Added BlizzardProfile transition tuning and changed-only notifications. Existing Snow.Enabled/Snowfall gates and extended SnowfallRate supply enable/severity; no duplicate controls. Current authored snowfall rates remain below 1, so default storm is OFF.
- Add `WorldBlizzard` (or similar) once under `EnvironmentRuntime`, not per chunk, species or tree. It exposes `Intensity` as a smoothly resolved value, with a single transition callback while changing, then sleeps at the reached value. Notify relevant renderers on input/transition changes; avoid repeated identical shader writes.
- Existing F9 Snowfall intensity now spans 0–3. A collapsible Blizzard transition subsection exposes real-second transition tuning and reset to normal snowfall. No preview command mutates wind, coverage or snow masters; therefore no snapshot/restore ownership is needed. Production World is wired through EnvironmentRuntime.
- Do not serialize debug preview into campaign/saves. Runtime default remains inactive. Existing Snow master OFF / Snowfall OFF / zero rate should prevent storm flakes; define whether severity also fades to zero or simply waits dormant with desired target preserved.
- Acceptance: F6 starts visually unchanged; F9 0→1→0 smooths state; disabled snowfall creates no flakes; snow coverage can remain when falling snow is off; old F9 rain/snow/wind controls still work; removing world disconnects signals and timers. No idle controller `_Process` at settled 0 or settled 1.

### B1 — Very heavy, directional, layered snowfall and blowing ground snow [x] (implemented; visual validation pending)

**Goal:** visible blizzard density and strong near-horizontal gusting with controllable performance.

- Extend `WorldSnowfall` or introduce one grouped `SnowfallVisualLayers` helper with **far, mid and near** GPU layers; reuse generated snowflake atlas, shared wind and the already-cached viewport world rectangle. Small soft distant flakes, ordinary flakes, and fewer larger faster foreground flakes.
- Keep a **single combined particle budget** across the layers, not 1,300 for *each*. Start by measuring today's 1,300 normal-snow baseline; a provisional severe-weather ceiling around **2,600 combined GPU flakes** is a test target, subject to profiling/quality tuning, not a promise. Avoid resetting GPU process material or reallocating textures every frame.
- Add a separate low-count **spindrift/snow streak** layer near ground or a cheap world-space shader: translucent wisps moving across open ground with shared wind; avoid per-tree collision/queries, per-flake Nodes and duplicated full-world quads.
- Gust waveform/seed varies strength/speed/opacity within authored bounds; left and right wind behave properly; storm gusts affect visual flakes without mutating the user's global wind setting unless explicitly requested.
- Respect viewport zoom, active camera bounds and streamed world ownership. Extra layers allocate/emit **only at positive storm intensity** and stop/free cleanly when intensity returns to zero; ordinary snow remains its own original presentation.
- Acceptance: test 0/0.5/1 at wind -3, 0, +3; moving camera and zoom, 1920x1080; immediate disable and rapid re-enable; no sprite-cutting/giant particle boxes; no permanent extra draws when storm ends; no per-flake CPU loops.

### B2 — Cold daylight, thick haze and distance-based whiteout [ ]

**Goal:** make the world feel dim, obscured and threatening, without hiding the player or wrecking night lighting.

- Compose a cold-grey/blue **blizzard overcast** in `WorldDayNight` alongside existing rain overcast/cloud shadows/lightning. Preserve day/night clock, sunrise, local torch lights and underlying rain semantics. Storm should reduce midday visibility/light more than nighttime brightness; prevent double-darkening if another weather mode is manually enabled.
- Blend larger/denser low-ground haze via existing `WorldAtmosphere` and `GroundHaze.gdshader`. Use runtime-only extra parameters/cached blended uniforms; do not permanently change biome `AtmosphereProfile` values or create another always-on haze renderer. Existing haze toggles/off mode must be respected.
- Add a soft, irregular **distance visibility veil** centered on the actual player/camera position (not blindly assumed screen center), with tunable clear inner radius, outer falloff, noise/drift and cold tint. Distant scenery remains vaguely silhouetted rather than becoming solid black; nearby enemies and player remain distinguishable. The veil is presentational only: no AI occlusion, collision or combat targeting changes.
- Place veil/grade **before** the gameplay HUD and F9 overlays. Coordinate with existing fullscreen `ColourGrade` (CanvasLayer 10) to avoid full-screen double sample passes if possible. Only rebind camera/viewport/player and settings inputs when they change; time-driven shader motion may continue while active.
- Acceptance: compare noon/dusk/night and active torches; storm at 0/1; different cameras/zoom/resolution, forest and meadow; restore original ambient/haze immediately/gradually after blizzard; snow OFF returns normal visibility; F9/HUD are never darkened or covered.

**B0–B2 checkpoint:** visual playtest with screenshots/video and profiler before deciding more expensive overlays. This is the first satisfying blizzard preview target.

### B3 — On-screen flurries, lens snow and icy screen edges [ ]

**Goal:** communicate that the *camera/player* is in a freezing storm, not just the terrain.

- Introduce optional `FEEDBACK/ScreenEffects/`-owned presentation (or an existing suitable shared screen effects owner), controlled by the blizzard runtime state; avoid putting GUI/code in weather simulation. Screen flakes can briefly strike/smear/fade on the lens, independent of falling world snow.
- Add a subtle **procedural edge-frost** screen effect with irregular crystals/shards; intensity grows/fades smoothly, mostly along outer edges/corners, leaving a large central clear area. Keep center targeting/health bars/HUD readable; provide frost strength and enable toggle.
- Prefer a single bounded shader/overlay or tiny pooled finite set of impacts, not hundreds of UI child nodes or CPU flakes. Avoid large animated source PNGs by default; author art if procedural quality is insufficient.
- Explicit draw order: world-only fog/grade below gameplay HUD layer 10; if frost should feel like lens glass, choose an overlay order that still avoids covering gameplay UI and F9 layer 20. All Controls must pass mouse input through.
- Acceptance: no screen frost or screen impact allocations while disabled; no lingering artifacts after OFF/scene teardown; all viewport sizes, 16:9 and other ratios, zoom and UI layouts remain correct. Add accessibility/low-effects option to hide lens flakes/frost without turning off weather.

### B4 — Storm polish, reuse and performance acceptance [ ]

**Goal:** merge phases cleanly, settle budgets and leave a reusable hook for later **ultra-heavy rain**.

- Polish fade/gust curves, foreground readability and optional wind sound hooks. Avoid gameplay debuffs, extra weather scheduler, lightning or rain mechanics in this pass without separate approval.
- Expose a small generic, data-driven storm-presentation contract for future torrential rain (wind gust amplitude, lighting/haze/visibility/edge overlays), **without migrating current rain until justified**. Blizzard stays authored under Snow; shared future orchestration can live in `ENVIRONMENT/Weather/Storms/`.
- Keep strict rain/snow precedence for future scheduler; when manually setting both via F9, choose deterministic visual blending/precedence and never stack two expensive weather-screen veils.
- Test all master OFF, storm-only OFF, normal snowfall, coverage-only, combined rain+snow debug, zero wind, strongest left/right wind, day/night, chunk unload/reload and viewport resizing.
- Profile CPU/GPU/frame pacing on the target Windows machine at 1920×1080: baseline clear, ordinary snow, B1 heavy snow, B2 visibility, B3 frost and all-on; measure loaded chunk counts, particles, material writes, draw calls and **zero-state work**. Set bounded budgets based on results, not assumed FPS improvements.
- Document changed files, configuration knobs, remaining active callbacks and test evidence; update this file from planned to implemented **per phase**, not globally before user acceptance.

## Non-negotiable performance lifecycle

| State | Required work / shutdown |
| --- | --- |
| Blizzard never activated | No storm GPU emitter, lens overlay, haze extension or per-frame blizzard controller; ordinary world costs unchanged |
| Blizzard ramps up/down | One bounded intensity transition; changed-only material updates; active emitters/shaders operate only for enabled layers |
| Blizzard holds steady | Controller sleeps; GPU shader/time and genuinely active snowflake motion may continue, without redundant per-frame C# setters |
| Blizzard intensity reaches 0 | Stop/free optional snow and spindrift layers after any intentional fade; disable fullscreen passes; restore only transient composed parameters |
| Snow master disabled / World unloaded | No active snowfall, screen frost or weather haze override; disconnect signals, free temporary effects, restore authored material identities and previous presentation |
| Streaming chunks / offscreen | No scans of unloaded scenery, per-tree CPU checks or per-chunk storm controllers; leverage existing chunk-visibility/cached source registration |

## Validation/handoff for Codex

1. Build: `dotnet build -p:NuGetAudit=false` from repository root; verify Godot .NET editor/import resolves new Resources/shaders.
2. Re-run existing baseline `res://TESTS/Generation/ValidateSnowSleeping.tscn` and `res://TESTS/Generation/ValidateRainSleeping.tscn`; also test `ValidateCloudWindCaching.tscn` and any haze/shader regression covering the code touched. Baseline failures must be reported separately from new regressions.
3. Add focused `TESTS/Generation/ValidateBlizzard*.cs/.tscn` or equivalent for profile defaults, no idle callbacks, 0→1→0, layer budgets, wind sign, transition wake/sleep, cleanup, chunk lifecycle, save isolation and restoration of baseline values. Keep tests scoped to each phase; do not assert screenshots as a headless test.
4. F6 user checklist: 0/0.5/1; -3/0/+3 wind; maximum snowfall rate; settled coverage 0/1 independently; noon/night; rain toggles; F9 and gameplay HUD visible; camera zoom/resize; open-area and forest performance; screen effects OFF.
5. For each implementation pass update this document status/checkmarks, README if needed, document the actual **wake trigger, sleep condition, remaining per-frame costs, measured tests vs deferred visual checks**, and commit once per user-approved job.

**Out of scope:** weather scheduling, seasons integration beyond existing systems, enemy eyesight, temperature, survival penalties, snow footprints, new ground materials, snow physics, distributed snow accumulation, or a generic storm manager built before the blizzard demo proves useful.

## B1 implementation handoff

Normal World now lazily creates SnowfallVisualLayers at positive resolved storm intensity. Three additional depth layers reuse the normal snowflake atlas; their combined cap is 1300 extra, for 2600 total including the unchanged normal 1300. Ground spindrift is a separate default 64 (bounded to 128) GPU budget. F9 Blizzard toggles/budgets/gust tuning are session-only. At zero/master OFF optional nodes are freed. No B2 lighting/haze or B3 screen overlay. [B1 evidence and brief local checklist](B1_Validation.md).
