# Oak

Oak.tres is canonical species data for the shared ../Shared/Tree.tscn. Its inline TreeVisualVariant configures the existing sprite, root anchor, scale, footprint and placement bounds. The former species scene is removed; biomes continue referencing the same .tres path.

Hit feedback, wind, snow, shadows and optional burning use shared components. OakFireProfile.tres supplies the existing charred art and anchor.

For new species/variants, edit Resources and biome references only. See ../README.md.

2026-10-23 sprite refresh: tree_01.png is the downsampled Tree_Oak01.png (222x295, 83,613 bytes); oak_charred.png is Tree_Oak01_Charred.png (229x286, 66,006 bytes). The original texture paths/import UIDs are retained. The inline healthy SourceAnchor (109.6,258) and SpriteScale (0.575) compensate for approximately 5x smaller source dimensions to preserve the existing rendered footprint and unchanged world-space trunk collider. OakFireProfile explicitly aligns burnt art at (113,247) for fire crossfade/mirror. Check a rendered oak and charred/reset state in Godot after import.

SE1: tree_01_fm.png is the 222×295 opaque grayscale Avalonia mask. Oak.tres explicitly binds its red channel as foliage weight; OakSeasonalFoliage.tres authors the Gradient (RGB target / alpha influence). Summer around 0.5 is untouched (SE1b January-based year). F9 → Environment → Seasonal foliage enables the preview. This resource change affects the strict campaign fingerprint: use F6/new campaign pending SE2 migration.

SE1b: Oak palette now includes pale winter blue-green and separate saturation/brightness tone controls. F9 adds overall strength and stable early/late timing variation; January is the start/end of the preview year.

SE3 source audit passed for tree_01_fm.png: opaque grayscale, matching 222×295 source canvas, no selected transparent padding. [Export/assignment guide](../../Shared/Seasonal/MASK_AUTHORING.md); masks are explicitly assigned, not discovered by filename.
