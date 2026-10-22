# Rocks
Rock.tres references Rock01.tscn and Sprites/rock_01.webp. The scene owns its grounded polygon; shared placement handles scale and reflection. SnowResponse.Rock opts into the production shared snow-cap adapter. Content owns sun/contact shadow settings.


Rock01 owns a LocalLightOccluder using its RockFootprint for native point-light blocking. It follows generated scale/mirroring automatically and does not modify collision or custom sun shadows. An optional Outline can override the light geometry.
