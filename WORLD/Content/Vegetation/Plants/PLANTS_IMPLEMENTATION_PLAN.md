# Data-Driven Plants — One Shared Scene, Multiple Artwork Variants

> **Status: PLANNED — 24 October 2026.** This document is the Codex implementation handoff for **plants only**. The subsequent **grass scene consolidation is deliberately deferred to a separate Regular Chat task** after plant implementation is visually accepted. No gameplay files are changed by writing this plan. Read the root `README.md`, `CodePreferences.md`, the relevant feature READMEs and actual HEAD code before coding; implement only explicitly authorized phases. One GitHub commit per authorized job.

## Goal and non-negotiable constraints

- **Exactly one shared `Plant.tscn`**, not one scene per `Plant001.png` … `Plant008.png`.
- One initial **`Plants.tres` definition** holds all eight artwork variants. The PNGs are **visual variants of the same plant population**, not eight independently configured world populations or eight separate species scripts. Prefer eight references/inline variant entries, **not eight standalone .tres files**.
- These eight textures are **Oak Forest only** for now: add `Plants.tres` as one population in `WORLD/Biomes/Definitions/OakForest/Biome.tres`; do not add them to Pink Meadow.
- Keep grass (`Grass.tres`, four Tuft scenes) and trees (`Tree.tscn`, TreeDefinition) **working and structurally unchanged** in this plants job. The user will request a second, smaller pass to convert grass to one `Grass.tscn` after plants establish the reusable mechanism.
- Prepare for **100+ plant artworks and many biomes**. A reusable lightweight vegetation runtime and definition/variant contract may serve future groups (forest plants, marsh plants, desert plants, flowers and grass), each with their own Resource identity and per-biome density. **One scene per vegetation category, not per sprite or per biome.**
- Production integration is mandatory: plants must visibly populate **`SCENES/World/World.tscn`** with the existing P3 chunk generator, not work only in a standalone test scene.
- No hardcoded checks for `OakForest`, `Plant001` or a specific bitmap name inside generation code; no reflection/directory scanning of resource folders during runtime generation.
- Aim for zero idle per-plant CPU work. Reuse existing wind/snow presentation and chunk lifecycle; no collision for decorative plants initially; no per-plant `_Process` or `_PhysicsProcess`.

## Phase overview

| Phase | Objective | Recommended assistant | Status |
| --- | --- | --- | --- |
| **PL0** | Audit vegetation definitions, two generation paths, density limits, root sprites, environment hooks and SAVE impacts | Regular Chat | **Source audit recorded below** |
| **PL1** | Build one reusable decorative-vegetation definition/variant/runtime contract and `Plant.tscn` | Codex | Planned |
| **PL2** | Import eight plant sprites, create one `Plants.tres` and connect to Oak Forest biome data | Codex | Planned |
| **PL3** | Wire both existing placement paths + environment/streaming, validate in production World and document results | Codex | Planned |
| **GR0** | **Later, separate request:** migrate grass to one shared `Grass.tscn` using the finished PL1 mechanism | Regular Chat for small pass, Codex if integration complexity arises | Deferred — **not part of plant job** |

Codex may implement **PL1–PL3 together in one reviewed commit** because all three are necessary to show working plants in production. Do not silently begin GR0.

## Actual current source audit (24 October 2026)

