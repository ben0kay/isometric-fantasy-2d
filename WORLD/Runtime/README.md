# World runtime

WorldScene composes the existing adjacent-biome playable preview: seed, ground, player/camera/bounds, pond-aware startup placement, skeleton, environment and fire. P1 additionally builds an independent coarse finite MacroMap and disposable Blueprint manifest from WorldMasterConfig.Generation; the optional F8 panel observes it. Actual gameplay still uses the preview layout.

Environment caches still initialize after all preview actors exist. Chunk streaming, registration lifecycles, durable save selection and deltas remain future passes. See WORLD/Generation/World/P0_P1_Audit.md for migration points.

P2 plans required/preferred feature reservations before the biome map and includes them in the Blueprint. FeaturePlan exposes immutable geometry/permissions and an independent hash; F7 overlays these footprints. The original preview placement does not yet consume the new larger-world reservations.
