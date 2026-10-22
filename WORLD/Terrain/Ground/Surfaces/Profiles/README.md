# Terrain Surface Profiles

> **Status:** Empty production destination; no `TerrainSurfaceProfile` class or `.tres` assets created yet.

Future Godot Resource profiles should hold **data** describing an appearance: optional base texture/material type, base/shade/accent/dry palette colours, organic patch size and strength, fine-detail strength, optional noise reference and any surface-specific parameters.

A biome (`WORLD/Biomes/`) can reference a shared surface profile or choose its own profile. Profiles should not contain spawn logic, terrain generation or hard-coded biome names in reusable renderer code.

**Initial reference:** the current Oak Forest palette in `DEBUG/TopDownTest/Biomes/OakForest.tres` and ground appearance settings in `DEBUG/TopDownTest/Ground/DefaultGroundConfig.tres`. First promotion goal is to preserve the existing Oak Forest appearance exactly; avoid speculative extra features.
