# Implementation Documentation Audit & Backlog
**Re-audit:** 2026-10-24 · **Action:** documentation inventory and discrepancy review only · **Only this file changed**

## Read this first: evidence and limits
This is a **second, deeper audit** of reachable plans, module READMEs, validation notes, and selected implementation source files on the repository default branch. It separates **(A) documentation defects**, **(B) explicitly planned implementation**, **(C) code present but validation incomplete**, and **(D) design ideas/scaffolding that must NOT be treated as authorized tasks**.

**Coverage limitation — do not call this an exhaustive repo audit:** The GitHub connector exposes file-by-path and code search, but code search returned no results for broad terms and there was no recursive tree/list operation available. Local `git clone` was blocked by network DNS. Consequently, a complete `git ls-files '*.md'` inventory and comparison of every Markdown file to all live source files was **not possible**. The file paths listed in the coverage register below were actually opened; remaining directories must be enumerated in a local checkout before a '100% audited' claim. A checked source file's existence confirms implementation *material*, not working behavior.

### Confidence levels
- **Verified contradiction:** Both relevant current documents read, or source file presence verified.
- **Reported implemented:** Owner plan/validation says implemented, but not fully source/runtime verified here.
- **Reported planned:** Plan explicitly says future/unimplemented; no claim that every code path was searched.
- **Requires reconciliation:** Statements are inconsistent, dated, or scope-dependent; investigate before editing.
- **Acceptance outstanding:** Headless/source checks or implementation do not establish Windows visuals, gameplay feel, performance, or campaign restoration.

## A. Documentation corrections — high confidence (NO reimplementation)

