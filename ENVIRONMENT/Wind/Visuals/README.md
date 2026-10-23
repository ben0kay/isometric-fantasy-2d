# Vegetation response
VegetationSway.gdshader copies the tested row-weighted crown sway and shadow-only mode. Per-species material tuning stays in WORLD content. VegetationWind creates shared normal/mirrored material variants and updates only those variants. It restores authored materials when removed.

The copied shader uses a shared uniform per orientation instead of per-instance uniforms, preserving mirrored wind while avoiding the debug software renderer's instance-uniform limit. Trees/grass have no individual CPU wind processors.

## P1-A changed-only uploads

VegetationWind remembers the last applied shared Wind.Strength. Each active chunk/group callback performs one strength comparison; unchanged strength skips the material loop and every shader upload. Attach invalidates the cache so newly created materials always initialize, even at unchanged global strength. Existing off-screen SetProcess(false) culling remains; explicit re-entry _Process(0) applies changed wind before display. Shader TIME continues crown/grass sway between uploads, so static strength does not freeze animation. Shared wind/wetness/snow followers retain their separate existing ownership. No per-tree processes or new scans were added.

MaterialWrites counters support ValidateCloudWindCaching: stable-frame writes stay unchanged; changed wind/attach/re-entry apply; unload restores authored materials. The remaining active per-group comparison is deliberate, not zero total CPU cost. No FPS gain is claimed without target-PC profiling.


## Resolution-independent sway and clipping margins — October 2026

Production VegetationSway moves canopy samples by `strength_world_pixels * Wind.Strength` in **world units**, converting through `MODEL_MATRIX` scale and `TEXTURE_PIXEL_SIZE`. Camera zoom does not change the world displacement. A 222px oak and a 1000px oak rendered at the same size sway equally when their authored world strength matches; changing PNG resolution no longer amplifies wind.

The vertex stage extends the Sprite2D quad by the *current* maximum horizontal wind displacement, remapping UVs beyond 0..1 so the extra pixels draw transparent until foliage sways into them. This fixes sharp edge cutoff without permanently padding/upsizing source PNGs. The draw margin collapses when wind is zero, minimizing calm overdraw; mirrored (FlipH) material variants expand outward on the correct sides. The shader still uses one shared material per orientation/profile, not individual CPU update loops.

The renamed shader setting is `strength_world_pixels` (not `strength_pixels`), tuned in per-species/grass `.tres` Resources; shipped defaults preserve approximately the old displayed sway rather than the new oak PNG's accidentally amplified sway: oak 1.61, pink blossom 2.55, grass 1.5 world units at wind strength 1. Negative wind strength reverses motion; global -3..3 remains untouched. Existing wetness/snow/fire shader composition relies on the unchanged `sample_uv` variable and final `COLOR = pixel * sprite_tint * inside;` statement. Shadow-only uses the same expanded, swaying geometry.

**Validation:** build/import and visually test max wind -3 and +3, calm 0, mirrored oak, grass, pink blossom, snow/rain and projected shadows in F6 World. Confirm no rectangular clipping or visible edge stretching, including camera zoom and forest chunks. The expanded quad increases overdraw only when wind is nonzero; benchmark storms, not just idle FPS. Godot's item-rect culling may not account for vertex shader margins near viewport edges, so inspect screen boundaries too. No rendered FPS measurement is claimed here.
