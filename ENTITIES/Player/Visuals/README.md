# Player / Visuals

Presentation remains separate from 3D movement and collision. `PlayerVisual.cs` aligns the visual card once with the fixed camera yaw and positions its bottom at the ground. Later, visuals may be exchanged for a 3D model without rewriting movement.

**Current art:** `Sprites/player.png` — a transparent, static full-body pixel-inspired fantasy explorer image. The test `DEBUG/VisualTests/Player.tscn` loads this shared image as a `Sprite3D`. The `ENTITIES/Player/DefaultPlayerConfig.tres` sets `SpritePixelSize = 0.00124` for its 1448-pixel height (~1.8 world units). Resizing the art requires retuning pixel size.

Alpha-scissor, nearest texture filtering and double-sided shadow casting are configured in the test scene. Inspect silhouette, transparency, scale and shadowing in Godot: these PNGs are high-resolution illustrations and may require future resolution/style adjustments for a consistently chunky pixel-art aesthetic. Animation sheets and Blender source files remain future work.