| ID | Priority | Exact files | Finding / evidence | Action if later approved | Best assistant |
| --- | --- | --- | --- | --- | --- |
| D01 | HIGH | `COMBAT/README.md`, `COMBAT/Projectiles/README.md`, `COMBAT/Projectiles/RANGED_IMPLEMENTATION.md` | Combat overview and projectile README still say archer is planned / 'R0 foundation', whereas RANGED plan and `R1_Validation.md` explicitly say **R1 implemented**. `ENTITIES/Shared/AI/RangedCombatAI.cs` and `COMBAT/Projectiles/Projectile.cs` exist. | Change top-level summaries to R0/R1 implemented, R2 remaining; retain R1 acceptance caveats. | Regular Chat |
| D02 | HIGH | `ENTITIES/Species/README.md`, `ENTITIES/Species/Undead/README.md`, `ENTITIES/Species/Undead/Skeletons/README.md` | Species overview says all child folders are placeholders; Undead says category scaffolding only; Skeletons README confirms Basic production Resource/shared Entity. R1 docs confirm Archer. | Mark Skeleton Basic + Archer as implemented exceptions, other species placeholders. Update obsolete sample 'Skeleton.tscn' tree to actual shared `Entity.tscn` architecture. | Regular Chat |
| D03 | HIGH | `COMBAT/Projectiles/R1_Validation.md`, `SCENES/World/README.md` | R1 validation's historical statement 'Default World still uses its original five Basic melee skeletons' conflicts with newer World README: **eight disposable F6 skeletons (five Basic, three Archer)**. | Date/label original R1 snapshot and point to current F6 actor setup; preserve R1 standalone ArcherPractice fixture. | Regular Chat |
| D04 | HIGH | `WORLD/Terrain/Ground/Surfaces/README.md`, `GS5_Validation.md`, `PROCEDURAL_GROUND_RECIPES.md`, `GS5_PNG_DIRT.md` | Surface README has adjacent contradictory 'Current Dirt/Rock use Dust/Basalt' and 'Current Dirt uses grayscale PNG'. GS5 validation starts with retired SVG description; procedural note still discusses texture-free Dirt after supersession. Newest GS5 note says **256×256 grayscale PNG Dirt, Basalt Rock**, organic masks preserved. | Put latest status at top; retain SVG→Dust→PNG history under clearly labeled superseded sections. Never restore retired art as a 'missing feature'. | Regular Chat |
| D05 | HIGH | `COMBAT/COMBAT_IMPLEMENTATION.md`, `COMBAT/C3_Implementation.md`, `FEEDBACK/README.md` | Combat umbrella still refers to impact particles as future, while C3 plan reports **C3.3a–d implemented**, FEEDBACK documents particle slots, tree leaves, bone/flesh impacts. | Refresh umbrella baseline and C3 status; C3.4/C3.5 remain planned. | Regular Chat |
| D06 | HIGH | `WORLD/Liquids/README.md`, `SCENES/World/README.md`, `WORLD/Water/README.md` | Liquids README opens with 'Production pond integrated' and then says 'documentation/folder foundation only; no liquid implementation copied from DEBUG; World unchanged'. World README confirms actual pond/wading. Water README says old water-only folder is noncanonical. | Clearly distinguish **existing fixed production pond/wading** from **unimplemented generalized reusable liquid framework**. Do not create a second Water implementation. | Regular Chat |
| D07 | HIGH | `ENVIRONMENT/README.md`, `ENVIRONMENT/Seasons/README.md`, `SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md` | General Environment README says 'seasons are future work' in older context; Seasons docs say manual seasonal foliage **SE0/SE1/SE1b/SE2 code exists**. `SeasonController.cs` exists. | Say 'manual foliage preview implemented; automatic calendar/season progression and biome climate remain future'. | Regular Chat |
| D08 | MED | `ENVIRONMENT/Lighting/README.md`, `ENVIRONMENT/Lighting/LocalLights/README.md` | Local light plan says LL0/LL1 implemented, LL2/LL3 planned. Ensure top-level lighting overview doesn't imply full light-type architecture/streaming budget completed. | Add phase summary/link; keep sun shadows and local-light shadows separate. | Regular Chat |
| D09 | MED | `WORLD/Generation_Implementation.md` | Very large (~67k chars) plan mixes current P0–P4 completed status with explicit historical pre-P4 'Continue disabled', old adjacent world and original P3 performance notes. Some history is labeled, but current/old directions are hard to separate. | Create clear active-status/next-action index; archive or collapse historical instructions **only on approval**, retain evidence/commit provenance. | Regular Chat |
| D10 | MED | `SCENES/World/scene organize.md`, `SCENES/World/README.md` | SO0 original mixed `Actors` hierarchy is historical; SO2 typed roots are implemented and documented. | Lead with actual SO2 tree, label SO0/1 snapshots historical; keep SO3/4 and GL proposals visibly unapproved. | Regular Chat |
| D11 | MED | `ENVIRONMENT/Seasons/README.md`, seasonal plan, `SE2_Validation.md` | Seasonal summary has SE2 as future within earlier phase description, later records it implemented; SE2 validation says source/software GL pass but native acceptance pending. | Use one dated status table: SE0/1/1b implemented, SE2 implemented awaiting native tests, SE3 planned, SE4 deferred. | Regular Chat |
| D12 | MED | `COMBAT/C3_Implementation.md`, `FEEDBACK/README.md` | 'C3.1 immediate Codex handoff' reads as unstarted despite damage-number code being present; build/visual acceptance is explicitly pending. | Change instruction to validation/polish handoff; **do not duplicate damage-number module**. | Regular Chat |
| D13 | MED | `WORLD/Generation/README.md`, `WORLD/Runtime/README.md`, `WORLD/Generation_Implementation.md` | Generation README focuses on P1 macro and preview placement and does not summarize implemented P2/P3/P4 at same level; Runtime README does. | Add short pointer to current P0–P4 status, avoiding implying later geography or A-Life is done. | Regular Chat |
| D14 | MED | `SYSTEMS/WorldInteractions/Fire/README.md`, `DEBUG/README.md`, `SCENES/World/README.md` | Fire README says F5 opens independent debug sandbox; DEBUG and World READMEs say F5 opens Boot, F6 opens selected debug scene or World. | Correct stale F5 instruction in Fire README; distinguish F6 direct World from F5 campaign. | Regular Chat |
| D15 | MED | `ENTITIES/Player/README.md`, `COMBAT/COMBAT_IMPLEMENTATION.md` | Player summary says 'later combat phases remain unfinished' without concise breakdown; C0–C2.5/C3.2 are already implemented. | Specify existing player combat and outstanding polish C3.4/3.5/C4/C5, without calling combat entirely unimplemented. | Regular Chat |
| D16 | LOW | `WORLD/Terrain/README.md`, layered ground plan | Terrain overview stops at GS2/GS3 while GS4/GS5 exist. | Add latest GS4/GS5 status and GS6 acceptance reference. | Regular Chat |
| D17 | LOW | `WORLD/Content/Vegetation/Trees/README.md`, `Pine/README.md` | Generic shared Tree implemented for Oak/Pink, Pine has **art only**. This distinction is already correct; preserve it during any summary cleanup. | No correction required unless another plan claims Pine spawns. | None |
| D18 | LOW | `CORE/README.md`, `CORE/Diagnostics/README.md` (linked, not opened) | CORE overview says foundation-only, but also identifies Diagnostics as implemented exception. | Verify diagnostics if broader summary says CORE entirely blank; otherwise no change. | Regular Chat |

