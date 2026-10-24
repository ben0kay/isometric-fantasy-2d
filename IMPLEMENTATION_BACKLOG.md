# Project Implementation Notes Audit & Backlog
**Audit date:** 2026-10-24  
**Scope:** Read-only audit of reachable, named project plans and owning READMEs on GitHub default branch. **Only this backlog file was created.** No existing project files were edited.  
**Important limitation:** GitHub file search returned no indexed results and the available connector did not provide a recursive repository file listing. Therefore this is a **substantial targeted audit, not a verified exhaustive inventory of every Markdown file**. Do not interpret an unlisted plan as completed or absent. Existing source code was not exhaustively cross-checked; statuses below are grounded in the referenced plans/READMEs, not fresh runtime testing.

## Legend
- **Implemented / validate:** Documentation reports code present, but local Windows/visual/performance acceptance remains open.
- **Planned:** Documentation explicitly describes work as not yet implemented.
- **Docs drift:** Older wording is contradicted by a newer plan/README and needs reconciliation, **not** reimplementation.
- **Review:** Ambiguous, historical, or needs source-code verification before changing status.

## 1. Documentation drift / likely already implemented

| Priority | Area / source | Finding | Suggested action | Suggested assistant |
| --- | --- | --- | --- | --- |
| High | `COMBAT/COMBAT_IMPLEMENTATION.md` vs `COMBAT/C3_Implementation.md` | Combat umbrella still describes impact particles as future work in its baseline text; C3.3a–d are marked implemented in C3 plan and `COMBAT/README.md` documents material feedback. | Refresh umbrella's current-state prose; retain C3.4/C3.5 as planned. | Regular Chat |
| High | `COMBAT/Projectiles/README.md` vs `COMBAT/Projectiles/RANGED_IMPLEMENTATION.md` | Projectile README title says 'R0 foundation' and introduction implies R1 adds archer; ranged plan says R1 **implemented** and R1 validation exists. | Update README headline/status to R0–R1 implemented; R2 planned. | Regular Chat |
| High | `WORLD/Terrain/Ground/Surfaces/README.md` | It says 'Current Dirt/Rock use procedural Dust/Basalt' immediately before newer text stating Dirt uses 256×256 grayscale PNG. | Replace contradictory 'current' wording with a dated history and point to `GS5_PNG_DIRT.md`. Rock remains Basalt. | Regular Chat |
| High | `WORLD/Terrain/Ground/Surfaces/GS5_Validation.md` | Opens by describing retired Dirt/Rock SVG artwork, though later warns it was superseded by Dust/Basalt; Dirt was subsequently superseded again by PNG. | Put current PNG Dirt/Basalt Rock status first; mark SVG and Dust Dirt descriptions historical. Keep original validation evidence. | Regular Chat |
| Medium | `WORLD/Terrain/Ground/Surfaces/PROCEDURAL_GROUND_RECIPES.md` | Early body still says Dirt/Rock profiles have no texture; top banner correctly says Dirt superseded by PNG. | Label texture-free Dust Dirt section historical; keep Basalt Rock current. | Regular Chat |
| High | `WORLD/Generation_Implementation.md` | ~67k-character document retains superseded 'Continue disabled', pre-P3 world layout, and pre-P4 save guidance despite top status P0–P4 implemented. Some historical passages are explicitly marked, but risk confusing future agents. | Split/archive old snapshots or add strong current-state navigation; do **not** treat historical instructions as open work. | Regular Chat |
| Medium | `SCENES/World/scene organize.md` | Old `Actors` mixed hierarchy is shown in detail, but document says it is a historical SO0 snapshot; SO2 ownership is already implemented. | Keep old tree only under clearly marked history, elevate actual SO2 tree and open SO3/SO4. | Regular Chat |
| Medium | `ENVIRONMENT/Seasons/README.md` | Earlier SE0–SE4 summary says SE2 completes integration; later paragraph confirms SE2 implemented. | Put latest SE2 status and outstanding validation in opening summary; distinguish SE3/SE4. | Regular Chat |
| Medium | `COMBAT/C3_Implementation.md` | C3.1 says code added but build/visual tests pending; C3.3 implemented. | Verify latest C3.1 validation before promoting to fully accepted. Update any old 'immediate handoff' wording that still reads like an unstarted task. | Regular Chat + Copilot |
| Medium | `ENVIRONMENT/README.md` | Contains an 'early startup-only validation' historical note and states seasons are future work in a general environment paragraph, while `ENVIRONMENT/Seasons/README.md` confirms manual seasonal preview implemented. | Clarify that **automatic calendar/biome climate** are future; manual seasonal foliage already exists. | Regular Chat |
| Medium | `SCENES/World/README.md` and `WORLD/Runtime/Streaming/README.md` | Some older descriptions refer to single skeleton; world README now says eight disposable F6 practice actors including three archers. | Ensure current practice-actor count is used in summaries; don't confuse persistent campaign skeleton with F6 debug actors. | Regular Chat |

