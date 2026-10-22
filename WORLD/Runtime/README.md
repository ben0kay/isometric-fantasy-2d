# World runtime

WorldScene composes the existing adjacent-biome playable preview: seed, ground, player/camera/bounds, pond-aware startup placement, skeleton, environment and fire. P1 additionally builds an independent coarse finite MacroMap and disposable Blueprint manifest from WorldMasterConfig.Generation; the optional F8 panel observes it. Actual gameplay still uses the preview layout.

Environment caches still initialize after all preview actors exist. Chunk streaming, registration lifecycles, durable save selection and deltas remain future passes. See WORLD/Generation/World/P0_P1_Audit.md for migration points.