## B. Planned phases / implementation backlog (explicit source status)

| ID | Priority | Phase / feature | What remains (do not assume approval) | Source | Suggested owner |
| --- | --- | --- | --- | --- | --- |
| I01 | HIGH | **ES2** | Species separation tuning/presets/fixtures. **First reconcile existing** `SeparationProfile.cs` and `EntityDefinition.Separation` (ES1 already added both) against ES2 list to avoid duplicate code. | `ENTITIES/Shared/Movement/EntitySeparationPlan.md`; Movement README; source existence checked. | Regular Chat audit → Codex |
| I02 | HIGH | **ES4** | Crowds/corridors/chunk lifecycle, multi-enemy combat and target-PC profiling; ES1 and ES3 already code-complete. | EntitySeparationPlan | Copilot + user, Codex fixes |
| I03 | HIGH | **R2** | Ranged polish, F9 ranged controls, mixed encounter/validation. **World already has three archer practice actors**; determine R2 delta before building another practice world. | `RANGED_IMPLEMENTATION.md`, `R1_Validation.md`, World README | Regular Chat audit → Codex |
| I04 | HIGH | **C3.4** | Distinct feedback for blocked hits/misses/defense outcomes. | `COMBAT/C3_Implementation.md` | Codex |
| I05 | MED | **C3.5** | Optional capped camera shake/audio. Audio infrastructure is largely scaffolding. | C3 plan; `AUDIO/README.md` | Regular Chat planning → Codex |
| I06 | MED | **C4** | Tune movement/attack feel on top of existing sprint/dodge/knockback; no second motor. | `COMBAT/COMBAT_IMPLEMENTATION.md` | Codex |
| I07 | MED | **C5** | Polished repeatable skeleton encounter, telegraphs, balance and regression. | Combat plan | Codex + user |
| I08 | HIGH | **P5** | Physical priority POIs/paths/cave links/geography; P2 only reserves feature space. | `WORLD/Generation_Implementation.md` | Regular Chat design → Codex |
| I09 | LATER | **P6** | Abstract distant A-Life; not active distant entities. | Generation plan | Regular Chat architecture → Codex |
| I10 | MED | **SO3/SO4** | Additional world-scene organization/lifecycle passes; exact scope to be reread at current HEAD before implementation. | `SCENES/World/scene organize.md` | Regular Chat audit → Codex |
| I11 | LATER | **GL0–GL3** | General location/session and cave/interior transitions; proposed, not approved. | Scene organize plan | Regular Chat → Codex |
| I12 | MED | **GS6** | Terrain visual/performance regression, campaign Continue and target-PC GPU/frame pacing. | `LAYERED_GROUND_IMPLEMENTATION.md`, `GS5_Validation.md` | Copilot + user |
| I13 | MED | **SE3** | Broader foliage-mask/species validation; don't generate charred artwork automatically. | Seasonal plan | Regular Chat + Codex |
| I14 | LATER | **SE4** | Automatic calendar, gameplay year progression and persistence; manual preview already exists. | Seasonal plan | Regular Chat → Codex |
| I15 | MED | **LL2** | General shared light-type architecture beyond existing torches/occluders. | `ENVIRONMENT/Lighting/LocalLights/README.md` | Regular Chat review → Codex |
| I16 | MED | **LL3** | Streaming/local-light shadow budgets and performance. | LocalLights README | Codex + profiling |
| I17 | LATER | **Pine content** | Author TreeDefinition, variants, anchors/colliders, weather/fire options and biome population. **Only PNG art exists now.** | `WORLD/Content/Vegetation/Trees/Pine/README.md` | Regular Chat content planning, Codex integration |
| I18 | LATER | **Inventory** | Storage/item transfer, weight/encumbrance, hotbar, UI and equipment; currently design notes only. | `UI/Inventory/PLAYER_INVENTORY.md`, `UI/Inventory/README.md`, `ITEMS/README.md` | Regular Chat design → Codex |
| I19 | LATER | **General liquids** | Canonical water/lava/toxic definitions/regions/surfaces/interaction framework and generated bodies. Existing fixed pond is not proof this is done. | `WORLD/Liquids/README.md`, `WORLD/Generation/Liquids/README.md` | Regular Chat architecture → Codex |
| I20 | LATER | **Caves/locations** | Underground domain and POI content currently scaffolding; coordinate with P5/GL. | `WORLD/Caves/README.md`, `WORLD/Locations/README.md` | Regular Chat → Codex |
| I21 | LATER | **Fire expansion** | Spread, lightning ignition, damage and offscreen fire not implemented by FIRE1/FIRE2. | `SYSTEMS/README.md`, `SYSTEMS/WorldInteractions/README.md` | Regular Chat → Codex |
| I22 | LATER | **Save expansion** | Seasons, inventory, caves, A-Life, extra run UI, migrations, archive cleanup, large-save performance. P4 basic campaigns accepted. | `SAVE/README.md` | Regular Chat → Codex |
| I23 | LATER | **Environment expansion** | Regional biome climate/environment switching, real calendar, moon, weather/gameplay integration. | `ENVIRONMENT/README.md`, `Lighting/README.md` | Regular Chat → Codex |
| I24 | LATER | **Gameplay foundations** | Crafting, harvesting, building, farming, cooking, fishing, skills, effects, general interactions. **These are folder plans, not approved phase tickets.** | `SYSTEMS/README.md`, `SYSTEMS/Building/README.md` | Regular Chat first |
| I25 | LATER | **Content/AI infrastructure** | Factions, roles, most species, core registries/events/simulation, external Avalonia content tooling. Existing Skeleton Basic/Archer and Diagnostics are exceptions. | `ENTITIES/README.md`, `ENTITIES/Species/README.md`, `CORE/README.md`, root README | Regular Chat first |
| I26 | LATER | **Audio** | Shared audio/mixers/ambience and content, separate from current visuals. | `AUDIO/README.md` | Regular Chat first |

