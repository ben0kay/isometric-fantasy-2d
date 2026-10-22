# Vegetation response
VegetationSway.gdshader copies the tested row-weighted crown sway and shadow-only mode. Per-species material tuning stays in WORLD content. VegetationWind creates shared normal/mirrored material variants and updates only those variants. It restores authored materials when removed.

The copied shader uses a shared uniform per orientation instead of per-instance uniforms, preserving mirrored wind while avoiding the debug software renderer's instance-uniform limit. Trees/grass have no individual CPU wind processors.

## P1-A changed-only uploads

VegetationWind remembers the last applied shared Wind.Strength. Each active chunk/group callback performs one strength comparison; unchanged strength skips the material loop and every shader upload. Attach invalidates the cache so newly created materials always initialize, even at unchanged global strength. Existing off-screen SetProcess(false) culling remains; explicit re-entry _Process(0) applies changed wind before display. Shader TIME continues crown/grass sway between uploads, so static strength does not freeze animation. Shared wind/wetness/snow followers retain their separate existing ownership. No per-tree processes or new scans were added.

MaterialWrites counters support ValidateCloudWindCaching: stable-frame writes stay unchanged; changed wind/attach/re-entry apply; unload restores authored materials. The remaining active per-group comparison is deliberate, not zero total CPU cost. No FPS gain is claimed without target-PC profiling.
