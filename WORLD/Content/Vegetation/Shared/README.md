# Shared vegetation

VegetationDefinition extends SceneryDefinition with one category Template, inline VisualVariants and an optional shared wind material. VegetationVisualVariant holds texture/atlas region, grounded anchor, scale and conservative placement bounds. VegetationInstance configures one Sprite before publication; it creates no materials and runs no process/physics callbacks. Set stable world-instance metadata in the placement owner, separately from the definition and variant index.

SelectVariant consumes an existing planner-generated sample in [0,1); it owns no random state. The definition requires at least one usable variant. PL3 now dispatches vegetation in streamed and compatibility-preview placement. Validate runs during population setup to reject missing templates/art and non-conservative bounds. Inherited PackedScene Variants remain for legacy content; vegetation uses Template plus VisualVariants.

Existing Seasonal/, Snow/ and Wetness/ adapters retain environment ownership. No tree fire/collision/falling or harvesting mechanics are copied into the lightweight plant controller.
