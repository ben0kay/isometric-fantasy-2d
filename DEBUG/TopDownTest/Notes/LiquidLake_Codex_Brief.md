# Liquid patches and lake — Codex debug implementation brief

**Status:** Debug lake/wading prototype implemented; see [Liquids README](../Liquids/README.md) for current behaviour, validation and removal. Remaining suggestions below are not automatically implemented.  
**Project:** Godot 4 C#, fully 2D top-down fantasy sandbox (NOT 3D/isometric).  
**Scope:** DEBUG/TopDownTest/ only. Do not add or modify production WORLD, ENVIRONMENT, ENTITIES, SYSTEMS or SAVE code during this pass.  
**Goal:** Build a beautiful, natural-looking little lake in the existing walkable forest debug map as the first example of a reusable **liquid-area system**, not a bespoke Lake.cs that will later need rewriting for quicksand, acid, toxic sludge, swamps or magical liquids.

Codex: inspect all current files, code preferences and scene wiring before implementing. You are welcome to use a *better* approach than the proposed technique if it is clearer, faster or more attractive. Keep this experiment easily toggleable/removable. Explain any material design trade-offs and update README(s) after implementation. No new water sprites are required for v1 unless an art-driven solution genuinely looks better.

## 1. What a lake means in this 2D game

A lake is **an arbitrarily shaped region in the 2D terrain**, with:
1. **Footprint / boundary:** where the liquid exists, plus a shoreline band.
2. **Liquid definition:** describes which liquid occupies the footprint (ordinary water, later quicksand, toxic waste, swamp sludge, etc.).
3. **Surface renderer:** renders its colour, edge, subtle movement, ripples, highlights and optional depth variation.
4. **Gameplay policy:** whether an actor can walk/wade/swim, slows down, takes damage, gets wet, sinks, etc. These are settings/effect hooks rather than individual hardcoded lake implementations.
5. **Placement exclusion:** generated land plants/trees/rocks must not populate the liquid by mistake.

The footprint is **not** the same thing as the liquid type. One shape with Water assigned could also be tested with Toxic Sludge by swapping a data resource. A water-specific future lake region can supply local depth/banks, but must not own another independent copy of wave or collision code.

**V1 recommendation:** Use a locally authored or seeded irregular polygon region + a contained 2D CanvasItem shader with world-space noise. This gives the convincing impression of liquid without fluid simulation or per-pixel CPU updates.

## 2. Existing code to read and preserve

- DEBUG/TopDownTest/TopDownVisualTest.tscn — main F6 map (1920 x 1280 world pixels; playable bounds approximately x=-480..1440, y=-320..960). Player starts at (480, 320), torch at (540, 345); brown clearing and path are already authored.
- DEBUG/TopDownTest/Generation/DebugWorldScatter.cs and ScatterMasterConfig.cs — seeded creation of trees/rocks in Actors._Ready(), including main-path and player clearing avoidance. Currently no reserved lake footprint.
- DEBUG/TopDownTest/Grass/DebugGrassPatches.cs — seeded decorative grass, references DebugWorldScatter.CanPlaceGrassDecoration(), no collision.
- DEBUG/TopDownTest/Ground/Shaders/GrassSurface.gdshader — existing organic terrain shading. Do not rewrite it into a liquid shader.
- DEBUG/TopDownTest/Ground/RockPatches/ — an example of a **separate** surface decoration/material, with noise helpers and organic edge treatment. Consider reusing a generalized technique, not tightly coupling liquids to gravel colours/formulas.
- DEBUG/TopDownTest/Ground/Noise/GroundNoise.tres — possible existing noise source; reuse a suitable texture if it helps and remains independent.
- DEBUG/TopDownTest/Weather/DebugWeather.cs — signed wind strength (-3..+3), velocity and rain amount. Reuse the existing signal/data; do not create a second weather controller.
- DEBUG/TopDownTest/DayNight/DebugDayNight.cs — CanvasModulate/ambient state, moving shadows and lighting. Liquid should naturally respond to the same scene lighting and torchlight.
- DEBUG/TopDownTest/SnowTest/ — disposable snow test, already includes a ground overlay; liquid must not disappear beneath its snow cover or become inexplicably snow-covered. Preserve the snow toggle and all other effects.
- DEBUG/TopDownTest/Atmosphere/, Polish/, Trees/ — fog, clouds, wind and shadows, which should all continue working normally.
- CodePreferences.md — small separated files, README per feature, exported/grouped Inspector settings, concise C# functions with explanatory comments and regions, performance-minded code, one GitHub commit per task.

**Current draw-order guidance, verify it in current scene:** ground Z=-10; grass clumps about -9; rock gravel -8; snow ground-cover -7; existing sun/contact shadows around -5; Actors at 0. Consider rendering the **opaque or mostly opaque liquid interior at approximately Z=-6** (above the snow overlay, below shadows/actors), with shoreline below or beside it as appropriate. Test actual Godot CanvasItem/Z/canvas ordering; don't blindly rely on these numbers. A transparent lake placed below the snow overlay will look wrong in winter.