## 2. Confirmed planned / not finished according to current documents

| Priority | Phase / area | Remaining scope | Evidence / dependency | Suggested assistant |
| --- | --- | --- | --- | --- |
| High | **C3.4** combat feedback | Correct distinct feedback for blocked hits, guard outcomes, misses and possible guard-break/parry if actually supported. | `COMBAT/C3_Implementation.md`; depends on authoritative defense results. | Codex |
| Medium | **C3.5** combat feedback | Optional bounded camera shake and audio for attacks/impacts. | `COMBAT/C3_Implementation.md`. | Codex; Regular Chat can plan sound/config |
| Medium | **C4** combat movement feel | Refine existing sprint/dodge, acceleration, attack movement, buffering and knockback integration; do not build duplicate movement system. | `COMBAT/COMBAT_IMPLEMENTATION.md`. | Codex |
| Medium | **C5** combat encounter | Tune repeatable full skeleton encounter, telegraphs, hit fairness and playtesting. | Same combat plan. | Codex + user acceptance |
| Medium | **ES2** entity separation | More complete species/personality-specific Resource profiles/presets; ES1 already has basic profile support, so **audit the delta before coding**. | `ENTITIES/Shared/Movement/EntitySeparationPlan.md` and Movement README. | Regular Chat audit, Codex integration |
| High | **ES4** entity separation | Crowd, narrow-space, lifecycle, combat and target-PC profiling regression/acceptance. | EntitySeparationPlan; ES1 and ES3 already implemented. | Copilot + user; Codex fixes |
| Medium | **R2** ranged combat | Mixed encounter refinement, bow/arrow feedback and practice/test handoff; **first inspect current F6 practice** because eight actors (including three archers) are already documented. | `COMBAT/Projectiles/RANGED_IMPLEMENTATION.md`, `SCENES/World/README.md`. | Regular Chat scope audit, Codex |
| Medium | **SE3** seasons | Broader foliage-mask validation and species expansion. | `ENVIRONMENT/Seasons/SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md`. | Regular Chat planning; Codex implementation |
| Later | **SE4** seasons | Actual calendar/gameplay/save integration; manual preview is not a persistent year simulation. | Same plan; `ENVIRONMENT/Seasons/README.md`. | Codex |
| High | **P5** world generation | Actual terrain/geography, priority locations, paths and cave connections. Existing P2 reserves features only; P3 terrain is flat. | `WORLD/Generation_Implementation.md`. | Regular Chat design review, Codex |
| Later | **P6** world generation | Abstract off-screen A-Life, not full distant physics. | Same plan; requires world/save design decisions. | Regular Chat architecture, Codex |
| Medium | **SO3/SO4** scene organization | Proposed later ownership/cleanup stages; inspect exact stage text and current scene before deciding scope. | `SCENES/World/scene organize.md`. | Regular Chat review, Codex |
| Later | **GL0–GL3** location/session architecture | Proposed general overworld/cave/interior location architecture; not authorized by the scene plan. | Same scene organization document. | Regular Chat design, Codex |
| Medium | **GS6 / ground QA** | Ground performance profiling/general QA and acceptance; not a request to rewrite GS5. | `GS5_Validation.md`, surfaces README. | Copilot + user |
| Later | **Save enhancements** | Inventory/equipment, caves, seasons, weather progression, A-Life state, multiple-run selection UI, permadeath cleanup, blob GC and general content migrations remain outside P4. | `SAVE/README.md`; these are **capabilities**, not necessarily approved phases. | Regular Chat planning |
| Later | **Content tooling** | External Avalonia content authoring, registry/discovery/validation and content packs not implemented. | Root `README.md` architecture section. | Regular Chat architecture, Codex |
| Later | **Environment** | Biome-region climate/environment blending, real calendar, climate simulation, fire spread and lightning ignition remain future work. | Root README and `ENVIRONMENT/README.md`; separate from implemented manual season preview. | Regular Chat planning, Codex |

## 3. Implemented but awaiting validation / user acceptance

