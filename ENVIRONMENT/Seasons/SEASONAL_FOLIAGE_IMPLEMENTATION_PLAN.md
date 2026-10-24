# SE0–SE4: Seasonal foliage and F9 year-position preview

> **STATUS: SE0 and SE1 implemented; SE1 software-GL checks passed. Godot build/visual acceptance pending. SE2/SE3 remain open.**
>
> This is a staged Codex handoff for real-time **Spring → Summer → Autumn → Winter** foliage previews. It is **not** approval to implement every phase at once. Start with SE0, request user acceptance, then continue one small phase at a time.
>
> **Important user decision:** The external **Avalonia Sprite Studio** now exports foliage masks; Oak already has `Sprites/tree_01_fm.png`. Godot must offer **optional foliage-mask input** through existing tree Resources and an unmasked fallback; Codex must **not** build the Avalonia app or require masks to exist now.

## Read existing code and ownership first

- Root README.md and CodePreferences.md (modularity, inspector Resources, comments, low idle cost, one commit per job).
- [EnvironmentRuntime.cs](../EnvironmentRuntime.cs) creates one shared wind, day/night, rain, snow and visual adapters, and attaches/detaches them for P3 chunks.
- [GameClock.cs](../Time/Clock/GameClock.cs) owns **hour of day only**; [Time/Calendar](../Time/Calendar/README.md) remains a placeholder. **P4 campaign saves work**, but they do not yet save a season/year.
- [F9 EnvironmentDebugPanel.cs](../../DEBUG/UI/Environment/EnvironmentDebugPanel.cs) already has collapsible Time, Weather, Lighting and Snow sections. Extend its **existing Environment drawer**, not a new debug hotkey/window.
- [TreeDefinition](../../WORLD/Content/Vegetation/Trees/Shared/TreeDefinition.cs) + [TreeVisualVariant](../../WORLD/Content/Vegetation/Trees/Shared/TreeVisualVariant.cs) configure one generic [Tree.tscn](../../WORLD/Content/Vegetation/Trees/Shared/Tree.tscn). Oak and Pink Blossom are species data. Pine is artwork-only and should not start spawning.
- [VegetationSway.gdshader](../Wind/Visuals/VegetationSway.gdshader) samples the original texture with a wind-warped **sample_uv**. [Wetness](../../WORLD/Content/Vegetation/Shared/Wetness/README.md), [snow](../../WORLD/Content/Vegetation/Shared/Snow/VegetationSnowAdapter.cs) and [fire erosion](../../WORLD/Content/Vegetation/Trees/Shared/Fire/FireErosionAdapter.cs) extend or swap its live material. **Do not overwrite them.**
- P3 creates/releases chunk visuals; P4 restores supported player/clock/tree state via campaign saves. The season preview must not introduce per-tree frame loops, unbounded shader caches or an unapproved save-schema change.

## Proposed folder ownership (do not create unused placeholders)

    ENVIRONMENT/Seasons/
      README.md                                 already exists
      SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md  this plan
      SeasonController.cs                      SE0: single production season state
      SeasonMasterConfig.cs                    SE0: Inspector authoring/defaults
      Definitions/                             existing, reserve for later calendar/climate

    DEBUG/UI/Environment/
      EnvironmentDebugPanel.cs                 SE0: add existing F9 controls

    WORLD/Content/Vegetation/Shared/Seasonal/
      README.md                                SE1
      SeasonalFoliageProfile.cs                SE1: species palette and optional influence
      SeasonalFoliageAdapter.cs                SE1/SE2: shared materials/lifecycle
      SeasonalFoliage.gdshaderinc              SE1: colour/pixel selection

    WORLD/Content/Vegetation/Trees/Shared/
      TreeDefinition.cs                        SE1: optional seasonal profile
      TreeVisualVariant.cs                     SE1: optional FoliageMask Texture2D
      Tree.cs                                  only if variant metadata needs wiring

    WORLD/Content/Vegetation/Trees/Oak/
      Oak.tres                                 SE1: palette opt-in
      Sprites/tree_01_fm.png                    existing Avalonia mask, bind in SE1

The global **season state belongs to ENVIRONMENT**; generic foliage visual logic belongs to **WORLD/Content/Vegetation/Shared/Seasonal**; each species owns palette and leaf-mask references; DEBUG owns removable F9 controls. Do **not** make a separate season copy of a tree scene/PNG, a seasonal biome class, or a per-tree seasonal processor.

## F9 interaction: continuous time of year