- New art: repository root contains `Plant001.png` through `Plant008.png`. `WORLD/Content/Vegetation/Plants/` presently contains only its README; no plant definition, scene or biome population exists.
- Grass: `WORLD/Content/Vegetation/Grass/Grass.tres` is a `SceneryDefinition` whose `Variants` are **four PackedScenes** (`Tuft01.tscn`–`Tuft04.tscn`). All four use a `Node2D/Sprite2D` structure with the same `GrassWind.tres`; this works but is unnecessary scene duplication and is **not** migrated in PL1–PL3.
- Trees: `TreeDefinition` has `VisualVariants` and one `WORLD/Content/Vegetation/Trees/Shared/Tree.tscn`; `Tree.Configure` assigns texture/anchor and collision before adding to the world. This is the **architectural precedent**, but plants should not inherit tree fire, hit, trunk collision, occlusion or harvest behavior.
- `SceneryDefinition` currently owns `Id`, `DisplayName`, legacy `Array<PackedScene> Variants`, `SpacingRadius`, `VisualMinimum/VisualMaximum`, `GrassLike`, `SnowResponse`, `CastSunShadow`, and `AvoidBiomeTransition`.
- `BiomePopulation` references one `SceneryDefinition` and holds independently adjustable preview `Count`, streamed `InstancesPerMillionUnits`, scale range, mirroring and preview-only cluster settings.
- **Both placement paths need a generic visual-template case:** `WORLD/Generation/Placement/ChunkSceneryData.cs` selects a variant and validates bounds, `WORLD/Runtime/Streaming/WorldChunkStream.cs::ApplyOne` currently instantiates tree template vs non-tree PackedScene; `WORLD/Generation/Placement/WorldSceneryPlacement.cs` does the analogous preview path. Keep old tree and packed-scene behavior exactly intact.
- Streaming candidates use a coordinate-keyed seeded lattice, stable content IDs, reservation/water checks, spacing and world/biome clipping. **Changing biome population composition changes generated placement**, even when code is deterministic; existing P4 content fingerprints/checkpoint compatibility must be respected. No silent re-generation of saved campaign scenery.
- Oak Forest currently has three populations (trees 26, rocks 11, grass 60 instances per million world units²; total **97**). The P3 generator validates total density against `1,000,000 / CandidateCellSize²`, with `CandidateCellSize = 96` (capacity approximately **108.5**). An additional plant population must **fit this limit**. One illustrative balance: trees 26 + rocks 11 + grass **45** + plants **20** = **102**, with Oak's existing grass *resource density* adjusted only; **do not convert grass scenes during this pass**. Alternative if preserving grass count: plants <=11.5, subject to margin and desired visual density. Preview-only `Count` must also be authored.
- `GrassLike=true` currently means a **lightweight low-vegetation placement category** (`WorldPlacementKind.LowVegetation`), rather than a requirement to be literal grass. Initial plants may use this category with `SnowResponse=Vegetation` and `CastSunShadow=false`; do not add new placement categories merely for naming.
- `ENVIRONMENT/Wind/Visuals/VegetationSway.gdshader` already supports world-unit sway and a root-weighted deformation. Grass uses `GrassWind.tres`; vegetation/snow systems attach by existing content metadata and shared materials. A small `PlantWind.tres` can reuse that shader if independent wind tuning is needed; **no separate plant shader or CPU sway processor**.

## Ownership and proposed files — lean, extensible

```text
WORLD/Content/Vegetation/
├── Shared/
│   ├── DecorativeVegetation.cs           # PROPOSED: reusable rooted Sprite2D template controller
│   ├── DecorativeVegetationDefinition.cs # PROPOSED: SceneryDefinition subclass, template + artwork variants
│   └── DecorativeVegetationVariant.cs    # PROPOSED: one inline entry's Texture2D + optional anchor/bounds
│
├── Plants/
│   ├── README.md                         # EXISTS; update upon implementation
│   ├── PLANTS_IMPLEMENTATION_PLAN.md     # THIS PLAN
│   ├── Plant.tscn                        # PROPOSED: ONE scene; DecorativeVegetation controller + Sprite
│   ├── Plants.tres                       # PROPOSED: ONE initial Resource, eight inline textures/variants
│   ├── PlantWind.tres                    # OPTIONAL: same shared wind shader; independent settings
│   └── Sprites/
│       ├── Plant001.png                  # MOVE from repo root
│       ├── Plant002.png
│       ├── Plant003.png
│       ├── Plant004.png
│       ├── Plant005.png
│       ├── Plant006.png
│       ├── Plant007.png
│       └── Plant008.png
│
├── Grass/                                # EXISTING; leave structure alone for now
│   ├── Grass.tres
│   └── Tuft01.tscn ... Tuft04.tscn
│
└── Trees/Shared/Tree.tscn               # EXISTING; leave alone

WORLD/Biomes/Definitions/OakForest/Biome.tres
   └── Populations += one Plants.tres entry

WORLD/Biomes/Definitions/PinkMeadow/Biome.tres
   └── NO new plant entry
```

