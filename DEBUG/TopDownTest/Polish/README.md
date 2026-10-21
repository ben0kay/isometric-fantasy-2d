# Optional debug visual polish

F5 runs the same top-down test. A second comparison panel beneath the clock has independent toggles for:
- Colour grading: mild saturation, black/white-preserving contrast curve and a subtle vignette. No image blur or pixel-grid distortion.
- Soft sun shadows: fixed nine-sample alpha filtering, weaker at the object's base and stronger toward its crown.
- Moving cloud shade: two layers of inexpensive smooth noise in world coordinates. Broad translucent patches drift across the map and fade at night.
- Flickering torch: a small authored torch near the player spawn, with a warm PointLight2D, radial light texture and gently varying intensity. Its flame remains luminous at night.

Pause the existing clock at midday to compare grading/clouds/shadows, then scrub to 22:00 to compare torchlight. Disabling all four returns to the previous daylight presentation; the torch collider remains as a small obstacle even while its artwork is hidden.

## Settings and ownership

**DefaultPolishConfig.tres** owns switches, Contrast, Saturation, Vignette, CloudOpacity, ShadowSoftness and TorchEnergy. Values are read live and can be tweaked through the Remote Inspector. The checkbox controls update this resource in memory; restarting restores saved settings. Configuration changes in the Remote Inspector affect the effects but do not refresh checkbox states automatically.

PolishDemo.tscn contains the cloud overlay, fullscreen grading and comparison UI. Torch.tscn is placed in the existing Y-sorted Actors group. DebugVisualPolish.cs coordinates them and uses the existing day/night controller's Daylight value. Existing shadow shaders accept a softness uniform; the optional polish controller controls it.

All additions and modified shaders/controllers remain under **DEBUG/TopDownTest/**. No production folders, imported artwork, gameplay rules, world-generation code or project rendering settings were changed. No external assets are needed.

## Rendering details and limits

Cloud shade is an art-directed overlay, not a volumetric cloud or lighting simulation; it shades all scene artwork uniformly. The torch uses actual 2D illumination but has no local occluders/shadows or normal/specular maps. The existing sun silhouettes still project only onto flat ground.

Soft shadows blur alpha only inside each sprite's rectangle; imported artwork needs sufficient transparent margins at extreme softness. The fixed filter approximates a penumbra; it does not simulate physical light size. Small low-resolution sprites receive subtler softening than the high-resolution tree. Disabling softness skips extra alpha samples.

Grading draws in CanvasLayer 10; both UI panels draw at layer 20 and are not graded. Nearest screen sampling preserves existing pixel edges. The torch flame uses an unshaded material; no HDR bloom is enabled. Glow, fog and particles are intentionally outside this initial four-effect comparison.

## Validation

All four shaders passed headless Godot 4.5.1 shader parsing/compilation as an additional compatibility check. Scene resource counts, references and UI ordering were checked. This is not a GPU-rendered screenshot test or a build in the project's Godot version: the project uses Godot .NET 4.7.x, and matching .NET tooling is unavailable here. C# build, visual appearance, toggle comparisons and GPU performance remain to be checked in your local editor.

Cost: one screen-copy grading pass, one procedural cloud overlay, one PointLight2D, and eight extra alpha samples per softened shadow pixel. Toggles disable the relevant rendering work. Profile before promoting this to a large forest.


### Shared noise seam correction

The shader-calculated grid noise and its later simplex replacement are superseded. Both overlays now sample **Atmosphere/AtmosphereNoise.tres**, a shared 512×512 seamless FastNoiseLite texture with linear filtering and repeat enabled. OrganicNoise.gdshaderinc performs only smooth texture sampling/warping; it no longer calculates gradient cells or sine hashes in the fragment shader. Both scene materials explicitly bind the same noise texture.

This addresses the shared path behind the visible straight cutoffs instead of changing only the pattern's shapes. The exact artifact reported on the user's graphics backend was not reproduced locally, so backend-specific diagnosis is not claimed as confirmed.

Validation now includes actual rendered Godot 4.5.1 Compatibility images using software OpenGL: haze with ambient night tint and a PointLight2D, plus cloud shade in isolation. Both rendered outputs were inspected for continuous patches. These are isolated rendering tests, not a C# build or full-scene screenshot in Godot .NET 4.7.x on the user's backend. Existing controls, day/night blending and drift are retained.

The texture is generated once and shared; there are no per-frame image uploads. Its seamless setting prevents a hard tiling edge, and explicit sampler filtering is independent of the pixel-art filtering used for scenery.

## Expanded world

The debug cloud-shade Polygon2D now covers the enlarged 1920×1280 world rectangle, while the UI colour grading remains a viewport overlay. Existing torch near the start clearing and comparisons are unchanged.

## Shared wind integration

Cloud shade now samples integrated displacement from DebugWeather instead of TIME multiplied by a separate CloudVelocity. Changing or reversing wind does not jump the cloud pattern. The old velocity setting was removed; Weather owns wind. See Weather/README.md for current validation.
