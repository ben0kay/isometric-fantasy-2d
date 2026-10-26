# Production environment
Implemented for `SCENES/World/World.tscn`: shared clock, signed wind, day/night illumination, projected/contact shadows, vegetation sway, cloud shade, colour grade, local torch light, bounded atmosphere particles, rain/ground splashes/lightning and removable snow coverage/snowfall.

`EnvironmentRuntime.cs` composes modules and schedules registered chunk cosmetics after production scenery is generated. `EnvironmentMasterConfig` references one ClockMasterConfig and a default BiomeEnvironmentProfile. Production modules never create sliders/panels or depend on DEBUG. The optional controls live in `DEBUG/UI/Environment/` and can be removed from World.tscn.

## Ownership
- Time/Clock: the single hour-of-day, cycle duration and pause state.
- Wind/Simulation: the single signed wind state, velocity and integrated displacement; Visuals adapts shared vegetation materials.
- Lighting/DayNight, Sun, Shadows, Colour, LocalLights: illumination calculations, cached silhouettes, grading and torch flicker.
- Atmosphere: reusable haze/noise, cloud shade and bounded pollen/leaf/smoke/ember trajectories.
- Weather/Rain, Storms, Snow: precipitation, overcast, lightning, snow progression and GPU flakes.
- WORLD: content identity/art/collisions and surface-specific snow adapters/shaders. ENTITIES/Player/Visuals owns shallow burial/clipping.

## Data and biome tuning
World defaults start at `DefaultEnvironmentConfig.tres` -> `Profiles/DefaultEnvironmentProfile.tres`. `WorldBiomeDefinition.Environment` optionally references a `BiomeEnvironmentProfile`. Each null component inherits the corresponding default; each supplied component replaces that whole component's settings. The clock is intentionally not overridden per biome.

Oak Forest references `WORLD/Biomes/Definitions/OakForest/Environment.tres`, with its own `Atmosphere.tres`; other components inherit world defaults. To tune Oak rain, lighting, snow, wind, clouds or grading, create/reference the corresponding typed profile there. Shared renderers remain unchanged. This is ready for future Avalonia tooling to edit the same Resource data, but toolbox integration is not implemented.

Selected profiles are explicitly cloned per component on startup. Developer controls edit private runtime copies; they do not save or mutate authored assets. Restart restores authored defaults. The two-biome preview resolves the primary biome profile once for the shared environment on load, matching DEBUG. Per-region environment sampling/blending, climates, calendars and seasons are future work.

## Future regional climate, precipitation and fantasy winter — not implemented

