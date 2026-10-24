# Shared ground surfaces
Implemented: GroundSurface applies a GroundSurfaceProfile once, tiles the original ground artwork and configures one private material per ground region. Profiles supply palette, variation and fine detail. Noise and shaders are shared. No per-frame C# material updates.

The shader copies the DEBUG Oak Forest west-side patch, noise, authored texture and fine-detail calculations, removing only two-biome blending. It does not reproduce optional debug lighting, weather or polish. Definitions can reuse ForestGrass.tres or add another profile without copying the shaders.

## Adjacent surfaces
GroundSurface now accepts AdjacentBiomeLayout and binds both palette/detail profiles plus the shared boundary. ForestGrass.tres and PinkMeadowGrass.tres share the existing shader/material/noise/art; settings blend on the GPU. This supports the two current grass-family surfaces; unrelated materials/textures are not forced into this composition.

GS1: Oak/Pink now author inline GroundStack Resources with a clearly labelled required base and empty optional overlays. Surface remains a compatibility adapter; renderers are unchanged. See [GS0/GS1 validation](GS0_GS1_Validation.md).

Pre-GS2 tint addition: biome-owned base Tint now works in streamed and preview rendering, defaults to white, and leaves source profiles untouched. See WORLD/Terrain/Ground/Surfaces/TINT_Validation.md for behavior and pending validation.

GS2/GS3: streamed rendering now compiles GroundStack directly and supports two opt-in ordered organic overlays, distinct-art biome blending and reusable detail styles. Production Oak/Pink remain base-only. Preview retains its original base-only path. See [validation and practice fixture](GS2_GS3_Validation.md); engine/Windows/performance acceptance pending.

GS4: F9 Ground Surfaces now offers session-only base/layer toggles, global coverage/opacity multipliers and reset. GroundSurfacePreview writes only changed uniforms, with no map rebuilding or idle CPU callbacks. See [combined local testing checklist](GS4_Validation.md); previous phase runtime/visual acceptance remains pending.

Current clarified structure: full-coverage base + a separately authored accompanying patch layer + up to two extra surface patches. Oak/Pink grass variation now lives in AccompanyingPatches and preserves the original look when enabled. F9 exposes that distinction; no dirt/rock added. [Contract, save bridge and local checks](ACCOMPANYING_PATCH_LAYER.md).

GS5 now supplies default-off, F9-previewable Oak Dirt/Rock extras, plus strength sliders for base detail/accompanying grass. Pink and existing base profiles are unchanged. See [preview instructions and pending checks](GS5_Validation.md).

Current Dirt/Rock use procedural Dust/Basalt recipes adapted from the old isometric game. They do not use SVG artwork; base grass still does. See [source, behavior and validation](PROCEDURAL_GROUND_RECIPES.md).

Current GS5 Dirt uses native 256×256 grayscale PNG artwork with biome-owned tint and unchanged Dust patch masks; Rock remains Basalt. See [authoring and validation](GS5_PNG_DIRT.md).
