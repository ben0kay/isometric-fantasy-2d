# Weather presentation
Rain/ contains WorldWeather and bounded GroundRainSplashes. Storms/ contains reusable visual lightning. Snow/ owns snowfall and settled coverage progression, while content-specific material adapters live with WORLD/ENTITIES. WeatherProfiles/ contains the typed rain/overcast/storm profile.

These reproduce existing visuals; no climate scheduling, seasons, rain physics or meteorological simulation is added. One WindController supplies wind to all precipitation. UI remains removable under DEBUG/UI.

## Layered rain and foliage wetness

RainVisualLayers under Rain/ now renders three soft GPU depth layers with shared weather/wind, drop-to-drop length/speed/alpha variation and the **same total MaxDrops budget**. GroundRainSplashes remains in Rain/; liquid splashes remain with WORLD/Liquids.

Foliage wetness is a data-driven visual adapter under WORLD/Content/Vegetation/Shared/Wetness/. It attaches between the wind variants and snow adapters, makes trees darker during rain and dries afterward. FIRE2 mirrors its live material parameters. Normal weather scheduling will eventually avoid simultaneous rain/snow; shader overlays still compose if enabled manually. See Rain/README.md and Wetness/README.md for scope and verification.

## Planned blizzard

The Snow folder owns the pending [B0–B4 blizzard implementation plan](Snow/BLIZZARD_IMPLEMENTATION.md). It proposes coordinating existing snow, wind, daylight, haze and screen effects for severe storms without duplicating climate simulation or changing the current rain/snow defaults. No phases are implemented yet.