## C. Already implemented — validation and acceptance backlog

| ID | Area | Current reported implementation | What is still NOT proven / must be checked |
| --- | --- | --- | --- |
| V01 | **P0–P2** | Macro biomes + feature reservations implemented; generation plan reports automated checks. | New biome content and physical P5 geography are separate work. |
| V02 | **P3** | Finite chunk streaming implemented; regression fixes for spawn trees, shadow flash, shader reuse. | Windows FPS/frame pacing, synchronous startup/nav spikes, chunk seams and long traversal. |
| V03 | **P4** | Profile campaigns/Continue/save restored; earlier user functional acceptance and automated tests recorded. | Large-save profiling, later SO2/C1 UI/render regressions; avoid invalidating accepted core P4. |
| V04 | **SO2** | Typed scene roots and compatibility tested. | Native Y-sort/seams, render/performance acceptance. |
| V05 | **AC0–AC4** | Actor collision plan explicitly closed for agreed scope; user accepted open-world behavior. | Optional corridor/bottleneck checks; don't reopen AC3 as missing implementation. |
| V06 | **C0–C2.5** | Shared combat HP/stats/contact/attack + player actions implemented. | Visual acceptance and known broader native shutdown issues; don't duplicate. |
| V07 | **C3.1** | Damage-number module code reported. | Latest build/visual checks; not a new feature ticket. |
| V08 | **C3.2** | Knockback/stagger implemented. | Windows feel/performance and optional corridors. |
| V09 | **C3.3a–d** | Shared bounded particle slots, tree leaves, bone/flesh impact profiles implemented. | Target-PC impact tuning/visual/performance. |
| V10 | **ES1/ES3** | Shared separation + engagement positioning implemented. `SeparationProfile.cs`, `EngagementPositioning.cs`, `CombatDebugPanel.cs` exist. | ES4 stress tests and user crowd-feel acceptance. |
| V11 | **R0/R1** | Shared projectile and ranged AI code exists; Archer definition/fixture reported; F6 World now documents 3 archers. | Projectile edge cases, obstruction, F9 R2 scope and target-PC behavior. |
| V12 | **SE0/SE1/SE1b/SE2** | SeasonController exists; seasonal mask/palette, manual dates and bridge reported. | SE1b/SE2 native Godot, Windows look, seasonal material/fire lifecycle and save compatibility. |
| V13 | **GS0–GS5** | Layered ground, overlays, F9 preview, grayscale Dirt and Basalt Rock reported. | Godot build/import, Windows visual seams, Continue migration and GPU timing. |
| V14 | **LL0/LL1** | Local torch shadows/occluders and two extra preview torches reported. | User acceptance and local-light performance; LL2/3 are distinct future phases. |
| V15 | **FIRE1/FIRE2** | Production ignition/burn/charred fallback and reusable effects documented. | Future spread/damage are not part of FIRE1/2; keep debug F5 instructions accurate. |
| V16 | **Shared trees** | Oak and Pink use one generic Tree scene; Pine artwork staged only. `Tree.cs` exists. | New species authoring and charred art per species remain separate. |

