# Biome ground stacks — GS1–GS3

Each biome authors an inline GroundStack with one required DefiningBaseGround and zero to two ordered OverlayLayers. A layer references a canonical GroundSurfaceProfile, local unique ID and readable label. No extra .tres per layer is required. Base fills 100%; its overlay-only fields never hide it.

Tint is biome-owned, opaque RGB with white neutral. It multiplies completed surface artwork/palette/detail, rather than recolouring the shared Resource. Surface profiles choose source texture, palette, Organic/Granular/Mineral detail and artwork influence. Current Grass profiles remain unchanged.

For overlays set Enabled, Coverage, Opacity, PatchSize, EdgeSoftness, WarpStrength and SeedSalt. Coverage biases the noise threshold; opacity controls blend strength. Array order is A then B; salts and slot offsets separate the masks. Disabled, zero-coverage or zero-opacity slots do not render. All original production arrays remain empty. Use the test fixture before authoring production overlays.

CompiledGroundBiome resolves stacks once during streamed setup, with Surface only for legacy fallback. GroundOverlayPalette packs two rows per biome into a single setup-time GPU table. No idle callbacks, per-tile objects or runtime Resource writes. GroundSurfaceProfile.Validate and stack validation reject missing data, invalid ranges/nonfinite values, duplicate IDs, null slots, too many layers and unsupported streamed materials.

The old adjacent preview remains base-only with tint. Arbitrary custom shaders are not interpreted by the streamed data renderer; use the shared material and supported detail styles. See [GS2/GS3 evidence and local checks](../GS2_GS3_Validation.md). GS4 F9 comparison controls are implemented; local engine/UI validation is pending.

GS4 GroundSurfacePreview supplies session-only comparison uniforms without changing these Resources. F9 controls are global strength/visibility multipliers, not authoring changes; see ../GS4_Validation.md.
