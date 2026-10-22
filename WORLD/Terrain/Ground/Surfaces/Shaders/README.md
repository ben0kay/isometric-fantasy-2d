# Surface Shaders

> **Status:** Empty production destination; no shaders copied yet.

Future home for reusable ground-surface shader modules (world-space noise sampling, organic palette patches and fine detail) and, where appropriate, distinct compositing shaders for grass, sand, mud or stone.

Initial DEBUG references: `DEBUG/TopDownTest/Ground/Shaders/GrassSurface.gdshader`, `PatchColours.gdshaderinc`, `FineDetail.gdshaderinc`, and `NoiseSampling.gdshaderinc`.

Shader helpers should be shared where visually appropriate; biome-specific colours/textures belong in Resources or parameters, not duplicated species/biome-named shader files. Cross-terrain edge blending belongs in `WORLD/Terrain/Transitions/`.