**One authoritative resolver is preferable to biome-owned precipitation systems.** The finite-world generator will eventually freeze broad contiguous climate/region zones; biome Resources (BP1 already defines Oak's baseline 15°C/65% RH) can later author climate affinity, snow eligibility and magical appearance overrides. At runtime, a shared world-position resolver combines macro zone + biome + live weather + future SE4 season/time to decide local snow/rain/snowpack. Baseline climate is **not** current weather; Oak may still snow during cold winter conditions. Fantasy grass, purple mushrooms and glowing flora may retain colour under frost.

**Crucial rendering boundary:** the existing snowfall and world-spanning snow-cover quad respond to global settings, not regional cold zones. Future regional integration must support a camera simultaneously seeing snow-covered land **and** warm uncovered land, using region/chunk-aware masks or another shared rendering technique. Don't replace current rain/snow with independent controllers in every biome; use shared GPU budgets, event/chunk/camera-based cached queries, and turn OFF updates completely when inactive. Individual foliage/ground Materials still decide how a given species looks when covered.

**Current reality:** P0/P5 geology/hydrology drives biome placement; macro climate zoning, position-based temperature, weather scheduler and snow masking by biome remain planned. BP0/BP1 climate data has **no runtime consumers**. Avoid changing existing weather appearance or save identity until separately authorized; future coarse climate fields need a versioned frozen save blueprint. [World direction](../Notes/World_Generation_Direction.md) · [main plan](../WORLD/Generation_Implementation.md).

## Defaults and limits
Preserves the debug settings: 30 real seconds per day, 08:00 start, wind +1, rain target 0% (off), snow off, coverage target 60%. Mixed rain/snow is permitted; set rain to zero for pure snow. Existing effects are visual approximations, not snow physics or fire/heat simulation. The torch's authored collision remains while its presentation is disabled, matching the original test.

Generation seed semantics remain; **F5 launches Boot/Main Menu**, not a DEBUG experiment. Run production World directly with F6 or use a P4 campaign through F5. Production P3 now registers/unregisters streamed environment adapters explicitly; see WORLD/Runtime/Streaming and the root README for current world/liquid/fire/entity status. The early startup-only validation below is historical.

## Validation
C# build uses the repository's Godot.NET.Sdk 4.7.2 and .NET 8. Godot 4.5.1 .NET runtime assertions cover biome override/default resolution, runtime isolation, one clock/wind, no-panel startup/removal, wind changes, day/night, rain/overcast, manual lightning, snow on/off/removal, automatic accumulation/melt, player burial/camera stability, environment removal and movement. Native 1920×1080 software OpenGL Compatibility renders were inspected for controls, day/night, rain/lightning, 25/60/100% snow and restoration at full production scenery density. Windows D3D12 and target hardware performance remain untested.

Integration note: the concurrent FIRE2 commit contained a C# CS0136 local-name collision in DEBUG/TopDownTest/Fire/Debug/FireTestController.cs. The ignition event delegate was renamed from ignite to onIgnited to allow the full project to compile; fire behaviour and all other original debug files are preserved.

P3 streaming: chunks own bounded wind/shadow/snow adapters; atmosphere and rain source caches refresh on chunk lifecycle changes. One shared clock/wind/weather persists across the finite world. Snow ground cover and pollen/haze remain camera-local. See WORLD/Runtime/Streaming/README.md for lifecycle and current biome-profile limits.

P3 stability: EnvironmentMasterConfig now owns shared cosmetic culling and preparation/retention margins. Chunk wind/wetness/snow updates suspend outside artwork bounds plus margins; re-entry synchronizes shared weather/wind/coverage. Sun shadows use their own conservative projection reach. Registration yields between adapters during streaming; no per-species update managers are added. See WORLD/Runtime/Visibility/README.md and the F7 culling comparison/timing counters. This is not suspension of gameplay or a native light-budget implementation.

Incremental scenery sources: chunk binding registers each prop with rain/atmosphere once, and explicit removal/unload/cancellation unregisters it. Full loaded-world source rescans on lifecycle changes have been removed after target-machine traces measured them as the dominant hitch. Static appearance changes refresh only the changed prop; new scenery reuses the same lifecycle. See the streaming README for the measured evidence and validation limits.

Production rain intensity zero is now a hard off state with signal-driven wakeup; dry wetness sleeps after drying. Shared ground queries and wind remain independent. Leaves use a bounded 40-slot pool; disabled leaf simulation/drawing is skipped. See module READMEs for exact limits.

## P2-A implemented — nearby cosmetic scheduling

Chunk artwork bounds are indexed; view/settings changes query nearby candidates plus currently active chunks. A safe expanded view window (SceneryVisibilitySlack, default 32 units; 0 exact) avoids repeated checks for tiny camera motion without missing prepared scenery. Retention hysteresis, new/refreshed bounds, culling toggles and camera jumps remain live. VisibilityPasses/VisibilityChunkChecks measure work; static views do not walk the chunk dictionary. Cached shared wetness removes transition-time node lookups. This changes cosmetic scheduling only, not chunk generation, physical simulation or ownership. See WORLD/Runtime/Visibility/README.md and FEEDBACK/Visibility/README.md for validation/CPU evidence.

## SO2 initial sources — automated validation passed, manual graphics pending
Initialize now receives the full actor source, persistent torch destination and explicit player reference. Initial wind/wetness/sun/snow/artwork/leaf caches use WorldActorSources.Initial across marked ownership containers while excluding chunks, which retain explicit registration. Smoke/embers use the torch's global position converted into renderer space. Effects, clocks and managers keep their original lifecycle; no frame-based rediscovery is added. See SCENES/World/SO2_Copilot_Handoff.md for deferred tests.

SO2 validation update: Codex completed the Godot 4.7.2/.NET 8 build/import and relevant headless regressions, including real old-layout disk checkpoint restoration. All passed; P4 write/read now pass 39/33 checks after adding committed-migration assertions. See SCENES/World/SO2_Validation.md. Earlier automated-pending wording is historical; rendered seam/Y-sort and target-PC performance checks remain open.
