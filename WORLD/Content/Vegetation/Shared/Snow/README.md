# Vegetation snow adapter
VegetationSnowAdapter opts into content SnowResponse.Vegetation, with GrassLike selecting the blade UV profile. It extends the known VegetationSway composition, shares orientation/foliage-profile variants and copies wind per material. Unsupported shaders are skipped with a warning. Source materials are restored on snow off/removal. No species-name tests or per-sprite frame loops are introduced.

Snow coverage updates are change-driven. While snow is visible, shared wind material followers and world-owned FoliageMaterialLinks keep composed wetness current without coverage polling. Snow off/unload unregisters both and restores sources; newly visible chunks receive current settings.

SE1: shared foliage_selection uses a valid authored mask, otherwise the existing original-colour/crown heuristic. Snow receives untouched source RGB for leaf identity separately from its visible colour. Orange/red leaves therefore retain snow response; the mask replaces colour and height gating. Snow copies retain the shared palette-state texture.
