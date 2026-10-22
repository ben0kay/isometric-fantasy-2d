# Production environment
Implemented for `SCENES/World/World.tscn`: shared clock, signed wind, day/night illumination, projected/contact shadows, vegetation sway, cloud shade, colour grade, local torch light, bounded atmosphere particles, rain/ground splashes/lightning and removable snow coverage/snowfall.

`EnvironmentRuntime.cs` only composes modules after production scenery is generated. `EnvironmentMasterConfig` references one ClockMasterConfig and a default BiomeEnvironmentProfile. Production modules never create sliders/panels or depend on DEBUG. The optional controls live in `DEBUG/UI/Environment/` and can be removed from World.tscn.

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

## Defaults and limits
Preserves the debug settings: 30 real seconds per day, 08:00 start, wind +1, rain target 55%, snow off, coverage target 60%. Mixed rain/snow is permitted; set rain to zero for pure snow. Existing effects are visual approximations, not snow physics or fire/heat simulation. The torch's authored collision remains while its presentation is disabled, matching the original test.

Generation seed semantics and F5 debug launch are unchanged. No lake/wading, axe, enemies, burning, saves or chunk streaming are extracted. Content/lighting caches assume the current startup-generated world; dynamic spawn/despawn/streaming registration is not implemented.

## Validation
C# build uses the repository's Godot.NET.Sdk 4.7.2 and .NET 8. Godot 4.5.1 .NET runtime assertions cover biome override/default resolution, runtime isolation, one clock/wind, no-panel startup/removal, wind changes, day/night, rain/overcast, manual lightning, snow on/off/removal, automatic accumulation/melt, player burial/camera stability, environment removal and movement. Native 1920×1080 software OpenGL Compatibility renders were inspected for controls, day/night, rain/lightning, 25/60/100% snow and restoration at full production scenery density. Windows D3D12 and target hardware performance remain untested.

Integration note: the concurrent FIRE2 commit contained a C# CS0136 local-name collision in DEBUG/TopDownTest/Fire/Debug/FireTestController.cs. The ignition event delegate was renamed from ignite to onIgnited to allow the full project to compile; fire behaviour and all other original debug files are preserved.

P3 streaming: chunks own bounded wind/shadow/snow adapters; atmosphere and rain source caches refresh on chunk lifecycle changes. One shared clock/wind/weather persists across the finite world. Snow ground cover and pollen/haze remain camera-local. See WORLD/Runtime/Streaming/README.md for lifecycle and current biome-profile limits.

P3 stability: EnvironmentMasterConfig now owns shared cosmetic culling and preparation/retention margins. Chunk wind/wetness/snow updates suspend outside artwork bounds plus margins; re-entry synchronizes shared weather/wind/coverage. Sun shadows use their own conservative projection reach. Registration yields between adapters during streaming; no per-species update managers are added. See WORLD/Runtime/Visibility/README.md and the F7 culling comparison/timing counters. This is not suspension of gameplay or a native light-budget implementation.

Incremental scenery sources: chunk binding registers each prop with rain/atmosphere once, and explicit removal/unload/cancellation unregisters it. Full loaded-world source rescans on lifecycle changes have been removed after target-machine traces measured them as the dominant hitch. Static appearance changes refresh only the changed prop; new scenery reuses the same lifecycle. See the streaming README for the measured evidence and validation limits.
