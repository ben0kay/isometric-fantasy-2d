# Shared Ground Surfaces

> **Status:** Folder and design foundation only. No production surface scripts, shaders, materials or Resources are implemented yet.

This folder is the **reusable visual framework for ground terrain**. Its job is to style a ground region using profile-driven palettes, textures, sampled noise and detail. Ground definitions and biomes choose the inputs; shared surface logic renders the result.

## Folder ownership

| Folder | Responsibility |
| --- | --- |
| [Shaders/](Shaders/README.md) | Shared shader compositions/includes for colour patches, fine detail, world-space noise sampling and future terrain-material-specific compositions. |
| [Noise/](Noise/README.md) | Reusable noise texture Resources and sampling defaults; avoid duplicate per-biome procedural noise textures without reason. |
| [Profiles/](Profiles/README.md) | Future inspector-editable surface-profile Resources: palette, texture references, variation strength, detail parameters and optional surface modes. |

Future controller/renderer script(s), if actually needed, should live at this folder level; avoid one controller per named biome.

## Data flow

`Biome definition -> ground definition / surface profile -> shared surface material or renderer -> terrain pixels`

- A biome can override **colour palette and surface-profile choice** without copying the common shader.
- A ground type can provide a **base texture** and select the appropriate shader composition.
- Shared sampling/shading remains independent of **camera movement**, using world-space coordinates where appropriate.
- Transition between *different* ground/biome regions belongs in `WORLD/Terrain/Transitions/`, not in the surface's internal colour noise.
- Individual grass tufts, rocks, flowers and trees are content placed above the ground, not pixels handled here.
- The environment owns weather/day-night; surface visuals can accept their inputs without taking ownership.

## First extraction target

Reproduce the current Oak Forest ground **identically** using `DEBUG/TopDownTest/Ground/` as reference:

- `GrassSurface.gdshader`
- `PatchColours.gdshaderinc`
- `FineDetail.gdshaderinc`
- `NoiseSampling.gdshaderinc`
- `GroundNoise.tres` and `GrassMaterial.tres`
- `DebugGroundSurface.cs` (reference for parameter/material wiring, not blindly reusable UI code)
- Oak Forest palette from `DEBUG/TopDownTest/Biomes/OakForest.tres`

Do **not** duplicate the Oak shader for Desert, Swamp, etc. merely to change a palette. Equally, don't force genuinely different materials such as sand, stone or ice through a grass-only composition; reuse lower-level shader helpers and choose a separate composition when needed.

## Implementation guardrails

- This pass creates **documentation-only scaffolding**; do not infer that classes, presets or scenes already exist.
- Before future promotion, inspect real DEBUG files and `CodePreferences.md`; change only what is required to remove DEBUG dependencies.
- Keep old debug comparisons functional and avoid new gameplay systems during extraction.
- Design for editable Godot Resources, reusable material instances where valid, bounded shader cost and clear module ownership.
