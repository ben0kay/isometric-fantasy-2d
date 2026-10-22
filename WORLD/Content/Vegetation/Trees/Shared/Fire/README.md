# Shared tree fire appearance

TreeFireProfile supplies charred artwork, source-pixel anchors and canopy sampling settings. TreeFireAppearance listens to the generic Burnable lifecycle; the body and TrunkFootprint never change. FireErosionAdapter composes erosion with the live wind/snow material and restores its source. CanopyFireEmissionMask caches bounded alpha-derived GPU emission points per artwork/transform/profile.

Oak supplies OakFireProfile.tres. Other species opt in with their own profile, Burnable and FireAppearance nodes; do not assign oak artwork to every tree. A missing/unsupported shader uses the FIRE1 crossfade fallback.
