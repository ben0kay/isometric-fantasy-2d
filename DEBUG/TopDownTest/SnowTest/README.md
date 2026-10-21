# Removable snow accumulation and snowfall test

Implements `Notes/Snow_Accumulation_Codex_Test.txt` as a disposable comparison. Build C# and F5/F6; the snow panel sits below gravel on the left. **The test starts OFF**, preserving the existing presentation.

## Controls

- **Snow test:** enables the whole comparison. Off immediately clears settled snow and hides all falling flakes; the saved coverage target is retained for the next comparison.
- **Settled snow slider:** 0–100% tree-canopy and ground coverage. Dragging scrubs immediately. Enabling or changing the Inspector target blends over TransitionSeconds (3 seconds default).
- **Falling snow / Snowfall intensity:** separate controls for slow, varied GPU flakes. Shared Weather wind drives their left/calm/right drift and existing tree sway.
- **Auto accumulate / melt:** grows coverage toward 100% while snowfall/rate is active, and melts toward zero when snowfall stops. Default full-range duration is 30 real seconds, independent of the accelerated day clock.

Rain and lightning settings are not changed by this test. For a pure snowstorm, set the existing rain slider to zero; mixed precipitation is possible for comparisons. Restart restores saved config defaults. Inspector settings also include snow colour and transition/accumulation duration. UI checkbox states only follow their UI; the coverage slider follows Inspector/automatic changes.

## Easy removal

In `TopDownVisualTest.tscn`, delete the **SnowTest** scene instance and its `snowtest` external-resource entry, then delete this **SnowTest/** folder. No other shader/controller depends on this folder. Godot can update the scene resource count when saving. No production folders, global shader settings or project rendering settings were changed.

Deleting the SnowTest node during play restores original tree material identities and mirrored wind parameters. The scene instance is deliberately last, after scenery generation and DayNight's material cache. For a quick comparison, the on/off toggle is sufficient.

## Separate pieces

- `SnowMasterConfig.cs` / `DefaultSnowConfig.tres`: all snow settings.
- `DebugSnowTest.cs`: comparison UI, automatic progression, ground-cover material and cleanup.
- `DebugSnowfall.cs`: C# **GpuParticles2D** emitter with a generated soft flake texture, varied size/speed, and camera/travel margins. World coordinates keep existing flakes from sticking to the camera. No per-flake nodes or CPU collision queries.
- `TreeSnowAdapter.cs`: temporary shared normal/mirrored material variants, retaining the existing wind shader composition and UV warp. Original materials/shaders remain unchanged; cached source wind values are copied per shared variant rather than per tree. Projected shadow materials are never replaced.
- `TreeSnow.gdshaderinc`: approximate foliage detection and uneven deposits using existing colour/brightness and sampled noise. Snow uses the already-swayed artwork UVs, preserves alpha, and excludes the lower crown/trunk region. No hand-painted masks or new tree sprites.
- `GroundSnow.gdshader`: irregular world-space coverage over grass, brown paths, clearing and gravel, at Z=-7 below actors and their shadows. Surface grain remains visible; patches do not move with camera or wind.
- `SnowTest.tscn`: the one removable scene instance.

The adapter validates a known composition anchor in the current debug tree shader and warns/skips unsupported shaders rather than modifying arbitrary artwork. If that wind shader is refactored, update this adapter too. Snow tinting is applied only outside the shadow-only branch as an additional safeguard.

## Visual limits and cost

This is a visual approximation of snow caught in a leafy canopy. It preserves brown trunks/roots but colour-based detection may miss yellow/brown leaves or dust an unusual green branch; it does not reconstruct hidden winter branches. Rock sprites and decorative grass sprites do not receive their own snow caps in V1; the ground layer covers the flat terrain beneath them. No snow depth, physics, footprints, persistence, season calendar or weather simulation is included.

At full coverage the ground can obscure the path; this is intentional coverage, not path deletion. The torch can still illuminate snow and there is no local heat-melt simulation. Disabling restores the pre-snow materials visually.

One additional ground draw, one bounded 1300-particle GPU emitter, and an extra noise sample for snowy tree pixels are the main costs. Default tree materials become two shared orientation variants, not dozens of per-tree material updates. Snow has no per-frame image uploads, automatic painted masks or copied tree textures. Profile target hardware before production use.

## Validation

All current debug C# scripts compile against GodotSharp 4.7.2 / .NET 8 and with Godot 4.5.1 source generators. Software OpenGL Compatibility rendering checked 0%, 25%, 60% and 100% coverage, brown trunks, dark shadow materials, ground/path coverage, the master toggle and restoring original tree materials after deleting the SnowTest node.

The initial full-density grass scene exceeded this software Compatibility renderer's instance-uniform capacity, so the final clean rendered/removal check used 49 decorative grass tufts (all 58 trees and 24 rocks retained) instead of 188. This is a local validation setting only; the committed scene's grass counts and project rendering settings are unchanged. Windows D3D12 appearance and performance still need local checking.

A separate headless timed check covers left/calm/right tree and snowfall wind, automatic accumulation/melting, off and removal. Rain/storm inputs remain independent and the snow controller never overwrites them.
