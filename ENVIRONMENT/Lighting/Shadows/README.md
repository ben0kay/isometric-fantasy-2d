# Sun shadows
SunShadowRenderer caches production sprites after scenery/wind setup. Shared silhouette materials preserve scale, mirror, offset and the sprite transform. Content data controls CastSunShadow and ContactShadowRadius; grass opts out. It updates projections from daylight and shared wind, never scans the scene each frame.

Art-directed flat-ground silhouettes do not occlude one another or climb terrain. More casters add draw/overdraw cost; dynamic registration is outside this fixed-world pass.
