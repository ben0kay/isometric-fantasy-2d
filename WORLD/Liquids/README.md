# WORLD/Liquids — shared liquid domain

**Status: documentation/folder foundation only. No liquid implementation is copied from DEBUG yet.** The current production World scene remains unchanged.

Liquid is an extensible domain: water, lava, toxic pools, sludge, quicksand and future types can reuse boundary/region mechanics without being forced to share identical visuals or player effects.

## Folder ownership

- [Definitions/](Definitions/README.md) — each liquid type's canonical editable `.tres` configuration, plus its type-specific optional profiles. Example: `Definitions/Water/Water.tres`, future `Definitions/Lava/Lava.tres`. Definitions reference common mechanics, selected surface renderer/shader and configured interaction responses.
- [Shared/](Shared/README.md) — common Resource classes, interfaces, region queries and reusable contracts. Avoid hardcoding tests for `water` / `lava` IDs in the general renderer.
- [Regions/](Regions/README.md) — authoritative bounded polygon/shoreline area and cached signed-distance queries shared by rendering, traversal, placement exclusions and precipitation impacts.
- [Surfaces/](Surfaces/README.md) — liquid appearance and effects, including shader families and profiles. Water ripples and lava movement must be independently configurable; do not force lava through a water-specific shader.
- [Interactions/](Interactions/README.md) — reusable opt-in gameplay response hooks for movement slowdown, wading/immersion, damage-over-time, resistance/immunity, and impacts. The liquid domain supplies effect requests/settings; player stats, potions, enchantments and damage resolution remain owned by their appropriate entity/combat/equipment systems.
- `WORLD/Generation/Liquids/` — **future** procedural construction/placement of water bodies, lava lakes, rivers and connected features, not the shaders or type definitions.
- `DEBUG/UI/Liquids/` — future developer-only liquid toggles and preview sliders. The runtime must not depend on DEBUG UI.
- `SCENES/World/World.tscn` — eventually instantiates one or more liquid region instances; scene composition is not liquid implementation.

## Canonical relationship

`Liquid type definition -> selected surface/profile + interaction settings -> bounded LiquidRegion -> renderer/interaction consumers`

A biome (e.g. volcanic biome) references a **lava definition**, rather than copying lava artwork/logic into the biome folder. A lake/pond instance defines shape/location independently of liquid type. The same lake geometry could host water or lava using a different definition; rules may reject combinations when needed.

## First production copy (future separate job)

Copy/adapt the existing `DEBUG/TopDownTest/Liquids/` **pond** into production using `Definitions/Water/`, shared Regions/Surfaces/Interactions and the existing production clock/wind/rain/player. Reuse the region's cached 96-point polygon and small one-time distance field where practical; eliminate all `DEBUG/TopDownTest/` runtime paths. Preserve the debug scene completely. Integrate with `ENTITIES/Player/Visuals/Surfaces/` so liquid immersion does not conflict with existing player snow burial. Do **not** implement lava, damage, potion immunity, rivers, carving, hydrology or new gameplay in that copy pass.

The DEBUG pond is a **masked liquid overlay on ground**, not excavated terrain. A future lake-carving generator can produce region polygons/terrain masks and feed the same LiquidRegion contract. Carving, shores and terrain adjustments belong to `WORLD/Generation/Liquids/` and `WORLD/Terrain/Transitions/`; visual liquid surfaces remain here.

## Migration note

Existing `WORLD/Water/` and `WORLD/Generation/Water/` directories predate this shared liquid layout. Their files are not deleted or moved in this foundation pass. Before implementing the first pond, review those READMEs and retire redundant Water-only scaffolding in a deliberate follow-up; avoid two competing pond systems.

Keep modules simple, data driven, seed-capable for future generation, and optimized for shared material settings and startup caching. Read `CodePreferences.md` before implementing.
