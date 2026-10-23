# Biome ground stacks — GS1

Each biome authors one inline GroundStack: required DefiningBaseGround plus an ordered array of zero to two overlays. Each layer references a canonical GroundSurfaceProfile and has a local unique ID and readable label. No extra resource file per layer is required.

Base fills 100% regardless of overlay-only fields. Existing Oak/Pink reference the original profiles unchanged and have no overlays. Enabled overlays are rejected until GS3 supplies rendering; coverage determines mask threshold later, avoiding duplicate threshold/coverage authorities. Tint remains the existing profile palette in GS1; no ignored tint control is exposed.

WorldBiomeDefinition.Surface is a temporary compatibility adapter: a configured stack wins; a missing stack uses legacy Surface. An invalid configured stack throws rather than falling back. Old renderers remain unchanged. Validation allocates only during surface resolution at setup; no frame callback, node, material, shader or per-tile overhead is introduced. GS2 can compile/cache validated data for rendering.

Inspector ranges are also checked in code. Missing base/profile/art/material, duplicate IDs, null arrays/entries, excess slots, nonfinite/out-of-range overlay values and unsupported enabled overlays reject. Shared Resources are never changed by resolution.
