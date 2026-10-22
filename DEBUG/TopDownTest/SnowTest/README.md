# Removable snow accumulation and snowfall test

Implements `Notes/Snow_Accumulation_Codex_Test.txt` as a disposable comparison. Build C# and F5/F6; the snow panel sits below gravel on the left. **The test starts OFF**, preserving the existing presentation.

## Controls

- **Snow test:** enables the whole comparison. Off immediately clears settled snow and hides all falling flakes; the saved coverage target is retained for the next comparison.
- **Settled snow slider:** 0–100% tree-canopy, decorative grass, rocks and ground coverage. Dragging scrubs immediately. Enabling or changing the Inspector target blends over TransitionSeconds (3 seconds default).
- **Falling snow / Snowfall intensity:** separate controls for slow, varied GPU flakes. Shared Weather wind drives their left/calm/right drift and existing tree sway.
- **Auto accumulate / melt:** grows coverage toward 100% while snowfall/rate is active, and melts toward zero when snowfall stops. Default full-range duration is 30 real seconds, independent of the accelerated day clock.

**Snow automatically affects trees, grass, ground and all opted-in rocks together.** There are no separate grass or rock snow switches. Rock-cap depth remains a visual-tuning Inspector property (`RockCapDepth`), not an enable switch.

Rain and lightning settings are not changed by this test. For a pure snowstorm, set the existing rain slider to zero; mixed precipitation is possible for comparisons. Restart restores saved config defaults. Inspector settings also include snow colour and transition/accumulation duration. UI checkbox states only follow their UI; the coverage slider follows Inspector/automatic changes.

## Easy removal

