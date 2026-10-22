# Weather profiles
WeatherProfile owns rain intensity/transitions, drop/splash limits, overcast and storm settings copied from DEBUG. Wind lives in its own profile/source. A biome can supply a local WeatherProfile; null component references inherit world defaults. Values are cloned for runtime previews.
