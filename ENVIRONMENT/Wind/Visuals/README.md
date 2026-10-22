# Vegetation response
VegetationSway.gdshader copies the tested row-weighted crown sway and shadow-only mode. Per-species material tuning stays in WORLD content. VegetationWind creates shared normal/mirrored material variants and updates only those variants. It restores authored materials when removed.

The copied shader uses a shared uniform per orientation instead of per-instance uniforms, preserving mirrored wind while avoiding the debug software renderer's instance-uniform limit. Trees/grass have no individual CPU wind processors.
