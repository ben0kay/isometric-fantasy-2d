# Production World — Performance Refactor Handoff

**Date:** 2026-10-22  
**Status:** Original source audit plus completed implementation handoffs below. Original observations are historical where a completed pass supersedes them.
**Target:** `SCENES/World/World.tscn` and production environment/runtime systems (Godot C#).  
**Authoring rules:** Read root `README.md`, `CodePreferences.md`, **[`SCENES/World/scene organize.md`](../SCENES/World/scene%20organize.md)**, `SCENES/World/README.md`, and the README beside each system before editing. Follow the new *Performance-first rules for every new mechanic*. Preserve data-driven Resource profiles, F9 live controls, streaming, and all DEBUG scenes.

## Related world-scene organization plan (mandatory reading)

Before changing world scene composition, runtime node ownership, effect containers, registration, or chunk lifecycle, read **[`SCENES/World/scene organize.md`](../SCENES/World/scene%20organize.md)** at current HEAD. It explains the existing World/Actors/Environment hierarchy, the enabled P3 chunk streamer, Y-sorting, object ownership, runtime containers, and dependencies that must not be broken by a casual reparent.

**Coordination rule:** This performance plan and the scene-organization document are **two separate plans**. Do not silently implement scene reparenting or an unapproved organization phase while optimizing environmental systems. If an optimization relies on changing node paths or ownership, first cross-check the scene-organization plan and explicitly identify its affected phase; preserve both streaming and legacy compatibility mode. Node containers organize ownership but do not automatically improve performance. Inspect both plans and current source before beginning each phase.

## Scope and ownership

This audit is based on reading production source, **not benchmark measurements**. Priorities are candidate impact, not proven CPU cost. Work in small validated passes, comparing frame traces on the target PC with and without the proposed change.

**Do not modify rain or falling-leaf systems in this pass:** they are being worked on separately. This includes `WorldWeather`, `RainVisualLayers`, `GroundRainSplashes`, liquid rain splashes, hit leaves and atmospheric falling-leaf code. Coordinate interfaces with those changes rather than overwriting parallel work. Vegetation wetness is rain-dependent: assess it in a separate coordinated pass.

## Baseline and acceptance criteria

Before changing code, capture a comparable idle scene and an active scene with F9/runtime trace diagnostics: 1920x1080, current zoom and widest zoom, steady camera and moving camera, same world seed/nearby scenery density. Record per-system elapsed ms, worst spikes, callback counts, allocation and GPU/frame observations where available. Be explicit when a result is unavailable; don't invent a percentage improvement.

A refactor is complete only when: (1) the OFF/idle state demonstrably avoids needless callbacks, redraws and material updates; (2) activation and reactivation still work from Inspector/F9 signals or explicit methods, without sleeping controllers permanently; (3) transitions and existing visible effects remain intact; (4) streamed chunks load/unload/reappear correctly; (5) profiles remain data-driven and generic. When truly continuous effects run, justify their cadence.

## P0 — Dormant effects should sleep

### P0-0 — Atmospheric pollen/dust: reported large FPS regression (investigate first)

**Files:** `ENVIRONMENT/Atmosphere/WorldAtmosphere.cs`, `ENVIRONMENT/Atmosphere/AtmosphereProfiles/AtmosphereProfile.cs`; inspect atmosphere scene, shader and F9 toggle wiring.

**User observation (not yet benchmark-verified):** Disabling the atmospheric pollen/dust option produces approximately **+30 FPS on average** on the user's machine. This is the highest-priority reported A/B performance symptom. Record before/after **frame times in milliseconds**, not FPS alone, alongside CPU `WorldAtmosphere.Process` / `WorldAtmosphere.Draw` trace costs and Godot rendering profiler data. Check identical camera/seed/zoom and multiple repetitions. Do not assume `DrawCircle` itself is the root cause before isolating CPU and GPU work.

**Observed source:** `WorldAtmosphere._Process` calls `QueueRedraw()` every frame regardless of whether pollen, smoke, embers, haze or other custom-drawn effects need it. Its `_Draw` path loops over up to `PollenCount` (default 35; maximum 100) and calls `DrawPollen` / `DrawCircle` with per-speck trigonometry; shared atmospheric shader bounds and uniforms are also written each frame. `Config.Pollen=false` skips the pollen drawing loop, **but not the other atmosphere updates or redraw request**. Confirm what exactly the F9 switch disables before attributing the 30 FPS to pollen alone.

**Refactor:** Preserve the subtle drifting look while making pollen work bounded and independently controlled. When pollen is disabled or count is zero, do no pollen calculations or drawing and do not request redraws **for pollen**. Other atmosphere effects may still legitimately need redraws; evaluate separately rather than disabling the entire atmosphere node. While pollen is enabled, benchmark a single batched GPU/instanced particle approach versus existing immediate-mode `DrawCircle` rendering (or a modest controlled drawing cadence if visually acceptable). Avoid per-speck nodes, per-frame allocations, duplicate shader writes and camera-region work where unchanged. Keep `AtmosphereProfile`, F9 toggle and reactivation live; do not modify atmospheric **falling leaves**, which are owned by concurrent work.

**Acceptance:** Document repeatable baseline and pollen-off FPS/frame-ms, `Process`/`Draw` and GPU measurements; pollen OFF does no pollen simulation/drawing; switching it back ON restores the previous appearance without requiring a restart; haze, torch smoke/embers, wind, camera zoom and leaf effects remain correct. Update atmosphere README with verified findings.


### P0-A — Snow coverage and snow materials

**File:** `ENVIRONMENT/Weather/Snow/WorldSnow.cs`

**Observed:** `_Process` runs continuously, even with `Config.Enabled=false` or stable zero coverage. It updates camera-local cover geometry, cover visibility, multiple ground shader parameters and iterates `_chunks` to call tree/rock adapters, plus calls persistent adapters every frame.

**Refactor:** Separate coverage transition/accumulation from material application. Cache applied coverage, tint and relevant settings; update dependent adapters only when actual values change (with a suitable small threshold for animated transitions) or a new chunk registers/returns to view. When disabled or at a stable endpoint, stop the unnecessary snow callback until an explicit state change. If settled nonzero coverage remains visible, keep its shader rendering but stop redundant C# material writes. Watch `SetCoverage`, auto-accumulate, on/off, chunk registration and F9 slider changes.

**Acceptance:** Stable zero snow makes no repeated tree/rock shader writes or chunk walks; stable accumulated snow preserves appearance; slider changes, enabled toggles, accumulating/melting, and re-entry apply correctly.

### P0-B — Snowfall particle emission

**File:** `ENVIRONMENT/Weather/Snow/WorldSnowfall.cs`

**Observed:** Every frame sets `Visible`, `Emitting`, `AmountRatio`, visible-camera bounds, emitter position, wind direction and velocity, even when snowfall disabled/rate zero. Configured particle budget currently 1,300 GPU flakes.

**Refactor:** When snowfall is OFF, stop emission **and** idle callback; define explicit wake-up for F9/Resource updates. While active, update wind material only when changing, and camera coverage only when view position/size materially changes. Preserve wide-zoom edge coverage, transitions, and world-coordinate GPU motion.

**Acceptance:** No particle-property writes per idle frame; re-enable and zoom/camera movement don't produce gaps.

### P0-C — Lightning out of storm

**File:** `ENVIRONMENT/Weather/Storms/WorldLightning.cs`

**Observed:** `_Process` always checks storm threshold and calls `UpdateFlash()`; that unconditionally updates impact light properties and calls `QueueRedraw()` even with `Flash=0` and no storm.

**Refactor:** Wake storm cadence on threshold crossing / explicit manual `Trigger`. During active flash/tail update continuously as needed; when no storm and the flash is fully gone, turn off processing and redraw once to clear stale bolt geometry. Maintain rare-strike timing and F9 manual lightning, and avoid coupling to automatic weather scheduling not yet implemented.

**Acceptance:** Dry/no-strike state has no repeated lightning draws or light property updates; storm onset, strike, cooldown, storm end and manual triggering still work.

### P0-D — Disabled cloud shade

**File:** `ENVIRONMENT/Atmosphere/AmbientEffects/CloudShade.cs`

**Observed:** Every frame writes `Visible` and four shader uniforms regardless of `Config.Enabled`.

**Refactor:** Apply enable/disable as state change and skip disabled updates. During active drift, keep continuous animation if genuinely needed; skip uniform writes that have not changed. Ensure live F9 changes can reactivate without relying on a disabled node's own `_Process`.

**Acceptance:** Disabled clouds perform no recurring material writes; re-enabled clouds resume at current daylight/wind without jumping.

## P1 — Reduce repeated updates without freezing active visuals

### P1-A — Shared vegetation wind shader synchronization

**File:** `ENVIRONMENT/Wind/Visuals/VegetationWind.cs`

**Observed:** One shared adapter per chunk/group (good), but `UpdateWind()` rewrites `wind_multiplier` on each cached material every frame irrespective of unchanged `Wind.Strength`.

**Refactor:** Cache last-applied wind strength; update only on meaningful change or initial attach/reactivation. Keep consistent wind across material clones; don't add per-tree processes. A continuously varying signal may still require updates while active.

### P1-B — Colour grade settings

**File:** `ENVIRONMENT/Lighting/Colour/WorldColourGrade.cs`

**Observed:** Rewrites `contrast`, `saturation`, `vignette`, and `Visible` every frame, even if settings never change or grade disabled.

**Refactor:** Apply once and only on F9/profile changes; while disabled no shader updates. Ensure F9 changes propagate explicitly.

### P1-C — Torch light static settings versus animation

**File:** `ENVIRONMENT/Lighting/LocalLights/TorchLight.cs`

**Observed:** `ApplySettings()` continuously writes visibility, light toggles, texture scale, colour, shadow configuration/filter; actual flicker energy does need changing while lit. Current setup may include multiple preview torches from `EnvironmentRuntime`.

**Refactor:** Apply static settings on initialization/config changes only; active lit flicker can keep a small animation callback. When torch off, stop flicker callback until toggled back on. Do not change gameplay fire or the current shadow appearance.

### P1-D — Sunlight and shared shadows (profile first)

**Files:** `ENVIRONMENT/Lighting/DayNight/WorldDayNight.cs`, `ENVIRONMENT/Lighting/Shadows/SunShadowRenderer.cs`, `ENVIRONMENT/Time/Clock/GameClock.cs`

**Observed:** Day/night updates ambient and calls `Shadows.UpdateProjection` every frame, walking cached shadow casters. The clock is intentionally continuous.

**Refactor:** Profile before touching: cache stable shadow parameters; consider a measured capped cadence or thresholds for expensive caster projections while preserving smooth solar transition, camera movement, culling and lightning responsiveness. Avoid replacing smooth lighting with visibly stepped updates or disabling the clock.

### P1-E — Coordinated only: wetness/wind forwarding

**File:** `WORLD/Content/Vegetation/Shared/Wetness/VegetationWetness.cs`

**Observed:** `_Process` loops over `_sources` to forward wind shader params before its wetness change guard, including fully dry steady state. The wetness material update is partly cached already.

**Refactor later (coordinate with rain Codex):** Avoid needless wind synchronization when values unchanged. Sleep settled wetness work only after ensuring it wakes for rainfall onset, drying, changing wind and streamed chunk reloads. Do not edit during concurrent rain implementation.

## P2 — Streaming and spatial relevance

### P2-A — Cosmetic chunk activation checks

**File:** `ENVIRONMENT/EnvironmentRuntime.cs`

**Observed:** `_Process` computes camera view and walks all registered `_chunkVisuals` every frame to choose active status. It already turns chunk cosmetic wind/wetness off when outside view: keep that capability.

**Refactor:** Reuse camera/streamer movement or zoom change notifications, or throttle comparisons to bounded intervals when appropriate; evaluate on chunk register/unregister and sufficiently changed view. Preserve hysteresis/retention margin and avoid blank or stale cosmetics when the camera jumps. Coordinate with `WORLD/Runtime/Streaming/WorldChunkStream.cs` rather than adding a competing full scan.

### P2-B — Per-tree occlusion candidate selection

**File:** `FEEDBACK/Visibility/OcclusionController.cs`

**Observed:** Every ~0.1s tests every registered visual for player overlap and every frame loops over every registered visual to animate alpha, even when none are fading. This is shared (not per-tree processing), but scales with registered scenery count.

**Refactor:** Use chunk/spatial nearby candidates for obstruction and a small active-fade collection for per-frame opacity changes. Re-evaluate on player movement/registered changes and preserve fade restore/unregister. Do not break species-generic visibility profiles or chunk stream cleanup.

## Existing patterns worth preserving

- `SYSTEMS/WorldInteractions/Fire/Simulation/Burnable.cs`: `_Ready` registers and calls `SetProcess(false)`; `TryIgnite` wakes only the selected tree; completed burn disables processing.
- `SYSTEMS/WorldInteractions/Fire/Simulation/FireRuntime.cs`: one-time signal registration, no frame-loop scan of all burnables; burning effect instances are temporary and capped.
- `WORLD/Content/Vegetation/Trees/Shared/HitResponse/TreeHitResponse.cs`: activation/deactivation pattern for hit feedback (being refactored elsewhere; avoid conflicts).
- Spatial/cached actor-source registration from the existing P3 streaming work: **don't restore full per-frame or load-triggered forest rescans**.

## Codex execution guidance

1. Start by **profiling P0-0 pollen/dust** because the user reports approximately +30 FPS when disabled. Then tackle **P0-A and P0-B**, followed by P0-C/D; use measured results to reprioritize. Inspect the latest files and adjacent READMEs before editing, because concurrent development may already have changed them.
2. Make a small scoped change, compile and test in Godot when available, inspect frame traces, and update the relevant subsystem README with what actually changed.
3. Continue P1 then P2 based on measured cost, not blind preference for eliminating every `_Process`.
4. Use **one GitHub commit per requested job**, preserve unrelated changes and leave DEBUG untouched.
5. At each handoff report: activation trigger; shutdown condition; idle callbacks/material writes; scaling approach; measured before/after if obtainable; known unverified behavior.

**Do not claim a refactor succeeded solely because `SetProcess(false)` appears in code.** A sleeping component must still reliably wake when the user changes an F9 setting, a gameplay event fires, a chunk reappears or a world service transitions state.

## Rain / falling-leaf pass completed separately

Rain intensity now defaults to zero. Zero immediately removes GPU rain, clears ground/liquid splash pools and sleeps their callbacks; positive intensity wakes them through profile-change notifications. Shared scenery ground-query data remains independent so manual lightning still works while dry. Lightning sleeps without rain or an active flash (addresses P0-C); foliage wetness sleeps after drying and shared wind updates its cached material followers on changes (addresses the wetness-forwarding portion of P1-E). The separate VegetationWind adapter audit remains pending.

Falling leaves retain one shared renderer with at most 40 reused slots; disabled/zero-count leaves skip integration. This does not disable other atmosphere effects. Validation: 22 rain lifecycle checks, 167 scenery visibility checks, P3 generation checks and tree/fire/debug smoke checks passed under local Godot 4.5.1; target 4.7.2 and graphical FPS remain unverified. See subsystem READMEs for details and the isolated Mono shutdown caveat.

## Snow master-switch pass completed — P0-A / P0-B

P0-A and P0-B are implemented in the separately authorized snow pass. Enabled=false immediately clears/restores all snow surfaces and removes GPU snowfall. Coverage sleeps at settled endpoints; Resource Changed events, SetCoverage and explicit chunk registration/re-entry apply changes/wake transitions. Rate zero/Snowfall=false removes flakes independently of settled coverage. Active snowfall only rewrites changed view/wind inputs. The cover is now one static world-bounded quad (world-space shader, viewport clipping), avoiding camera-driven coverage callbacks. Shared wind and source-keyed wetness links preserve snow-overlay composition without polling; they unregister at zero snow/unload. No scene reparenting or weather probability scheduler was added.

Remaining idle work: streamed adapter setup/caches and shared player surface/water helper; settled visible snow still renders. Active snowfall checks camera/wind each frame; continuous coverage transitions update active chunk adapters. P0-D and remaining P1/P2 work are unchanged and pending. Tests and limitations are recorded in ENVIRONMENT/Weather/Snow/README.md. Local Godot 4.5.1 checks verify lifecycle/appearance parameters, not target 4.7.2 graphical FPS or particle continuity; target-PC trace comparison is still required.

## P0-0 pollen/dust pass implemented — target-PC A/B still required

The old per-speck DrawPollen/DrawCircle path is removed. PollenDust now owns one MultiMesh GPU-drawn quad batch (default 35, cap 100), with seeded shader trajectories, shared wind displacement and daylight response. OFF or zero count frees the helper. No per-speck C# updates, node processes, instance-buffer rebuilds or redraw requests are used during animation. Atmosphere profile setters now emit Changed; F9/Inspector updates wake it directly. Shared atmosphere redraws remain only for leaves/visible torch smoke/embers and one clear when they stop; all-off sleeps. Haze settings/bounds are cached, and disabled haze skips shader writes. No leaf trajectory/source algorithm or rain/snow behavior was changed.

Wake triggers: profile changes, first/last foliage registration and torch visibility. Active work: shared view/daylight/wind checks, changed GPU inputs, and existing enabled non-pollen effects. Off cost: no pollen batch/work; if all atmosphere effects are off, no atmosphere callback. Scaling: one capped batch globally, no per-tree/species polling. Source registration avoids repeated state refresh except zero/nonzero transitions.

ValidatePollenBatch exercises lifecycle/count limits, no pollen-only redraws, unchanged GPU input caching, synthetic canvas bounds, wind/daylight, independent effects and torch visibility. Rain/snow/visibility regressions accompany it. BenchmarkPollen records reproducible CPU-only callback samples; see atmosphere README for numbers. In isolated headless samples, 35-speck Draw mean went from 0.05282ms to no Draw callbacks; shared Process mean from 0.01907ms to 0.00540ms. All-off no longer processed/redrew. This local CPU reduction is too limited to attribute the full reported 30–40 FPS delta: real GPU/driver timings, appearance and target Godot 4.7.2/D3D12 remain unverified. Do not mark the user's machine-level symptom resolved without their repeat A/B capture.

Concurrent FPS diagnostics fixes were preserved without changing DEBUG behavior in this pass. P0-D and remaining P1/P2 phases are still pending; completed rain/snow handoffs remain valid.
