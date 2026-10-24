# Seasonal foliage

SE1 implements shared Resource-driven palettes and optional artwork masks on the existing sway shader. EnvironmentRuntime binds SeasonalFoliageAdapter after wind and before wetness/snow, including chunk publication and reverse-order release. No replacement scene, PNG, per-tree processor or global shader parameter is used.

- TreeDefinition.SeasonalFoliage opts a species into recolouring. Oak uses OakSeasonalFoliage.tres; Pink Blossom stays unchanged.
- TreeVisualVariant.FoliageMask is optional: grayscale source-size PNG, red channel weight, white leaves / black bark and background / grey partial influence. Mask alpha is ignored; artwork alpha is preserved once by sprite output. Oak references Sprites/tree_01_fm.png (222×295). The mask follows the same warped sample_uv, scale and mirroring as the artwork. It replaces colour/crown heuristics completely.
- Missing/invalid mask uses the existing green/pink colour and crown selection. Invalid dimensions warn once per loaded variant, then fall back. Validation reads dimensions only. New masks can be supplied gradually. Use lossless import, no mipmaps; the shader uses nearest filtering, repeat disabled and no source_color conversion for mask data.
- SeasonalFoliageProfile.Palette is an inspector Gradient: RGB specifies target leaf colour, alpha specifies influence. Summer (mid-year, around 0.5) alpha zero returns exact original RGB; 0 and 1 share January/Winter. Extra authored stops provide yellow → orange → red → brown. Source luminance and cluster shading remain; no perfect bare winter branches are promised.
- Each currently used species profile owns one runtime 16×2 float ImageTexture. Season commands update that texture synchronously; copied wet/snow/active-fire materials retain the same texture reference. No shader recompilation, material replacement, tree scan or chunk regeneration occurs on scrubbing. Disabled/zero-alpha commands canonicalize to a neutral state and skip redundant uploads.
- Material groups are bounded by loaded chunk/source material/mask/profile, not individual trees. Palette references and mask-validation entries are counted and released when chunks detach. Offscreen chunks need no season work; their shared state remains current. The existing wind/weather lifecycle is retained.

Visible stack: wind sample → original RGB saved → seasonal palette → rain darkening → snow → existing fire erosion → sprite tint/alpha. Weather selects leaves from original RGB or the same explicit mask, never from autumn RGB. Applying wetness after recolouring preserves darkening. No-mask wetness now uses sample_uv consistently with snow (previously its crown used unwarped UV).

The foliage_healthy instance flag suppresses selection after existing burn completion and restores on existing burn reset. These two compatibility assignments prevent mask-based recolouring of charred art; flame placement/charred art/burning mechanics are unchanged. Active FIRE2 material copies receive palette updates through the shared texture.

Idle: no adapter/controller process callbacks or texture uploads. Opted-in rendered fragments fetch one palette-state texel (plus one tone texel only when active) even when disabled; actual mask/recolour work exits at zero influence. Rain/snow use the explicit mask when active, independently of the season toggle. This is a GPU cost tradeoff, not a measured FPS claim.

See [SE1 validation](../../../../../ENVIRONMENT/Seasons/SE1_Validation.md) and [plan](../../../../../ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md). SE2 owns broader integration/performance acceptance and save compatibility.

## SE1b: organic timing and winter tone

The year is January-based. Palette.ToneControls (optional Gradient) encodes saturation in red and brightness lift in green; absent means saturation 1 / lift 0. Profile TimingVariationScale defaults to 1. Winter uses pale cool colour + desaturation + modest luminance lift, independently of snow. Source-black remains black. Global ColourStrength multiplies the final seasonal blend, so zero restores source RGB while weather can remain active.

The 16×2 atlas stores 16 early/late variants: row 0 target RGB/influence, row 1 tone controls. SeasonalFoliageAdapter samples all groups only on explicit control changes. Each sprite's seasonal_bucket instance uniform is assigned once from a deterministic FNV-1a world_instance_id hash; fixed compatibility-preview positions provide fallback IDs. All buckets share the same material/texture, including snow/fire copies. Variation zero uses the same date for every column; default ±14 days shifts transition timing without random flicker or per-frame work. Do not use runtime object IDs or string.GetHashCode for this identity.

Both normalized endpoints wrap to January 1. The Summer neutral plateau covers the default offsets. See [SE1b validation](../../../../../ENVIRONMENT/Seasons/SE1b_Validation.md). Automatic calendar progression remains deferred.
