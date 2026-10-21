# Debug trees: collision and wind

All 12 clearing trees instance **Tree01.tscn**. Edit that one scene to update this sprite variant everywhere.

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

## Sway

TreeSway.gdshader borrows the old isometric-new-2d TreeWind shader's layered sine waves, gusts and placement-based phase. This implementation offsets texture sampling **per row**, so rows below UV.y=0.70 remain exactly still. A vertex bend on a single quad would shear the trunk too; sampling allows a fixed lower region without a subdivided mesh.

Edit **Tree01Wind.tres** in the Inspector:
- strength_pixels: 14 source-image pixels by default (~1.6 world pixels at 0.115 scale). Set 0 to disable sway.
- speed: 0.8 for slow wind; 0 freezes at each tree's phase.
- canopy_end: image row where movement stops, normalized from 0 at top to 1 at bottom. Adjust for different foliage/trunk proportions.

The collision never moves. One shared GPU shader/material animates the trees without per-frame C# callbacks. It still has texture sampling/overdraw costs; large forests need profiling. Existing linear filtering is preserved.

One flat image cannot separate leaves and branches at the same height: upper branches move with foliage. Sampling stays inside the original sprite rectangle, so excessive strengths may clip the canopy at side margins. Use subtle strengths or padded artwork for future assets.

## Validation

Checked PNG dimensions and alpha bounds, all 12 scene instances, resource references, polygon convexity and zero sway below the canopy cutoff. Godot shader compilation, rendering and collision play-testing remain pending; no Godot executable is installed here. C# gameplay and the imported PNG are unchanged.
