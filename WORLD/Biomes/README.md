# Overworld biomes

> **SE4 / BC3 — 26 October 2026:** BC3 adds optional WeatherResponse, separate from PlacementClimate and inert Climate baselines: snow/rain permission, local thermal offset/season amplitude and cover multiplier. A single shared ENVIRONMENT resolver consumes it; no biome owns weather Nodes. [Current handoff](../../ENVIRONMENT/Climate/README.md).


> **PreviewMap / BC2 — 26 October 2026:** BC2 implemented, awaiting local validation. `PlacementClimate` now influences site selection, growth and gap filling through the shared generator. Oak/Pink soft preferences preserve sparse-content coverage; Tundra/Desert hard limits enforce cold/hot placement. `Climate` baselines and weather remain separate. Use `DEBUG/PreviewMap/PreviewMap.tscn` for a no-chunk whole-world view. [Current handoff](../Generation/Authoring/BC2_Validation.md).


> **BC0/BC1 delivery — 26 October 2026:** BC0/BC1 now implement a separate frozen macro climate field and F7 climate view. Biome selection remains globally weighted until BC2; Snowy Tundra and Desert remain ground-only placeholders. Regional weather remains BC3. [Audit / local handoff](../Generation/Authoring/BC0_BC1_Validation.md).


**26 October update:** Snowy Tundra and Desert ground-only placeholders now join Oak/Pink in the normal finite World biome collection (F6/F7 or new campaign). No populations or new weather logic; climate placement remains BC1/BC2. Existing campaigns are not migrated by this addition. World Author ocean placement is broadly middle-biased with seeded offsets, not fixed-centred. Planned North/Mid/South starts must satisfy configurable clearance from major POI footprints as well as water/obstacle/access safety; see the [World Author plan](../Generation/Authoring/WORLD_AUTHOR_IMPLEMENTATION_PLAN.md).


Current identity colour: **Id / DisplayName / Colour** now live together. Colour drives the macro-map swatch and base-ground tone through one shared resolver. Oak/Pink retain their current grass appearance; their map swatches now match their ground tones. Dirt/rock keep independent tints. [Authoring, compatibility and validation](IDENTITY_COLOUR.md).
Implemented: `Definitions/OakForest/Biome.tres` and `Definitions/PinkMeadow/Biome.tres` are the two production biomes. All individual biome folders live under Definitions; Shared owns common Resource types. Cave biomes remain separate.

**Want to preview more biomes without drawing their artwork?** Follow the [placeholder-biome F7 authoring guide](PLACEHOLDER_BIOME_AUTHORING.md). Add lightweight distinct biome `.tres` Resources to `WORLD/Generation/World/DefaultWorldGeneration.tres → Biomes`. They may reuse current canonical art; unique IDs and map colours distinguish regions. **Today this tests globally weighted placement, not climate clustering**. [BC0–BC3](Definitions/OakForest/SPAWNING_IMPLEMENTATION_PLAN.md) plans continent-scale climate regions and eligibility without renumbering BP2 ore/BP3 entities.

WorldBiomeDefinition references a ground surface profile and an ordered array of inline BiomePopulation resources. Each population references canonical content and stores count, scale, mirroring and optional cluster settings. Populate these in the Inspector; no separate spawn file per species per biome is needed.

Future Avalonia tooling can edit stable IDs and these Resource references/settings. Toolbox integration is not implemented. Never duplicate content assets under a biome or hardcode a species into placement code.

Environment: WorldBiomeDefinition.Environment optionally references BiomeEnvironmentProfile. Null components inherit global defaults; selected components are cloned at runtime. OakForest/Environment.tres references a local atmosphere profile; lighting/wind/weather/snow/grade/cloud/torch can also be overridden through the same data contract. The current shared world environment resolves the primary biome profile at startup; per-region environment switching/blending is not yet implemented.

Layouts/OakAndMeadow.tres selects the two definitions and the curved boundary. Biome art/content remain in canonical WORLD folders. World bounds remain in WorldMasterConfig.

P1 macro generation: each definition now includes enabled/weight/target-diameter settings; map colour comes from Identity. `WorldMasterConfig.Generation.Biomes` is the arbitrary-size source collection for the new finite map. The old Layout remains only for the playable preview. See [macro generation](../Generation/World/README.md).

P3: streamed populations use explicit InstancesPerMillionUnits; Count/cluster settings retain preview-only semantics. Scale/mirror/variant settings remain shared. All enabled biome definitions supply palettes, textures and local scenery to arbitrary regions. Automatic regional environment-profile blending remains future work; see WORLD/Runtime/Streaming/README.md.

GS1: Oak/Pink now author inline GroundStack Resources with a clearly labelled required base and empty optional overlays. Surface remains a compatibility adapter; renderers are unchanged. See [GS0/GS1 validation](../Terrain/Ground/Surfaces/GS0_GS1_Validation.md).

GS2/GS3: streamed setup reads GroundStack directly (legacy Surface remains fallback). Biomes may author at most two ordered enabled organic overlays; existing production arrays stay empty. Ground surfaces/overlays belong in WORLD/Terrain, decorative grass objects remain Content. See ../Terrain/Ground/Surfaces/GS2_GS3_Validation.md.

Base/patch clarification: each explicit stack may author AccompanyingPatches separately from DefiningBaseGround and the two extra OverlayLayers. Oak/Pink own inline accompanying grass variation with the original colours/scale; production extra arrays remain empty. See ../Terrain/Ground/Surfaces/ACCOMPANYING_PATCH_LAYER.md.

GS5 Oak has two extra inline layer entries referencing canonical Dirt/Rock profiles, both Enabled=false with AllowSessionPreview=true. F9 can inspect them without saving biome changes. Pink remains unchanged. See ../Terrain/Ground/Surfaces/GS5_Validation.md.

**BP0/BP1:** optional typed Climate, EntitySpawns and ResourceSpawns links now exist. Oak has a 15°C / 65% baseline and disabled, empty spawn profiles; Pink inherits null links. Configuration only: no live climate, gameplay spawns or scenery changes. [Audit, contracts, compatibility and local validation](Definitions/OakForest/BP0_BP1_Audit.md).

## Future biome snow/weather response and climate placement (DESIGN ONLY)

**Prefer one shared world climate/weather runtime.** WORLD/Generation should first plan a broad macro climate-zone map across the finite world. `WorldBiomeDefinition` data can later express suitability for that zone (soft preferences, optional hard requirements, rare magical exceptions) and surface/vegetation snow/frost reactions. **Do not give every biome its own snow controller or clock.** Existing per-object `SnowResponse` and foliage material adapters remain in place.

A temperate Oak biome with its BP1 **15°C baseline** can still receive snowfall during winter cold; a hot volcanic province could reject ordinary snowfall; enchanted cyan grass, purple plants and glowing fungi could retain unusual hues or use tinted frost. These are **future authorable response rules, not implemented exports or actual content**. Year/season palette changes remain separate from falling snow and snowpack.

Future shared ENVIRONMENT resolves **effective local weather by world position** (macro zone + biome rule + clock/season + weather) and applies it to visible chunks/camera using regional masks and bounded updates. A camera can cross two zones at once; switching the entire current global snow quad from the player's biome alone would be wrong. Null climate links must be handled safely, with full eligible land coverage and deliberate P4/generator save compatibility. [Generation plan](../Generation_Implementation.md) · [direction](../../Notes/World_Generation_Direction.md).
