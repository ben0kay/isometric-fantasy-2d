# WORLD/Generation/Biomes

**Future climate/thematic-zone algorithms, NOT implemented here.** This folder currently holds documentation only. The running P0/P5 macro biome region builder lives in [Generation/World](../World/README.md) and still selects biome sites largely by weights, region size and hydrology/geography constraints.

**Implementation handoff:** [BC0–BC7 climate/world-author plan](../../Biomes/Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) and [World Author detailed design](../Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md) cover normalized north/centre/south authoring zones, 50× finite area, region spawn choices, progressive F7, and regional weather. BC0 starts with a versioned coarse climate contract, then F7 climate preview, biome-affinity placement and eventually one shared environment response. These are separate from BP2/BP3 ore and creature spawns. [Placeholder F7 guide](../../Biomes/PLACEHOLDER_BIOME_AUTHORING.md) shows what can be authored now and why it will not form climate clusters before BC2.

Desired finite-world layout: seed **large contiguous cold, temperate, hot and enchanted provinces** first, optionally guided toward broad map areas and influenced by oceans/future relief. Then place **compatible** biome types in each province using optional soft affinity/hard constraints and explicit magical exceptions. A province may contain many biomes; a biome may recur in compatible provinces. Never fabricate missing definitions or leave valid land empty with only Oak/Pink configured.

BP0/BP1 already added authored `BiomeClimateProfile` baseline data; **no climate-zone sampling, biome suitability algorithm or snow gating currently consumes it**. Later runtime weather decisions remain with one shared ENVIRONMENT owner, not per-biome snowfall scripts. Freeze/version macro zone data on new-world generation, use cheap indexed lookups and preserve saved campaigns.

[Main plan](../../Generation_Implementation.md) · [world direction](../../../Notes/World_Generation_Direction.md) · [biome authoring](../../Biomes/README.md).
