# Overworld biomes
Implemented: `Definitions/OakForest/Biome.tres` and `Definitions/PinkMeadow/Biome.tres` are the two production biomes. All individual biome folders live under Definitions; Shared owns common Resource types. Cave biomes remain separate.

WorldBiomeDefinition references a ground surface profile and an ordered array of inline BiomePopulation resources. Each population references canonical content and stores count, scale, mirroring and optional cluster settings. Populate these in the Inspector; no separate spawn file per species per biome is needed.

Future Avalonia tooling can edit stable IDs and these Resource references/settings. Toolbox integration is not implemented. Never duplicate content assets under a biome or hardcode a species into placement code.

Environment: WorldBiomeDefinition.Environment optionally references BiomeEnvironmentProfile. Null components inherit global defaults; selected components are cloned at runtime. OakForest/Environment.tres references a local atmosphere profile; lighting/wind/weather/snow/grade/cloud/torch can also be overridden through the same data contract. The current shared world environment resolves the primary biome profile at startup; per-region environment switching/blending is not yet implemented.

Layouts/OakAndMeadow.tres selects the two definitions and the curved boundary. Biome art/content remain in canonical WORLD folders. World bounds remain in WorldMasterConfig.

P1 macro generation: each definition now includes enabled/weight/target-diameter/overview-colour settings. `WorldMasterConfig.Generation.Biomes` is the arbitrary-size source collection for the new finite map. The old Layout remains only for the playable preview. See [macro generation](../Generation/World/README.md).

P3: streamed populations use explicit InstancesPerMillionUnits; Count/cluster settings retain preview-only semantics. Scale/mirror/variant settings remain shared. All enabled biome definitions supply palettes, textures and local scenery to arbitrary regions. Automatic regional environment-profile blending remains future work; see WORLD/Runtime/Streaming/README.md.
