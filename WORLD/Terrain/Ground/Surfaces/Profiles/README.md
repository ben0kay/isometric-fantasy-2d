# Surface profiles
GroundSurfaceProfile owns texture, material, palette and surface detail values. ForestGrass.tres matches the DEBUG Oak Forest palette. Biomes reference profiles; shader code has no species/biome identity.

PinkMeadowGrass.tres preserves the debug pink palette with the same shared ground artwork/material/noise. Both profiles can tune palette, patch scale, variation and fine-detail strength independently.

GS3 profiles can choose Organic (existing default), Granular or Mineral detail plus ArtworkInfluence (existing default 0.22). The streamed renderer supports shared data composition, not arbitrary shader code. Existing profiles/resources are unchanged; sand/soil fixtures live only under TESTS/Terrain.

Clarified base contract: explicit stacks read only BaseColour/artwork/detail/tint from the base profile. AccompanyingPatches owns the grass variation palette and distribution separately. Old profile patch fields are grouped as extra-surface/legacy defaults and do not silently populate an explicit base. See ../ACCOMPANYING_PATCH_LAYER.md.

Recipe applies to extra layers: Artwork (default) requires Texture; Dust/Basalt require no artwork and use BaseColour/FreshColour as low/high procedural tones plus MaterialFeatureSize. The canonical Dirt/Rock profiles now use these old-project recipes. Base ground remains artwork-based. See ../PROCEDURAL_GROUND_RECIPES.md.