## 3. Suggested small, modular DEBUG implementation

Create only what is needed. Illustrative folder structure (not mandatory exact filenames):

DEBUG/TopDownTest/Liquids/
- README.md — intended scope, settings, controls, easy removal.
- LiquidMasterConfig.cs + DefaultLiquidConfig.tres — settings for liquid test activation/appearance, optional seed/shape preset, shoreline/ripple strength and debug visualisation.
- LiquidDefinition.cs (Godot Resource) — **shared liquid-type data**: stable ID, display name, surface colours, opacity, ripple/wave speed, edge/foam style, ambient movement, optional movement policy/effect metadata. Keep physics and shaders separate.
- Definitions/Water.tres — initial visible lake liquid type. Optional quicksand or toxic profile for a comparison, only if trivial; do not spend time implementing their gameplay systems.
- LiquidRegion.cs or LiquidRegion.tscn — one shaped instance/region referencing a LiquidDefinition and polygon/shape data.
- LiquidSurface.gdshader plus a couple of narrowly scoped shader helpers (for example LiquidColour.gdshaderinc, LiquidMovement.gdshaderinc, ShoreEdge.gdshaderinc); avoid one massive multi-purpose shader.
- Optional LiquidTestController.cs or LiquidTest.tscn — connects one lake to the existing test, a compact toggle/controls panel and clean removal.

**Do not** make separate WaterRenderer.cs, QuicksandRenderer.cs, ToxicRenderer.cs that duplicate exactly the same area/edge/animation rules. LiquidDefinition can control common shader parameters; optional per-type visual modules are fine later when differences warrant them.

**Important future ownership:** A promoted production liquid renderer / material rules may belong to WORLD/Water/ or a more general WORLD liquid-surface domain to be decided; liquid-specific biome placement belongs to WORLD/Generation/Water/ and biome data; ENVIRONMENT provides weather, not water body artwork; player survival/conditions and other entities decide how to respond to liquid exposure. For this task, all new implementation stays in DEBUG.

## 4. Lake appearance and organic geometry (visual V1)

Build **one fairly large irregular lake**, perhaps centred near (930, 710), away from the existing player start/torch and the north-to-east path; choose the actual position and dimensions after checking collisions and debug controls. A rough starting width 300-400 pixels and height 180-250 pixels is fine. Keep within the authored map. Prefer at least one accessible bank that a player can walk beside.

Visual targets:
- A recognizable irregular **shoreline**, not a sharp blue rectangle or perfectly smooth oval. Use a hand-authored Polygon2D outline or low-frequency seeded perimeter perturbation with **stable** shape; avoid jagged noisy pixels and visible straight segments.
- Shallow near-shore colours and darker, slightly deeper-looking centre. This is **art direction**, not 3D depth: a depth/intensity gradient derived from an edge-distance mask or region-relative coordinate is sufficient.
- Subtle moving surface: slowly drifting highlights, tiny broad ripples and restrained interference, optional wind direction and intensity influence. **World-space UV/noise** so waves do not crawl with the camera; do not wiggle the entire lake boundary.
- An optional narrow bank of damp earth, gravel, exposed mud or sandy/rocky shore in muted fantasy-pixel-art colours. Allow asymmetry, gaps and varied thickness, rather than an equal solid ring all the way around.
- Very subtle shoreline lapping/foam; ordinary calm lake water shouldn't look like a stormy ocean. The result should complement the richly shaded oak tree/rocks and existing organic grass.
- Avoid broad translucent rectangles, rectangular shader seams, exaggerated bloom, large waves or flat solid cyan. Keep edges matched to the underlying grass and authored brown path/clearing.
- No painted full-screen overlay, no global colour grading change and no permanent modification of the underlying GrassSurface.gdshader.
- Existing light changes (day/night, lightning, torch, clouds/fog) should continue affecting the lake; consider a restrained nighttime highlight but avoid emissive water by default.

**Implementation suggestion:** Draw the actual lake as a Polygon2D/triangulated mesh or local quad with a polygon/coverage mask; surface pattern can sample world coordinates in a single CanvasItem shader. For coherent bank-width and shallow-water gradient, a low-resolution **precomputed distance/coverage texture** or polygon-based inner/outer strips can be used; choose whichever is simpler and visually robust. Do not recalculate distance from every shader pixel to dozens of shoreline line segments, or rasterize/upload a mask each frame. Noise textures may be generated once and reused.

## 5. Gameplay geometry and other liquid types

