# Decorative grass
Grass.tres references four reusable Tuft scenes and their Sprites. These have no collisions. Biome population data selects counts, scale and clusters; WORLD/Generation/Placement owns the algorithm. Ground surface shading is separate.

GrassWind.tres drives shared canopy sway; content data chooses the blade snow profile and Z=-6 decorative layer. Grass does not cast individual sun/contact shadows.

2026-10-23 sprite refresh: four source PNGs from the project root (Grass01 through Grass04) replaced the four production grass_small_01 through grass_small_04 textures in place. All eight old/new grass PNGs use 128x128 canvases, so existing Tuft scene offsets, material, biome population scale and source resource paths were left unchanged. Only PNG contents changed; source PNGs were removed from root to keep canonical assets in this folder.
