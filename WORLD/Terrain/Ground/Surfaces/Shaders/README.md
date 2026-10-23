# Ground shaders
GrassSurface composes shared noise sampling, broad colour patches and fine detail. Extracted from the debug west-side surface, independent of two-biome blending and environment effects.

GS2/GS3: StreamedGround retains the old base math and conditionally includes bounded LayeredSurface helpers for two overlays and actual artwork transitions. No per-frame CPU updates. Default no-overlay output matched pre-GS2 software-GL inputs; Godot/Windows validation is pending. See ../GS2_GS3_Validation.md.