In `TopDownVisualTest.tscn`, delete the **SnowTest** scene instance and its `snowtest` external-resource entry, then delete this **SnowTest/** folder. No other shader/controller depends on this folder. Godot can update the scene resource count when saving. No production folders, global shader settings or project rendering settings were changed.

Deleting the SnowTest node during play restores original tree, grass and rock materials and mirrored wind parameters. The scene instance is deliberately last, after scenery generation and DayNight's material cache. For a quick comparison, the on/off toggle is sufficient.

## Separate pieces

- `SnowMasterConfig.cs` / `DefaultSnowConfig.tres`: all snow settings.
- `DebugSnowTest.cs`: comparison UI, automatic progression, ground-cover material and cleanup.
- `DebugSnowfall.cs`: C# **GpuParticles2D** emitter with a generated four-frame crisp pixel flake atlas, varied size/speed, and camera/travel margins. World coordinates keep existing flakes from sticking to the camera. No per-flake nodes or CPU collision queries.
- `TreeSnowAdapter.cs`: temporary shared normal/mirrored material variants for trees and grass, retaining the existing wind shader composition and UV warp. Original materials/shaders remain unchanged; cached source wind values are copied per shared variant rather than per tree. Projected shadow materials are never replaced.
- `RockSnowAdapter.cs` / `RockSnow.gdshader`: opt-in rock snow caps using the **same** settled coverage, colour and terrain noise, with one material for all rocks and no additional rock textures. The adapter skips the rock's unshaded sun silhouette and restores the original Sprite2D material when disabled or removed.
- `TreeSnow.gdshaderinc`: approximate foliage detection and uneven deposits using existing colour/brightness and sampled noise. Snow uses the already-swayed artwork UVs, preserves alpha, and uses a configurable lower cutoff (tree crown or grass-blade profile). No hand-painted masks or new tree sprites.
- `GroundSnow.gdshader`: irregular world-space coverage over grass, brown paths, clearing and gravel, at Z=-7 below actors and their shadows. Surface grain remains visible; patches do not move with camera or wind.
- `SnowTest.tscn`: the one removable scene instance.

The adapter validates a known composition anchor in the current debug tree shader and warns/skips unsupported shaders rather than modifying arbitrary artwork. If that wind shader is refactored, update this adapter too. Snow tinting is applied only outside the shadow-only branch as an additional safeguard.

## Visual limits and cost

This is a visual approximation of snow caught in a leafy canopy. It preserves brown trunks/roots but colour-based detection may miss yellow/brown leaves or dust an unusual green branch; it does not reconstruct hidden winter branches. Rock sprites now receive automatic noisy snow caps, concentrated on the upper-facing portion of their artwork. Decorative grass receives the vegetation snow treatment with its own blade profile. No snow depth, physics, footprints, persistence, season calendar or weather simulation is included.

At full coverage the ground can obscure the path; this is intentional coverage, not path deletion. The torch can still illuminate snow and there is no local heat-melt simulation. Disabling restores the pre-snow materials visually.

One additional ground draw, one bounded 1300-particle GPU emitter, and an extra noise sample for snowy tree pixels are the main costs. Default tree and grass materials each become two shared orientation variants, not dozens of per-tree material updates. Snow has no per-frame image uploads, automatic painted masks or copied tree textures. Profile target hardware before production use.

## Validation

All current debug C# scripts compile against GodotSharp 4.7.2 / .NET 8 and with Godot 4.5.1 source generators. Software OpenGL Compatibility rendering checked 0%, 25%, 60% and 100% coverage, brown trunks, dark shadow materials, ground/path coverage, the master toggle and restoring original tree materials after deleting the SnowTest node.

The initial full-density grass scene exceeded this software Compatibility renderer's instance-uniform capacity, so the final clean rendered/removal check used 49 decorative grass tufts (all 58 trees and 24 rocks retained) instead of 188. This is a local validation setting only; the committed scene's grass counts and project rendering settings are unchanged. Windows D3D12 appearance and performance still need local checking.

A separate headless timed check covers left/calm/right tree and snowfall wind, automatic accumulation/melting, off and removal. Rain/storm inputs remain independent and the snow controller never overwrites them.

## Snow detail / reusable vegetation pass

Falling flakes now randomly select one of four opaque pixel atlas shapes (small cross, diamond, sparse speck, branched crystal), with nearest filtering and varied scale/speed. There is no Gaussian alpha blob. The GPU particle animation material selects a frame at emission without cycling through frames.

Ground coverage now uses a dedicated 1024×1024, two-octave smooth `SnowCoverageNoise.tres`, world-space domain warp and a narrow, soft transition. Fine snow grain affects only the surface colour, never the coverage edge. Subtle pale-blue roll shading and restrained microdetail avoid the former high-frequency salt-and-pepper texture. No camera-locked mask or full-screen blur is used. This pass adds six sampled-noise reads per drawn ground-cover pixel.

The adapter is **not bound to tree_01.png or an oak texture filename**. It is also not a universal automatic snow shader for arbitrary artwork: it expects the current TreeSway composition point, green foliage colours and an appropriate UV coverage profile. Tree defaults protect the trunk; grass defaults cover most blades and leave their bases less dusted. `Attach(group, noise, grass: true)` supports the directly generated grass sprites irrespective of texture filename, including grass_small variants; tree mode supports the current Tree-prefixed actor roots. New orange leaves, snowy branches, rocks or an unrelated shader need a matching profile/mask or adapter rather than just renaming an image.

Grass support adds no per-tuft frame loop. The same adapter builds shared orientation variants, copies cached wind, preserves sprite alpha and restores original tree/grass materials on removal. Decorative grass automatically follows the same master settled snow coverage as trees, rocks and ground. Existing on/off and single-folder removal still apply.

Validation for this pass includes C# compilation, GPU atlas setup, rendered snow-level comparisons and grass material adaptation in the current debug scene. Software Compatibility validation still uses reduced decorative-grass density as described above; Windows D3D12 and hardware performance remain local checks.

## Distributed ground accumulation and vegetation layering correction

Ground coverage now uses overlapping small world-space patches (roughly 16–32 world pixels), distributed throughout the terrain. The slider grows and joins these deposits; it no longer leaves large regional green islands. Surface shading remains independent. Coverage is monotonic, with exact zero at 0% and opaque ground coverage at 100%; it is an artistic progress control rather than an exact percentage of world area.

GrassPatches now draws at Z=-6, above the ground snow at -7 and gravel at -8, while shadows remain at -5 and actors at 0. This fixes snow obscuring grass blades at full ground coverage. Snow on the grass artwork follows the same master coverage through its vegetation shader; disabling Snow test restores green grass. Changing the grass layer is independent of the removable snow component and has no dependency on its files.

Correction validation: rendered 0%, 25%, 70% and 100% in Godot 4.5.1 software OpenGL with no shader errors. Inspected the distributed deposits and visible grass blades at 100%, and asserted that grass draws above ground snow. Existing coverage, off and material-restoration checks passed. As with prior checks, decorative grass density was reduced locally for software rendering; committed counts remain unchanged.


## Wider transition and shallow snow depth

Biome blending now spans 240 world pixels (previously 120), editable with DefaultBiomeConfig / BlendWidth. SnowDepth.gdshaderinc adds smooth shallow drifts and restrained bank/hollow shading without altering distributed coverage or the exact 0/100% coverage endpoints. SnowMasterConfig exposes DriftScale, DriftContrast, PlayerSinks (on by default) and MaximumSinkPixels (4). Snow itself remains opt-in as before. Player burial begins above 25% settled snow (a small dip at the default 60%), varies with the same world-space depth field, and reaches at most 4 pixels; body/collider/camera and movement speed remain unchanged.

PlayerSurfaceVisual is the single sprite/material owner shared by optional snow and lake tests. Liquid immersion takes precedence; snow produces no water rings and keeps the projected sun shadow. Toggles/melting ease the sprite back up. Removing both experiments restores its original position/material. The helper is installed by either test, so snow burial also works with the Liquids scene removed.

Validation: all debug C# compiled against GodotSharp 4.7.2 / .NET 8 and native Godot 4.5.1 source generators. Software OpenGL rendering was inspected at full snow. Runtime checks passed for varied 0–4px burial, default 60% accumulation, camera stability, retained sun shadow, deeper lake priority, melt/off restoration, helper cleanup, and snow with the Liquids scene absent. Target Windows rendering/performance remains unverified.

## Shader Wind and Snow — pink blossom profile

The same SnowTest adapter now handles both green oak foliage and pink blossom foliage. Sprites can optionally set the integer metadata `snow_foliage_profile`: 0 (default) finds green leaves, 1 finds pink blossoms using red/blue colour separation from brown bark. The profile is chosen per sprite when shared snow material variants are built and does **not** change the shared TreeSway animation, GPU draw count or contact collider. The original green/grass mode remains the default for existing artwork.

`Trees/PinkBlossom01.tscn` sets mode 1; any future non-green species can opt into the same profile without adding species-name conditions. The panel now has a **Shader Wind and Snow** heading, as requested. Colour heuristics are approximate: unusual bark/blossom colours may need a future configurable mask or custom foliage-channel texture. Snow remains initially off as before.

## Rock snow cap pass — shared shader response

`Rock01.tscn` now opts into snow via a reusable `debug_snow_rocks` group. The new `RockSnowAdapter` caches generated rock sprites **once** after DayNight builds shadows. When **Snow test** has settled coverage above zero, a single shared `RockSnow.gdshader` material appears on the rocks. The shader samples the same `GroundNoise.tres` as vegetation; it estimates upper-facing rock surfaces using local sprite UV height, brightness and two noise scales, rather than tinting the entire image white. The bottom/front portion stays rocky at full accumulation, while small caps build progressively with the normal 0–100% snow coverage.

`SnowMasterConfig.cs` exposes `RockCapDepth` (0.25–0.85, default 0.58) for shaping the snow caps. Rocks automatically receive settled snow without a separate checkbox. No extra rock artwork was necessary. The same implementation supports rocks in Oak Forest and Pink Meadow and future new rock sprites if their root joins the opt-in group. Different image proportions might need a different cap-depth parameter or a custom mask; no species-specific snow controllers were introduced.

**Performance and removal:** One shared shader material for all opted-in boulders, only a few uniform updates per frame, no per-rock update loop except on toggle transitions, no new scene nodes/collision and no texture uploads. With snow off, original rock materials are active and the extra shader is not drawn. Removing SnowTest restores all cached rock and vegetation materials without modifying the day/night shadow cache, mirrored colliders, gravel or generation.

**Test checklist:** Enable Snow test and drag settled snow 0/25/60/100%; confirm caps gradually grow on both biomes' rocks and their lower faces remain darker, while trees and grass also whiten. Turn **Snow test** off to restore the original appearance of everything, then turn it back on. Optionally remove the SnowTest instance to verify cleanup. Check flipped/scaled rocks, projected sun shadows, rain and lightning. The implementation has been statically reviewed, but Godot rendering and .NET compilation still need local verification.

## RockSnow.gdshader fragment compilation fix

Godot canvas-item shader `fragment()` functions cannot use `return;` to exit early. The previous rock snow shader had an invalid `return;` after the zero-coverage check (reported by Godot at `RockSnow.gdshader:27`), causing the shared rock material to fail compiling in `RockSnowAdapter.Attach` even if SnowTest began disabled. That block has been removed: the existing `snow_coverage * 2.5` mix weight already becomes zero when snow coverage is zero, and the adapter restores original rock materials whenever snow is off. Rock scene, spawn code, texture, collision, settings and generation counts were not changed.

**Separate warning:** `DebugWorldScatter._Ready()` can warn if the requested tree/rock count could not fit the map with its configured spacing. It is unrelated to the shader compile failure; inspect the preceding scatter totals to see whether any rocks were actually skipped.

## Shared snow coverage cleanup (October 2026)

The separate **Snow on grass sprites** and **Snow on rocks** checkboxes were removed because they needlessly duplicated the existing master snow-coverage control. The one `Snow test` switch plus `Settled snow` slider now controls ground, both tree species, decorative grass and every opted-in rock together. `Falling snow` still controls particles; `Auto accumulate / melt` optionally connects those flakes to evolving surface coverage. When automatic accumulation is off, flakes may fall while the current settled coverage remains constant (by design).

The snow panel's bottom edge was shortened after removing the controls to fit within a 1080-high viewport. The prior screenshot lacked not only the rock checkbox but also the `Shader Wind and Snow` heading present in the GitHub source, indicating the running C# assembly was older than the current source. **After pulling, stop the game, rebuild the Godot .NET C# project in the editor and restart the scene.**

Rock caps still require a successful shader compile (the invalid `return;` in `RockSnow.gdshader` was fixed in an earlier commit). Both the scene group and the rock adapter remain reusable; no rock sprites, collisions, biome generation, grass counts or production folders were changed.
