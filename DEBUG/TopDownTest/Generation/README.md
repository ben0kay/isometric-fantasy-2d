# Debug procedural world scatter

> **Disposable 2D visual-test generator only.** Does not generate terrain geometry, world chunks, biomes or production WORLD data.

## What was changed

- The hand-placed **12 trees and 5 rocks were removed** from `TopDownVisualTest.tscn`.
- The original 960×640 clearing became a **2560×1728** playable grass map, world coordinates **(-800,-544) → (1760,1184)**, keeping the starting clearing, player, torch and ground detail near the centre.
- The old polygon trail extends north and east; all four invisible colliders and camera bounds match the larger map.
- On scene startup, the script attached to the Y-sorted `Actors` node procedurally places **80 trees and 34 rocks** by default. The generator runs **once in `_Ready()`**, not every frame. These props will be visible in the **running scene**, not placed permanently into the editor scene.
- Both sets reuse `Trees/Tree01.tscn` and `Rocks/Rock01.tscn`, including the previously authored ground anchors and root collision polygons. Future content variants can extend these templates later.

## Inspector settings

Open `DefaultScatterConfig.tres` to adjust:

| Setting | Default | Purpose |
| --- | --- | --- |
| Seed | 213876 | Deterministic but randomized-looking placement. Change the seed for a different map; identical seed/settings reproduce the same arrangement. |
| WorldBounds | (-800,-544,2560,1728) | Sampling region. **Scene grass, bounds, camera and overlay quads must match if size is changed.** |
| TreeCount / RockCount | 80 / 34 | Desired amounts, limited by available clear space. |
| AttemptsPerObject | 65 | Caps rejection sampling. The generator logs a warning if counts cannot fit. |
| TreeScaleMin / TreeScaleMax | 0.93 / 1.07 | Uniform tree root scale range. |
| RockScaleMin / RockScaleMax | 0.90 / 1.10 | Uniform rock root scale range. |
| MirrorChance | 0.50 | Probability of horizontal sprite/footprint mirroring for either kind. |
| TreeSpacingRadius / RockSpacingRadius | 55 / 32 | Coarse spacing radii, scaled by each object's size; not a physics/shape radius. |
| ExtraSpacing | 8 | Gap beyond pairwise radii, helps avoid overlap. |
| PlayerSpawn / StartClearingRadius | (480,320) / 180 | Leave a safe starting area around the player and torch. |
| PathHalfWidth | 27 | Extra no-prop margin along the visible trail. |
| EdgeMargin | 75 | Keep objects and most art away from the map edges. |

The inspector exposes the parameters in grouped categories. Tree and rock objects get a random *scale and mirror flag* after selecting a collision-safe ground position. When mirrored, **the CollisionPolygon2D points are reflected too**, with reversed vertex order preserving the polygon winding. The **StaticBody2D root gets only a positive, uniform scale**, which scales visual art and physics footprint together. No negative root scaling or per-frame collision updates.

The center-to-center spacing test keeps objects from overlapping **at their bases**, with some canopy overlap possible by design. This is visual scattering and approximate clearance, **not a complete vegetation/obstacle packing or navigability solver**. If you substantially increase counts/spacing, some requested objects may be skipped; the output log reports actual placed totals.

## Setup order and effects

0. `Liquids/Lake._Ready()` builds its reserved polygon before scenery placement.
1. `Actors._Ready()` creates all objects synchronously before the sibling DayNight and Atmosphere nodes' `_Ready()` run.
2. `DayNight` caches the generated sprites once and creates the usual projected sun silhouettes and grounding contact ovals. Tree wind uses the shared shader material and per-position phase.
3. `Atmosphere` caches the new tree locations for falling leaves, uses the full world bounds for pollen and its ground-haze shader, and can drift continuously.
4. `Polish` cloud-shade polygon now covers the whole map; post-processing controls still follow the camera.

**Performance:** This is deliberately bounded, roughly 114 static props by default. Placement is a small startup operation and no procedural work is performed per frame. Each prop still gets a sunlight-shadow draw/update in the debug day/night system. Profile the visual effects and shadows if raising prop counts substantially.

## Test checklist

- Build Godot C#; open `TopDownVisualTest.tscn` and press F6 (or F5 as configured).
- Look for `DEBUG world scatter | seed ... | trees ..., rocks ...` in the Godot output.
- Walk all directions: the grass, shadows, mist and cloud shade should continue, while the player remains within the larger bounds.
- Use **Debug → Visible Collision Shapes**. Try both mirrored/unmirrored rocks and trees; roots/rock bases should block movement, crowns should not.
- Change Seed and run again to confirm another arrangement. Keep the same seed and settings to reproduce it.
- Scrub time and enable wind, haze, leaves and cloud shade to check they work on *generated* objects.
- Current build/render/physics checks are documented in Liquids/README.md; Windows D3D12 and target hardware remain local checks.

## Decorative grass patches

Grass/ runs after tree/rock scattering and uses CanPlaceGrassDecoration to avoid occupied bases, the main trail, and the starting clearing. Grass has a separate seeded RNG and does not change tree/rock placement.

## Liquid footprint reservation

Liquids/Lake registers and builds its boundary before Actors.Ready. The generator caches those regions once and rejects tree/rock positions within the shoreline plus an object/art margin. CanPlaceGrassDecoration uses the same boundary with a bank margin. The footprint stays reserved while the liquid scene exists, even with its display toggled off; remove that scene and restart to fill the clearing normally. World size now matches the 2560×1728 authored ground, path, bounds, camera and overlays. See Liquids/README.md for three-seed placement validation and current Godot build/render checks.
