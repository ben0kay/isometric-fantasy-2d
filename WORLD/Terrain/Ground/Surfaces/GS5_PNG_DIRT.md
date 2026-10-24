# GS5 grayscale dirt artwork

Dirt now uses `Textures/Dirt01_gs.png` (256×256), Recipe `GrayscaleArtwork` (3), instead of procedural Dust. Rock remains Basalt. The old-project adaptation document is historical for Dirt.

The shared streamed renderer uploads grayscale extra artwork to a separate nearest-filtered, non-mipmapped RGBA8 atlas. Each tile is copied at native 256×256 with no resize; identical texture references share a tile. Existing base and legacy artwork retain the original 32px atlas and sampling. The current one-tile upload is 256 KiB; atlas allocation scales with distinct configured grayscale textures, not chunks. No extra draw pass or frame callback. Default-off previews preload once; hidden/zero-opacity layers return before artwork sampling. F9 writes changed uniforms only.

Grayscale shading is `texture.r * 2 * Surface.BaseColour * layer.Tint`, blended using artwork alpha and the existing organic mask/opacity. Mid-grey is neutral. Dirt BaseColour is white; each biome sets its own dirt-layer Tint (Oak starts warm brown: 0.50, 0.36, 0.22). Reuse the profile/PNG across biomes without changing grass tint. MaterialFeatureSize controls repetition in world units, initially 256 (one source pixel per world unit). Native grayscale source dimensions currently require 256×256; other recipes retain their existing contracts.

The Dust mask family is retained exactly for grayscale Dirt: same world-space frequency, coverage threshold, warp, salt, softness and F9 multipliers. Oak coverage 0.4, opacity 0.8, patch size 500, softness 0.10, warp 0 and salt 431 are unchanged. Layers remain OFF by default. Oak base/accompanying settings, ForestGrass, Pink and Rock are unchanged.

Save compatibility follows the existing narrow terrain migration pattern: exact current Oak/Pink/Dirt/Rock hashes gate reconstruction of the full previous dependency fingerprint from two frozen source files. Unrelated content differences still reject. No schema/world-ID/scenery changes.

Validation: run `python TESTS/Terrain/check_ground_shader_gl.py`. Mesa checks pass for unchanged pre-GS2 base output (maximum difference 0.0), all 256×256 synthetic source pixels/tint, exact Dust mask parity, zero opacity, independent controls and negative-coordinate chunk seams. Static checks verify PNG dimensions, preserved authored mask fields and frozen migration sources. Godot fixtures now check byte-identical native PNG upload and the migration bridge.

Not run here: Godot/.NET build/import, engine fixtures, actual campaign restoration, Windows visuals or target performance (tools unavailable). Locally run `TESTS/Terrain/ValidateGroundStack.tscn` and `ValidateGroundLayers.tscn`; then F6 World → F9 Ground Surfaces → Overlay A. Compare A off/on, coverage/opacity, master and reset; check walking across chunks. F9 reset restores grass-only authored state. Reopen Godot to import the existing PNG if needed.