The class and file names above are recommendations, not a demand to create extra abstraction layers. Use the *smallest* reusable contract that lets grass adopt the same system later. Do not add a separate scene per PNG, duplicate visual-`.tres` assets per PNG, or add an Oak-specific C# class.

### Definition vs scene vs biome — important long-term distinction

| Responsibility | Lives where | Example |
| --- | --- | --- |
| **Runtime object template** | One `.tscn` per vegetation category | `Plant.tscn` |
| **Reusable behavior/visual setup** | Shared C# implementation | `DecorativeVegetation.cs` |
| **Artwork group** | One data Resource per meaningful group, with inline variant textures | `Plants.tres` today; optional `SwampPlants.tres` later |
| **Individual textures** | Sprites folder | `Plant001.png` ... `Plant008.png` |
| **Where/how many** | BiomePopulation under each biome `.tres` | Oak references Plants; Pink does not |
| **Optional future gameplay** | Shared harvest/interaction contract, opted in via data | Herbs harvestable, ferns decorative |

One `Plants.tres` with eight variants means the eight sprites appear as a **single weighted population** with randomized variant selection. If later some plants require individual spawn rarity, different harvest rewards or biome restrictions, **split them into additional definitions referencing the same `Plant.tscn`**, not additional scenes. Do not prematurely author a separate `.tres` per artwork when shared defaults are sufficient.

## Phase implementation details

### PL0 — existing-code audit (source review completed; no code changes)

**Objective:** confirm root asset paths/import dimensions and transparent ground-anchor alignment, Tree vs PackedScene dispatch, preview/streamed data selection, environment metadata, density constraints and P4 compatibility.

**Current dependencies:** see verified source audit above. Codex must re-read HEAD and check actual pixel sizes/transparent padding before fixing anchors; do not assume all eight PNGs have the same content bounds merely because they share a naming pattern.

**In scope:** plan the minimal reusable definition/controller and plant visual variation contract. **Out of scope:** changes to code/resources. **Preserve:** current Oak grass, Oak/Pink trees, rocks and saved-world contract. **Performance:** no additional idle polling and stable resource caching. **Pass:** no hidden dependency on one species-name or one PNG path; density budget calculated. **Assistant:** Regular Chat (completed design audit), Codex re-verifies at coding time.

### PL1 — reusable decorative vegetation template

**Objective:** establish *one* configurable scene and authoring API for decorative plants and later grass.

**Implementation approach:** prefer `DecorativeVegetationDefinition : SceneryDefinition` with one optional `PackedScene TemplateScene` referencing `Plant.tscn` and a `Godot.Collections.Array<DecorativeVegetationVariant>` for visual textures. A lightweight `DecorativeVegetation : Node2D` configures its single `Sprite2D` with the seeded variant index **before AddChild**; do not choose artwork using node-local RNG or `_Ready` randomness. Support `Sprite.FlipH`, a grounded bottom-centre/source-pixel anchor, positive display scale and conservative placement bounds. Optional variant-specific anchor/bounds should be **inline** and default from shared settings when possible, to make 100+ artworks practical to author. Validate missing textures, empty lists, invalid scale/bounds and optional duplicated variant IDs at configuration/registration time.

**Files:** shared classes under `Vegetation/Shared/`, `Plants/Plant.tscn`. Avoid modifying `TreeVisualVariant` or piling plant-specific fields into trees.

