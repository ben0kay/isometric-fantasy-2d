# ENVIRONMENT / Wind

> **Status: production scaffolding only.** Nothing in this folder drives the game yet. The working prototype remains in `DEBUG/TopDownTest/Weather/` and is unchanged.

## One source of wind

This module will own **shared atmospheric wind**: direction, speed, gusts, and eventually world-region or biome modifiers. Trees, long grass, particles, rain, snow and water ripples should **read the same wind state**, then apply their own response strength.

```text
ENVIRONMENT/
└── Wind/
    ├── README.md
    ├── WindMasterConfig.cs
    ├── DefaultWindConfig.tres
    ├── Simulation/
    │   ├── README.md
    │   ├── WindController.cs
    │   └── WindState.cs
    └── Visuals/
        └── README.md
```

### Responsibilities

- `WindMasterConfig.cs` / `DefaultWindConfig.tres`: Inspector-editable **wind-wide settings**, currently blank on purpose. Possible future parameters: baseline wind speed, bearing, gust strength/frequency and biome adjustment rules.
- [Simulation](Simulation/README.md): central wind calculation, stable shared state, interpolation and later local wind-field sampling.
- [Visuals](Visuals/README.md): adapters/shaders that interpret that wind for trees, grass, airborne leaves, drifting fog and future liquid surfaces. Do **not** store tree or grass species artwork here.

### Planned contract

The debug test currently uses a **signed horizontal** `WindStrength` (-3 left, +3 right) and a fixed `DriftPerStrength` vector. Production should instead distinguish:

1. **Direction** — a 2D vector (or angle) allowing all horizontal-plane directions.
2. **Speed** — nonnegative scalar magnitude in defined world units.
3. **Gusts** — optional temporary variations in speed/direction.
4. **Velocity** — derived vector for drift and precipitation.
5. **Modifiers** — biome, season, elevation, shelter or local weather changes, if useful.

A future `WindController` will produce a `WindState` snapshot. Renderers should consume it without changing it. The wind system should not depend on the Rain, DayNight, Trees or Grass controllers.

### Different effects, shared source

| Consumer | Response to shared wind | Ownership |
| --- | --- | --- |
| Oak canopy / bushes / grass tufts | Species-specific bend, amplitude and animation phase | WORLD content + reusable Wind/Visuals shader |
| Falling leaves / pollen / smoke / embers | Drift and sometimes spawn frequency | ENVIRONMENT/Atmosphere/Particles |
| Rain and falling snow | Directional precipitation slant | ENVIRONMENT/Weather/Rain and Snow |
| Cloud shade and ground mist | Integrated world-space displacement | ENVIRONMENT/Weather/Clouds and Atmosphere/Fog |
| Future lakes and other liquids | Small directional ripple/highlight bias | WORLD liquid renderer, fed by ENVIRONMENT/Wind |
| Player/objects, eventually | Optional gameplay forces | Their owning gameplay systems, only when needed |

**Important:** A single wind velocity does **not** imply identical bend/drift on all objects. Keep the material and species response parameters with the corresponding consumers, not repeated wind sources.

### Existing working reference (do not move yet)

- [Debug wind and rain config](../../DEBUG/TopDownTest/Weather/WeatherMasterConfig.cs)
- [Debug wind coordinator](../../DEBUG/TopDownTest/Weather/DebugWeather.cs)
- [Debug tree/grass sway shader](../../DEBUG/TopDownTest/Trees/TreeSway.gdshader)
- [Debug falling-leaf response](../../DEBUG/TopDownTest/Atmosphere/DebugAtmosphere.cs)
- [Debug snow wind response](../../DEBUG/TopDownTest/SnowTest/DebugSnowfall.cs)

Nothing has been migrated, and no production node has been added to any scene.

### Migration checklist for later

1. Decide a consistent 2D direction convention, world-speed units and update timing.
2. Implement `WindState` and `WindController` so there is one authoritative wind simulation, separate from rain/weather presentation.
3. Expose the same state for scene effects; adapt existing tree/grass shader strengths individually.
4. Update rain, snowfall, cloud haze, leaves and future lake shaders to sample shared wind without introducing duplicate timers/strength controls.
5. Add only the regional/local wind modifiers actually needed; integrate with biomes and calendar later.
6. Profile scenes with many trees/grass; keep animation on the GPU and avoid an individual wind `_Process` call per object.

No production wind logic, debug code changes or environment-wide scene wiring were introduced by the foundation pass.
