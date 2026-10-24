# Generated liquid runtime — WG2

`GeneratedWaterRuntime` belongs to one WorldScene under TerrainRoot. It resolves canonical liquid definitions, retains only bodies intersecting loaded chunk rectangles, and prepares/releases their cropped `LiquidSurfaceTile` meshes through `WorldChunkStream`. A body has one cached shoreline, one animation phase and one rain pool, regardless of how many chunks it spans. There is no second chunk scheduler.

Geometry queries for scenery use the frozen full WaterBodyPlan, including unloaded neighbors. Player immersion selects a resident region through the existing WorldLiquidWading owner; ground navigation receives generic world-space polygons through WorldEntityPlacement. No generated water physics walls block the player: WG2 permits wading through the entire lake; swimming/depth rules are WG7. Ground enemies avoid generated water through the shared navigation grid. The original fixed pond keeps its established behavior.

Chunk fields use a global four-world-unit texel lattice plus a halo. Image work yields every eight rows under the existing stream application budget; textures/materials upload only once ready. Publishing a water tile waits for its scenery/ground chunk. Cancellation disposes its unfinished Image; unloading drops tile resources and frees the resident region/rain pool after its last owner. Renderer shader/noise resources are shared by Godot's resource cache; field/material instances are tile-specific.

No residents, disabled water, or static dry presentation stops the runtime callback. Animate/rain/config events wake it again; continuous animation uses one bounded camera/body visibility pass and updates visible bodies only. Rain pools sleep on dry/disabled/offscreen states. WorldLiquidWading retains one player physics lookup and sleeps its render callback after immersion settles. No body has its own simulation loop. Config changes never rebuild or move the saved shoreline.

See [WG2 validation, settings, save compatibility and local checks](../WG2_Validation.md). Lake-only generated-water worlds use blueprint schema 5; WG3 river worlds use schema 6; schema 4 retains no playable generated lakes, even when it contains WG1 metadata. Normal production water generation remains opt-in; use the disposable water practice scene for immediate testing.

## WG3 shared river input

The same runtime also presents schema-6 river polygons. Body edge bounds conservatively reject dry chunks/points and camera regions inside a winding corridor’s overall AABB; resident published tiles remain the only geometry owners. No separate river rendering or current simulation is added. Immutable centerline/width metadata belongs to generation/SAVE and is reserved for future WG6 flow. [WG3 contract](../../Generation/Liquids/Rivers/README.md).
