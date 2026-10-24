# Old-project dust/basalt adaptation

The author rejected the first SVG-based optional surfaces and requested the old isometric game's blended basalt/soil appearance. Dirt and Rock now use texture-free, data-driven extra-layer recipes. The existing grass base/artwork and its independent accompanying patches remain unchanged; extras still start OFF and use the same F9 toggles/coverage/opacity sliders.

## Source and adaptation

From ben0kay/isometric-new-2d:

- VISUALS/Drawings/Ground/BasaltSurface.gdshaderinc (blob 22f536fff4eeea5f8daa964e66b35a89c083b95a): original dark low/high palette, sparse hairline fracture thresholds, detail and grain coefficients.
- VISUALS/Drawings/Ground/DustSurface.gdshaderinc (blob 133e3c1b69e9619dae153cd2060170029c01ed9c): original muted low/high sediment palette and fine grain.
- VISUALS/Atmosphere/GroundSun.gdshader: original strata/detail/grain sampling offsets/frequencies and broad dust/mineral threshold families.

ProceduralGround.gdshaderinc accepts a recipe, two data colours and MaterialFeatureSize. GroundSurfaceProfile.Recipe selects Artwork (existing default), Dust or Basalt; BaseColour/FreshColour supply low/high procedural tones. MaterialFeatureSize converts world pixels to the old tile-coordinate material family (32 by default, configurable). Noise samples use pixel-quantized absolute world coordinates and stable layer salt. Old lighting/elevation and the whole old world shader are deliberately not ported; current Godot lighting/weather/streaming retain ownership.

Current recipes are supported on extra layers only. Base ground still requires artwork and rejects a procedural selection rather than silently rendering it incorrectly. Other biomes can reuse these extra profiles without biome-name branches. Artwork-based fixture/legacy overlays retain their existing renderer path.

## Blending and assets

Oak dirt now uses broad 500-unit masks, coverage 0.4 and 0.10 edge softness. Rock uses 280-unit masks, coverage 0.38 and 0.08 softness. Both use zero warp, replacing the swirly domain deformation. The procedural branches use the old dust/mineral threshold families; coverage multipliers are still threshold bias, not exact area percentage. Underlying grass remains dominant at default preview strengths.

Dirt/Rock profiles reference no texture, and their obsolete SVGs were removed. They do not add atlas tiles or sample repeated artwork. Optional material data uses the already existing 9-column table (recipe and scale occupy unused channels). Mask-out, disabled/default-off and zero-strength fast paths remain. No extra draw/pass, sampler, per-chunk material, CPU frame callback, bake job or slider-triggered texture rebuild. Active procedural material uses three shared noise samples; zero-warp masks skip warp noise samples. Actual Windows GPU/frame-time cost is unmeasured.

## Compatibility and evidence

A narrowly gated migration checks exact current Oak/Pink/Dirt/Rock source hashes and reconstructs the previous full dependency fingerprint using frozen previous Oak/Dirt/Rock text. Earlier explicit terrain bridges remain supported; unrelated content differences still reject. Frozen .txt references to retired SVGs are fingerprint source text only, not loaded Godot assets. World IDs, scenery, collision and save schema remain unchanged. Real disk restoration is still pending locally.

Passed: Mesa software-GL compile/render checks for old dust/basalt colour-formula parity against frozen source references, independence from artwork pixels, deterministic negative-coordinate sixteen-chunk seams, F9 controls/reset and unchanged default grass (max pre-GS2 synthetic-input difference 0.0). Static resource dependency and migration reconstruction checks passed. Engine fixtures assert texture-free recipes and the new migration path.

Not run here: C# compilation, Godot shader import/headless suites, Windows visual acceptance or performance. This runner has no Godot/.NET installation. Software checks verify calculations, not the final appearance under the game's lighting.

Pull, reopen Godot/import, build, F6 World → F9 Ground Surfaces. In Oak enable Dirt alone, Rock alone, then both. Compare default strengths, adjust coverage/opacity and reset (both OFF). Check Pink transition, chunk boundaries and Continue. Run the commands in GS4_Validation.md. The original grass/accompanying controls and session-only semantics are retained.
