# Disposable liquid-region / lake and wading test

F5/F6 shows one lake southeast of the starting clearing, centred near **(1080,800)** in the enlarged **2560×1728** world. The right-side panel below Weather has **Show lake / wading**, surface animation, shoreline and ripple controls. **Go to lake bank** moves the player safely to its west bank for comparisons. Walk into the water with the arrow keys.

## Behaviour

The lake is walkable. Near the shore the player gradually dips; deeper inside its **sprite** moves down up to 14 world pixels and clips below the waterline. The CharacterBody2D, feet collider and camera do not sink or change height. Default water slows movement to 70% in the deeper part. A pair of restrained rings marks the waterline; the contact/sun shadows are hidden while submerged. Walking out or disabling the comparison restores the normal appearance/speed; deleting the Liquids scene instance during play restores the original player material too.

This is visual wading, not swimming, drowning, buoyancy, survival or damage. Other liquid types reuse the region/renderer/response, with different data. No hardcoded water ID check selects a separate renderer.

## Files / separate ownership

- `LiquidDefinition.cs` / `Definitions/Water.tres`: stable ID/name, shallow/deep/highlight/bank palette, wave speed, movement multiplier, maximum visual submerge and shore-to-depth distance. Swap this Resource for a different visual/traversal profile; toxic damage or quicksand gameplay is not implemented.
- `LiquidMasterConfig.cs` / `DefaultLiquidConfig.tres`: comparison switches, ripple amount, shore width and **startup** centre/radius/shape seed. Appearance updates live. Shape edits require restarting.
- `LiquidRegion.cs`: creates a stable 96-point harmonic perimeter and exposes signed shore distance / `Contains(worldPoint, margin)`. Positive distances are inside. One polygon drives every placement and player-depth query.
- `LiquidRegion.cs` also generates a small **384×256 floating-point distance texture once**, so the shader samples shoreline distance rather than calculating distances to 96 segments for every pixel. No per-frame geometry/mask rebuilding or texture uploads. Moving/scaling the region root after startup is outside this prototype contract; use the startup config.
- `Shaders/LiquidColour.gdshaderinc`: palette/depth only; `LiquidMovement.gdshaderinc`: waves only; `ShoreEdge.gdshaderinc`: variable-width wet bank only. `LiquidSurface.gdshader` composes them with regular CanvasItem lighting. The mask's Y coordinate explicitly matches the QuadMesh UV convention.
- `LiquidWading.cs` / `Shaders/WadingSprite.gdshader`: player visual response and one shared surface-speed multiplier, separate from liquid appearance and shape. Only the demo player is connected in V1.
- `LiquidTestController.cs` / `LiquidTest.tscn`: grouping, compact controls and existing Weather inputs. No new weather controller or global post-process.

Water has an opaque interior so old ground textures/snow do not show through. Its shallow edge and tiny highlights provide depth without 3D terrain or external artwork. Wind integrates surface drift, avoiding pattern jumps when its slider changes. Animation off freezes accumulated movement. Existing day/night, torch and lightning lighting still apply. Fog/cloud shade continue above the lake. No reflection, refraction, new normal-map lighting, ice, fishing, water simulation or per-raindrop water ripples are included.

## Placement and draw order

Liquids appears **before Actors** in the main scene. Its region is registered/built before tree/rock scattering, and decorative grass subsequently calls the same scatter exclusion helper. Trees/rocks reserve an artwork/base/shore margin; grass reserves an extra bank margin. There are no solid lake colliders and no invisible rectangular barrier. The field rectangle is only a rendering support; outside the actual water/bank its alpha is zero.

Ground is Z=-10, gravel -8, ground snow -7, liquid surface -6, sun/contact shadows -5 and actors 0. Decorative grass is also -6, but its placement excludes the lake and bank. This keeps the lake unfrozen/visible when ground snow is enabled. Dry-ground rain splashes and manual lightning ground targets exclude active water using the same region query. Snowflakes/rain can still pass over the lake as foreground weather.

The footprint stays **reserved for startup scenery whenever the Liquids scene exists**, including if its display starts off. Toggling the lake does not re-scatter props, so disabling it reveals an empty grass clearing in its place. Remove the scene instance and restart to populate that area normally. The lake is located away from the starting clearing and the north/east trail.

## Larger world

Bounds are **(-800,-544) → (1760,1184)**, with the original player/torch positions preserved. Camera limits, grass rectangle, trail endpoints, four bounds colliders, cloud shade and haze polygons match. Snow ground coverage and pollen use the shared scatter bounds automatically. Default population is now **80 trees / 34 rocks**; its seed stays 213876. Scenery still generates once, not continuously, and this is not chunk streaming or biome generation.

## Easy removal / undo

For comparisons, turn **Show lake / wading** off. To remove the feature completely:

1. Delete the **Liquids** instance and its external-resource entry from TopDownVisualTest.tscn.
2. Remove the small `LiquidRegion` cache/query additions from `Generation/DebugWorldScatter.cs` and `Weather/DebugRainSplashes.cs` (search `LiquidRegion` / `_liquids` / `debug_liquid_regions`).
3. Delete this Liquids/ folder. The generic `SurfaceSpeedMultiplier` in TestPlayerMovement may remain at its default 1, or be removed with its multiplication term.

The grass, weather, snow, original tree/rock artwork and production folders have no shader/resource dependency on Liquids. The independently enlarged world and smoother gravel can remain. Reverting the single task commit undoes all three parts together.

## Cost and validation

One masked lake draw includes its shoreline; one temporary player material plus two small drawn rings handle wading. Queries use cached polygon edges, with no scene searches or per-pixel CPU loops during play. The distance texture rasterization is a bounded startup cost. Surface shader work is limited to the local lake rectangle. Profile real hardware before scaling to many lakes.

All debug C# scripts compile with Godot 4.5.1 source generators and against GodotSharp 4.7.2 / .NET 8. Actual software OpenGL renders checked the bank, dipped player, normal shadow material, snow ordering and restoring the player when disabled. Sampled distance-field values were compared to polygon queries. Three scatter seeds placed all **80 trees + 34 rocks** outside water/shore margins, with grass exclusions and valid triangulatable outlines; another LiquidDefinition palette used the same renderer. Local render checks reduced decorative grass density (all tree/rock counts retained), as in the existing snow tests. Windows D3D12 appearance and performance remain local checks.

Additional physics checks verified slower movement in deep water, unchanged camera child position, collision at the expanded east/north map limits, and original player-material restoration when the whole liquid test was removed.
