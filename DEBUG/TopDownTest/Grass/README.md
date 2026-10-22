# Decorative grass patches — disposable top-down visual test

Four updated transparent 128×128 grass sprites (`Grass_Small01.png` through `Grass_Small04.png`) have replaced the original four grass variants. They are now stored under `DEBUG/TopDownTest/Artwork/` as `grass_small_01.png` through `grass_small_04.png` and referenced by `GrassPatches.tscn`. The old `grass_01.png`–`grass_04.png` assets have been deleted. The PNG contents were moved unchanged from the repository root.

## Generation

GrassPatches.tscn is a modular scene referenced by the main test **after Actors**. It creates 36 scattered clusters of 3–7 clumps each, plus 30 solitary clumps (roughly 210 requested, fewer if constrained). Tufts are sampled from circular distributions with slight central density, seeded from the same world seed with a separate offset, and avoid the main path, start clearing and physical tree/rock bases. Each clump selects one of four artwork variants, a subtle scale (0.26–0.42), brightness (0.88–1.0) and optional mirroring. No collisions, physics, individual shadows or per-clump C# ticks; clumps draw above terrain and below actors.

The **placement, scale, collision-free behavior, and shared TreeSway wind material are unchanged**. Both the original and replacement textures are 128×128, so existing ground anchoring and variation settings remain compatible.

## Wind

GrassWind.tres reuses the exact TreeSway.gdshader used by trees, but with gentler grass-only strength_pixels=4.5, speed=1.3 and canopy_end=0.88, so the lower stalks remain rooted. Per-placement shader phase prevents synchronized sway, and mirrored sprites correct wind orientation. One **shared ShaderMaterial** follows Weather.WindStrength (-3 left, 0 still, +3 right); no duplicate wind controller or script shader implementation.

## Editing and testing

DefaultGrassPatchConfig.tres owns count, spacing, radius, seed-independent variation and scale values. Change them and restart the scene to regenerate. Adjust GrassWind.tres for grass-only animation; Weather's existing slider responds live. The original tree/rock scatter and Ground/ organic terrain shader are left as-is. In the editor, grass appears only during play, not in the stored scene. Godot/.NET wasn't available for compilation or a full-scene visual playback test here.
