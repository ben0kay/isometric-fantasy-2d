# Debug trees: collision and wind

The runtime debug generator instantiates **Tree01.tscn** about 58 times by default, controlled by `../Generation/DefaultScatterConfig.tres`. Edit Tree01 to update every tree's sprite, root anchor and footprint. Positions, positive uniform scale and mirror flags are **seeded in `Actors._Ready()`**, not manually placed in the editor.

## Anchor and collision

The imported PNG is 1086×1448; its meaningful opaque artwork ends around row 1331, leaving ~117px of transparent bottom padding. The previous offset aligned the image canvas bottom, not the visible roots, and retained the old 14×12 placeholder rectangle.

Tree01 uses a hand-selected ground anchor at source pixel **(548, 1290)** near the root footprint centre. With sprite scale 0.115 and Centered off, Sprite position is **(-63.02, -148.35)**. The StaticBody2D origin is the ground/sorting point. **TrunkFootprint** is a shallow convex polygon covering the base and broad roots, rather than the whole vertical tree silhouette.

Open Tree01.tscn, select TrunkFootprint and drag its points in the 2D editor. Run with **Debug → Visible Collision Shapes** to inspect the footprint against the artwork.

For future tree shapes:
1. Duplicate Tree01.tscn, assign the new texture.
2. Choose its source-pixel ground anchor and set Sprite position to **-anchor × sprite scale**, with Centered off.
3. Adjust the footprint polygon around that sprite's ground-contact area.
4. Duplicate the wind material if different sway settings are needed.
5. Instance the scene under the Y-sorted Actors node. Uniformly scale the whole tree root for size variations so artwork and collision scale together.

Alpha outlines cannot reliably identify a ground footprint: leaves, roots, shadows and padding differ. Author the footprint once per sprite variant. No new collision code is needed for each tree.

## Runtime seeded variation and collider scaling

- Tree root scales default to a **uniform random 0.93–1.07**. This scales the full StaticBody2D and the CollisionPolygon2D footprint together, keeping the collision at the roots; the world anchor is unchanged.
- Each generated tree has a configurable **50% horizontal mirror chance**. The generator mirrors the Sprite2D and the polygon's X coordinates (reversing order to preserve winding), *without negative physics-body scales*.
- The default random seed is **repeatable**, so restarting with the same settings produces the same shapes, sizes and positions. Change the seed to compare new scenes.
- Trees avoid each other's base/spacing radius, rock footprints, the original clearing and the main walking route. Some canopy overlap is natural and intentional.
- The day/night demo's sun silhouettes include the root scale and horizontal Sprite2D flip; contact shadows match object sizes. Existing wind material is shared among generated trees.


## Sway

TreeSway.gdshader borrows the old isometric-new-2d TreeWind shader's layered sine waves, gusts and placement-based phase. This implementation offsets texture sampling **per row**, so rows below UV.y=0.70 remain exactly still. A vertex bend on a single quad would shear the trunk too; sampling allows a fixed lower region without a subdivided mesh.

Edit **Tree01Wind.tres** in the Inspector:
- strength_pixels: 14 source-image pixels by default (~1.6 world pixels at 0.115 scale). Set 0 to disable sway.
- speed: 0.8 for slow wind; 0 freezes at each tree's phase.
- canopy_end: image row where movement stops, normalized from 0 at top to 1 at bottom. Adjust for different foliage/trunk proportions.

The collision never moves. One shared GPU shader/material animates the trees without per-frame C# callbacks. It still has texture sampling/overdraw costs; large forests need profiling. Existing linear filtering is preserved.

One flat image cannot separate leaves and branches at the same height: upper branches move with foliage. Sampling stays inside the original sprite rectangle, so excessive strengths may clip the canopy at side margins. Use subtle strengths or padded artwork for future assets.

## Validation

Checked the existing root anchor, reusable sprite references, polygon reflection logic and separation rules in the generator. Godot shader compilation, rendering and collision play-testing remain pending; no Godot executable is installed here. The imported PNG remains unchanged. The new C# generator and enlarged debug scene still require Godot compilation/play-testing.


## Axe impact feedback

Tree01 now opts into [HitFeedback](HitFeedback/README.md). Close aimed axe swings wobble only the struck tree artwork and emit a bounded burst of leaves. Collider, GPU wind and snow remain separate. No tree destruction or health is implemented.

## Pink Blossom tree — Shader Wind and Snow

The adjacent Pink Meadow now spawns **PinkBlossom01.tscn** using the same biome-aware scatter, root-collision mirroring, tree wind shader, DayNight projected shadows and SnowTest adapter as the forest oak. The artwork is a transparent palette-optimised rendering of the new concept, located at `Artwork/tree_pink_blossom_01.png` (160×213 source pixels), with a taller in-world proportion and an independently aligned root anchor. Its `PinkBlossomWind.tres` adjusts the sway strength for the smaller texture but **shares `TreeSway.gdshader`** rather than copying shader code. `snow_foliage_profile = 1` on the sprite opts into the reusable pink-blossom snow colour profile; green oak uses the default profile 0. The debug snow panel calls this shared experiment **Shader Wind and Snow**.

Pink trees also opt into the existing axe `HitFeedback/TreeHitFeedback.cs` through the same `HitFeedback` node contract, with their own `HitFeedback/PinkBlossomHitConfig.tres` for pink petals on a hit. The original oak hit behavior remains unchanged. Biome-scoped count and template live in `Biomes/PinkMeadow.tres` (default 36), not in a second tree spawner.
