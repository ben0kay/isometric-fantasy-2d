# Ground stack layers

- DefiningBaseGround: required full-coverage texture, single BaseColour and biome Tint; fine artwork/detail stay on the base.
- AccompanyingPatches: optional inline GroundPatchLayer with own label/ID, Enabled, Shade/Fresh/Dry colours, Strength and PatchSize. Missing means base-only for explicit stacks. Oak/Pink preserve their existing grass variation here.
- OverlayLayers: zero to two **extra** surface patches, still empty in production. Future dirt/rock go here, not in AccompanyingPatches.

Resolve/validate once at setup. Accompanying colours reuse the existing maps and GPU formula; no extra draw/pass or sampler. IDs are unique across all three parts. Legacy Surface-only assets adapt their old profile patch fields; explicit stacks ignore those hidden defaults. Extra surfaces can still use their own profile palette composition.

F9 separates accompanying visibility from base detail and extra master/A/B controls. GroundSurfacePreview is world-owned, session-only and writes changed uniforms. Authored data is never changed. See [current contract, migration and checks](../ACCOMPANYING_PATCH_LAYER.md).

GS5: AllowSessionPreview can preload an authored-off extra for explicit F9 activation. Oak A/B use this; they start OFF and reset OFF. Ordinary disabled extras stay unavailable. Default visibility follows authored states; explicit F9 toggles override only their slot, and all-off skips GPU layer sampling. See ../GS5_Validation.md.
