# Layered Ground Surfaces — Production Implementation Plan

> **Status: GS0 source audit complete; graphical/performance baseline pending. GS1 implemented, awaiting local build/runtime/visual validation. GS2/GS3 implemented, awaiting local engine/visual/performance validation; GS4 controls implemented, awaiting local engine/UI validation; GS5 optional Oak dirt/rock and F9 strength sliders implemented, awaiting visual/local validation; GS6 remains pending.** See [GS0/GS1 handoff](GS0_GS1_Validation.md). Codex must execute only the phase explicitly assigned by the author. Creating this note does not authorize coding.
>
> **Absolute priority: DO NOT change how Oak Forest currently looks by default.** First extract its existing ground behaviour into a reusable layered architecture; only later offer optional, individually enabled layers for dirt and rock breakup. The supplied illustrated Oak Forest scene is art-direction inspiration, not an exact terrain shader replacement.
>
> **Project rules:** Read root `README.md`, `CodePreferences.md`, relevant feature READMEs and actual HEAD code before edits. Production Godot C#, low idle cost, modular Resource-driven authoring, one GitHub commit per approved job, and separate visual/performance validation.

## Pre-GS2 addition — biome-owned tint

Implemented: BiomeGroundLayer.Tint (opaque RGB, white default), applied to the completed base appearance by both existing renderers. Production biome/profile Resources remain unchanged. Streamed tint uses a shared coarse RGB map only when any biome is tinted; all-white skips that upload/sample. No per-frame CPU work. Overlay tint becomes effective with GS3, since overlays remain unsupported now. This is an explicitly authorized tint extension, not completion of GS2. See TINT_Validation.md for local checks and limitations.

## Optional appearance revision after GS5

The author rejected the first optional SVG materials. Dirt/Rock now use procedural recipes adapted from the old isometric game's dust/basalt shaders. No base/companion changes; no dirt/rock SVG dependency. See [current appearance and pending checks](PROCEDURAL_GROUND_RECIPES.md).

## 1. Current clarified stack — author request supersedes the original base-mottling design

