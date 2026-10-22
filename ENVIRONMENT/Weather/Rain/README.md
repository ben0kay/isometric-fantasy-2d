# Rain and ground impacts
WorldWeather keeps camera-centred GPU rain, the generated streak texture and real-time intensity transitions. GroundRainSplashes retains a fixed 96-slot pool, bounded attempts and cached prop/player exclusions over camera-visible map terrain. No lake dependencies are copied. Splashes feed the same cached clear-ground query to lightning.
