# Foliage mask authoring — SE3

Use one optional mask per distinct healthy sprite. All instances, seasonal palettes and rain/snow reuse it. Missing masks retain existing green/pink/crown detection; masks can be added gradually.

## Avalonia → Godot

1. Export the healthy sprite at its final source resolution first. Preserve this canvas while making the mask. Oak currently uses 222×295; the mask must be 222×295 too, regardless of in-game scale. Crop/resize both together if you change the source later.
2. Refine the selection in Avalonia: include dark outer leaves, exclude bark/branches/roots and leaf gaps. Export an **opaque grayscale PNG**: white=full foliage, black=excluded, grey=partial influence. Store weight in RGB brightness (Godot samples red), not transparency. Transparent source padding should be black in the mask.
3. Save beside the artwork: `Sprites/tree_01.png` + `Sprites/tree_01_fm.png`. The suffix is a convention, not automatic discovery. Do not copy masks into biome folders or change the healthy PNG destructively.
4. In the tree definition's VisualVariants entry, assign Texture and **FoliageMask** explicitly. Oak's inline variant in Oak.tres is the working example. A species' SeasonalFoliage palette is separate; a mask can improve weather selection even without seasonal recolouring.
5. Godot import: lossless compression, no mipmaps, no size limit/downscale, no premultiplied alpha, unchanged red channel. The shader uses nearest/non-repeating data sampling without source_color conversion. Full-canvas PNG textures are supported; AtlasTexture regions intentionally fall back until a region-UV contract is implemented.
6. Run the offline audit below, then check the real sprite with Autumn, rain/snow, strong wind and mirroring. Refining selection quality is still an artistic decision: software cannot infer whether a particular dark pixel is bark or a leaf.

```sh
python TESTS/Seasons/check_foliage_masks.py --self-test
python TESTS/Seasons/check_foliage_masks.py path/to/tree.png path/to/tree_fm.png
```

With no explicit image paths the audit finds explicit ExtResource pairs in canonical tree .tres variant sections. It checks PNG readability, dimensions, grayscale, opaque mask alpha, selected padding and import settings. It does not silently fix or resave artwork. A mask with no selected visible pixels gets a note, since an all-black mask can be deliberate.

## Runtime and failure behaviour

FoliageMaskValidation checks source/mask metadata once per unique currently loaded variant, cached by SeasonalFoliageAdapter. Missing mask is silent. Wrong dimensions, missing healthy art or atlas-region textures produce one binding diagnostic per loaded variant and use automatic detection. Cache references release with chunk unload; no pixel readbacks, filesystem scans or validation callbacks run per frame or on season scrubbing. MaskValidationChecks exposes the count for local checks.

A typo in a .tres external path can fail Godot's Resource loader **before** runtime validation can run; fix that path or clear the reference. The offline audit reports missing files/unresolved references. It cannot suppress engine import/load errors.

A valid explicit mask replaces colour and crown-height detection; multiplying those heuristics back in would reject selected dark leaves. It samples the same warped sample_uv as the source. Final sprite alpha clips the result once; mask alpha is not a second opacity multiplier. Grey weights soften effect influence; they do not make the healthy sprite transparent.

## Wind is spatial sway, not leaf detection

VegetationSway moves horizontal texture rows using a height-based crown weight. The top moves most; movement fades between normalized source Y=0.12 and canopy_end (default 0.70); lower rows/roots remain still. Sine waves/gusts and a world-position phase vary the motion. Upper branches in those rows move too. Wind strength is compensated for sprite scale and expressed in world units; camera zoom still changes apparent screen movement.

The draw quad expands for a transparent wind margin so foliage is not clipped. Seasons/rain/snow sample their selection at the resulting source UV; they do not decide which pixels wind moves. Using the foliage mask to isolate extra leaf flutter would be a separate feature, not part of SE3.

See [SE3 validation](../../../../../ENVIRONMENT/Seasons/SE3_Validation.md) for test coverage and local commands. No fire-mask feature, new art or calendar is added.
