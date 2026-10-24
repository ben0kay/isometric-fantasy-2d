# Liquid regions

Implemented: WorldLiquidRegion owns cached polygon/distance queries and pond-style liquid presentation. The authored pond builds its established 96-point local outline; WG2 supplied PlannedWaterBody geometry uses the full immutable world polygon and chunk-cropped fields. Both share liquid definitions, shaders, rain and wading contracts. Regions do not generate new lake locations or carve terrain. See [WG2](../WG2_Validation.md) and [runtime lifecycle](../Runtime/README.md).