Use a **Year position** slider with one normalized cyclic value 0.0–1.0. The screen label follows the nearest seasonal quarter, but visual palettes interpolate smoothly between authored key points:

| Slider | Season label | Preview expectation |
| --- | --- | --- |
| 0.00 | Spring | Fresh/light green |
| 0.25 | Summer | **Original untouched lush Oak** |
| 0.50 | Autumn | Leaves becoming golden/yellow/orange/red |
| 0.75 | Winter | Species-defined dormant colour; existing snow still controls snow |
| 1.00 | Spring | Same appearance as 0.00 (seamless wrap) |

Make more than four palette stops possible (e.g. early yellow → deep orange → red → brown). Use a compact inspector-editable Godot Gradient/Resource, not hardcoded per-species RGB branches. Preserve underlying leaf shading and highlight variation rather than painting the entire crown a solid colour.

In F9 → Environment, insert a **collapsible Seasonal foliage section after Time**, collapsed initially. Add:
- **Enable seasonal foliage** toggle, **OFF by default** (the live world initially looks exactly as it does today).
- **Year position** slider, 0–1, step 0.01 or finer; drag for real-time transitions with no Apply/reload.
- Live **Spring/Summer/Autumn/Winter** label. An intensity slider is optional only if genuinely useful; don't clutter F9 with duplicate sliders.

These are **manual visual preview controls**. Do not implement a second GameClock or make the current 30-second day equal an entire year. The F9 panel calls the production controller; the production controller must run when F9 is absent.

## SE0 — season state and F9 slider ONLY

**Implement first. No tree recolouring in this phase.**

1. Introduce one production SeasonController from EnvironmentRuntime and one small SeasonMasterConfig Resource (or reuse existing world config only when clearly more suitable). Add an optional typed Seasons reference to **EnvironmentMasterConfig** and its default .tres so the controller has a clear production-owned Inspector configuration. It exposes Enabled, YearPosition, readable season name and an on-change signal/event.
2. Constructor/defaults and F9 edits must operate on **world-owned runtime settings**, not mutate authored .tres. Default Enabled=false, YearPosition=0.25/Summer.
3. Add toggle, slider and text to the existing F9 Environment drawer; allow scrubbing even if GameClock is paused. No always-running season _Process needed for manual state.
4. Do not change weather, snow, solar clock, calendar, seasons for creatures, P3 generator/chunks, P4 save schema, sprite shader or tree definitions.
5. **Acceptance:** F6 loads, F9 displays season labels as slider moves, disable/enable correctly updates state, optional F9 panel removal does not break the world, no new idle cost and existing controls are unaffected.

**Stop and ask for acceptance of F9 controls before SE1.**

## SE1 — masked Oak recolour with existing no-mask fallback, data-driven species

1. Add SeasonalFoliageProfile Resource and one reusable seasonal foliage shader/adapter, shared across existing Tree.tscn instances. Add an optional seasonal profile to TreeDefinition and optional **FoliageMask Texture2D to TreeVisualVariant**.
2. Bind Oak's existing `Sprites/tree_01_fm.png` explicitly on its visual variant. Both this mask and `tree_01.png` are 222×295; the mask is opaque grayscale. Bring the core SE3 import contract and cached dimension validation into SE1. Read its red channel as foliage weight, preserve source alpha, and sample with the artwork's same warped `sample_uv`. An explicit valid mask **replaces** colour detection and the generic crown cutoff: do not multiply those heuristics back into it, which would reject deliberately selected dark/lower leaves. Keep healthy artwork and physical footprint unchanged.
   - **No mask:** retain current green/pink colour and crown detection for rain/snow, including their existing tuning. Seasonal recolouring can use the same original-colour fallback, preserving its known imperfect coverage. Masks are optional and can be added gradually.
   - **Invalid mask:** issue a bounded diagnostic and fall back safely, never sample mismatched dimensions silently.
   - Masks are scalar data, not display colour: sample red without `source_color` conversion, disable repeat/mipmaps, use filtering consistent with the pixel artwork, and validate greys/edges in Godot. Never use mask alpha as foliage weight (this export is fully opaque).
   - One mask per distinct artwork, shared by all instances and seasons; no runtime filename discovery, duplicate nodes or separate seasonal PNGs.
