# Shared tree fire appearance

TreeFireProfile supplies charred artwork, source-pixel anchors and canopy sampling settings. TreeFireAppearance listens to the generic Burnable lifecycle; the body and TrunkFootprint never change. FireErosionAdapter composes erosion with the live wind/snow material and restores its source. CanopyFireEmissionMask caches bounded alpha-derived GPU emission points per artwork/transform/profile.

TreeDefinition configures the generic template automatically. TreeVisualVariant resolves the healthy texture/anchor per selected artwork; species/variant fire profiles provide optional charred art and canopy settings. Oak has charred art; Pink Blossom burns without a replacement image and keeps its original texture. Healthy axe foliage is suppressed for either species while burning/burnt and restored on reset. Erosion only attaches when replacement artwork exists. A missing/unsupported shader uses the FIRE1 crossfade fallback when charred art is available.
