# Shared biome data
WorldBiomeDefinition holds identity, surface and ordered populations. BiomePopulation holds placement settings inline within a biome. Content resources remain canonical under WORLD/Content. No region topology or climate generation is implemented.

`WorldBiomeDefinition.Hydrology` optionally references `BiomeHydrologyProfile`: hard freshwater lake/river permissions plus 0–1 suitability weights. Null preserves permissive weight-1 defaults. WG1 consumes lake settings only; river settings are reserved for WG3. Profiles are authoring data, never per-biome managers or copied liquid visuals. See [water settings and compatibility](../../Generation/Liquids/WG0_WG1_Validation.md).
