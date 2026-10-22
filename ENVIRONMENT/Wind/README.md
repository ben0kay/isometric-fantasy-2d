# Shared wind
WindController is the sole production wind source. WindMasterConfig supplies signed Strength (-3 left, 0 calm, +3 right) and DriftPerStrength. State exposes strength, world-pixel velocity and integrated displacement. Rain does not own wind.

Vegetation, shadows, atmosphere, rain, cloud shade and snow consume this source. The tested signed model is deliberately retained for this copy pass; full direction/gust/region simulation remains future work. Biomes may override the wind profile without duplicating its controller.

Composite foliage materials can register/unregister as shared-wind followers. WindController initializes their wind value at registration and updates followers only when strength changes; wetness simulation can sleep while dry. Material followers are removed with their owning adapters and cleared on world exit. Shared wind displacement remains independently active for other effects.