## D. Design references — **NOT** automatic implementation tickets

- `Notes/Ideas/FutureMechanicIdeas.md`: explicitly says **ideas/design reference only, do not implement yet**. Survival, preservation, clothing, shelter, food, injuries, etc. are inspiration for future approved plans.
- `Notes/Ideas/Playerbuildings.md`: explicitly future design ideas, no approval to create building systems.
- `UI/Inventory/PLAYER_INVENTORY.md`: planned tactile backpack, weight, stacks and gear; no inventory mechanics or UI yet.
- `WORLD/Water/README.md`: obsolete water-only scaffolding, superseded by canonical Liquids; do not implement parallel system.
- `CORE/README.md`, `ITEMS/README.md`, `AUDIO/README.md`, `SYSTEMS/README.md`: largely scaffolding/ownership contracts, not hundreds of individually approved tasks.
- `Notes/ARCHIVE_DONOTREADUNLESSINSTRUCTED/`: deliberately archived; should **not** be mined as active backlog without permission.

## E. Cross-document implementation traps

1. **R1 vs R2:** R1 standalone practice fixture and later eight-skeleton F6 practice are different snapshots. Avoid building another mixed scene until exact R2 delta is determined.
2. **ES1 vs ES2:** `SeparationProfile.cs` already exists. ES2 must add genuine species behavior/fixtures, not reintroduce a Resource class.
3. **C3.3:** Hit particles and health bars already exist. C3.4 feedback outcomes and C3.5 camera/audio are the next planned slices.
4. **GS5:** Latest Dirt uses imported grayscale PNG; historical SVG and procedural Dirt are superseded. Basalt Rock is still procedural.
5. **SE2:** Code/source/software-GL verification is not native Windows acceptance; SE4 calendar is not implemented.
6. **P2/P5:** Reserving feature space is not generating actual roads/caves/landforms.
7. **Fixed pond/general liquids:** Production pond/wading is not the complete generic liquid domain.
8. **F5/F6:** F5 is Boot/Main Menu; F6 runs whichever selected scene. Old notes sometimes incorrectly describe F5 as launching the DEBUG forest.
9. **Source exists ≠ tests passed:** Existence checked for `Projectile.cs`, `RangedCombatAI.cs`, `SeparationProfile.cs`, `EngagementPositioning.cs`, `SeasonController.cs`, `Tree.cs`, `CombatDebugPanel.cs`. Runtime and profile tests not executed in this audit.

## F. Coverage register — actually read in this pass and preceding targeted audit

**Project/architecture:** `README.md`, `CodePreferences.md`, `CORE/README.md`, `DEBUG/README.md`, `DEBUG/UI/README.md`, `UI/README.md`, `FEEDBACK/README.md`, `ITEMS/README.md`, `AUDIO/README.md`, `SYSTEMS/README.md`.

