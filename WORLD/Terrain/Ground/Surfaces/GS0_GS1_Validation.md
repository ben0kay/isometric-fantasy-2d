# GS0 audit and GS1 handoff — 23 October 2026

Baseline revision: `b69421380b47012819c49a32ade0b1ce214a1329`.

## Status and evidence

GS0 source audit and migration map complete. Graphical reference captures and target-hardware CPU/GPU/memory baseline are **pending**, so GS0 acceptance is incomplete. GS1 data schema implemented; build/runtime/visual acceptance pending. This runner has neither dotnet nor Godot installed. No runtime, image-parity or FPS pass is claimed. Static checks verify unchanged renderer/profile/art/noise files, exact resource conversion and equality of the full frozen-baseline dependency set with the migration substitution. Migration hashes use the existing uppercase SHA256 digest convention. Do not proceed to GS2/GS3 aesthetics before local comparison.

## Exact migration map

- WorldBiomeDefinition.Surface previously referenced one GroundSurfaceProfile directly. GroundStack now owns an inline base layer; Surface resolves that profile for existing consumers. Legacy resources/tests that set Surface still work when no stack exists. Invalid stacks fail explicitly.
- OakForest/Biome.tres and PinkMeadow/Biome.tres preserve UIDs, populations, IDs and settings; only surface ownership becomes an inline base stack. Original ForestGrass/PinkMeadowGrass Resources are untouched. No overlays authored.
- StreamedGroundPalette.Build still reads Surface and builds one world material, five coarse maps and a nearest-sampled 32px artwork atlas; WorldChunkStream still creates a clipped quad per chunk. No renderer edits.
- GroundSurface.Configure and GrassSurface retain the independent adjacent-biome preview. Both renderers resolve the same original profiles through the adapter.
- Streamed shader world-space warp, shade/fresh/dry blending, 0.22 artwork influence and pixel-snapped fine grain remain untouched. Current nearest-cell artwork transition limitation is deferred to GS3.
- F9 retains exclusive Environment/Combat and Environment-only diagnostics; no Ground drawer until GS4. ENVIRONMENT snow/weather remain separate.
- CampaignSession fingerprints .tres/.tscn text. A narrow GS1 migration checks exact SHA256 of BOTH converted biome files, substitutes frozen previous biome text, and compares the full remaining dependency fingerprint. Changes elsewhere still reject. The next ordinary checkpoint uses the new fingerprint; no immediate disk rewrite. It does not bypass unrelated historic incompatibilities or compose the older SO1 scene migration.

## Repeatable graphical baseline (local Windows required)

Use two worktrees: baseline revision above and this change. Same Godot .NET version/renderer, hardware, window resolution, display scaling and project settings. In a disposable test configuration only, set WorldMasterConfig RandomSeedOnLoad=false and FixedSeed=213876. F6 World; never use a campaign for baseline captures. Restore uncommitted settings afterward.

Record renderer/version, GPU/driver, resolution, actual ActiveSeed, exact player/camera position, zoom, clock, wind, rain/snow/atmosphere/F9 values. Freeze time through the existing Environment controls; set identical weather and camera settings in both worktrees. Choose and record four positions on the baseline: spawn Oak, distant Oak, Pink Meadow, Oak/Pink boundary (locate using F7). Reuse those exact coordinates, camera and time for the changed revision. Capture with F7/F9 hidden. Ground should match; moving actors/particles should not be treated as ground differences. Also repeat with streaming disabled for legacy preview.

For each revision record cold startup separately from warmed traversal. After loading settles, sample 30 seconds idle, then the same 60-second route crossing chunk boundaries and returning. Capture frame-time distribution/peaks, CPU process/physics, GPU timing where available, memory, material/draw counts, loaded chunks and F9 slow-frame traces. Log cold/warm runs separately. Headless monitor values are not GPU measurements. This document contains **no measured performance numbers**.

## Local checks

From repository root with Godot .NET 4.7.2 and .NET 8 available (replace `godot` with your executable):

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . TESTS/Terrain/ValidateGroundStack.tscn
godot --headless --path . TESTS/Generation/ValidateMacroGeneration.tscn
godot --headless --path . TESTS/Generation/ValidateChunkStreaming.tscn
godot --headless --path . TESTS/DebugUI/ValidateDebugHud.tscn
```

Require zero exit status and explicit PASS output; report native shutdown failures separately. New fixture covers both authored bases, arbitrary third-biome legacy fallback/stack precedence, invalid authoring, disabled/unsupported overlays and Resource serialization. Run existing SAVE/P4_Copilot_Handoff.md write/read and real disk restore checks too. For deliberate migration evidence create/save a campaign on the baseline, Continue it on this revision, verify same world/player/edits, save/reopen, then confirm a genuinely changed dependency still rejects (restore test edits). Earlier incompatible campaigns remain protected.

## Performance contract

Activation: Resource resolution during setup only. Shutdown: returns immediately; no callbacks or persistent simulation. Idle CPU work: none added. Scaling: bounded two-overlay validation and one stack/base Resource per biome, no per-chunk materials or nodes. Graphical and timing parity remain unverified until the local checks above.

Subsequent authorized pre-GS2 tint addition changes the two surface shaders and setup wiring; unchanged-renderer statements above describe the original GS1 revision. See TINT_Validation.md. Original GS0 graphical/performance and GS1 local acceptance remain pending.
