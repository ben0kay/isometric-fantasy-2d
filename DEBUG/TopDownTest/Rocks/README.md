# Debug rocks: shared sprite, root anchor and collision

The disposable generator now instantiates **about 24** boulders from `Rock01.tscn` during `Actors._Ready()`; see [Generation](../Generation/README.md). The old `Artwork/rock.svg` and its obsolete SVG import metadata have been removed from this **debug folder only**.

## Art

- `Artwork/rock_01.webp`: transparent, tightly cropped and compressed 224×121 artwork derived from the **latest rock image generated in this conversation**. The user's locally placed asset was not present in the connected GitHub branch, so this imports the available chat image rather than guessing a filename from their computer. The high-resolution image is unchanged locally; the smaller WebP avoids committing a massive debug texture.
- Sprite is rendered at `0.315` relative to its source image, about 70×38 Godot world pixels before camera zoom. The bottom of the visible rock aligns with the `Rock01` root near its base.
- The generated image is transparent outside the rock; **no added ground shadow** is included. The existing day/night debug test renders the contact oval and projected sunlight shadow separately.
- Sprite uses linear filtering while scaled down. Adjust in the Sprite Inspector if experimenting with crisp nearest-neighbour pixels.

## Collision and Y-sorting

- Root `StaticBody2D` is the ground anchor; the shared `RockFootprint` is a convex polygon around the physical rock's lower mass, not a giant rectangle covering the transparent sprite canvas.
- Open `Rock01.tscn`, select **RockFootprint** and edit its points in the 2D editor if you want it wider/narrower. Godot's **Debug → Visible Collision Shapes** makes alignment easier to judge.
- Every generated rock gets a **positive, uniform 0.90–1.10 scale** by default, applied to the complete StaticBody2D so the sprite and collision scale together. Its sunlight and contact shadows are sized accordingly.
- Roughly half of rocks are mirrored horizontally: Sprite2D.FlipH is paired with a reflected, correctly wound RockFootprint polygon. Negative body scaling is avoided for physics stability.
- Rock spacing is checked against other rocks and trees, plus the initial clearing and the walkable trail, using approximate placement radii. Seed and density are Inspector-tunable under `Generation/DefaultScatterConfig.tres`.
- The debug day/night projected shadow now includes the root transform basis, so resizing rock instances also resizes the sunlight silhouette.

## Scope / verification

This is a bounded **runtime-scattered** visual test, not a production WORLD generation system. Scene/resource references and collision vertices were inspected statically. Godot rendering and physics play-tests are not available in this connector environment.
