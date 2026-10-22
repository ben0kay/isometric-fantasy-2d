# Tree fire — Codex FIRE1 plan and follow-up implementation handoff

**Status:** FIRE1 IMPLEMENTED; FIRE2–FIRE4 remain planned.  
**Project:** 2D top-down fantasy survival prototype (Godot .NET / C#).  
**Scope:** `DEBUG/TopDownTest/` only. No production code changes in FIRE1.  
**Art choice:** Use **`Charred Leafless Oak Sprite.png`** currently uploaded at repository root. **Do not use** `Charred Oak Tree Sprite.png` for this initial prototype. Keep the unused file unchanged.  
**Agreement:** Codex should implement **FIRE1 only**, commit once, and leave an exact handoff for later FIRE2–FIRE4 implementation in ordinary ChatGPT. Do not quietly implement later passes.

## 0. What the final prototype should eventually show

A living oak struck by lightning catches fire, progressively loses its healthy canopy and turns into a **standing charred leafless oak**. Only the visuals change: the same tree `StaticBody2D` persists, keeps its world position and appropriately grounded trunk collision. An optional later phase could topple the tree or reduce it to ash, with configurable odds, but **no collapse/ash, spreading fire, tree damage/loot or survival effects in FIRE1**.

Desired sequence (all times are artistic defaults, editable later):

1. **Impact / ignition (0 s):** small flash and sparks near the struck tree.
2. **Burning (roughly 0–6 s):** active flames, smoke, embers, wind drift; crown gradually darkens, breaks into irregular burned holes and gives way to a dead image.
3. **Burnout (roughly 6–8 s):** flames shrink, smoke trails off; original healthy visual is completely gone.
4. **Standing dead:** original tree object now displays the charred leafless texture; no per-frame burn processing; shadow shows the **leafless** silhouette, not the former full canopy.

This is an **art-directed progression** rather than a physically simulated wildfire. Rendering design and gameplay ignition should be separate, allowing lightning, an axe, fire magic, lava or neighboring fires to call the **same tree ignition contract** in future.

## 1. Existing source files Codex must inspect before touching anything

- `CodePreferences.md`: C#, modular folders, README/config, method comments immediately above methods with `// =========================================================`, regions where appropriate, performance-oriented helpers, **one commit per job**.
- `DEBUG/TopDownTest/Trees/Tree01.tscn`: oak root `StaticBody2D`, child `Sprite` and `TrunkFootprint`; currently `Sprite.position = (-63.02,-148.35)`, `Sprite.scale=(0.115,0.115)`, `centered=false`. The original `tree_01.png` is 1086×1448 with a visually anchored trunk near source pixel (548,1290). The root is at the **ground anchor**, not sprite centre.
- `DEBUG/TopDownTest/Trees/Tree01Wind.tres` / `TreeSway.gdshader`: shared material, animated canopy, still trunk. DayNight adjusts shared `wind_multiplier`.
- `DEBUG/TopDownTest/Generation/DebugWorldScatter.cs`: creates oak and blossom trees once in `Actors._Ready()`; root positions, positive scales and mirrored art/footprint, Y-sort. **Never add a parallel tree spawner** for burning.
- `DEBUG/TopDownTest/Biomes/OakForest.tres` / `PinkMeadow.tres`: FIRE1 applies to **oak species only**. Pink blossom trees stay unchanged.
- `DEBUG/TopDownTest/Trees/HitFeedback/TreeHitFeedback.cs`: child axe-hit receiver caches `Sprite.Transform`, makes tree wobble and emits leaves. A burning/dead oak must eventually stop producing *healthy* leaves.
- `DEBUG/TopDownTest/DayNight/DebugDayNight.cs`: crucial: caches a `Caster` and builds a **separate** sunlight-shadow `Sprite2D` at startup, initially pointing at the healthy texture. **Changing the tree sprite texture alone does not change that cached shadow texture.**
- `DEBUG/TopDownTest/SnowTest/TreeSnowAdapter.cs`: runtime snow material variants replace tree sprite materials; the adapter also restores cached originals on scene removal. This needs coordination with any temporary fire shader/material so neither system overwrites the other.
- `DEBUG/TopDownTest/Weather/DebugLightning.cs`: important: `Trigger(true)` currently obtains its point from `DebugRainSplashes.TryGroundPoint()` which intentionally excludes tree/rock artwork. **Current visible lightning cannot select or hit an oak.** FIRE3 must explicitly implement a separate tree-target path.
- `DEBUG/TopDownTest/Weather/DebugWeather.cs`: one shared `WindStrength` (-3..+3) and `Velocity`. Do not create another wind simulation. Existing rain is independent of the accelerated day/night clock.
- `DEBUG/TopDownTest/Atmosphere/DebugAtmosphere.cs`: pre-existing smoke, ember and falling-leaf drawing for ambient/torch effects. Fire-specific effects should be located per active tree or in a pooled shared fire renderer; avoid binding tree burning to the one torch.
- `DEBUG/TopDownTest/SnowTest/DebugSnowTest.cs`, `TopDownVisualTest.tscn`: startup order matters. The SnowTest instance is created **after** Actors and DayNight, so material adapters are installed after DayNight caches its shader references.
- `DEBUG/TopDownTest/Notes/LiquidLake_Codex_Brief.md`: example of a contained, self-documenting debug experiment and handoff style.

**Check the live files again before implementation**, as other commits may change them.

## 2. FIRE1 — the only Codex implementation requested now

**End-of-pass demonstration:** open the main `TopDownVisualTest.tscn`, press a clearly labeled **Ignite nearby oak** button, and watch **one real generated oak** transition to the uploaded charred leafless artwork after a configurable delay. The tree stays put and retains its original collision, while the rest of the game still works.

FIRE1 is intentionally small: **one reusable receiver + one oak configuration + basic no-particles visual transition + a manual test control**. Do **not** connect random lightning yet or build GPU flames, smoke, spreading fire, heat, damage, ash piles, collapse, saving or networking.

### 2A. Assets and coordinates (do this first)

1. Move/copy the exact binary Git blob `Charred Leafless Oak Sprite.png` from repository root into something like `DEBUG/TopDownTest/Artwork/tree_oak_charred_leafless_01.png`. **Preserve the artwork**, do not regenerate it; reuse its SHA when moving through GitHub. The second root `Charred Oak Tree Sprite.png` is reserved for future tests and must not be substituted.
2. Open/inspect the healthy and dead source images in an image viewer or Godot. **Do not assume their canvas sizes, transparency, aspect ratios or trunk anchors are identical.** Compare transparent bounding boxes, trunk/branch intersections and visible base.
3. Prefer generating a **transparent padded/aligned derivative** of the dead PNG if necessary to share the healthy tree's canvas and ground anchor. Document both the unmodified source image (when kept) and any prepared runtime copy; no stretches or material hacks to conceal a displaced trunk. If instead using two different `Sprite2D` transforms for the transition, compute their anchors explicitly and keep the same world-space ground contact.
4. Confirm sprites remain aligned across the existing whole-root scale (about 0.93–1.07) and mirrored variants. Retain `Tree01.tscn`'s authored `TrunkFootprint` unless testing proves the bare tree needs a different root footprint.

### 2B. Small module structure

Proposed, not mandatory exact names:

```text
DEBUG/TopDownTest/
├── Fire/
│   ├── README.md                       # Current status and how to test/remove
│   ├── FireMasterConfig.cs             # Exported per-oak timings and art choice
│   ├── DefaultFireConfig.tres
│   ├── Simulation/
│   │   └── BurnableTree.cs             # Component on oak; owns finite state and API
│   ├── Visuals/
│   │   └── (FIRE1 minimal blend only)   # No full fire particles yet
│   └── Debug/
│       └── FireTestController.cs       # Button and reset/show status
├── Trees/
│   └── Tree01.tscn                     # Adds optional BurnableTree child
└── Artwork/
    └── tree_oak_charred_leafless_01.png
```

A `FireTest.tscn` containing the debug controls and no world-specific dependencies beyond an exported Actors reference is welcome. Keep the main test scene integration **one removable instance**, with only a narrow optional child added to the oak template. Avoid new autoloads or global managers.

**Prefer data-driven tree options:** FIRE1's oak config owns alive/dead textures (or clean references to those resources), ignition-to-burnout duration, transition duration, and whether the tree may reignite. Avoid hardcoded char texture paths in `BurnableTree.cs` or multiple species checks. A future pink-blossom config should only need different textures/parameters, not duplicated logic.

### 2C. Minimal state contract for the next implementer

Implement a small lifecycle such as `Healthy → Burning → Burnt` (and `Reset` for debug). **Expose the API clearly and keep it stable** for FIRE2 and FIRE3:

- `TryIgnite(Vector2 worldImpactPosition, ...source...) → bool` (or a documented equivalent): returns false if already burning/burnt or disabled, true only on a fresh ignition. Make the ignition-source argument optional or an enum/data value; don't couple `BurnableTree` to `DebugLightning`.
- Read-only `State`, `BurnProgress` (0 to 1), and source or world impact coordinates for FX placement.
- A reliable **one-shot event/signal** on ignition and on final transition to burnt. Later GPU particles, lights, shadows and lightning should subscribe rather than poll every tree.
- `ResetBurn()` used only for test UI; restores healthy texture/state and cleans up transient visuals without deleting/reinstantiating the tree.
- `_Process` should be **disabled on idle and burnt trees**. Only a tree actively burning advances its timer.

An exact class/enum layout is up to Codex, but **record the final public methods and signals in the Fire README** so later passes can target the real API.

**No tree HP system** is needed now. Fire is an independent state component; weapon-damage integration can come later.

### 2D. Minimum visual demonstration

Use a **simple temporary overlap/crossfade** of healthy and leafless sprites or an equally small transition effect. This first pass only needs to establish:

- The original alive tree is visible before ignition.
- On ignition, the receiver begins its timer; its appearance gradually shifts toward the dead artwork over a configurable few seconds.
- On completion, **the original permanent tree `Sprite2D` displays the dead texture**. Do not destroy the `StaticBody2D` or spawn a second permanent tree.
- The interim overlay should use consistent Sprite2D position, scale, flip, source transform, filtering and ground anchor. Don't show two offset trunks or let the crossfade alter the collision.
- Prefer preserving the original `Sprite2D.Material` on a simple FIRE1 blend, because wind/snow materials can be runtime-swapped. **Do not replace the existing TreeSway.gdshader wholesale** to get the first animation working.
- Distinct burnt visual is allowed to stop tree crown sway. If leaving the canopy shader on the burnt tree creates implausible branch movement, document it for FIRE4.
- Avoid animated, noisy foliage disappearance for FIRE1 if it risks overcomplicating the snow/wind layering. FIRE2 can implement a proper GPU burn mask/shader after the state-and-art swap is proven.

If the dead art cannot be aligned closely, **do not silently commit a visible jump**. Either solve anchor/padding in FIRE1 or explicitly report an unresolved blocker with screenshots/comparison.

### 2E. Critical integration decisions

- **Sun shadow:** FIRE1 should ideally refresh the exact cached sunlight shadow texture **once on final burnout and once on reset**. The cached `DebugDayNight.Caster.Shadow.Texture` does not automatically track `Sprite.Texture`; do **not** rebuild the entire shadow cache or spawn extra shadow sprites. A small, reusable `RefreshShadowAppearance(Node2D tree)` or a targeted signal connection is preferable. At a minimum the test must not leave a green full-crown silhouette after the dead texture appears.
- **Snow:** current TreeSnowAdapter swaps the tree's material at runtime. Don't overwrite it with a permanent fire-only ShaderMaterial or allow `ResetBurn()` to restore a stale one. A no-shader alpha transition avoids most complications. FIRE1 may visibly leave charred wood largely uncovered by snow; a true charred-wood snow profile can be handled in FIRE4. Test with SnowTest initially off and with it enabled.
- **Axe / HitFeedback:** preserve the existing node and collision. At minimum ensure a dead tree cannot emit a burst of **green healthy foliage** on axe hit (e.g., skip leaf burst if burnt, or configurable 0 leaves). FIRE4 can add ash/chip feedback; don't overhaul the weapon system.
- **Ambient falling leaves:** DebugAtmosphere caches generated tree anchors and may keep spawning ambient leaves from charred trees; note this as a future FIRE4 fix rather than rewriting all atmosphere in FIRE1.
- **Biome:** initially apply only to `Tree01.tscn`, not `PinkBlossom01.tscn`; the generator should continue to spawn oak and pink trees normally.
- **No gameplay obstruction changes:** keep static root, mirrored collider, Y sorting, player pathing, lake exclusion, gravel and tree scatter counts intact.
- **Lifecycle timing:** use elapsed real time, not the compressed 30-second day cycle.

### 2F. FIRE1 debug interface

Add a small unobtrusive control group (inspect existing Weather/Atmosphere/Snow/Fire panel positions first):

- **Ignite nearest oak** (within configurable radius of the player; deterministic tie-breaking / choose closest live oak only). If no oak is in range, show a status message; **don't silently spawn or teleport**.
- **Reset nearby oak** (or Reset all debug burning trees, clearly labeled) to repeatedly test without reloading.
- A compact status label such as `Healthy / Burning 52% / Burnt`.
- Optional **Instant char** button to isolate sprite/anchor/shadow checks without waiting for the burn timer.

Do not wire this button to `DebugLightning.Trigger(true)`: that method currently strikes **open ground**, not trees.

### 2G. FIRE1 acceptance criteria / tests

- [ ] The two charred root PNGs are treated correctly: only **Charred Leafless Oak Sprite.png** is imported/used for FIRE1; other charred image remains untouched.
- [ ] Same oak root and footprint persist: tree coordinates, mirror, local/root scales, collider and hit receiver unchanged.
- [ ] FIRE1's healthy→burning→burnt transition is visible, configurable and repeatable through reset.
- [ ] First `TryIgnite` succeeds; repeated calls while burning/burnt do not create more overlays/timers/signals.
- [ ] Burnt tree displays the **correct charred leafless texture at the same ground anchor**, both normal and flipped.
- [ ] Sun shadow changes to the bare silhouette on burnout and returns to healthy on reset; no extra duplicate shadow nodes.
- [ ] Snow test on/off and axe interaction do not permanently corrupt materials or cause exceptions; dead oaks don't shed green foliage from a hit.
- [ ] Pink blossom trees, rocks, ground, lake, snow, rain, wind, lightning, lighting and biome generation remain unchanged.
- [ ] Idle/burnt trees do not have active per-frame `_Process`; only actively burning oaks update.
- [ ] FIRE1 can be removed: detach `BurnableTree` from the oak template and remove the Fire test instance/folder; no production files were modified.
- [ ] Build Godot .NET C# and launch the test scene if available; inspect the anchor, mirrored tree, snowfall and sunlight shadow. If runtime execution isn't possible, **do not claim a visual test**.

### 2H. FIRE1 deliverable and handoff contract

After implementation, Codex should:

1. Make **one GitHub commit** with the art import, module, oak scene hookup, optional scene control and relevant READMEs. Never bundle FIRE2+ into the same work.
2. Update this note's **FIRE1 Status** section (below) with completion date, commit SHA, exact class/API/signal names, actual file paths and any modified cross-system hooks.
3. Explicitly describe any deviations from this plan, anchor/format decisions, shader/material compromises and Godot compile/render evidence.
4. List remaining known issues, especially snow/material layering, projected shadow, leaf emission and transition artifacts.
5. Stop there so the user can ask the next implementer to continue with FIRE2.

## 3. FIRE2 — IMPLEMENTED: flames, smoke, embers and transition polish

**Depends on:** a working FIRE1 receiver and documented ignition/state events. Do not replace the state machine.

**Original scope (implemented by FIRE2 unless noted below):**

- `FireVisualController` or `BurningTreeEffect` attached only to **active burning trees**; subscribe to lifecycle events and pool/reuse effect resources. No processor on every static tree.
- **GPU flame particles** `GpuParticles2D` for bottom/trunk and a small number of art-directed canopy ignition patches; pixel-art-compatible animation atlas or small procedural stylized flame texture if suitable.
- Separate smoke and ember effects; capped particles and lifetimes; fade out after burn completion rather than abruptly deleting all effects. Smoke/fire colours should complement existing torch and lightning lighting.
- **Single temporary PointLight2D per burning tree** (or capped shared pool) with subtle flicker and realistic illumination at night. Reuse principles of existing torch/lightning, but don't change their code unnecessarily.
- Read `DebugWeather.WindStrength/Velocity` so **all burning effects** move with the same wind, without making fire own a second wind controller.
- Upgrade the FIRE1 blend to an **irregular shader-based foliage erosion/char wipe** driven by `BurnProgress`; underlay is the dead-tree texture. Keep tree roots aligned, original alive sprite's `Material`, snow and wind coherent. Stable world-/tree-seeded noise; **no CPU pixel rewriting each frame**.
- Protect sprite alpha and prevent tree-trunk ghosting (prefer foliage-only mask where possible), with coherent silhouettes/shadows.
- Add flame/smoke intensity settings (count/caps, warm colour, emission size, drift, burnout tail, fade, optional light strength) to FireMasterConfig.
- Smoke/embers should remain separate from the pre-existing torch particle system; reusable effects can later attach to grass/buildings/objects without duplicating rendering logic.

**Performance target:** multiple simultaneous burning trees should not create unbounded lights/emitters, resize textures every frame, or redraw all trees. Caps and offscreen policy are part of the implementation. Document a test with 1, 5 and ~10 simultaneous fires.

## 4. FIRE3 — lightning hits an actual eligible oak

**Important existing limitation:** `DebugLightning.Trigger(true)` calls `DebugRainSplashes.TryGroundPoint()`, which rejects **all tree/rock artwork**. Simply looking for a tree at the resulting open-ground impact will almost never ignite anything. There must be a distinct selection path.

Implement:

1. A toggleable **Tree strike chance** in the lightning/Fire config and a debug **Strike nearby oak** button. Ground strikes must continue using the old dry-ground point selection.
2. Build a **cached/queried eligible-oak set** from BurnableTree components; do not raycast every pixel of all trees every frame. Restrict candidates to the visible camera rectangle with sensible canopy margin; optionally favour the highest canopy or nearest eligible tree. Exclude already burning/burnt trees.
3. Use the selected tree's **canopy hit point in world coordinates** as the final lightning bolt endpoint (not merely the root/ground collision), and call **that exact receiver**'s `TryIgnite(worldImpact, Lightning)` once at impact. A positive chance to hit a tree should not inadvertently convert ground strikes into hidden failures.
4. Keep existing nearby flashes, open-ground visible strikes, rain thresholds, manual preview buttons, cooldown, radial light, branching bolt, day/night ambient restoration and rain-ground-splash behaviour working.
5. For deterministic manual testing, pressing **Strike nearby oak** should reliably strike one visible, healthy oak when eligible; when none is in range report it in UI rather than flashing an unrelated tree.
6. Lightning remains the ignition **source**, not the owner of a tree's burn timer/effects. Wind/rain conditions should not prevent a manual debug ignition at this stage.

Separate out any chance that rain suppresses ignition for later climate/flammability work; it must not interfere with reliably testing the first fire.

## 5. FIRE4 — robust integration, optional aftermath and future gameplay

Once the effect is visually stable:

- **Shadow:** ensure shadow during erosion/afterburn reflects actual partially bare/final leafless state. Avoid creating a new shadow object every transition frame or mutating shared shadow shaders for unrelated healthy trees.
- **Snow/weather:** snow melts or is visually suppressed on actively burning trees; charred branches can accumulate snow later through a separate **charred-wood coverage profile** in the existing adapter. Don't break pine/oak/blossom/grass variants.
- **Hit feedback:** burnt trees react with ash/wood chips instead of green leaves and can still be chopped if desired.
- **Ambient foliage:** stop choosing burnt trees as sources for ambient healthy falling leaves; update cached emitter eligibility when state changes.
- **Rain:** eventually decrease fire intensity / extinguish some burning trees, with configurable thresholds and independent states (wetness, flammability). Keep the world Weather simulation separate from tree status.
- **Scorch/debris:** optional ground scorch decal using the same modular terrain-surface/decal conventions, not an expensive new full-screen pass.
- **Outcome selection (later, NOT FIRE1):** configurable chance for a standing dead tree (default 100% initially), collapse into fallen log with new art/appropriate collider, or stump/ash with separate art and cleared collision. Don't fake collapse by merely rotating the static collider unpredictably.
- **Spread/heat/biomes (much later):** neighboring flammable foliage, tree density, wind direction and humidity; bounded event-driven/interval checks rather than per-frame all-to-all tree searches.
- **Persistence:** if the game later saves world state, store tree ID and burn state/outcome so burned trees stay dead after reloading; not needed in the disposable test.

**Suggested production ownership when promoted:** environment/weather supplies lightning and wind; a shared Fire/Combustion or survival/environmental-hazard system owns ignition/status; the tree/vegetation content owns live/dead art and flammability; rendering/particles and local lights are reusable visual components; generation only creates initial tree instances. Do not implement production scaffolding as part of this test.

## 6. Cross-system non-negotiables

1. **Only oak initially.** The pink biome remains intact; future species opt in through resources/components.
2. **One tree entity throughout burning.** Never delete/recreate the tree just to replace a sprite; preserve trunk collision and scene identity.
3. **No second wind simulation.** Fire and embers follow the Weather wind already driving trees, grass, smoke and rainfall.
4. **Materials are already layered:** DayNight clones shared sway materials; SnowTest installs snow variants after DayNight; Fire overlays or texture swaps must coordinate with both.
5. **Shadow images are cached independently from source texture.** Account for this explicitly in FIRE1.
6. **No hidden tree strikes by default:** existing lightning is ground/nearby flash until FIRE3 deliberately targets eligible oaks.
7. **Bounded effects:** process active fires only; share art/material where possible; avoid per-frame CPU image editing and per-tree global scene scans.
8. **Reset and removal:** restore normal art/material and cleanup; keep the whole test isolated.
9. **One implementation commit per pass** and update related README(s) plus this handoff note.
10. **No speculative claims of running Godot:** document what was compiled and visually tested versus only statically inspected.

## 7. FIRE1 Status — Codex should update this section

**State:** FIRE1 IMPLEMENTED — 21 October 2026. FIRE2–FIRE4 were not implemented.

- Commit: the single **Implement FIRE1 oak burn lifecycle and aligned charred art transition** commit containing this status. Resolve its exact SHA from this file's Git history; the final response supplies the link. A commit cannot embed its own final SHA without changing that SHA.
- Art: `Fire/Artwork/oak_charred_source.png`, exact existing blob cb9f85a132406bb567a09fc328b910d223ead325. Root source and unused second charred image preserved. No derivative/resampling: explicit source anchors healthy (548,1290), dead (565,1235), different source widths reflected on FlipH, Sprite.Offset alignment preserves original Sprite.Transform/root scale/collider.
- API: `Fire/Simulation/BurnableTree.cs`; FireTreeState Healthy/Burning/Burnt, TryIgnite(Vector2,string="Debug") → bool, ResetBurn(), State, BurnProgress, WorldImpactPosition, IgnitionSource.
- Signals: Ignited(Vector2,string), BurnCompleted(), BurnReset(), AppearanceChanged(). See Fire/README.md for lifecycle semantics.
- Controls: Fire/FireTest.tscn, Fire/Debug/FireTestController.cs; main scene gains one optional instance after SnowTest. Ignite nearest healthy oak within configurable radius, reset all and selected progress; no spawn/teleport.
- Shadow hook: targeted DebugDayNight.RefreshShadowAppearance(Node2D), called by AppearanceChanged subscription on final/reset; existing caster reused.
- Axe hook: generic TreeHitFeedback.SetHealthyLeaves(bool) clears/suppresses healthy hit foliage through burning/burnt; reset restores it. Wobble and collision preserved.
- Snow: no material replacement or stale material restoration by fire. Alpha-only transition and final texture/offset swap preserve the current runtime material. Charred branch snow/wind polish deferred.
- Deviations: anchor offsets instead of a padded derivative; exact original source retained under Fire/Artwork. No Instant char button needed. No particle pass, lightning, collapse/ash, damage or production changes.
- Remaining limitations: basic translucent crossfade, healthy shadow until final, ambient leaves still choose charred anchors, branch sway/dedicated charred snow profile deferred. Continue with FIRE2 using this receiver, not a replacement state machine.

- Validation: native Godot 4.5.1 / .NET 8 C# build, runtime API/integration assertions and actual software OpenGL mid/final renders passed. Normal/mirrored anchors, stable colliders/materials, one-shot events, shadow texture/offset, snow on/removal and reset controls checked. Reduced grass for software rendering only; existing 115/116 scatter warning unchanged. Windows D3D12/hardware performance unverified. The concurrently fixed rock-snow shader is preserved, not rewritten by FIRE1.

**Next action:** FIRE2 is implemented. Continue with FIRE3 (tree-targeted lightning), then FIRE4 integration, only when requested.

## 8. FIRE2 Status — 22 October 2026

**State:** FIRE2 IMPLEMENTED, pending local Godot C# compile/GPU-render verification. One atomic GitHub commit covering the files described in `Fire/README.md`; inspect repository history for exact SHA.

- Reused `BurnableTree.Ignited`, `BurnCompleted`, `BurnReset`; left FIRE1 state, timer, source texture alignment, collider, shadow event and debug controls intact.
- Added `Visuals/BurningTreeEffect.cs`: GPU flames, grey smoke, hot embers, temporary flickering PointLight2D, common procedural textures, old-particle tail, and per-ignition cleanup.
- Added `Visuals/FireErosionAdapter.cs` and `FireErosion.gdshaderinc`: per-active-tree dynamic composition with whatever wind/snow material currently exists; uneven canopy loss reveals FIRE1's charred underlay without changing global materials.
- Expanded `FireMasterConfig.cs` with exported particle caps/intensities, wind response, char tint, light appearance, smoke tail and a max-active-effect limit (default 10).
- `FireTestController` caches ignition subscriptions and gives each actively burning oak at most one FX child; excess active fires use FIRE1's fade.
- `BurnableTree.SetFoliageErosion(bool)` provides the optional FX presentation hook, returning to FIRE1's crossfade if shader adaptation is unavailable.
- Scope limits: FIRE3 tree lightning is **not** connected yet; FIRE4 partial shadows, charred-wood snow, ambient leaves and spread remain unimplemented.
- Run a local Godot .NET build and test wind -3/0/+3, snow on/off, full burning→smoke tail, Reset all, and 1/5/10 active fires. No actual Godot rendering/test run is claimed by this commit.
