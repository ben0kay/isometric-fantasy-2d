# Overworld biomes
Implemented: `Definitions/OakForest/Biome.tres` is the only production biome. All individual biome folders live under Definitions; Shared owns common Resource types. Cave biomes remain separate.

WorldBiomeDefinition references a ground surface profile and an ordered array of inline BiomePopulation resources. Each population references canonical content and stores count, scale, mirroring and optional cluster settings. Populate these in the Inspector; no separate spawn file per species per biome is needed.

Future Avalonia tooling can edit stable IDs and these Resource references/settings. Toolbox integration is not implemented. Never duplicate content assets under a biome or hardcode a species into placement code.
