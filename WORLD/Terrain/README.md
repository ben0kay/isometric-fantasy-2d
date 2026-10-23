# WORLD/Terrain

Production ground surface profiles/shaders and streamed palette rendering live under Ground/Surfaces; transitions live under Transitions. Cliffs/elevation remain scaffolding. Terrain generation algorithms belong under WORLD/Generation/Terrain.

GS1 adds explicit biome ground stacks without renderer changes. See [audit and validation](Ground/Surfaces/GS0_GS1_Validation.md) and [phase plan](Ground/Surfaces/LAYERED_GROUND_IMPLEMENTATION.md). Graphical/performance baseline and GS1 local validation remain pending.

GS2/GS3 direct ground-stack rendering and two optional overlays are implemented, with original production appearance retained by default. See [evidence and pending checks](Ground/Surfaces/GS2_GS3_Validation.md).
