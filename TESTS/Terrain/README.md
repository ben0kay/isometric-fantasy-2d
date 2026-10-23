# Terrain validation and practice

- `ValidateGroundStack.tscn`: authoring, fallback, stack precedence, tint, validation and serialization checks.
- `ValidateGroundLayers.tscn`: frozen pre-GS2 base-map/atlas parity plus GS3 table packing, order, independent slots, zero layers, non-grass styles, determinism and shared chunk material checks.
- `GroundLayersPractice.tscn` (F6): isolated Sand + Soil test, 1/2 toggle A/B, R reset, Z zoom. Uses duplicated fixture Resources and a negative-coordinate 4×4 map; it changes no production biome/campaign.
- `check_ground_shader_gl.py`: optional Linux Mesa/numpy fragment translation/compile/render checks. Verifies pre-GS2 default formula parity, nonzero overlays, A/B toggles, zero-opacity parity and sixteen 512-unit chunks against combined output. This does not replace Godot import/rendering or target profiling.
- `PreGS2GroundPalette.cs` and References/PreGS2StreamedGround.txt are frozen test-only references. Do not update them to make parity tests pass.

See [GS2/GS3 validation](../../WORLD/Terrain/Ground/Surfaces/GS2_GS3_Validation.md) for commands and honest run/deferred status. C# engine suites are authored but not executed in this runner, which has no Godot/.NET installation. Fixture artwork is deliberately simple test data, not approved production art.

GS4: GroundLayersPractice now reuses the actual Ground drawer (F9). Keys 1/2 and R change the same session preview uniforms without rebuilding maps. ValidateGroundLayers adds API reset/nonmutation/world-isolation assertions; software-GL checks cover all new comparison uniforms. See ../../WORLD/Terrain/Ground/Surfaces/GS4_Validation.md for combined local testing.
