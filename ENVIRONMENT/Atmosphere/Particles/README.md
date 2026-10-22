# ENVIRONMENT/Atmosphere/Particles

Reusable effects such as drifting dust, floating spores, falling leaves, fireflies, ash and pollen. Biomes and weather choose which effects are active and at what intensity. Budget particle counts for dense forest and multi-layer scenes.

> **Status:** Future architecture / folder placeholder only. No new production environmental mechanics are implemented in this folder.

## Screen dust overlay — current

PollenDust.cs now owns a CanvasLayer at layer 5 and one capped MultiMeshInstance2D batch. It is separate from the world canvas, below colour grading (10) and developer UI (20). Particle motion uses only shader TIME and viewport size: walking, camera zoom/rotation, scenery, wind and day/night do not reposition or change it. Future world-anchored pollen should be a separate effect, not added as a camera input here. Legacy AtmosphereProfile.Pollen/PollenCount fields are retained for serialized profile compatibility; F9 labels them Screen dust and Screen dust budget.

The shader staggers softly fading 8-second appearances within 24-second cycles, with slow individual drift, muted warm colouring and low alpha. The budget defaults to 35 slots (cap 100), of which roughly a third are in their appearance window at any time; the slider is a cap, not an exact visible count. No per-speck nodes, physics, CPU simulation, frame callbacks or instance-buffer rebuilds are used. Viewport SizeChanged updates bounds only on resize; profile events update the count. The atmosphere owner does not process just to support screen dust. OFF/zero frees the layer/batch and disconnects resizing. Haze/leaves/torch effects keep independent world-space lifecycles.

ValidatePollenBatch now passes 26 checks, including camera/wind/daylight independence, zero/off/reactivation, bounded counts, no polling/redraws and resize/removal. Local headless Godot 4.5.1 checks do not establish visible shader appearance on target 4.7.2/D3D12; inspect the subtle drift/fades on the target PC. This replaces the earlier camera-local world-pollen presentation, not the shared GPU batching optimization.
