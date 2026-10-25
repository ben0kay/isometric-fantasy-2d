# PL3 production plant integration

Implemented from main `029fbfc`; awaiting native and Windows validation. No PL4 harvesting, grass/rock migrations or unrelated tree changes.

## Behavior and ownership

ChunkSceneryData consumes the existing candidate RNG sample for vegetation selection (including one-image index 0), without adding draws for legacy content. Candidate IDs remain definition-based. Per-variant radii/bounds now participate in halo, scaled/mirrored edge checks, cross-chunk spacing, water and reservation exclusion. Setup validates all vegetation resources; missing artwork/template or bounds smaller than the rooted PNG fail explicitly. Camera artwork margin includes every plant variant.

WorldChunkStream.ApplyOne and WorldSceneryPlacement instantiate the definition Template and call Configure before AddChild. Population scale stays on the root, sprite anchors/mirroring stay in VegetationInstance. Metadata retains content, biome, weather and shadow flags; streamed objects retain world-instance IDs and now record world_visual_variant. Existing chunk publication registers wind, snow and environment receivers; existing unload detaches them. Compatibility preview uses its existing seeded RNG and environment startup. No new managers, per-species scene/script or materials per instance.

Normal SCENES/World/World.tscn F6 needs no further wiring. Oak uses all eight authored populations; Pink receives none. PL2 densities and art settings are unchanged. F5 New Campaign uses the same production path.

## Save policy

Local generator version increases from 1 to 2: PL2 vegetation candidates were previously skipped, but now materialize and affect local layout. SaveCompatibility already compares this version strictly. Older version-1 campaigns are deliberately refused instead of silently reinterpreting their layout/deltas; no deletion, migration, fingerprint bypass or save-schema change. Retain old saves; use F6 or a new campaign. New v2 save/Continue must be checked locally.

## Performance

Activation is chunk instantiation or preview generation. Configure writes sprite properties once; plant Process/PhysicsProcess stay disabled. Wind animation runs through the existing shared shader/material adapters and visibility gating. Snow uses existing change-driven adapter composition. No per-frame resource loads, folders scans or added global loops; the definition's template/material references are shared. Validation runs at setup, not per frame. Unload uses existing chunk teardown; no new subscriptions/cache. Static sprite rendering and visible shared wind GPU work remain real costs. No FPS, memory or hitch improvement is claimed without profiling.

## Executed here

- Reviewed actual streamed/preview placement, shared wind/snow and SAVE compatibility paths.
- Verified all eight resource references exist and all authored bounds contain their full rooted PNG extents using Pillow/source data.
- git diff --check passed.
- Neither dotnet nor Godot is installed. Compilation/import, runtime fixtures, graphical climate behavior, save/Continue and profiling were not run.

## Local commands

From repository root with .NET 8 and Godot 4.7.2 .NET (replace godot with your executable):

```sh
dotnet build
godot --headless --editor --path . --import --quit
godot --headless --path . TESTS/Vegetation/ValidateVegetation.tscn
godot --headless --path . TESTS/Vegetation/ValidatePlantWorld.tscn
godot --headless --path . TESTS/Generation/ValidateChunkStreaming.tscn
```

Expected vegetation fixture markers: PL1 vegetation contract PASS and PL3 plant World PASS, exit 0. The new World fixture checks eight real resources, Oak/Pink permissions, production publication, canonical metadata, dormant instances, candidate revisit/order, temporary two-image candidate selection and compatibility preview. It does not substitute for visual or performance checks. Existing broad streaming test has historical baseline issues; report exact failures rather than claiming a clean suite.

## Manual acceptance

1. F6 normal World: walk through Oak and Pink at 1920×1080; confirm plants have grounded bases, reasonable scale/mirror, no water overlap or biome-edge art leakage. Tune authored data only after observing it.
2. F9 Environment: move wind to zero/back; enable snow, vary coverage, disable/re-enable. Confirm plants sway and snow returns to original art without stuck materials.
3. Travel several chunk distances away and return: verify same plant types, positions, mirrors, scales and variants; repeat with snow already active. Profile frame time/allocations, active chunk/node/material counts and unload cleanup.
4. Disable streaming in a disposable config to inspect compatibility preview; restore normal settings afterward.
5. New v2 campaign: save, leave, Continue and revisit plants. Confirm older v1 campaign rejection is graceful and files remain intact.
6. Run fixtures without editing first; report commands, exit codes, errors and screenshots/profiler evidence. Native AtlasTexture loading/anchor coverage is in PL1; atlas-specific shader appearance still requires visual testing before shipping atlas-authored content.