**World/terrain:** `WORLD/README.md`, `WORLD/Generation_Implementation.md`, `WORLD/Generation/README.md`, `WORLD/Runtime/README.md`, `WORLD/Runtime/Streaming/README.md`, `WORLD/Biomes/README.md`, `WORLD/Content/README.md`, `WORLD/Terrain/README.md`, `WORLD/Terrain/Ground/Surfaces/README.md`, `LAYERED_GROUND_IMPLEMENTATION.md`, `GS5_Validation.md`, `GS5_PNG_DIRT.md`, `PROCEDURAL_GROUND_RECIPES.md`, `WORLD/Content/Vegetation/Trees/README.md`, `WORLD/Content/Vegetation/Trees/Pine/README.md`, `WORLD/Water/README.md`, `WORLD/Liquids/README.md`, `WORLD/Generation/Liquids/README.md`, `WORLD/Caves/README.md`, `WORLD/Locations/README.md`.

**Entities/combat:** `ENTITIES/README.md`, `ENTITIES/Player/README.md`, `ENTITIES/Shared/README.md`, `ENTITIES/Shared/Movement/README.md`, `EntitySeparationPlan.md`, `ActorCollisionPlan.md`, `ENTITIES/Species/README.md`, `ENTITIES/Species/Undead/README.md`, `ENTITIES/Species/Undead/Skeletons/README.md`, `COMBAT/README.md`, `COMBAT/COMBAT_IMPLEMENTATION.md`, `COMBAT/C3_Implementation.md`, `COMBAT/Projectiles/README.md`, `COMBAT/Projectiles/RANGED_IMPLEMENTATION.md`, `COMBAT/Projectiles/R1_Validation.md`, `SCENES/World/README.md`, `SCENES/World/scene organize.md`.

**Environment/other:** `ENVIRONMENT/README.md`, `ENVIRONMENT/Lighting/README.md`, `ENVIRONMENT/Lighting/LocalLights/README.md`, `ENVIRONMENT/Seasons/README.md`, `SEASONAL_FOLIAGE_IMPLEMENTATION_PLAN.md`, `SE2_Validation.md`, `SYSTEMS/WorldInteractions/README.md`, `SYSTEMS/WorldInteractions/Fire/README.md`, `SYSTEMS/Building/README.md`, `UI/Inventory/README.md`, `UI/Inventory/PLAYER_INVENTORY.md`, `Notes/Ideas/FutureMechanicIdeas.md`, `Notes/Ideas/Playerbuildings.md`, `SAVE/README.md`.

**Source-file existence checks:** `ENTITIES/Shared/Movement/SeparationProfile.cs`, `ENTITIES/Shared/AI/EngagementPositioning.cs`, `ENTITIES/Shared/AI/RangedCombatAI.cs`, `COMBAT/Projectiles/Projectile.cs`, `ENVIRONMENT/Seasons/SeasonController.cs`, `WORLD/Content/Vegetation/Trees/Shared/Tree.cs`, `DEBUG/UI/Combat/CombatDebugPanel.cs`. Binary PNG was not fetched as UTF-8; its existence/content is supported by GS5 authoring notes, not independently inspected.

**Not comprehensively inventoried:** All unlinked `.md` files and subfolders under `Notes/`, `DEBUG/`, `TESTS/`, `WORLD/`, `ENVIRONMENT/`, `ENTITIES/`, `SYSTEMS/`, `UI/`, `FEEDBACK/`, `CORE/` and `AUDIO/`; all C#/GDScript and Resources; current build/test results. A truly exhaustive audit requires a recursive Git tree listing or local checkout and source-by-source checks. Do not infer that unlisted files are absent.

## G. Recommended execution order — approval required for every job

1. **Regular Chat:** resolve D01–D16 documentation drift, starting with actual contradictions (R1 status, Species/Undead, ground art, fire F5, liquids). Only after user authorizes editing original files.
2. **Regular Chat:** compare ES2 and R2 requirements against existing source and F6 setup; reduce duplicate implementation risk.
3. **Copilot + user:** close outstanding visual/Windows/target-PC checks for P3, GS5, SE2, ES1/ES3, R1, SO2 and C3.
4. **Regular Chat planning:** discuss LL2/LL3, C3.4/C3.5, P5 and optional next gameplay systems before Codex.
5. **Codex:** implement only explicitly chosen phases; one job/commit, with tests and owning README/plan update.
6. **Complete inventory:** In a local clone run `git ls-files '*.md'` and inspect every active plan against code, including unlinked docs. This is the necessary step before the word 'complete' can honestly be applied to this audit.

**No existing implementation plan, README, C# script, Resource or scene was modified by this re-audit.**
