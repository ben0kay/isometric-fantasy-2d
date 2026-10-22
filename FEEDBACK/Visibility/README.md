# Shared player occlusion fading

`OcclusionController` is created once by `WorldScene`; it tests opted-in registered world tree visuals against the player. Tree definitions expose an `OcclusionProfile` (default enabled), with minimum alpha, fade-in/out rates and visual padding. All generic trees use it with no species-specific scenes/scripts. The controller processes only active fades and checks spatial candidates periodically; there are no per-tree `_Process` loops.

The fade applies to the tree Sprite2D `Modulate.A`, preserving its existing material, wind, snow, burn/charred child visuals and node collision. On restoration/unload the original alpha is restored. Other props can opt in later by registering their visual node via `Register` or by exposing a generic occluder wrapper; the shared detection/fade controller is not tree-specific.

Occlusion is based on visual bounds and world Y depth: only tree art covering the player's approximate upper-body point while the player stands behind the tree trunk is faded. This deliberately does not fade nearby trees which aren't blocking the player. The first release is a bounded box approximation; a future foliage mask may refine irregular silhouettes.
