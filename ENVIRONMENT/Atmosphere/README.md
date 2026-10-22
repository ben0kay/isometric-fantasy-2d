# Atmosphere
WorldAtmosphere owns an independent GPU screen-dust layer and bounded leaf/smoke/ember pools and reads shared daylight/wind. Fog/ holds haze shading; AtmosphereProfiles/ holds data, shared noise and organic sampling; AmbientEffects/ owns independent CloudShade.

AtmosphereProfile tunes switches, haze palette/density/scale and capped particle counts. Leaf emitters are cached from content metadata instead of tree names. Torch emissions consume the actual local fixture. Oak Forest supplies a local Atmosphere profile; other biomes can reuse these renderers. No UI lives here.


Ambient falling leaves cache the configured generic tree and hit receiver, using TreeDefinition.FoliageColour for the same colour as axe-hit bursts. HasFoliage=false and burning/burnt leaf suppression apply to ambient leaves too. No per-tree leaf colour Resource or per-frame tree search is needed.

P3 source-cache fix: streamed props register/unregister through EnvironmentRuntime.RegisterScenery/UnregisterScenery. Trees keep a dense cached source list with indexed swap removal; registration caches hit receiver, world position and species colour. Startup handles persistent/preview roots only. Chunk completion no longer runs FindChildren over the loaded world. Leaves reject off-view or unpublished sources before drawing; source positions use the renderer's local coordinates. Tree appearance changes refresh one source. Static scenery that moves or changes authored foliage data must explicitly refresh its source registration.

Ambient falling leaves use at most **40 reused CPU particle slots for the entire atmosphere renderer**, not 40 per tree or separate leaf nodes/physics bodies. Profile LeafCount and wind determine how many are drawn. Sources use TreeDefinition.FoliageColour; HasFoliage=false, hidden/unpublished/off-view sources and burning/burnt leaf suppression prevent emission. The particles are small drawn polygons. The shared atmosphere loop still serves haze/pollen/torch effects, but disabling Leaves or setting LeafCount=0 now skips leaf drift integration and leaf-wind smoothing as well as drawing. Disabled/zero-count smoke and embers likewise skip their respective integration loops. This bounds work; it is not a claim that all atmosphere drawing is free or GPU-batched.

## P0-0 pollen/dust performance pass (historical camera-local presentation)

Pollen now uses Particles/PollenDust: one bounded instanced GPU-drawn quad batch, not a DrawCircle call plus trigonometry for each speck every frame. The count remains default 35 / maximum 100 globally. Pollen=false or count zero removes the batch and all pollen-only work. Re-enabling through F9/Inspector profile setters recreates it directly. The active helper has no _Process; WorldAtmosphere passes only changing shared inputs. A pollen-only scene does not request immediate-mode redraws; shader TIME drives its animation.

The atmosphere also caches haze polygon bounds/static settings and only writes changing daylight/wind. Haze disabled/density zero skips its updates. Parent redraws occur only for enabled leaf/visible torch smoke/ember animation, plus one clear when those stop. All effects off sleeps WorldAtmosphere; profile changes, first/last foliage source registration and torch visibility events wake/re-evaluate it. The leaf trajectories, source pool, colours and fire suppression are unchanged. A shared active callback still checks view/wind/daylight and integrates enabled other effects; this is not a claim all atmosphere costs disappear while pollen alone is disabled.

Validation: ValidatePollenBatch passed 24 checks for bounded count, default/zero/off/re-enable, unchanged-input caching, no per-speck nodes/process/redraws, synthetic camera canvas bounds, wind/daylight, independent haze/leaves/smoke/embers and torch visibility wake. BenchmarkPollen measures CPU callback submission with other atmosphere effects disabled on a fixed-seed world; it does not time GPU work. Local runtime is Godot 4.5.1/.NET 8 rather than target 4.7.2/D3D12. Target-PC graphical look, GPU timings and the reported 30–40 FPS symptom remain to be checked; no recovered FPS amount is claimed.

Local isolated CPU samples (180 frames per state after warm-up; Godot headless/dummy renderer, fixed seed, no other atmosphere effects; different runs can vary):

| Pollen count | Before: Draw mean / p95 (ms) | Before: Process mean (ms) | After: Draw callbacks | After: Process mean (ms) |
| --- | --- | --- | --- | --- |
| 35 | 0.05282 / 0.11346 | 0.01907 | 0 / 180 frames | 0.00540 |
| 0 | 0.00050 / 0.00079 | 0.02549 | 0 / 180 frames | 0.00000 |
| 100 | 0.30289 / 1.51699 | 0.05666 | 0 / 180 frames | 0.00472 |
| 0, repeated | 0.00056 / 0.00105 | 0.03134 | 0 / 180 frames | 0.00000 |

These are CPU callback timings, not total-frame/GPU timings. They demonstrate removed submission/redraw work but do **not** explain or prove recovery of the user's entire 30–40 FPS delta. The new GPU batch has rendering cost outside these scopes. Repeat F9 pollen ON/OFF at the same location/zoom on the target PC and compare frame milliseconds and renderer profiling.

## Screen-space dust follow-up — current presentation

The user chose subtle screen dust rather than world pollen. Particles/PollenDust is now a CanvasLayer with independent seeded shader drift and sparse fades. It has no world/camera/wind/daylight inputs. Legacy Pollen/PollenCount profile fields remain compatible; F9 now shows Screen dust and Screen dust budget. Dust alone requires no owner/helper frame callback, unlike the historical camera-local implementation above. Viewport resize and profile events are its only CPU updates; OFF/zero removes it. Future scenery/world pollen can be another independent module. See Particles/README.md for timing/budget and 26-check validation. Earlier CPU tables describe the previous batching pass, not this new presentation or target-PC FPS.
