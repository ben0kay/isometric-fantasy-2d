# Weather profiles
WeatherProfile owns rain intensity/transitions, drop/splash limits, overcast and storm settings copied from DEBUG. Wind lives in its own profile/source. A biome can supply a local WeatherProfile; null component references inherit world defaults. Values are cloned for runtime previews.

## Layered rain and wet leaves

The production WeatherProfile now includes grouped Rain visual layers (layer count toggle, softness, size/speed/alpha variance and foreground strength) and Wet foliage settings (enabled, darkening strength, wet/dry transition seconds). The total MaxDrops cap is **shared between all GPU layers**, not multiplied by their count. Effects use the existing weather instance for rain/wind; biome profiles can override the same typed resource. The tree shader adapter lives at WORLD/Content/Vegetation/Shared/Wetness/; see ENVIRONMENT/Weather/Rain/README.md.
