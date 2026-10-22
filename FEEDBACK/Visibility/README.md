# Shared player occlusion fading

`OcclusionController` is created once by `WorldScene`; it tests opted-in registered world tree visuals against the player. Tree definitions expose an `OcclusionProfile` (default enabled), with minimum alpha, fade-in/out rates and visual padding. All generic trees use it with no species-specific scenes/scripts. The controller processes only active fades and checks spatial candidates periodically; there are no per-tree `_Process` loops.

The fade applies to the tree Sprite2D `Modulate.A`, preserving its existing material, wind, snow, burn/charred child visuals and node collision. On restoration/unload the original alpha is restored. Other props can opt in later by registering their visual node via `Register` or by exposing a generic occluder wrapper; the shared detection/fade controller is not tree-specific.

Occlusion is based on visual bounds and world Y depth: only tree art covering the player's approximate upper-body point while the player stands behind the tree trunk is faded. This deliberately does not fade nearby trees which aren't blocking the player. The first release is a bounded box approximation; a future foliage mask may refine irregular silhouettes.

## P2-B implementation details

Registered visual bounds (including offsets, scale/mirroring and profile padding) enter a world-scoped SpatialBoundsIndex. The shared player-position probe remains each frame; ordinary moving-player detection retains the 0.10-second cadence, with source/profile changes and large teleports able to refresh immediately. An unchanged player/source state performs no new detection query. Only nearby bounds candidates plus previously obstructing visuals are tested; the latter ensures moving/teleporting away restores old blockers. Visible-in-tree checks exclude hidden/unpublished chunk roots, and the original behind-anchor rule remains. This is still a rectangle approximation, not texture-alpha masking.

Only entries actively changing opacity are visited for interpolation; settled alpha makes no material/tint writes. Fade endpoints snap to exact target alpha. Unregister/controller exit restore the captured original alpha, disconnect events and remove spatial/fade references. Last-entry removal sleeps the controller. Profile setters emit Changed so live edits update settled blockers. Source visibility/rect/lifetime and active hit-transform events refresh only the affected bounds. Non-static opt-in owners refresh moving bounds at the shared check interval; static transforms edited externally must call Refresh(visual). No per-species scenes or per-prop idle processors are added.

Counters RegisteredCount, ActiveFadeCount, DetectionPasses, CandidateChecks and FadeWrites support validation. ValidateSpatialScheduling passed 32 checks; the fixture includes 250 distant blockers while nearby checks stay below 10, exact non-unit source alpha restoration, stationary idle, front/behind depth, hidden ancestors, profile changes, teleport, unregister/source removal and moving generic props. Existing streamed visibility/cancellation and P3 suites also pass.

CPU-only diagnostic (fixed seed, 49 loaded chunks, 360 synchronous simulated 120Hz samples per state, 16 warm-ups; moving mode shifts player/canvas through already loaded scenery without streaming):

| State | Visibility mean before / after (ms) | Occlusion mean before / after (ms) |
| --- | --- | --- |
| Stationary | 0.00350 / 0.00150 | 0.01915 / 0.00011 |
| Moving | 0.00329 / 0.00183 | 0.01797 / 0.00024 |

After p95 visibility/occlusion: stationary 0.00130/0.00020ms, moving 0.00170/0.00070ms. Measured allocations across each sample window: before 0/96 bytes, after 0/0 bytes. Host timings vary; these are direct C# CPU callback measurements, not engine/GPU/whole-frame FPS. Godot 4.5.1/.NET 8 headless was available; target 4.7.2/D3D12 visuals and actual FPS remain unverified. Keep existing sun/native-light/shader rendering budgets distinct from cosmetic scheduling.
