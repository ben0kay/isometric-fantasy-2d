# Macro generation overview

Optional `MacroMapDebugPanel` in the production World scene. Press **F7** after F6 to show/hide the finite biome blueprint, seed/version/hash, world identity, region counts and cell coverage. Colours come from biome Resources; dark borders distinguish region instances. This is an overview of the new map, not the player location or a streamed playable world.

Removing the panel leaves generation and all gameplay functional. Generation settings belong to `WORLD/Generation/World/DefaultWorldGeneration.tres`, referenced by WorldMasterConfig; biome data stays in canonical biome definitions. There are no generator settings owned by this UI.

P2: tinted polygons include feature clearance; white crosses mark anchors. The legend lists placed required/preferred features and warnings. Geometry and all permissions remain owned by WORLD/Generation/World/Features. No new biome or terrain is rendered by the overlays.
