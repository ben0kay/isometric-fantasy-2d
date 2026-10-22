# Shared ground surfaces
Implemented: GroundSurface applies a GroundSurfaceProfile once, tiles the original ground artwork and configures one private material per ground region. Profiles supply palette, variation and fine detail. Noise and shaders are shared. No per-frame C# material updates.

The shader copies the DEBUG Oak Forest west-side patch, noise, authored texture and fine-detail calculations, removing only two-biome blending. It does not reproduce optional debug lighting, weather or polish. Definitions can reuse ForestGrass.tres or add another profile without copying the shaders.
