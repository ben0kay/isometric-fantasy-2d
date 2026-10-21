# ENVIRONMENT — Time, Climate and Atmosphere

> **Status: folder scaffolding and design notes only.** This does NOT implement production day/night, weather, wind, seasons, shadows or fog. The working experiment in `DEBUG/TopDownTest/` remains independent and unchanged.

## Ownership

- **WORLD** owns *what/where*: terrain, biomes, trees, buildings, caves, and the sprites/definitions of those objects.
- **ENVIRONMENT** owns *conditions and changing presentation*: time, calendar, lighting, weather, wind, seasonal progression and atmosphere effects.
- **WORLD/Biomes** owns *local overrides*: e.g. a cursed forest has unusual pale ground fog and subdued sunlight, or a mountain biome receives more snow. A future biome can have an optional `Atmosphere.tres` or reference a reusable profile defined here.
- **WORLD/Content/Vegetation/Trees/Oak** owns the oak species' *visual variants* and any spring/summer/autumn/winter graphics. Never duplicate oak sprites under `ENVIRONMENT/Seasons/`.
- **ENTITIES** owns animal/NPC/enemy implementation; these systems can react to environmental state without being moved here.

## Folder map

| Folder | Intended responsibility |
| --- | --- |
| [Time/](Time/README.md) | Single authoritative game clock and calendar, shared by lighting, seasons, crops and gameplay. |
| [Lighting/](Lighting/README.md) | World illumination, sun/moon cycles, shadows and placed/local light emitters. |
| [Weather/](Weather/README.md) | Rain, snow, storms, weather profile/transition logic and precipitation. |
| [Wind/](Wind/README.md) | Wind strength, gusts, direction and rendering hooks (trees, grass, particles). |
| [Seasons/](Seasons/README.md) | Seasonal progression plus future `Spring.tres`, `Summer.tres`, `Autumn.tres`, `Winter.tres` *definitions* — NOT four separate seasonal gameplay implementations. |
| [Atmosphere/](Atmosphere/README.md) | Reusable localized fog, particles, ambient visuals and atmospheric presentation profiles. |

## Design: global conditions + biome modifiers + local visuals

1. **Clock/calendar** supplies the current time and date; the **season controller** derives seasonal progress from the calendar. Avoid multiple independent timers.
2. **Weather/wind** produce conditions by region, influenced by season and climate. Keep wind as shared data so one gust can influence trees, grass, fog and other effects consistently.
3. **Biome settings** reference optional local atmosphere/climate profiles. Examples: haunted forest ground mist; alpine precipitation; swamp humidity. Biomes should reference shared effects/shaders from this module rather than duplicate them.
4. **Lighting & atmosphere renderers** combine global day/night, regional weather, biome overrides, and local effects. Blend as a player travels between biomes; fog may be spatially confined to the gloomy region rather than tinting the whole screen on entry.
5. **World objects and entities respond** to those environmental values (tree foliage, crop growth, wind sway, animal behaviour, freezing rivers). They retain ownership of their own sprites and gameplay logic.

### Example: biome-specific pale ground fog

- Future file: `WORLD/Biomes/GloomyForest/Atmosphere.tres` (optional) supplies fog density, colour, drift and light modifiers.
- Future reusable effect: `ENVIRONMENT/Atmosphere/Fog/` draws low-lying mist that can remain visible at the boundary.
- A normal forest can disable mist or choose different settings; neither needs a private copy of the fog shader.

### Example: one oak across seasons

- `WORLD/Content/Vegetation/Trees/Oak/Oak.tres` owns the oak definition and its physical sprite variants.
- Each variant may optionally point to spring/summer/autumn/winter images, palettes, foliage overlays or other art. Not every tree requires 4×N bespoke PNGs.
- `ENVIRONMENT/Seasons/Definitions/` will own *shared seasonal rules* (daylight/temperature/growth/rain/wind tendencies) without storing tree-specific images.
- The season system exposes **season and progress/transition factors**. Oak visuals can gradually change colour and, with suitable art, lose leaves; hard swaps of radically different silhouettes need an explicit transition design.
- Keep the generated physical oak variant stable across reloads. Season changes its appearance, not its randomly assigned identity.

## Current debug test and eventual promotion

See [day/night debug notes](../DEBUG/TopDownTest/DayNight/README.md) and [tree wind debug notes](../DEBUG/TopDownTest/Trees/README.md).

The existing preview already has a sped-up day/night clock, colour grading, projected silhouette shadows and a **wind multiplier** for shader-driven canopy sway. This is a **disposable demonstration**, not an active production dependency. When ready to promote it, review limitations and extract reusable functions/materials without coupling production systems to `DEBUG/`. The shadow projection is art-directed 2D rather than physically accurate 3D lighting; profile overdraw in dense forests.

## Planned content convention

- `EnvironmentMasterConfig.cs` and `DefaultEnvironmentConfig.tres` are intentionally **empty placeholders**, following the project's inspector-config convention.
- Do not create speculative controller implementations or empty `Spring.tres` etc. until actual definitions and data fields have been designed.
- Document major modules with a local README; prefer shared, data-driven definitions over one script per season/biome/weather type.
- The future Avalonia editor should edit the same environment, season and biome profile data the game loads, not maintain a separate source of truth.