3. Recolour using a hue/palette operation that preserves each pixel's brightness/texture/shadows and carries variation across leaf clusters. Summer matches the current source image; autumn progresses through yellows/oranges/reds without abruptly flipping an entire tree.
4. Opt in Oak through its .tres; Pink Blossom should **stay pink/unchanged by default** unless a separate palette is authored later. Other trees/evergreens can opt out with no extra species scripts.
5. Update **only cached shared material groups on season change**, not per tree every frame. Shader/material variants may be keyed by source material, selected sprite/mask, flip and species palette as necessary. A slider movement must **not recompile shaders**, regenerate chunks, resample sprite images on CPU or mutate source Resources.
6. **Acceptance:** green Summer Oak looks the same; Autumn oak leaves shift while trunk/root remain normal; original collision/wind alignment and mirrored variants remain intact; pink blossoms don't turn orange.

**Masks remain optional, but masked Oak is the primary SE1 acceptance fixture.** Check both its dark outer leaves and unmasked fallback. Establish shared foliage selection for seasons/rain/snow in SE1 so SE2 validates and completes integration rather than repairing an incompatible recolour path.

## SE2 — shader composition + P3/P4 compatibility and performance

**Important:** Existing wetness/snow masks classify green/pink **pixel colours**. After turning green leaves orange/red, snow/wetness must **not lose foliage detection**. Codex must compute leaf identity from the author-provided mask when valid, otherwise the untouched original texture, and use that selection through the visual stack. Preserve each weather effect's existing no-mask fallback. Separate leaf identity from snow deposition noise/brightness so a selected leaf need not become uniformly white. A final whole-sprite tint or blindly appending a shader snippet can break existing systems.

Implemented SE1 visible order: original texture sampled after wind → preserve original foliage identity → seasonal palette → wetness → snow (using original foliage identity) → optional active FIRE2 erosion/charred effects. Adapt the real current material/shader owners rather than building an unrelated replacement tree shader.

- **Wind:** mask texture and healthy texture sample the **same wind-adjusted sample_uv**, respecting source alpha, mirror, scale, sprite offset and source anchor. Never use world-space or screen-space UVs for the mask.
- **Wetness/snow:** preserve live settings and material follower links; orange/red foliage still becomes darker in rain and white when covered by snow. The user intends rain and snow to normally occur separately later; current manually enabled combinations must not corrupt the shader.
- **Fire:** a charred/burnt tree must not regain colourful leaves, even if the slider moves; preserve healthy-reset, burnt texture, FIRE2 GPU canopy effects and any charred no-art fallback.
- **Lighting/visibility:** shadows, native torch occluders, tree fade/occlusion, Y sorting and rooted collision must not change. No geometry change is needed for colour tint.
- **Streaming:** new chunks immediately inherit the current season at publication; unloading unregisters owned materials/links and frees references. Offscreen cosmetic suspension/wakeup must continue working. Avoid a global "update all tree sprites" pass on every slider tick.
- **P4:** existing saved campaign New/Continue and supported burnt/removal states remain compatible. F9 year-position preview is **not serialized yet**; do not change P4 schema/version as a side effect.
- **Performance:** idle season system should have no per-frame work when unchanged/disabled. Shared shader/material count scales with unique source variants, not number of loaded trees; no Texture2D.GetImage in draw/update loops, no per-leaf nodes, no unbounded world-scoped static cache. Check FPS, frame spikes, cache growth and cleanup during movement/scrubbing.

**Acceptance:** F6 P3 walk across chunks and back, reload when season=Autumn (no green flash), snow/rain toggles, wind -3/0/+3, fire ignite/burn/char/reset, mirrored trees, P4 Continue, optional F9 removal. Run C# compile and Godot runtime tests; report visual/performance checks separately from headless results.

## SE3 — mask authoring validation and regression coverage

**The external Avalonia tool and first Oak mask already exist. Core binding/format validation belongs in SE1; SE3 broadens validation and documents the authoring workflow. Do not build or change the editor.**

