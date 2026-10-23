# Shared ground surfaces
Implemented: GroundSurface applies a GroundSurfaceProfile once, tiles the original ground artwork and configures one private material per ground region. Profiles supply palette, variation and fine detail. Noise and shaders are shared. No per-frame C# material updates.

The shader copies the DEBUG Oak Forest west-side patch, noise, authored texture and fine-detail calculations, removing only two-biome blending. It does not reproduce optional debug lighting, weather or polish. Definitions can reuse ForestGrass.tres or add another profile without copying the shaders.

## Adjacent surfaces
GroundSurface now accepts AdjacentBiomeLayout and binds both palette/detail profiles plus the shared boundary. ForestGrass.tres and PinkMeadowGrass.tres share the existing shader/material/noise/art; settings blend on the GPU. This supports the two current grass-family surfaces; unrelated materials/textures are not forced into this composition.

GS1: Oak/Pink now author inline GroundStack Resources with a clearly labelled required base and empty optional overlays. Surface remains a compatibility adapter; renderers are unchanged. See [GS0/GS1 validation](GS0_GS1_Validation.md).

Pre-GS2 tint addition: biome-owned base Tint now works in streamed and preview rendering, defaults to white, and leaves source profiles untouched. See WORLD/Terrain/Ground/Surfaces/TINT_Validation.md for behavior and pending validation.

GS2/GS3: streamed rendering now compiles GroundStack directly and supports two opt-in ordered organic overlays, distinct-art biome blending and reusable detail styles. Production Oak/Pink remain base-only. Preview retains its original base-only path. See [validation and practice fixture](GS2_GS3_Validation.md); engine/Windows/performance acceptance pending.
