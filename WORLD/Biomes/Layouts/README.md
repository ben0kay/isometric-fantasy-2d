# Small world layouts
OakAndMeadow.tres references the primary Oak Forest and secondary Pink Meadow definitions, with the copied curved boundary and blend settings. WorldMasterConfig.Biomes selects this layout. AdjacentBiomeLayout supports one primary region and an optional second region; At(worldPosition) queries membership without loading actors.

This is a deliberately small composition for previewing two contrasting biomes, not the future finite/infinite world generator. Bounds stay in world config, separate from biome definitions and boundary data. Primary/secondary sides, curve and palette blending are seed-independent; scenery placement uses the chosen world seed.

Integration: the concurrently added production pond is retained, including placement exclusions and wading. Its copied liquid classes were given unique WorldLiquid* names (and references updated) because the incoming global names collided with the preserved DEBUG liquid classes and prevented compilation. No original debug files were changed.