**Preserve:** TreeDefinition/Tree.tscn behavior and legacy SceneryDefinition.PackedScene Variants; grass untouched; existing wind source. **Out of scope:** harvesting, collisions, foliage masks, particle systems, new shaders and P3 replacement. **Performance:** no per-instance idle updates; cache loaded `PackedScene`/textures and reuse one plant wind material. **Pass:** one `Plant.tscn` renders any of eight provided textures correctly and with fixed rooted location after mirroring and scaling. **Assistant:** Codex (C# and reusable architecture).

### PL2 — author Oak-only assets and biome population

**Objective:** move and wire eight existing images into one plant definition and make Oak Forest own its density.

**Implementation approach:** move `Plant001.png`–`Plant008.png` from the repo root to `Plants/Sprites/` (preserve Godot import metadata if applicable; check image alpha/orientation). Create **one** `Plants.tres`, referencing the one template and eight inline visual variants. Give it stable unique `Id` (e.g. `plants.oak_understory`) and readable display name; default `GrassLike=true`, `SnowResponse=Vegetation`, `CastSunShadow=false`, safe `AvoidBiomeTransition` and artwork-appropriate `SpacingRadius`, `VisualMinimum/Maximum`.

Use existing `VegetationSway.gdshader` through shared `PlantWind.tres` only if separate plant tuning is desirable. Match ground-foot anchoring and choose conservative visual bounds for the **largest of the eight sprites**, or per-variant bounds if noticeably different.

**Biome data:** add one new `BiomePopulation` reference to `OakForest/Biome.tres`. Set `InstancesPerMillionUnits` and preview `Count` reasonably; keep total density within current P3 limit (illustrative 45 grass +20 plants +26 trees +11 rocks =102). Pink Meadow's biome `.tres` remains byte-for-byte unchanged.

**Preserve:** Oak's existing tree/rock identities; no grass structural migration. **Out of scope:** eight plant definitions or eight scenes, plant gathering, placing plants in every biome. **Performance:** one Resource list; existing coordinate-keyed variant choice. **Pass:** Godot sees all 8 textures; only Oak regions select Plants; no new root PNG leftovers; valid density. **Assistant:** Codex.

### PL3 — full production-streaming + legacy preview hookup and validation

**Objective:** both production streamed chunks and compatibility-preview world can instantiate the one plant template using the chosen visual variant.

**Implementation approach:** extend **only the necessary dispatch points** in `ChunkSceneryData.cs`, `WorldChunkStream.cs::ApplyOne` and `WorldSceneryPlacement.cs` to support the new decorative-vegetation definition. Keep **all existing tree and PackedScene behaviors** as fallback. Use an explicit shared helper/contract when it materially removes duplication; avoid creating a giant vegetation manager. The generator must select content/variant deterministically by coordinates/seed, while the lightweight scene applies texture/anchor; no new per-chunk/random population system.

Preserve `GrassLike` low-vegetation reservation semantics, P2 water exclusions, distance from spawn, area bounds, biome seams, footprint spacing, P3 publish/unload, wind/snow registration, Y sorting and stable `world_content_id`/instance metadata.

**Production-first acceptance:** run `SCENES/World/World.tscn` (F6) with streamed mode and see the eight plant appearances mixed in **Oak only**. Keep independent headless/fixture tests supplementary, not the sole working example. Exercise F7/F9 if relevant and confirm wind/snow responses. Test a fixed seed in different visit orders and after leave/return; no duplicated plants at chunk boundaries, no spurious Pink Meadow plants, no oversized sprites or arbitrary placement near the player's feet.

**Save safety:** compare unchanged trees/grass placement when new plant population is disabled; changes caused by the *new Oak population/density data* are expected and must be documented. Existing campaign content fingerprints/migrations must reject unsupported composition changes rather than silently rerolling old runs. If the current system needs an explicit generator/content compatibility update, perform the narrow required action or flag it without corrupting old saves. **Do not delete old saves** or bypass SAVE checks.

**Performance:** no `_Process`/`_PhysicsProcess` on idle plants; no per-frame folder scans; bounded scene counts/instantiation based on existing streamed candidate lattice and application budget; material shared by compatible variants; test loaded/idle/travel/unload and spot-check allocations/FPS rather than claiming unmeasured gains.

**Out of scope:** GR0, new harvesting/growth, globally rebalancing every biome, rewriting TreeDefinition or replacing chunk streaming. **Pass:** .NET/Godot build/import and relevant generation/streaming/vegetation tests pass or are explicitly marked awaiting local validation; Windows visual acceptance may be deferred with instructions. **Assistant:** Codex, with local Copilot validation where requested.

## Deferred follow-up GR0 — convert Grass after plants are accepted

**Not authorized for the plant implementation.** Once PL1–PL3 work in production, migrate Grass from four PackedScene Tufts to **one `Grass.tscn`** using the **exact same DecorativeVegetation scene controller/definition/variant contract** created for Plants.

- Keep `Grass.tres` as the canonical **separate Grass definition** with its four current `grass_small_0*.png` artwork variants; preserve its stable `Id = "grass.tuft"`, `GrassWind.tres`, no-collision, snow, low-Z, scale and spacing.
- Create only one `Grass.tscn` (no four separate Tuft scenes). Replace Grass.tres scene references with inline visual texture variants, then remove the obsolete Tuft scenes **only after searching all references** and verifying production/compatibility tests.
- Preserve grass counts/positions/variant selection as far as migration contracts allow; changing serialized representation can affect SAVE dependency fingerprints. Validate old saved campaigns or explicitly document incompatibility. Do not rewrite the shared decorative generator again in GR0 unless a real gap is discovered.
- Task should ideally be a **small Regular Chat / lightweight Codex edit** after the plant framework is working; avoid a second independent grass class or a new scene for each grass PNG.
- The final intent remains **separate content Resource per category/group**, not one giant `Vegetation.tres` containing all grass, plants, trees and every biome's content.

## Important constraints / decisions

1. **100+ artwork files is not 100+ active scenes per species**; runtime creates only instances of the shared template in *loaded chunks*, choosing a stored variant index. Many PNG varieties may still increase unique texture/material switches: profile batching/atlas only if needed later; do not promise draw-call savings merely from fewer scene files.
2. Grouping eight artworks in one definition means **one population density** across the group. If each artwork later needs its own scarcity, harvest rules or biome rules, make a new definition/group referencing `Plant.tscn`, not a new scene.
3. Use the existing **BiomePopulation** data rather than a separate Plants-only generator. Per-biome art eligibility is based on explicit Resource references, not searching a folder and automatically placing everything.
4. Make sprite foot placement stable at different dimensions. Optionally reuse sensible bottom-centre default and add per-art variant overrides only if necessary. No hardcoded `Vector2(-64,-116)` assumption for all future artwork.
5. Wind and snow are optional shared adapters. No per-plant script should tick continuously; optional future harvesting uses `SYSTEMS/Harvesting/` and `SYSTEMS/Drops/`, not duplicated harvest code in `Plant.tscn`.

## Validation / handoff checklist for Codex

- [ ] Exactly one `Plant.tscn`, one initial `Plants.tres`, eight plant sprite PNGs in `Plants/Sprites/`; no extra scene/resource per PNG.
- [ ] One reusable decorative vegetation controller and resource contract; not hardcoded for Plants, Oak or the texture names.
- [ ] Oak biome has one plant population; Pink biome has none; old trees, rocks and grass still render.
- [ ] Seeded variant choice, mirroring, scaled rooted anchors and artwork bounds work consistently across streamed chunk reloads and legacy preview.
- [ ] Density validator passes under current candidate settings and no plant placement inside water, protected starts or wrong biome.
- [ ] Shared wind/snow can affect plants without new per-item frame loops; idle/unload state clean.
- [ ] Save compatibility explicitly reviewed; no silent changes to active saved campaign outcomes.
- [ ] F6 **production World** visual inspection supported; tests are supplementary; results and known risks reported.
- [ ] Only **PL1–PL3** implemented; leave GR0 for the user's next request.
- [ ] Update affected READMEs, phase status, and record exact commit, changed files and measured/untested performance checks in the same authorized implementation commit.

**Implementation prompt for Codex:** "Read `WORLD/Content/Vegetation/Plants/PLANTS_IMPLEMENTATION_PLAN.md`, root README and CodePreferences, then implement PL1–PL3 as one production-World vertical slice. One Plant.tscn, one Plants.tres with eight Oak-only sprite variants, shared future-proof decorative vegetation code; keep existing Grass Tuft scenes untouched. Check streamed and preview generation, density budget, environmental adapters and old campaign compatibility. One GitHub commit; record tests and deferred Windows checks."