Contract for each TreeVisualVariant's optional FoliageMask:
- Grayscale **PNG exactly the same source pixel width and height as the healthy PNG**, preserving the same canvas origin and any source-image transparent-padding geometry; **NOT** the scaled display size in the game. Prefer an opaque grayscale mask (black in the transparent-padding area) so brightness in the specified channel, not imported alpha, determines foliage weight.
- Agree on one sampled channel (recommend **red**): **white/1.0 = foliage**, **black/0.0 = trunk/branch/root/background**, greys = fractional/soft-edge foliage. Multiply by the original sprite alpha. The Studio and Godot importer must use the same convention for mask alpha vs channel brightness.
- Sample the mask at the **same warped sample_uv** as the healthy sprite texture. Sprite mirroring and scaling should need no separate mask node, material transform, or reanchoring.
- Existing location: WORLD/Content/Vegetation/Trees/Oak/Sprites/tree_01_fm.png; keep masks beside their source sprites and reference it in TreeVisualVariant.FoliageMask, never as a biome-specific texture.
- Missing mask → safe **SE1 foliage-colour heuristic fallback** (no loading error). Wrong dimensions/missing import/invalid reference → validation error or bounded warning, not silently offset leaves. Cache texture/size checks per variant; never scan or read back every tree each frame.
- The Avalonia editor can eventually **select green pixels, paint/erase the selected leaf region and export the mask and .tres reference**. The healthy source PNG is never destructively changed; a new tree still uses the same Shared/Tree.tscn.
- **Do not confuse the existing FIRE2 CanopyFireEmissionMask with this mask:** it uses sprite alpha to place flame particles, and does **not** reliably distinguish leaves from brown branches.

Use the existing Avalonia Oak export to prove mask alignment, dark-leaf inclusion and bark isolation; do not replace it with a temporary mask. Test resized/mirrored/offset trees, antialiased leaf gaps and invalid/missing masks.

## SE4 — real year/calendar + weather seasons / leaf shedding (DEFERRED)

**Do not implement in SE0–SE3. Obtain explicit approval.**

- Extend the **single** ENVIRONMENT/Time/Calendar to actual days/months/year progress. Feed that value into SeasonController, retaining F9 as a temporary manual preview override.
- When year/season must persist, add a **versioned, backwards-compatible P4 save-schema change**, restore before chunks publish to avoid wrong-season flashes. F9 visual preview need not persist.
- Season-dependent rain/snow tendencies (user preference: **not simultaneously**), temperatures, daylight length, growth/harvest, wildlife routines and falling leaves belong to separately approved systems; keep data-driven biome/species distinctions.
- A shader can hide selected foliage alpha but **cannot reveal branches that weren't painted underneath**. A convincing bare winter deciduous tree may still need a separate authored bare/charred-style sprite; do not promise perfect winter bark by only recolouring a lush PNG.
- Optional individual tree variation should use a few **stable shared palette buckets** derived from stable IDs, not one unique active material per tree.

## Copy/paste: first Codex task

> Read root README.md, CodePreferences.md, ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md, ENVIRONMENT/EnvironmentRuntime.cs, DEBUG/UI/Environment/EnvironmentDebugPanel.cs, ENVIRONMENT/Time/Clock/GameClock.cs, and the vegetation wind/wetness/snow/fire implementation. **Implement SE0 only:** one production season-preview state plus small inspector SeasonMasterConfig and a collapsible **Seasonal foliage** section in the existing F9 Environment drawer (Enable default off, continuous Year position 0..1 slider, live Spring/Summer/Autumn/Winter label). No tree tint yet, no real calendar/automatic time-of-year simulation, no P4 saving/schema changes, no per-tree Process, no new F-key, no unrelated scene/folder refactor. Preserve production P3 and running campaigns. Update the owning README and phase table, report precise testing/deferred checks and commit once. **The Avalonia Oak mask already exists; bind it in SE1, preserving unmasked detection. Do not modify the external tool or extend fire effects.**

## Phase status (update only after testing/implementation)

| Phase | Current status | Acceptance |
| --- | --- | --- |
| SE0: F9 year-position state/preview | IMPLEMENTED; static review only | Local build / F9 acceptance pending |
| SE1: masked Oak shader + existing maskless fallback | IMPLEMENTED; software-GL checks passed | Godot build / Windows acceptance pending |
| SE2: wet/snow/fire/streaming/performance integration | SE1 composition foundations present; broader checks and save bridge pending | Pending |
| SE3: mask authoring validation and regression coverage | NOT IMPLEMENTED | Pending |
| SE4: calendar/P4 integration and gameplay seasons | DEFERRED | Separate future approval |

## SE0 implementation handoff

See [SE0_Validation.md](SE0_Validation.md). No leaf recolouring, mask binding, weather/fire changes or save-schema changes are implemented by SE0. Continue with SE1 after the F9 controls are accepted.

## SE1 implementation handoff

See [SE1_Validation.md](SE1_Validation.md). Oak mask/profile binding and shared weather selection are implemented; software-GL checks passed. Native Godot validation and SE2 campaign compatibility remain pending. SE1 updates Oak.tres, so the strict fingerprint can reject older campaigns; use F6/new campaign until an explicit SE2 migration is validated. No new fire effect was added.
