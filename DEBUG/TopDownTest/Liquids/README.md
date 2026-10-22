# Disposable liquid-region / lake and wading test

F5/F6 shows one lake southeast of the starting clearing, centred near **(1080,800)** in the enlarged **2560×1728** world. The right-side panel below Weather has **Show lake / wading**, surface animation, shoreline and ripple controls. **Go to lake bank** moves the player safely to its west bank for comparisons. Walk into the water with the arrow keys.

## Behaviour

The lake is walkable. Near the shore the player gradually dips; deeper inside its **sprite** moves down up to 14 world pixels and clips below the waterline. The CharacterBody2D, feet collider and camera do not sink or change height. Default water slows movement to 70% in the deeper part. A pair of restrained rings marks the waterline; the contact/sun shadows are hidden while submerged. Walking out or disabling the comparison restores the normal appearance/speed; deleting the Liquids scene instance during play releases wading; the shared helper retains any active snow burial.

This is visual wading, not swimming, drowning, buoyancy, survival or damage. Other liquid types reuse the region/renderer/response, with different data. No hardcoded water ID check selects a separate renderer.

## Files / separate ownership

- `LiquidDefinition.cs` / `Definitions/Water.tres`: stable ID/name, shallow/deep/highlight/bank palette, wave speed, movement multiplier, maximum visual submerge and shore-to-depth distance. Swap this Resource for a different visual/traversal profile; toxic damage or quicksand gameplay is not implemented.
- `LiquidMasterConfig.cs` / `DefaultLiquidConfig.tres`: comparison switches, ripple amount, shore width and **startup** centre/radius/shape seed. Appearance updates live. Shape edits require restarting.
- `LiquidRegion.cs`: creates a stable 96-point harmonic perimeter and exposes signed shore distance / `Contains(worldPoint, margin)`. Positive distances are inside. One polygon drives every placement and player-depth query.
- `LiquidRegion.cs` also generates a small **384×256 floating-point distance texture once**, so the shader samples shoreline distance rather than calculating distances to 96 segments for every pixel. No per-frame geometry/mask rebuilding or texture uploads. Moving/scaling the region root after startup is outside this prototype contract; use the startup config.
- `Shaders/LiquidColour.gdshaderinc`: palette/depth only; `LiquidMovement.gdshaderinc`: waves only; `ShoreEdge.gdshaderinc`: variable-width wet bank only. `LiquidSurface.gdshader` composes them with regular CanvasItem lighting. The mask's Y coordinate explicitly matches the QuadMesh UV convention.
- `LiquidWading.cs` / `../Surfaces/PlayerSurfaceVisual.cs`: liquid response and one shared surface-speed multiplier, separate from liquid appearance and shape. Only the demo player is connected in V1.
- `LiquidTestController.cs` / `LiquidTest.tscn`: grouping, compact controls and existing Weather inputs. No new weather controller or global post-process.
- `LiquidRainSplashes.cs`: one bounded 96-slot pool per region, placed over the liquid but below actors. Samples the camera-visible lake rectangle, accepts only points at least a little inside the polygon, and draws a tiny splash/expanding elliptical ripple using the active liquid's highlight colour. No individual nodes for raindrops, no collision callbacks, no extra GPU rain emitter.

Water has an opaque interior so old ground textures/snow do not show through. Its shallow edge and tiny highlights provide depth without 3D terrain or external artwork. Wind integrates surface drift, avoiding pattern jumps when its slider changes. Animation off freezes accumulated movement. Existing day/night, torch and lightning lighting still apply. Fog/cloud shade continue above the lake. Rain now creates separate **small surface splashes and expanding rings** on any liquid profile; these are stylized sampled impacts, not collisions for every GPU raindrop. No reflection, refraction, new normal-map lighting, ice, fishing or physical water simulation is included.

## Rain splashes on liquid surfaces

The existing `Weather.Rain` intensity drives how often small impacts appear on the liquid. These **reuse the dry-land rain splash visual language**, but form flatter expanding rings appropriate for a lake instead of ground splatters. The impact colour comes from `LiquidDefinition.HighlightColour`, so the exact same renderer also works for future mud, quicksand or toxic pools.

- **Rain at 0:** no new splashes; remaining rings finish fading naturally.
- **Light to heavy rain:** proportionally more impacts, using the same gradual rain transition as the existing Weather slider.
- **Lake disabled:** splashes disappear and the pooled effects are cleared.
- **Surface animation disabled:** the continuous wave shader pauses, while rain impacts can still appear because they are separate real-time precipitation.
- **Existing Ground splashes toggle:** still affects **dry ground only**. The new **Rain surface splashes** switch in the Liquid panel independently controls the lake.

Tune `RainSurfaceSplashes`, `RainSplashRate` (default 72 candidate impacts/second at full rain), `RainSplashOpacity`, `RainSplashSize`, and `RainSplashLifetime` (default 0.6 s) in `DefaultLiquidConfig.tres` / `LiquidMasterConfig.cs`. Impact density depends on the portion of the lake visible and its actual polygon shape; it is **not a physical count of falling raindrops**.

