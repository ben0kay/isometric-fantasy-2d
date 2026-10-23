# Biome ground stacks — GS1

Each biome authors one inline GroundStack: required DefiningBaseGround plus an ordered array of zero to two overlays. Each layer references a canonical GroundSurfaceProfile and has a local unique ID and readable label. No extra resource file per layer is required.

Base fills 100% regardless of overlay-only fields. Existing Oak/Pink reference the original profiles unchanged and have no overlays. Enabled overlays are rejected until GS3 supplies rendering; coverage determines mask threshold later, avoiding duplicate threshold/coverage authorities. Each layer now exposes Tint in its biome-owned entry. White preserves the source profile; RGB multiplies the completed base appearance, including artwork and fine detail. Alpha must be 1. Existing profiles retain their palettes; tint is an additional colour choice, not an exact replacement of the palette.

WorldBiomeDefinition.Surface is a temporary compatibility adapter: a configured stack wins; a missing stack uses legacy Surface. An invalid configured stack throws rather than falling back. Both existing renderers apply base tint; the broader stack migration remains GS2. Validation allocates only during surface resolution at setup; no frame callback, node, material, shader or per-tile overhead is introduced. GS2 can compile/cache validated data for rendering.

Inspector ranges are also checked in code. Missing base/profile/art/material, duplicate IDs, null arrays/entries, excess slots, nonfinite/out-of-range overlay values and unsupported enabled overlays reject. Shared Resources are never changed by resolution.
