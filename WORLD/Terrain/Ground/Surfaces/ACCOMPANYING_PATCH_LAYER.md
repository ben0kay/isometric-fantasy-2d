# Explicit accompanying patch layer — clarification before GS5

The author clarified the stack after manually checking the previous implementation in regular World and the practice scene. Those prior functional checks were reported passed; exact engine-suite logs and target performance numbers were not supplied. This extraction itself still needs a local Godot check.

## Current authoring contract

1. **DefiningBaseGround**: full coverage, source texture, single BaseColour and biome-owned Tint. Fine artwork/detail belong to this base.
2. **AccompanyingPatches**: a separate optional inline GroundPatchLayer Resource. Own ID/label, Enabled, Shade/Fresh/Dry colours, Strength and PatchSize provide underlying organic flavour. It does not consume either extra slot.
3. **OverlayLayers**: zero to two extra ordered surface patches (future dirt/rock, etc.), with the existing independent controls.

Oak and Pink both explicitly author their previously implicit grass flavour as accompanying layers, ON by default. Their palettes, 0.7 strength and 320-unit scale are copied exactly; original surface .tres/artwork/noise are untouched. Both extra arrays remain empty. No dirt/rock content or new production textures were added.

Explicit stacks with no accompanying Resource are base-only: the renderer never infers hidden mottling from their source profile. Surface-only legacy assets adapt the old palette fields during setup to retain their earlier look. GroundSurfaceProfile's old patch fields remain clearly grouped as **extra surface / legacy defaults**, used by extra material composition and legacy fallback, not by an explicit base stack. There is one active patch authority per biome.

## Rendering and F9

Streamed setup reads accompanying data into the same existing shade/fresh/dry maps and settings channel. Legacy preview does the equivalent binding. Companion colour blending remains before shared artwork/fine-detail composition to preserve the established image exactly. This is a logical/data layer, not an additional GPU draw/pass or texture. Disabled/missing/zero-strength accompanying data compiles to neutral base-only values; there is no mask work where strength is zero.

F9 now separates **Base appearance**, **Accompanying patch layer** and **Extra patch layers**. Show accompanying patches is independent of base fine detail and the extra-layer master/A/B toggles. All are session-only comparisons. Default reset respects authored enable/strength via compiled data; off/unconfigured companion layers remain off. Closing F9 retains overrides; scene unload discards them. Current biome's accompanying label and authored state are shown. The old API ShowBaseVariation remains a compatibility alias for ShowAccompanyingPatches.

Performance: same five appearance maps, optional pre-existing tint/style maps and shared world material; no extra sampler, draw, chunk node or frame callback. Resource resolution and legacy adaptation happen only during setup. F9 retains its visible-only 4Hz refresh and changed-only uniform writes. Actual target-hardware cost is not measured here.

## Save compatibility

Two frozen previous biome texts are stored under SAVE/Migrations/References/PatchLayer*.txt. Only the exact newly converted Oak AND Pink resource hashes permit fingerprint reconstruction with these previous texts; all other dependencies remain strict. The existing older GS1 bridge remains narrowly supported for the same exact converted assets. Saved world IDs, geography, scenery IDs and deltas do not change. A successful ordinary checkpoint uses the current fingerprint. This is not permission to bypass unrelated content differences.

## Verification and local check

Performed: software-GL fragment compilation/render comparison (pre-GS2/default maximum absolute error **0.0**), companion visibility/base-only comparison, reset, extra slots, deterministic sixteen-chunk seams; static numeric parity of extracted palettes; unchanged original profile .tres; full dependency reconstruction equality against pre-extraction text; exact migration hash checks and whitespace checks.

Not performed here: Godot import/C# compilation, engine fixtures, graphical Windows parity or disk restore/profiling. Godot and dotnet remain unavailable. The author's reported tests preceded this extraction, so they are not claimed as proof of this new change.

On the PC: build/import; F6 regular World → F9 Ground Surfaces. With accompanying patches ON, Oak and Pink should retain their tested look. Toggle them OFF to see the underlying single-colour grass base with artwork/detail, toggle fine detail separately, then reset. Extra A/B remain unconfigured. Repeat in compatibility preview and verify existing campaign Continue. No dirt/rock authoring is part of this job.

Run the existing local commands in GS4_Validation.md, especially ValidateGroundStack, ValidateGroundLayers and ValidateDebugHud. Tests now cover explicit no-companion base, separately serialized companion, enabled/disabled and invalid/duplicate companion data, pre-GS2 map parity, both migration paths and updated F9 captions. Engine tests are authored but unexecuted here.
