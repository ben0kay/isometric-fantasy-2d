# Shared ground surfaces
Implemented: GroundSurface applies a GroundSurfaceProfile once, tiles the original ground artwork and configures one private material per ground region. Profiles supply palette, variation and fine detail. Noise and shaders are shared. No per-frame C# material updates.

The shader copies the DEBUG Oak Forest west-side patch, noise, authored texture and fine-detail calculations, removing only two-biome blending. It does not reproduce optional debug lighting, weather or polish. Definitions can reuse ForestGrass.tres or add another profile without copying the shaders.

## Adjacent surfaces
GroundSurface now accepts AdjacentBiomeLayout and binds both palette/detail profiles plus the shared boundary. ForestGrass.tres and PinkMeadowGrass.tres share the existing shader/material/noise/art; settings blend on the GPU. This supports the two current grass-family surfaces; unrelated materials/textures are not forced into this composition.