Implementation is **one draw node per region**, a fixed **96-impact pool**, at most **8 spawn attempts per frame** with up to 8 bounded polygon checks each, no per-frame texture upload or collision tests. Region startup caches a world-space lake bounding rectangle for fast camera clipping. Normal 2D lighting still tints the impacts. The old dry-ground splash pool and rain emitter are untouched.

**Test:** Go to lake bank, set Rain to 0, then 25%, 55%, and 100%; watch surface rings increase. Turn Wind between -3 and +3 to confirm the existing surface wave motion remains independent. Toggle Rain surface splashes, Surface animation, Show lake, and Ground splashes separately. Walk/wade across the water while raining; the wading rings and rainfall impacts should coexist.

## Placement and draw order

Liquids appears **before Actors** in the main scene. Its region is registered/built before tree/rock scattering, and decorative grass subsequently calls the same scatter exclusion helper. Trees/rocks reserve an artwork/base/shore margin; grass reserves an extra bank margin. There are no solid lake colliders and no invisible rectangular barrier. The field rectangle is only a rendering support; outside the actual water/bank its alpha is zero.

Ground is Z=-10, gravel -8, ground snow -7, liquid surface -6, liquid rain splashes -4 (same level as dry-ground splashes), sun/contact shadows -5 and actors 0. Decorative grass is also -6, but its placement excludes the lake and bank. This keeps the lake unfrozen/visible when ground snow is enabled. Dry-ground rain splashes and manual lightning ground targets exclude active water using the same region query. Snowflakes/rain can still pass over the lake as foreground weather.

The footprint stays **reserved for startup scenery whenever the Liquids scene exists**, including if its display starts off. Toggling the lake does not re-scatter props, so disabling it reveals an empty grass clearing in its place. Remove the scene instance and restart to populate that area normally. The lake is located away from the starting clearing and the north/east trail.

## Larger world

Bounds are **(-800,-544) → (1760,1184)**, with the original player/torch positions preserved. Camera limits, grass rectangle, trail endpoints, four bounds colliders, cloud shade and haze polygons match. Snow ground coverage and pollen use the shared scatter bounds automatically. Default population is now **80 trees / 34 rocks**; its seed stays 213876. Scenery still generates once, not continuously, and this is not chunk streaming or biome generation.

## Easy removal / undo

For comparisons, turn **Show lake / wading** off. To remove the feature completely:

1. Delete the **Liquids** instance and its external-resource entry from TopDownVisualTest.tscn.
2. Remove the small `LiquidRegion` cache/query additions from `Generation/DebugWorldScatter.cs` and `Weather/DebugRainSplashes.cs` (search `LiquidRegion` / `_liquids` / `debug_liquid_regions`).
3. Delete this Liquids/ folder. The generic `SurfaceSpeedMultiplier` in TestPlayerMovement may remain at its default 1, or be removed with its multiplication term.

The shared Surfaces/ helper and its immersion shader are used by both snow and liquids. Keep Surfaces/ while either experiment remains, and remove its typed Liquid hooks when deleting liquid scripts. Grass, weather, original artwork and production folders otherwise retain their existing ownership. The independently enlarged world and smoother gravel can remain. Reverting the single task commit undoes all three parts together.

## Cost and validation

One masked lake draw includes its shoreline; one temporary player material plus two small drawn rings handle wading. Rain impacts add a small pooled draw node with no physics bodies, allocated once per liquid region. Queries use cached polygon edges, with no scene searches or per-pixel CPU loops during play. The distance texture rasterization is a bounded startup cost. Surface shader work is limited to the local lake rectangle. Profile real hardware before scaling to many lakes.

All debug C# scripts compile with Godot 4.5.1 source generators and against GodotSharp 4.7.2 / .NET 8. Actual software OpenGL renders checked the bank, dipped player, normal shadow material, snow ordering and restoring the player when disabled. Sampled distance-field values were compared to polygon queries. Three scatter seeds placed all **80 trees + 34 rocks** outside water/shore margins, with grass exclusions and valid triangulatable outlines; another LiquidDefinition palette used the same renderer. Local render checks reduced decorative grass density (all tree/rock counts retained), as in the existing snow tests. Windows D3D12 appearance and performance remain local checks.

Additional physics checks verified slower movement in deep water, unchanged camera child position, collision at the expanded east/north map limits, and original player-material restoration when the whole liquid test was removed.


## Wider transition and shallow snow depth

Biome blending now spans 240 world pixels (previously 120), editable with DefaultBiomeConfig / BlendWidth. SnowDepth.gdshaderinc adds smooth shallow drifts and restrained bank/hollow shading without altering distributed coverage or the exact 0/100% coverage endpoints. SnowMasterConfig exposes DriftScale, DriftContrast, PlayerSinks (on by default) and MaximumSinkPixels (4). Snow itself remains opt-in as before. Player burial begins above 25% settled snow (a small dip at the default 60%), varies with the same world-space depth field, and reaches at most 4 pixels; body/collider/camera and movement speed remain unchanged.

PlayerSurfaceVisual is the single sprite/material owner shared by optional snow and lake tests. Liquid immersion takes precedence; snow produces no water rings and keeps the projected sun shadow. Toggles/melting ease the sprite back up. Removing both experiments restores its original position/material. The helper is installed by either test, so snow burial also works with the Liquids scene removed.