Keep a generic **footprint/area** usable independently of how the surface is drawn:
- Expose a point-in-liquid query for placement rules and later swimming/pathfinding, ideally from one authoritative boundary/mask. Allow a caller-supplied margin / bank clearance.
- If the demo needs physical area detection, prefer an optional Area2D + CollisionPolygon2D shaped like the lake (or a polygon decomposed safely where necessary), with enter/exit detection. **Do not make all liquid a solid wall by default**; a pond can be wadeable while a mud pit slows actors.
- A single LiquidDefinition should eventually express e.g.:
  - Water: normal blue-green animated liquid, optional wetness/swimming/slow wading.
  - Quicksand: warm tan/brown, thick sluggish surface; strong movement reduction / sinking hook.
  - Toxic waste: sickly green, occasional bubbles/haze; damage/poison exposure hook.
  These are design *profiles* and future effects, **not a request to implement full swimming, drowning, poison, sinking, physics buoyancy, damage-over-time or player survival** now.
- Suggested configurable traversal type for later: allowed, slowed, blocked, swim-required. Reuse shared interaction/effect handling instead of editing TestPlayerMovement.cs for every liquid type.
- V1 can be **visual-only**, with a visible test footprint and optional area-enter debugging. If the lake is visually traversable without a gameplay effect, clearly document that limitation. If water is temporarily blocked to avoid a player walking on it, make the blocking policy explicit and easy to switch.

Avoid coupling the renderer to the future loot, fishing, weather or gameplay systems.

## 6. IMPORTANT: Integration with current procedural map

This is the main trap in a one-lake test: the seeded tree, rock and grass generators already fill the map. **Do not merely draw a lake on top of them**, leaving tree trunks and boulders visible underwater or grass blades poking through its middle.

- Have one defined lake footprint ready **before** generating scenery, or use a shared definition/mask consumed by both generators from startup. Respect the main scene _Ready order and avoid circular runtime dependencies.
- Extend DebugWorldScatter.FindPlacement() so tree and rock candidates **do not intersect the actual lake surface** or a sensible shoreline margin, including the approximate object/footprint radius.
- Extend DebugWorldScatter.CanPlaceGrassDecoration() (which DebugGrassPatches uses) to avoid underwater decorative tufts, perhaps allowing a few deliberate shoreline grasses only *outside* the actual water polygon.
- Keep the lake outside the start clearing and the authored path; if its footprint crosses those, handle it explicitly instead of silently covering the path.
- Don't let rock-gravel decals draw through the lake interior (ideally the generator excludes rocks; check soft edges near shores).
- The small baked ground-detail Polygon2D specks can remain underneath an opaque lake fill; don't require removing every old detail by hand.
- Preserve the tree/rock scatter seed and its independent RNG sequence as much as practical; shape data should have its own deterministic seed. A change to liquid type should not randomize all the other props.
- Runtime disabling/re-enabling the *visual* liquid does not automatically regenerate scenery. If the test has both 'lake enabled at generation' and 'show lake' toggles, document their differing behavior; a full revert can be achieved by removing the optional lake node and starting the scene again.
- If a lake is intended to be placed in a predetermined region (preferred for this first art comparison), choose that geometry at edit/config time, not by rolling for a completely new lake position every frame or every visit.

**Coordinate contract:** everything uses the same 2D world coordinates as DebugWorldScatter.Config.WorldBounds, not screen, viewport or camera-relative positions. The water interior, shoreline mask, player-area detection and spawn exclusion must agree on where the edges are.

## 7. Weather, snow and effects: reuse, don't duplicate

- Wind: read DebugWeather.WindStrength or Velocity. On calm days, tiny ambient movement; stronger wind adds small directional waves/highlights. Don't alter the global tree/rain wind to accommodate water.
- Rain: optional later rain-impact circles/ripples across the surface; if feasible in V1, use a small capped set of particles or shared shader perturbation, not per-raindrop physics. Keep existing rain splashes on dry ground unchanged.
- Snow: existing SnowTest/GroundSnow.gdshader covers the ground as a broad overlay at about Z=-7. Ensure the liquid surface displays *above* the snow ground-cover and doesn't acquire a snowy film by accident; a future **frozen lake** is a separate profile/state experiment.
- Night/lightning/torch: keep regular Godot 2D modulation/lighting working; don't render an unlit screen-space lake panel.
- Fog/atmosphere: can continue drawing over the lake. No need to implement a separate custom fog system for it.

## 8. Controls, tuning and removal

Create a compact **Liquid/Lake debug panel** with, at minimum:
- Enabled/show lake (default **on** for this new test, or off if keeping the previous visual baseline is more useful; make the choice clear).
- Surface animation on/off.
- Wave / ripple strength slider (0 = nearly still, up to a modest upper limit).
- Shoreline toggle or edge width slider.
- Optional debug footprint outline and liquid profile selector (Water/Quicksand/Toxic *visual* presets if cheap).
- Keep saved tunables in a dedicated Inspector-editable config/resource, not magic constants scattered through code.
- Don't overlap existing left-side Ground/Rock/Snow panels and right-side Atmosphere/Weather controls; inspect current UI positions before placing controls. An unobtrusive collapsible panel is okay.

