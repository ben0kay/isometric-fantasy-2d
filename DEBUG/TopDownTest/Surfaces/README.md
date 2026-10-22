# Shared debug surface visuals

PlayerSurfaceVisual owns the demo player sprite position/material for both snow and liquid immersion. Each optional experiment registers once through Ensure(player). LiquidWading owns shore depth, movement and rings; DebugSnowTest owns shallow drift depth. The helper applies only one visual response per frame, prioritizing active liquid, and leaves physics/camera untouched. PlayerImmersion.gdshader clips the dipped sprite at the ground/water line. No production code is involved.

This folder must remain while either snow or liquids uses it. Removing an optional scene at runtime is supported; deleting its scripts also requires removing the corresponding typed hooks here. Removing both scene instances restores original sprite state and removes the helper. Settings stay with SnowMasterConfig and LiquidDefinition; this common adapter introduces no separate tuning config.
