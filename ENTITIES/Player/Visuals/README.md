# Player / Visuals

Reusable presentation code lives here. All disposable prototype SVGs and the test Player scene live in `DEBUG/VisualTests/` so this code has no dependency on temporary art.

`PlayerVisual.cs` aligns the Visuals child once to the locked camera's yaw. The Sprite3D stays upright (no pitch), and its bottom edge sits at the body's feet. The body does not rotate. Replace this child with a model later without changing input or movement.

`DEBUG/VisualTests/Artwork/player_placeholder.svg` is an original 48×72 explorer. Godot rasterizes SVG during import at its declared dimensions; it is not a live vector mesh. Nearest texture filtering preserves crisp pixels. Configured pixel size 0.025 gives a 1.8m-tall card; PlayerMasterConfig controls scale. Boots touch the image's bottom edge.

The sprite uses lighting, double-sided rendering, alpha scissor (0.5) and double-sided shadow casting. Transparent background should not cast a rectangle. These settings need visual verification in Godot; no fake blob shadow has been added. Solid scenery can occlude the player; cutaway/fading is future work. Only one static frame exists; Source3D and Animations remain reserved for the later Blender-to-sprite workflow.
