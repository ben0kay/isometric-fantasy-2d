# GS2 / GS3 — implementation and local validation

## Status

GS2 direct stack compilation and GS3 two-slot streamed overlays implemented. Graphical acceptance and target-PC profiling are pending. GS0 Windows reference capture/performance baseline remains pending; no user sign-off is implied. The author explicitly requested GS2 and GS3 together; original appearances stay the production defaults. GS4 F9 controls were subsequently implemented; see GS4_Validation.md. GS5 authored Oak overlays remain unimplemented.

Baseline: remote `cb9dfaf7e77b9ac17e43a916aeeb8942356fe38e` (pre-GS2 tint). Production biome .tres, existing surface .tres, texture/noise assets, generation, world IDs, streamer lifecycle, collision and save formats are unchanged. No save bypass or automatic migration was added. Authored resource edits still invoke existing campaign compatibility policy.

## What changed

- StreamedGroundPalette resolves each GroundStack once into indexed setup data. Stack-equipped biomes no longer go through WorldBiomeDefinition.Surface; legacy resources retain a validated fallback. Old preview remains deliberately separate and base-only, including existing tint. GS3 overlays are supported in streamed mode only.
- Five original appearance maps, 32px nearest artwork, original palette/patch/grain equations and 0.22 artwork influence remain the default. A frozen pre-GS2 setup reference and shader exist only under TESTS/Terrain.
- Up to two overlays per biome use existing Resource fields: profile, label/ID, enabled, tint, coverage, opacity, patch size, edge softness, warp and integer SeedSalt. Enabled default remains false; original Oak/Pink arrays remain empty. Base always fills 100%.
- Independent masks use fixed shared noise, world coordinates, stable salt and slot offsets. SeedSalt changes mask placement; world seed continues to control geography, while visual noise keeps its existing fixed seed. Coverage is a threshold bias, not a guarantee of exact covered area. Zero coverage/opacity and disabled layers skip rendering. Full coverage is explicitly supported.
- Overlays compose A then B within each contributing biome; results blend using the same bilinear macro-cell weights as the base. Actual base artwork blends across different-art biome boundaries. Artwork is premultiplied while blending to avoid transparent RGB fringes; atlas lookup remains nearest with no mipmaps, preventing cross-tile filtering.
- GroundSurfaceProfile gains reusable Organic/Granular/Mineral detail styles and ArtworkInfluence. Organic/0.22 preserves current surfaces. Granular and Mineral provide non-grass detail compositions; source artwork/palette are still data. Style weights blend smoothly at base biome edges. Arbitrary custom shader code is explicitly unsupported in streamed profiles; use the canonical shared material and supported styles. Preview retains its established composition.
- Optional overlays use a nine-column RGBA-float lookup table, two rows per biome; artwork joins the world-owned atlas. There are no per-tile nodes, decals, per-chunk materials or frame callbacks.

## Verification actually run

Linux Mesa 25.2.8 / llvmpipe OpenGL 4.5, using `python TESTS/Terrain/check_ground_shader_gl.py` (numpy and system libEGL/libGL):

- Fragment source translated to plain GLSL and compiled/linked in software GL. This checks the calculation subset, **not Godot's shader parser/importer**.
- Pre-GS2 versus current default rendered output at fixed synthetic inputs: **max absolute channel difference 0.0**.
- Enabled overlays visibly alter pixels; repeated renders identical; separately rendered sixteen 512-unit chunks match the combined negative-coordinate image within 0.00002.
- Independent A/B toggles affect output; zero opacity reproduces the base within 0.000001.
- Static diff/whitespace and unchanged existing production Resource checks passed. A software-render comparison was visually inspected for masks and boundary composition. It is not a Windows/Godot baseline capture.

**Not run here:** dotnet compilation, Godot import/headless suites, gameplay/Windows captures, campaign disk regression and GPU/FPS profiling. Neither dotnet nor Godot is installed in this runner. No FPS improvement, engine pass or graphical acceptance is claimed.

## Performance contract and limits

Setup wakes compilation and uploads. Returning from setup stops all CPU work; nothing needs polling or shutdown callbacks afterward. Maps/materials are scene/world-owned; existing chunk meshes share them and unload normally. Rebuilding the disposable practice fixture replaces its shared material only on a key event.

Current production: empty overlays/same grass artwork/Organic styles skip overlay-table/style-map creation and their texture samples. Only global branch checks remain; measure overhead rather than treating identical pixels as a timing result. Nonwhite tint keeps its prior optional map.

With overlays: table is 9 × (2 × biome count) × 16 bytes. Maximum 256 biome appearances is explicitly validated (72KiB table, at most 768 32px atlas tiles; square atlas up to 896² RGBA8 pixels, about 3.1MiB before GPU overhead). Optional style/tint maps each cost 16 bytes per macro cell. Existing five coarse maps are unchanged. No unbounded layer list or work queue.

A pixel inside one biome evaluates at most two configured layers. At a four-way macro boundary up to eight may contribute; repeated biome IDs reuse their result. Different-art base blending fetches four source tiles. Disabled/zero-mask layers return before appearance sampling. This is bounded additional GPU work, **not measured target-hardware cost**. Measure each layer independently and both before raising coverage or biome complexity.

## Local commands and visual acceptance

From repository root with project-compatible Godot .NET 4.7.2 / .NET 8 (`godot` may need its full executable path):

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Terrain/ValidateGroundStack.tscn
godot --headless --path . TESTS/Terrain/ValidateGroundLayers.tscn
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . TESTS/DebugUI/ValidateDebugHud.tscn
```

Require zero exits and explicit PASS, with no shader errors. New engine suite compares original generated maps/atlas bytes against the frozen reference at two seeds, validates flags, slot ordering/disabled slots, independent mask salts, reversed definition ordering, reload/teardown material sharing and nonmutating authoring. These tests are written but unexecuted here. Run SAVE's existing write/read/disk restore handoff too; Continue should preserve supported edits and identity.

Open `TESTS/Terrain/GroundLayersPractice.tscn`, press F6. Sand is left, Soil right; 1 toggles Soil overlay A, 2 toggles Mineral overlay B, R restores both, Z changes zoom. Fixture Resources are duplicated before editing. Check all four A/B states, blend zone at x=0, chunk borders, negative coordinates and zoom. The original production World/F9 is untouched. The fixture is a neutral demonstration, not approved Oak art direction.

Use GS0_GS1_Validation.md's same-seed/camera/time/weather protocol on baseline and this revision: compare Oak, second Oak position, Pink and their border with no overlays. Repeat legacy preview; verify snow/rain/cloud/shadow/torch composition and unchanged save/Continue. Then profile startup, settled idle, traversal/return and A/B/both using disposable copies with overlays. Record CPU/GPU/frame pacing/memory, renderer/version and exact settings; headless passes do not establish smoothness. Do not enable new production Oak/Pink layers until explicitly approved.
