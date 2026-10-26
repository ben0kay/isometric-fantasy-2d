# Oak spawning profiles — configuration only

`Entities.tres` and `Resources.tres` are linked from Oak's `Biome.tres`. Both are disabled with empty typed entry lists. No world service consumes them yet, including when manually enabled. Existing trees/rocks/grass/plants remain in `Biome.tres → Populations`.

Entity entries reference canonical ENTITIES definitions, use relative selection weights against the profile's total individual density and active cap, and optionally group individuals. Resource entries reference canonical world SceneryDefinition data with per-entry density/spacing. They are not inventory item rewards and do not automatically make scenery harvestable. BP2 must verify deposit capability. No copied species assets, new species or ores belong here.

Sibling `Climate.tres` holds authored 15°C / 65% relative humidity, not current weather. Other biomes may leave all three links null. The shared types and explicit validation live in `WORLD/Biomes/Shared`. No timers, polling or chunk work are introduced. See [audit and local checks](../BP0_BP1_Audit.md) and [phase plan](../SPAWNING_IMPLEMENTATION_PLAN.md). Before live spawning/climate, version the exact BP1 save compatibility bridge and define durable IDs/depletion/death handling.
