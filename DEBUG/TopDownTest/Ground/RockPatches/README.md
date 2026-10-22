# Disposable gravel around placed rocks

The normal F5/F6 scene creates an irregular grey-brown gravel bed under and around each generated rock. The **Rock ground gravel** toggle and **Gravel spread** slider sit below the grass controls. Grass variation and gravel are independent comparisons.

## Separate responsibilities

- `RockPatchMasterConfig.cs` / `DefaultRockPatchConfig.tres`: Enabled, Spread (0.5–2), Opacity and dark/light gravel palette. Live Inspector settings; defaults return after restart.
- `DebugRockPatches.cs`: caches generated rocks once, sizes one ground quad from each rock sprite's scaled width, and updates comparison controls/shared uniforms. It never changes rock sprites, collision or grass materials.
- `OrganicEdge.gdshaderinc`: softly warped edge, broken outer coverage and guaranteed transparent margin before the quad boundaries.
- `GravelDetail.gdshaderinc`: cellular pebble colours, tiny darker seams and dirt grain in snapped world coordinates.
- `RockPatch.gdshader`: small composition entry point using normal CanvasItem lighting.
- `PebbleNoise.tres`: separate seamless cellular noise texture generated once for fine stone detail. Broad irregularity reuses the existing `Ground/Noise/GroundNoise.tres` resource.
- `RockPatchMaterial.tres` / `RockPatches.tscn`: resource wiring and grouped helpers.

The old isometric repo's reusable ground-surface approach inspires the arrangement. The grass patch/fine-detail shaders remain separate from these gravel shaders. There is no combined weather/lighting/terrain shader.

## Placement and scope

`RockGround` runs after Actors has scattered scenery. Decals are centred near the rock's ground anchor, with small seeded rotations and dimensions matching each rock's root scale. Their world-space pebble pattern gives each location different detail and remains fixed while the camera moves. Mirroring the artwork does not require mirroring terrain. Spread changes resize cached decals without respawning them.

Ground draws at Z=-10, gravel at -8, existing sun/contact shadows at -5, and actors at 0. Thus gravel lies below rocks and shadows, responds to day/night, lightning and torchlight, and is covered normally by fog/cloud shade. It can visually extend onto nearby path/clearing edges; this prototype has no terrain-type mask. It adds no collision or physical pebbles.

These helpers assume static rocks: moving/removing rocks after setup requires rebuilding or updating decals. Overlapping gravel patches alpha-blend and can become more opaque. Large spread may extend beyond map edges near border rocks. This is intentionally a contained visual experiment, not production terrain generation or persistent surface painting.

## Cost and validation

Default scene adds 34 small quad draws sharing one mesh and one shader material, plus one generated cellular texture. No full-map shader loop over rocks, per-frame texture uploads or per-pebble scene nodes. The toggle hides the entire decal group. Target hardware profiling is still required before promotion.

C# compiles with Godot 4.5.1 source generators and against GodotSharp 4.7.2 / .NET 8. Full-scene software OpenGL rendering verified all 24 decals, independent toggle behaviour and the on/off visual comparison. The screenshots were inspected for ground blending and preserved grass/scenery. Windows D3D12 and hardware performance remain local checks.

## Smoother transition pass

OrganicEdge now uses low-frequency two-axis warping and a wide continuous squared alpha falloff. Small stone noise no longer breaks up the coverage mask. Fine pebble contrast is restrained so cell colours read as small gravel rather than large square blotches. The support rectangle retains a fully transparent margin. Shape/material remain separate from the liquid and grass implementations. Lake placement reserves enough space around rocks to keep ordinary gravel out of the water.
