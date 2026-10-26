# World Author — authoring contracts and planned editor

**Status: planning only.** No runtime code, Inspector authoring Resources or dedicated World Author UI exists here yet. [Full implementation plan](WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) owns the agreed North-cold / central-ocean / South-hot / 50×-area / regional spawn / F7 roadmap.

**Ownership:** extend the existing `WORLD/Generation/World/WorldGenerationMasterConfig` and `WorldLayoutPreset` / water/geography contract rather than inventing a second generator. Future small typed Resource(s) under this folder can describe broad normalized geography/climate intent; **real plan generation** stays in WORLD/Generation. Per-biome climate affinity stays in WORLD/Biomes. Runtime snow/rain belongs to ENVIRONMENT. The optional F7 viewer and profiler belong to `DEBUG/UI/Generation`.

**Current behavior:** whole-world *coarse* P5 macro water/biome/feature planning runs at new-world startup; P3 builds only nearby 512-unit detailed chunks. Old campaigns restore frozen geography. F7 displays a static macro image but currently has no zoom/progressive preview. The 50×-area target and North/Middle/South spawn choice are future work, not current production settings.

Follow root [CodePreferences](../../../CodePreferences.md); prefer small phase deliverables and default brief validation.
