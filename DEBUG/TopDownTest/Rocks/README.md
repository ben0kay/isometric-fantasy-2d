# Debug rocks: shared sprite, root anchor and collision

Five boulders in `TopDownVisualTest.tscn` instance `Rock01.tscn`. The old `Artwork/rock.svg` and its obsolete SVG import metadata have been removed from this **debug folder only**.

## Art

- `Artwork/rock_01.webp`: transparent, tightly cropped and compressed 224×121 artwork derived from the **latest rock image generated in this conversation**. The user's locally placed asset was not present in the connected GitHub branch, so this imports the available chat image rather than guessing a filename from their computer. The high-resolution image is unchanged locally; the smaller WebP avoids committing a massive debug texture.
- Sprite is rendered at `0.315` relative to its source image, about 70×38 Godot world pixels before camera zoom. The bottom of the visible rock aligns with the `Rock01` root near its base.
- The generated image is transparent outside the rock; **no added ground shadow** is included. The existing day/night debug test renders the contact oval and projected sunlight shadow separately.
- Sprite uses linear filtering while scaled down. Adjust in the Sprite Inspector if experimenting with crisp nearest-neighbour pixels.

## Collision and Y-sorting

- Root `StaticBody2D` is the ground anchor; the shared `RockFootprint` is a convex polygon around the physical rock's lower mass, not a giant rectangle covering the transparent sprite canvas.
- Open `Rock01.tscn`, select **RockFootprint** and edit its points in the 2D editor if you want it wider/narrower. Godot's **Debug → Visible Collision Shapes** makes alignment easier to judge.
- If you uniformly scale any rock **root instance**, artwork, collision and contact-shadow size change together. Never apply visual-only Sprite scaling as an instance-level size variation.
- The debug day/night projected shadow now includes the root transform basis, so resizing rock instances also resizes the sunlight silhouette.

## Scope / verification

This is a fixed, editor-authored visual test; no runtime rock spawning or production WORLD content was implemented. Scene/resource references and collision vertices were inspected statically. Godot rendering and physics play-tests are not available in this connector environment.