| Area | What documents report | Remaining evidence to gather |
| --- | --- | --- |
| **P3 streaming** | Implemented, including fixes for grass-only spawn and flashing giant shadows. | Target-PC frame pacing, startup/streaming hitches, memory, scenery visibility and seams; a soft 3ms apply budget is not a whole-frame guarantee. |
| **P4 campaigns** | Original save/profile flows implemented, automated tested and user accepted. | Large-save profiling and later SO2/C1 visual/UI regression; do not reopen accepted original scope without a regression. |
| **SO2 ownership** | Automated scene/save ownership checks passed. | Windows Y-sort/seams and performance acceptance. |
| **C2/C2.5/C3.2** | Shared combat and knockback/stagger implemented. | Remaining visual feel/target-PC checks; known broader native shutdown failures should remain documented. |
| **C3.1** | Damage-number code present. | Confirm latest build/visual evidence; not equivalent to a planned blank module. |
| **C3.3a–d** | Particle and material hit feedback implemented. | Windows visual/performance acceptance. |
| **ES1/ES3** | Separation and engagement positioning implemented with F9 controls. | Five-plus enemy crowd feel, streaming, profiling and ES4 regression. |
| **R0/R1** | Shared projectiles and Skeleton Archer implemented; world F6 practice includes archers. | Review R1 validation and target-PC projectile/AI behavior; R2 remains. |
| **SE1b/SE2** | Manual date-based seasonal foliage, per-tree timing variation, save fingerprint bridge and charred-art fallback implemented. | Native/local/visual acceptance as documented. |
| **GS2–GS5** | Ground stack, overlays, F9 controls and grayscale PNG Dirt implemented. | Godot/.NET fixtures, Windows rendering, campaign Continue and GPU/frame-time checks. |

## 4. Potential duplicate work / architectural caution

1. **ES2:** A `SeparationProfile` and `EntityDefinition.Separation` already exist in the ES1 README. Do not implement ES2 verbatim without determining what genuinely remains (presets/expanded knobs/fixtures).
2. **R2:** The world README already documents three archers plus five melee skeletons in disposable F6 practice. Don't add a second mixed practice setup without checking whether only polish/testing remains.
3. **Ground surfaces:** Do not restore obsolete SVG/Dust Dirt simply because old GS5 notes describe it; current Dirt is 256×256 grayscale PNG and Rock remains Basalt.
4. **Combat:** Do not recreate impact particle systems or shared health bars as part of C3; C3.3 and existing health bars are already documented.
5. **Seasons:** Manual preview exists, but automatic calendar/persistence does not; avoid calling the entire seasons roadmap either unimplemented or finished.
6. **World generation:** P2 feature **reservations** are not P5 physical roads/caves/geography; P3 streaming is not P6 A-Life.

## 5. Additional audit coverage needed (not verified here)

The audit could not enumerate all repository Markdown files, so the following areas **must be inventoried separately** before claiming a full-project audit: `Notes/` (including Ideas and non-archived plans), `DEBUG/`, `WORLD/Content/`, `WORLD/Terrain/` beyond the inspected ground-surface files, `ENVIRONMENT/` lighting/fire/weather plans, `ENTITIES/` actor collision and species plans, `UI/`, `ITEMS/`, `SYSTEMS/`, `FEEDBACK/`, `AUDIO/`, `CORE/`, `TESTS/`, and other feature folders. Archive paths should be indexed as **historical** rather than treated as active backlog by default.

**Next audit method:** Obtain a recursive repository file inventory (for example `git ls-files '*.md'` in a local clone), inspect every plan's status/phase table and associated README/validation notes, then check actual code/resource existence for disputed claims. Append findings to this backlog only after verification. Never mark a phase complete solely because a similarly named file exists.

## 6. Suggested order for future work

1. **Regular Chat documentation cleanup:** reconcile the high-confidence drift items in section 1 **only after explicit approval**; no existing files were edited by this audit.
2. **Copilot/user validation:** prioritize GS5, SE2, ES1/ES3, R1, P3 streaming and SO2/C1 visual/performance checks.
3. **Regular Chat phase refinement:** audit ES2 vs existing profiles, R2 vs existing mixed practice, then discuss C3.4/C3.5 and P5 before assigning Codex.
4. **Codex implementation:** only explicitly approved phases, one commit/job; update owning notes and record validation.

**Status integrity:** 'Implemented' in this backlog means the inspected project documents say implementation exists; it is **not** a claim that this audit built, executed or profiled the game.
