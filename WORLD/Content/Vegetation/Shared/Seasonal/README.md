# Seasonal foliage

SE1 implements shared Resource-driven palettes and optional artwork masks on the existing sway shader. EnvironmentRuntime binds SeasonalFoliageAdapter after wind and before wetness/snow, including chunk publication and reverse-order release. No replacement scene, PNG, per-tree processor or global shader parameter is used.

- TreeDefinition.SeasonalFoliage opts a species into recolouring. Oak uses OakSeasonalFoliage.tres; Pink Blossom stays unchanged.
- TreeVisualVariant.FoliageMask is optional: grayscale source-size PNG, red channel weight, white leaves / black bark and background / grey partial influence. Mask alpha is ignored; artwork alpha is preserved once by sprite output. Oak references Sprites/tree_01_fm.png (222×295). The mask follows the same warped sample_uv, scale and mirroring as the artwork. It replaces colour/crown heuristics completely.
- Missing/invalid mask uses the existing green/pink colour and crown selection. Invalid dimensions warn once per loaded variant, then fall back. Validation reads dimensions only. New masks can be supplied gradually. Use lossless import, no mipmaps; the shader uses nearest filtering, repeat disabled and no source_color conversion for mask data.
- SeasonalFoliageProfile.Palette is an inspector Gradient: RGB specifies target leaf colour, alpha specifies influence. Summer (0.25) alpha zero returns exact original RGB; 0 and 1 share Spring. Extra authored stops provide yellow → orange → red → brown. Source luminance and cluster shading remain; no perfect bare winter branches are promised.
- Each currently used species profile owns one runtime 1×1 float ImageTexture. Season commands update that texture synchronously; copied wet/snow/active-fire materials retain the same texture reference. No shader recompilation, material replacement, tree scan or chunk regeneration occurs on scrubbing. Disabled/zero-alpha commands canonicalize to a neutral state and skip redundant uploads.
- Material groups are bounded by loaded chunk/source material/mask/profile, not individual trees. Palette references and mask-validation entries are counted and released when chunks detach. Offscreen chunks need no season work; their shared state remains current. The existing wind/weather lifecycle is retained.

Visible stack: wind sample → original RGB saved → seasonal palette → rain darkening → snow → existing fire erosion → sprite tint/alpha. Weather selects leaves from original RGB or the same explicit mask, never from autumn RGB. Applying wetness after recolouring preserves darkening. No-mask wetness now uses sample_uv consistently with snow (previously its crown used unwarped UV).

The foliage_healthy instance flag suppresses selection after existing burn completion and restores on existing burn reset. These two compatibility assignments prevent mask-based recolouring of charred art; flame placement/charred art/burning mechanics are unchanged. Active FIRE2 material copies receive palette updates through the shared texture.

Idle: no adapter/controller process callbacks or texture uploads. Opted-in rendered fragments fetch one palette-state texel even when disabled; actual mask/recolour work exits at zero influence. Rain/snow use the explicit mask when active, independently of the season toggle. This is a GPU cost tradeoff, not a measured FPS claim.

See [SE1 validation](../../../../../ENVIRONMENT/Seasons/SE1_Validation.md) and [plan](../../../../../ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md). SE2 owns broader integration/performance acceptance and save compatibility.
