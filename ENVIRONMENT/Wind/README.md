# Shared wind
WindController is the sole production wind source. WindMasterConfig supplies signed Strength (-3 left, 0 calm, +3 right) and DriftPerStrength. State exposes strength, world-pixel velocity and integrated displacement. Rain does not own wind.

Vegetation, shadows, atmosphere, rain, cloud shade and snow consume this source. The tested signed model is deliberately retained for this copy pass; full direction/gust/region simulation remains future work. Biomes may override the wind profile without duplicating its controller.
