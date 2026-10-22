# ENVIRONMENT/Atmosphere/Particles

Reusable effects such as drifting dust, floating spores, falling leaves, fireflies, ash and pollen. Biomes and weather choose which effects are active and at what intensity. Budget particle counts for dense forest and multi-layer scenes.

> **Status:** Future architecture / folder placeholder only. No new production environmental mechanics are implemented in this folder.

## Pollen / dust batch

PollenDust.cs and PollenDust.gdshader implement ambient pollen with one MultiMeshInstance2D (100 reusable seeded quad instances; default 35 visible). The shader computes drift/flutter/shimmer with built-in TIME; colour, radius and daylight response follow the previous circles. Shared wind displacement remains integrated by WindController. No per-speck C# trajectory, node, physics, circle drawing or per-frame instance-buffer upload exists. Tiny soft circle quads replace tessellated circle submissions. Visible appearance is intended to remain subtle, but exact edge/phase is not pixel-identical.

WorldAtmosphere creates this batch only for Pollen=true and positive PollenCount, and frees it immediately at false/zero. Profile Changed events permit reactivation without idle polling. While active, the owner checks shared view/daylight/wind; unchanged inputs avoid GPU writes. Bounds follow the camera and clip to the finite world, with an explicit conservative MultiMesh AABB. Haze, leaf, smoke and ember lifecycles remain independently controlled. No automatic biome/weather scheduling is introduced.