**Easy undo:** the liquid scene should be a separate instance in TopDownVisualTest.tscn, with its own folder/README. Removing the scene instance and small integration hooks should restore the previous test. Avoid editing the original organic grass, weather, snow and player source files unless a narrowly scoped interface is genuinely needed.

## 9. Performance and design constraints

- Prefer **one lake draw** plus a small shoreline draw instead of hundreds of animated sprites or physics bodies.
- Reuse local/shared shader materials for multiple future water bodies; share *visual logic*, not unique polygons.
- Use stable/pregenerated boundary data, noise, distance fields or mesh strips; **no per-frame procedural lake geometry rebuild**, texture upload, CPU fluid simulation, or per-pixel C# loops.
- Only update a handful of wind/rain/time uniforms as necessary.
- Single simple collision/overlap geometry for an entire lake if needed; don't add hundreds of collision bodies along the shoreline.
- Keep shoreline complexity moderate and validate non-self-intersecting polygons; concave shapes need supported triangulation/decomposition for Godot rendering/physics.
- Avoid introducing additional full-screen shader passes or duplicate full-map overlays for every liquid.
- Future very large worlds should generate/store lake geometry by world/region and render in chunk-safe segments where needed, rather than treating one local debug lake as final architecture.

## 10. Suggested implementation sequence for Codex

1. Inspect project preferences, the main test scene, shader/noise helpers, snow/rock-gravel ordering and the existing Weather API.
2. Define a small LiquidDefinition resource and one **shared** LiquidRegion footprint abstraction; create Water definition.
3. Make a single irregular lake visible with a good organic edge, muted colour/depth and calm movement. Compare against tree/rock sprite style at zoom 2.
4. Add dedicated config for waves, depth tint, shoreline opacity/width and simple debug show/compare.
5. Reserve the lake footprint in tree/rock/grass placement **before** scattering; verify generated objects don't appear underwater, including after seed changes.
6. Make wind affect lake ripples by reading existing Weather; confirm daylight, shadows and fog remain independent.
7. Confirm normal SnowTest toggle doesn't blanket the lake in snow and that dry ground still behaves exactly as before.
8. Optionally provide visual profile switching to Quicksand and Toxic and an Area2D detection hook **only if it doesn't bloat the prototype**.
9. Build/run test in Godot .NET if available, inspect visually and with profiler. Update the relevant README with locations/settings/cleanup and report anything unverified.

**IMPORTANT — BETTER IDEAS ENCOURAGED:** Codex may choose an alternative (e.g. precomputed mask texture, Polygon2D with distinct shoreline strip, a modest multi-pass material, a tiny local scene texture) if it looks more organic or runs better. Keep generic boundaries and per-liquid data ownership, avoid one giant catch-all file, explain the choice briefly. Do not launch into a full hydrology system, permanent world chunks, swimming mechanics or toxic damage on this first pass.

## 11. Acceptance checks

- [ ] Running TopDownVisualTest.tscn (F6) shows **one naturally shaped lake** of sufficient size to see clearly from the current 2D camera.
- [ ] Lake has appealing irregular banks, understated animated surface motion, colour/depth variety, and no obvious tiled squares or camera-locked noise.
- [ ] Same generic liquid scene/renderer can use a **different LiquidDefinition** without rewriting its shape/placement logic.
- [ ] Player can approach and trace along the bank; behavior on entry is clear (visual-only, blocked or overlap detection), with no invisible rectangular obstacle.
- [ ] Trees, rocks and decorative grass don't spawn **inside the lake** for several different existing scatter seeds; existing central trail remains usable.
- [ ] Water displays sensibly beneath shadows and above snow ground cover; snow toggling still works.
- [ ] Wind ±3 / 0 and rain 0–100 don't break lake visuals or original precipitation/vegetation effects.
- [ ] Day/night, torchlight, lightning, ground grass, rock gravel, fog, snow and grass sway all still work.
- [ ] Enabling/disabling/remove-scene comparison works without leaving duplicate materials or permanent ground modifications.
- [ ] New code is only under DEBUG/TopDownTest; documentation is updated; no per-frame geometry re-generation or unbounded effects.
- [ ] Godot project compiles and scene plays cleanly if tooling is available; otherwise be explicit about unverified rendering/physics.

---

**Related plans:** WORLD/Water/README.md, WORLD/Water/Lakes/README.md, WORLD/Generation/Water/README.md and Notes/World_Generation_Direction.md. Those production locations remain **scaffolding only**; this file requests a self-contained **debug test**, not a production feature or folder migration.
