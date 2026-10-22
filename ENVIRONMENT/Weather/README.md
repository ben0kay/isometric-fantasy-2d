# Weather presentation
Rain/ contains WorldWeather and bounded GroundRainSplashes. Storms/ contains reusable visual lightning. Snow/ owns snowfall and settled coverage progression, while content-specific material adapters live with WORLD/ENTITIES. WeatherProfiles/ contains the typed rain/overcast/storm profile.

These reproduce existing visuals; no climate scheduling, seasons, rain physics or meteorological simulation is added. One WindController supplies wind to all precipitation. UI remains removable under DEBUG/UI.