1. **Defining Base Ground:** one full-coverage source texture and base colour/tint, plus artwork fine detail.
2. **Accompanying Patch Layer:** optional, independently authored and toggled GroundPatchLayer for underlying flavour (Oak's existing greener/drier patches). It is not hidden inside the base and does not use an extra slot.
3. **Extra Patch Layers:** at most two optional ordered surface overlays, e.g. dirt and rock later.

This separation is implemented for Oak/Pink while preserving their existing enabled appearance. Oak now configures both extra slots as default-OFF F9 previews (Dirt and Rock); Pink remains empty. See GS5_Validation.md. See [accompanying extraction evidence and local check](ACCOMPANYING_PATCH_LAYER.md). Earlier GS0–GS4 implementation received user-reported World/practice functional checks; the extraction itself awaits local validation, and target performance evidence remains separate.

Decoration/scenery and ENVIRONMENT weather/lighting remain independent. Reusable grass/sand/soil/stone profiles and biome-owned tint remain the data-driven material source; no biome-specific renderer classes.

## 2. Historical pre-GS1 repository findings — verified 23 October 2026

- `WORLD/Biomes/Shared/WorldBiomeDefinition.cs` currently exports one `GroundSurfaceProfile Surface`. `WORLD/Biomes/Definitions/OakForest/Biome.tres` selects `ForestGrass.tres`; Pink Meadow selects `PinkMeadowGrass.tres`.
- `WORLD/Terrain/Ground/Surfaces/Profiles/GroundSurfaceProfile.cs` holds `Texture`, `Material`, four colour values (`BaseColour`, `ShadeColour`, `FreshColour`, `DryColour`), `Variation`, `PatchSize` and `DetailStrength`. Both existing profiles use `grass.svg` and the shared grass material/noise; Pink overrides its colour palette.
- Production default F6 uses P3 streaming: `WORLD/Runtime/Streaming/WorldChunkStream.cs` constructs one clipped ground quad per loaded chunk using `StreamedGroundPalette.Create`. `WORLD/Terrain/Ground/Surfaces/StreamedGroundPalette.cs` builds shared atlas, coarse biome colour/parameter maps and material once per world. `StreamedGround.gdshader` samples world-space noise/detail and artwork. Default chunk size 512 world units.
- The old 3840x1728 compatibility preview uses **different** `GroundSurface.cs` and `GrassSurface.gdshader` via `AdjacentBiomeLayout`. It must remain functional; prioritize the streamed renderer when implementing the new system.
- Existing streamed colour maps blend across macro biomes, but **artwork uses a nearest-cell atlas index**. When grass and sand/stone genuinely differ, that approach may introduce visible hard art transitions: add proper cross-material blending for multi-surface cases instead of assuming colour interpolation solves it.
- `WORLD/Terrain/Ground/Surfaces/Noise/GroundNoise.tres` provides shared seamless noise; `Shaders/NoiseSampling.gdshaderinc`, `PatchColours.gdshaderinc` and `FineDetail.gdshaderinc` have reusable shader helpers. Avoid divergent duplicate implementations.
- `DEBUG/TopDownTest/Ground/RockPatches` demonstrates organically edged gravel decals, but is **DEBUG-only**, not promoted to production. It can guide later localized decal work.
- `ENVIRONMENT/Weather/Snow/WorldSnow.cs` already renders independent snow ground cover. Do not merge weather, cloud shade or haze into ground Resources.
- `WORLD/Generation/Terrain/` remains primarily future terrain generation scaffolding, not an existing cliffs/roads/heightfield implementation.
- Some terrain READMEs still incorrectly label all of production terrain as scaffolding; correct those when authorized coding phases land.

## 3. Original phase data contracts and file ownership (see current clarified stack above)

**Do not delete, rename or reinterpret the existing Oak/Pink .tres profiles in GS1.** Reuse established Resources before adding types.

| Type / folder | Purpose |
| --- | --- |
| `WORLD/Terrain/Ground/Surfaces/Profiles/GroundSurfaceProfile.cs` | Canonical reusable **surface appearance** profile: source artwork/material family, current palette, organic base variation and fine detail. Retain old fields and resource paths for initial parity. Introduce new Dirt/Sand/Rock profiles only when needed. |
| `WORLD/Terrain/Ground/Surfaces/Layers/BiomeGroundLayer.cs` (proposed) | One biome-specific use of a surface profile: `Surface` reference, readable `Label`/stable ID, optional tint, enable state for overlays, coverage/opacity, noise scale, threshold/bias, edge softness, warp/breakup strength and stable offset/seed salt. Base is always 100% coverage; hide irrelevant overlay-only fields in authoring if practical. |
| `WORLD/Terrain/Ground/Surfaces/Layers/BiomeGroundStack.cs` (proposed, or `Stacks/` if better) | **DefiningBaseGround** (one required layer) plus ordered `OverlayLayers` (0–2 for the initial renderer). Inspector must group/label base separately from extras. Validate nulls, ranges, ordering, unsupported combinations and duplicate layer IDs. |
| `WORLD/Biomes/Shared/WorldBiomeDefinition.cs` | Owns reference to the ground stack for that biome. Keep Oak/Pink assets canonical under WORLD, not copied into biome folders. Prefer inline layer entries in each biome stack over excessive .tres files. |
| `StreamedGroundPalette.cs` + shared ground shader helpers | Compile each biome's surface stack into efficient world-scoped GPU maps/atlas resources; unchanged chunk quads and deterministic blending. No hardcoded biome names or one material instance per chunk. |
| `DEBUG/UI/Ground/GroundSurfaceDebugPanel.cs` | Removable F9 controls for session-only comparison and live overrides; communicates with production ground through a minimal interface, not a production-to-DEBUG dependency. |

**Migration:** Existing `WorldBiomeDefinition.Surface` is used by the preview and tests. Keep a temporary safe fallback/adapter as required while migrating and validating; eventually have one canonical stack, not permanently competing properties. Do not casually invalidate resource UIDs, saved worlds or Inspector edits. Changing the appearance schema should not change biome identity, macro region generation, scenery IDs, collision, chunk IDs or save deltas.

**Longer-term separation:** If/when many biomes share identical material art but choose different palettes, separate intrinsic reusable appearance defaults from the biome-specific tint/override explicitly, avoiding both giant duplicated profiles and a second hidden source of truth. Don't build that extra hierarchy until needed.

## 4. Masking and rendering policy

- Sample **world coordinates**. Camera motion and chunk origin cannot move/restart a pattern. Mask seeds/salts must be deterministic under fixed world/content configuration and unchanged by chunk build order or reload.
- Compose base first, optional layers by array order, then preserve downstream Godot lighting/atmosphere. Each overlay gets its own broad noise scale, warped boundaries, threshold/coverage, softness and opacity. Fine grain should add texture, not decide broad biome geography.
- Layer masks must create imperfect, interrupted patches rather than obvious circles, sharp tile boundaries or uniformly blended mush. Noise helpers should be reusable among grass/dirt/sand/rock, while genuinely different material families may use different shared detail compositions.
- Keep the current Oak base math/artwork ratio/32px sampling exactly until visual sign-off. In particular, preserve its existing profile values and colour blending. **All overlay slots disabled must produce original streamed output.**
- Blend different biome surface materials/textures, not merely their RGB colours. Handle neighbour material sampling at macro boundaries and avoid atlas bleeding; do not break seamless noise or the present two-grass transition to achieve this.
- Prefer **one existing ground quad per chunk**, shared material/atlas/maps per world, bounded layer slots, and zero per-frame CPU painting. Do not add a Sprite2D/TileMap/Node for every ground tile.
- Atlas/map costs must remain bounded for many biomes. If multi-layer shader work is too costly, compare strategies (compile bounded material variants, precompute sparse/chunk-local masks only on creation, reuse cached textures) using measurements, not guessed optimizations. Disabled/zero layers should skip needless shader work.
- No updates or GPU uniform writes while the ground is idle. Initializing/toggling/tuning should be event-driven. Avoid full macro-map reconstruction and texture uploads on every F9 slider change.
- Later real trails/roads, settlements and author-painted features may supply **external coverage masks** to constrain layers; don't tie those algorithms to the first two noise masks.

## 5. Implementation phases — Codex executes only the authorized phase

### GS0 — Current-state audit, screenshots and performance baseline (NO visual change)

1. Re-read root README/CodePreferences, feature docs and current actual code, including streamed ground, legacy preview, F9 and validation tests. Check HEAD because this plan may become stale.
2. Capture fixed seed and fixed position/zoom/window/time/weather **reference images** for Oak Forest, another Oak area, Pink Meadow and cross-biome edges. Record Godot renderer/version, seed, clock, camera, resolution and shader settings; require graphical Windows screenshots as needed. Use the supplied concept image only as *future optional-layer inspiration*.
3. Record current init, idle and walking/streaming CPU/GPU frame pacing, shader/material counts, memory and obvious hitches on target hardware. Note measurements vs estimates.
4. Identify dependencies and any existing tests to extend. Do not touch the shaders or re-colour Oak.

**Done when:** repeatable baseline and exact migration map exist. No implementation claims based only on documentation.

### GS1 — New explicit Ground Stack Resource schema (DATA ONLY)

1. Add the minimal `BiomeGroundLayer`/`BiomeGroundStack` Resources with clearly visible Inspector groups: **Defining Base Ground** and **Organic Overlay Layers**. Define conservative ranges and enabled defaults; base is mandatory, overlays optional.
2. Wire Oak to existing `ForestGrass.tres` as its only active base. Wire Pink to existing `PinkMeadowGrass.tres` likewise. Preserve every current palette, texture, material and noise value. Overlay list starts empty or disabled. Don't force another resource file per layer; prefer inline entries.
3. Add schema validation and a compatibility bridge for old `Surface` consumers without making two long-term authorities. Never hardcode Oak or Pink into general code. Support a third arbitrary biome configuration in tests.
4. Avoid changing rendering while this schema lands; use the old rendering path or a behaviour-identical adapter.

**Done when:** Inspector authoring clearly identifies the base; Oak/Pink/preview/load checks still work and images match GS0.

### GS2 — Streamed renderer migration with pixel-level base parity

**Implemented; engine checks and target visual acceptance pending.** See [GS2/GS3 evidence](GS2_GS3_Validation.md).

1. Make `StreamedGroundPalette.Build` and related streamed ground code read **Base Ground** through the new schema. Keep the exact current palette-map values, 32x32 artwork atlas behaviour, pixel snapping, noise frequency, base shade/fresh/dry blending, 0.22 artwork influence and fine-detail computation.
2. Preserve shared world-owned material and per-chunk clipped quad ownership; no new tiles, scene hierarchy or per-frame update callback.
3. Don't accidentally convert the preview renderer when doing streamed work; either retain a fallback or intentionally verify that a new preview adapter reproduces the old appearance.
4. Expand deterministic/cross-chunk/reload/teardown/regression tests where useful and compare Oak/Pink fixed-seed visual captures.

**Done when:** with zero optional overlays, **Oak looks exactly as before**, Pink Meadow unchanged, streamed world/saves/preview functional. This is a mandatory user-accepted parity gate before aesthetic layering.

### GS3 — Up to two opt-in organic surface overlays

**Implemented; engine checks and target performance/visual acceptance pending.** Production layers remain OFF/empty; see [fixture and evidence](GS2_GS3_Validation.md).

1. Implement sequential overlay A/B blending on the existing streamed ground quad, each controlled by biome Resource fields: profile/type, enabled, coverage, opacity, patch size, warp, mask threshold and edge softness.
2. Use stable world-space noise and independent masks. Properly support distinct source artwork and surface types (Dirt/Stone/Sand/Grass) without a grass-only composition hardwired for every type. Keep generalized shader includes/reusable helpers.
3. Solve **different-art biome boundaries** through actual texture/coverage transition sampling, not only coarse palette RGB blending. Test atlas indexing, padding/filter behaviour, neighbour biome texture choice and cross-chunk seams. Avoid regressions to the existing shared-grass Oak/Pink boundary.
4. Default new overlay features to **OFF** in all existing production biome assets. Empty overlays must not change current visuals or add meaningful idle work.
5. Provide a small non-Oak test fixture (e.g., Sand base + Dirt overlay), not a new forced production biome.

**Done when:** Grass+Dirt+Rock can render organic, seam-free opt-in patches and turn fully off; old base output remains matched, with documented GPU cost.

### GS4 — F9 adds a third "Ground Surfaces" category

**Implemented; local engine/UI/Windows acceptance pending.** See [GS4 combined testing handoff](GS4_Validation.md). No earlier phase has been user-tested/accepted yet.

**F9 host retained from the pre-GS4 structure:** `DEBUG/UI/Environment/EnvironmentDebugPanel.cs` owns the removable HUD shell, `DEBUG/UI/DebugCategoryMenu.cs` provides `AddCategory` and exclusive categories, and `DEBUG/UI/Combat/CombatDebugPanel.cs` owns Combat controls. There are already **Environment** and **Combat** categories. The shell stays named `EnvironmentDebugPanel` for scene/path compatibility. Its right-side `WorldDiagnosticsPanel` appears only in Environment. F7 is separate map-only UI.

1. Add `DEBUG/UI/Ground/GroundSurfaceDebugPanel.cs` and local README, then register a **third** `_panel.AddCategory("Ground Surfaces")` inside existing F9 host. Do not add another HUD, global hotkey, scene-root panel or unrelated diagnostics window.
2. Ground drawer suggested sections:
   - **Current biome:** effective name/ID, current Base Ground label/profile.
   - **Base appearance:** toggle internal colour patches/variation (normally ON) and fine detail (normally ON) for A/B comparison. Optionally a base-only preview mode; do not remove the base entirely in normal world play.
   - **Extra overlays:** master **Show optional layers** toggle (default follows authored layer states); separate switches for Overlay A and Overlay B, labelled from Resource names such as Dirt patches/Rocky areas; display disabled/unconfigured slots clearly.
   - **Live preview:** safe controls for layer coverage/opacity and, when inexpensive, noise scale, softness and warp. Changes should render as the user moves sliders without per-frame world recalculation.
   - **Reset Ground Preview:** restore all runtime effective overrides to original authored state. Closing F9 should **not** reset comparisons automatically; unloading the scene should.
3. Ground F9 changes are **temporary runtime overrides** only. Never modify or save `Biome.tres`, `ForestGrass.tres` or the campaign, nor alter world generation IDs, collision or scenery. Avoid mutating shared Resource instances; keep override state world-scoped. The panel may be removed without affecting production systems.
4. Expose a narrow production controller/API to read labels/effective settings and apply override toggles. The live panel never owns rendering. If per-biome slider feedback would require a full coarse palette rebuild per mouse event, limit this initial phase to efficient **global visibility/strength** uniforms and keep biome-specific authored tuning in Inspector until a cached efficient override mechanism exists.
5. Match current UI standards: top-left exclusive scrollable categories, no movement-key focus, F9 visibility remembered, panel resize responsive, hidden controls do **not** poll. Active status max ~4Hz or event-driven; preserve Environment-only right diagnostics and F7 map.
6. Expand `TESTS/DebugUI/ValidateDebugHud.cs` for third-category exclusivity, F9 hide/show, independent overlay toggles, reset, no debug poll while hidden, no authored Resource mutation and Combat/Environment regression.

**Done when:** the author can switch each layer off/on and compare the old base live from F9, with existing F9 functions untouched.

### GS5 — Add *optional* Oak Dirt and Rock configurations (still OFF by default)

**PNG Dirt revision implemented; local engine/visual acceptance pending.** Dirt now uses imported `Dirt01_gs.png` at native 256×256, biome-owned tint and broad Dust patch placement. A follow-up makes dirt interiors opaque and the boundary narrower with interlocking noise breakup; coverage and F9 controls remain. Oak grass is unchanged; Rock remains Basalt. See [current authoring and checks](GS5_PNG_DIRT.md). The earlier discussion note below records the preceding decision.


**Layer configuration and F9 controls implemented; visual direction under revision.** Both overlays remain OFF by default. The author tested the procedural dirt/basalt version and found that both read as shadows rather than recognizable dirt and small rocks. See [GS5 test instructions](GS5_Validation.md) for the existing controls.

**Likely next approach — discuss in regular ChatGPT before further implementation:** use imported PNG textures (or similar raster artwork) for warm brown dirt and greyish gravel with visible little stones. Let the artwork supply recognizable material detail and the shader handle deterministic world-space patch placement and irregular blending into grass. Texture choice, resolution, repeat scale, tint and blending details are still to be decided; this note does not authorize another implementation pass. Retain the existing base/accompanying/optional-layer structure and independent F9 controls.

1. Create reusable production dirt/rock/gravel appearance profiles only where needed, instead of copying/debug-promoting all RockPatches implementation.
2. Give Oak an Overlay A with broad subtle soil/dirt patches; Overlay B with much sparser, smaller rocky breakup, inspired by the reference image. Choose modest coverage, softened non-repetitive edges, independent colour and strength. Keep Oak's existing base as-is.
3. **Do not enable the new layers by default without the author's visual approval.** Demonstrate them using F9 in disposable test sessions. Provide fixed-seed comparison captures of Base only / Dirt / Rock / Both. The user will select the desired permanent look and tune it later.
4. Keep Pink Meadow visually unchanged; it should not inherit Oak's overlay parameters. Don't introduce trails, trees, plants, shadow decals, flowers or lakes in this phase.

**Done when:** Oak with overlays OFF matches GS0; overlays ON introduce optional natural variation that the author can inspect/adjust independently.

### GS6 — Generalization, regression/performance QA and docs

1. Validate another surface family (e.g., Sand or bare Dirt base) using the exact same Resources/renderer; prove no biome-name-specific code or grass-only assumptions. Support future 3rd/4th biome without changing core logic.
2. Test deterministic fixed-seed results under chunk streaming, unload/reload, world negative coordinates, camera zoom, at least two biome boundaries, atlas art transitions and wet/snow/cloud/shadow/torch effects. Test preview fallback and campaign save/Continue (stable world identity and edits).
3. Profile baseline vs each overlay individually vs both, with start/idle/traversal/chunk boundary and lots of loaded chunks. Measure GPU/frame pacing when available, plus initial material/texture generation memory and CPU allocations. No FPS claims based solely on headless tests.
4. Correct/readjust docs: `Surfaces/README.md`, `WORLD/Terrain/Ground/README.md` and `WORLD/Terrain/README.md` (stale scaffolding wording), plus affected `WORLD/Biomes`, streaming, F9 and testing READMEs. Document add-a-biome and add-a-surface authoring without coding a new class.
5. Handoff each completed phase with touched files, single commit SHA, functionality, what was actually tested, what was deferred to local Copilot/Windows validation, side effects, comparison screenshots and remaining risks. Do not silently ship another phase.

**Done when:** parity signed off, overlays reusable, UI and saves safe, documented and performance measured on target hardware.

## 6. Acceptance matrix

| Test | Requirement |
| --- | --- |
| **Oak look** | Before optional approval, all-off mode visually identical to old Oak at same seed/camera/clock/weather/renderer. |
| **Pink look** | Existing palette/texture detail and transitions unchanged. |
| **Inspector** | Every biome clearly selects a **Defining Base Ground** and an ordered, optional overlay array. Existing .tres paths remain valid through migration. |
| **Layer toggles** | Switch optional dirt/rock separately, master show/hide, base internal variation/detail comparison and reset without changing saved assets. |
| **Noise and seams** | World-space sampling continuous over chunk bounds; deterministic reload; distorted organic edges; no hard per-cell art/material boundary. |
| **Performance** | No per-tile nodes or ground per-frame CPU polling; shared materials/noise; bounded two-layer cost; disabled slots skip unnecessary work; measured impact recorded. |
| **F9** | Environment, Combat, Ground Surfaces are exclusive in one drawer; hidden panels sleep; F7 untouched; diagnostics remain Environment-only; no arrow-key focus. |
| **Isolation** | Ground changes don't mutate collision, scenery IDs, saves, atmosphere/weather/snow, other biome Resources or campaign state. |
| **Generality** | Grass and non-grass base/overlays all work through shared data contracts; no per-biome C# branches. |

## 7. Deliberately out of scope

Procedurally meaningful roads/trails, settlements, author-painted terrain masks, cliffs/elevation, shoreline/water transitions, leaf/twig/flower/stone object scattering, migrating DEBUG rock-base gravel, seasons and unlimited shader layer counts. They may compose with the ground system in later phases but are not prerequisites for the Oak baseline extraction.

## 8. First Codex handoff prompt

"Read root README.md and CodePreferences.md, then `WORLD/Terrain/Ground/Surfaces/LAYERED_GROUND_IMPLEMENTATION.md` and its referenced actual source/resources. **Implement GS0 only**: audit, fixed-seed ground appearance/performance baseline and necessary validation groundwork. Do not change Oak's appearance, add shaders/layers or implement GS1 onward. Respect one commit per approved job; report what was measured vs unverified and what the user should compare locally."

The author will explicitly request GS1, GS2 etc. **No later phase is automatically authorized.**
