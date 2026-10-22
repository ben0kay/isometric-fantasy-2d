# Macro generation overview

Optional `MacroMapDebugPanel` in the production World scene. Press **F7** after F6 to show/hide the finite biome blueprint, seed/version/hash, world identity, region counts and cell coverage. Colours come from biome Resources; dark borders distinguish region instances. This is an overview of the new map, not the player location or a streamed playable world.

Removing the panel leaves generation and all gameplay functional. Generation settings belong to `WORLD/Generation/World/DefaultWorldGeneration.tres`, referenced by WorldMasterConfig; biome data stays in canonical biome definitions. There are no generator settings owned by this UI.
