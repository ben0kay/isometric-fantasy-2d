# Plants

PL0 audited; PL1 foundation implemented, awaiting native validation. [Plan](PLANTS_IMPLEMENTATION_PLAN.md) · [Audit and checks](PL0_PL1_Validation.md).

Plant.tscn is the single lightweight scene. VegetationDefinition owns a Template reference, inline VisualVariants and optional shared wind material. Each variant accepts Texture2D/AtlasTexture, source-pixel root anchor, scale and conservative root-local bounds. Atlas anchors are relative to the region. Configure before AddChild; population scale belongs on the root.

## Existing PL2 asset staging

Eight source PNGs were independently moved to Sprites/plant_oak_001.png through plant_oak_008.png, with eight matching Definitions/plant_oak_XXX.tres files. These are staged SceneryDefinition resources with metadata/visual_variants, not production-spawnable VegetationDefinition resources. Preserve their plant.oak.001 through plant.oak.008 IDs, names and artwork references when converting to the new PL1 exports during remaining PL2 work.

Neither OakForest nor PinkMeadow populations were changed. PL2 still needs definition conversion and Oak registration; PL3 must connect both generation paths and existing wind/snow lifecycle. Plants do not spawn in normal World yet. Harvesting is separate PL4; trees, grass and rocks are unchanged.
