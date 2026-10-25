# Data-driven Plants — Individual Definitions, One Shared Scene

> Status: PLANNED. Revised 24 October 2026. Documentation only. This supersedes the earlier incorrect instruction to combine eight distinct plant types into one Plants.tres. The separate grass migration GR0 is not approved by this plan. Read root README.md, CodePreferences.md, the owning README files, and current production code before implementing. One commit per authorized job.

## The three responsibilities — core design

1. **Plant.tscn**: exactly ONE reusable scene for the plant category. All plant types instantiate this scene, with the same basic node tree and reusable mechanics. No per-image or per-biome plant scenes.
2. **One individual .tres per distinct plant type**: its stable ID, its PNG artwork, name, grounded anchor, placement and environment settings, and later its harvest requirements and drop table. The first eight separate types are Plant001.tres through Plant008.tres. Do NOT combine these eight distinct species into one Plants.tres. True cosmetic sprite variations of the same biological plant can optionally share one definition in the future.
3. **Biome.tres decides plant eligibility**: each biome has an explicit list of BiomePopulation entries referencing the *individual* plant .tres definitions that can spawn there. Each entry owns independent density, preview count, scale/mirror settings. A future Pine biome can reuse Plant001.tres directly and choose different density. A future volcanic biome can use its own VolcanicDeadPlant.tres without allowing it in Oak. Folders never grant biome permission automatically.

The distinction is: **the scene controls common behavior; a species Resource controls plant identity/properties; each biome controls which species appear and how often.**

| Shared plant Resource | Oak Forest | Future Pine | Future Volcano |
| --- | --- | --- | --- |
| Plant001.tres | Yes | Optionally yes | No |
| Plant002.tres | Yes | No | No |
| Plant003.tres | Yes | Optionally yes | No |
| Plant004–008.tres | Yes | Choose individually later | No |
| PineFern.tres | No | Yes | No |
| VolcanicDeadPlant.tres | No | No | Yes |

No special BiomePlantWhitelist system is needed now: the existing BiomePopulation array already provides the explicit allowlist. An optional grouped BiomeVegetationPalette is future authoring convenience only if lists grow unwieldy.

## Implementation phases

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| PL0 | Verify content classes, biome ownership, streamer, assets, save compatibility | Regular Chat | Source audit recorded; Codex rechecks HEAD |
| PL1 | Reusable one-scene vegetation definition/controller contract | Codex | Planned |
| PL2 | Move eight artwork files, author EIGHT plant Resources, register EIGHT Oak biome populations | Codex | Planned |
| PL3 | Wire production streamed World and compatibility-preview generation; test wind/snow/performance | Codex | Planned |
| PL4 | Later attach existing Harvesting/Drops capabilities and SAVE integration to plant types | Codex | Deferred; separate approval after H4 |
| GR0 | Later convert grass from four scenes to one Grass.tscn using same reusable setup | Regular Chat for small refactor; Codex if needed | Deferred; separate job |

Codex may implement PL1–PL3 together as a coherent production feature with one commit when authorized. Do not implement PL4 or GR0 in that job.

## Current source state verified 24 October 2026

- New source art: Plant001.png through Plant008.png are currently in the **repository root**. The Plants folder has this plan and its README, but no plant scenes/definitions.
- WORLD/Content/Vegetation/Shared/ already contains Seasonal/, Snow/, Wetness/. They are existing shared adapters. **This planning commit does not add C# files there**; Codex should add only the minimum visual setup classes required.
- Existing grass uses one Grass.tres and four PackedScene Tufts. This stays unchanged during plant implementation.
- Existing trees demonstrate single-template instancing using TreeDefinition, TreeVisualVariant, and Trees/Shared/Tree.tscn; do not copy tree-specific fire/collision/falling mechanics to plants.
- WORLD/Content/Shared/SceneryDefinition.cs owns inherited Id, DisplayName, old PackedScene Variants, spacing, artwork bounds, snow response, GrassLike, shadows and transition rules.
- WORLD/Biomes/Shared/BiomePopulation.cs contains a Content: SceneryDefinition reference, Count, InstancesPerMillionUnits, MinimumScale, MaximumScale and MirrorChance. WORLD/Biomes/Shared/WorldBiomeDefinition.cs contains the Populations array. **Each distinct plant Resource becomes one entry in this array.**
- WORLD/Generation/Placement/ChunkSceneryData.cs currently chooses from biome populations by content ID, selects deterministic candidate positions, and **rejects duplicated Content.Id in a biome**. Therefore each PlantXXX.tres must use a distinct stable ID. Current generator special-cases TreeDefinition; it needs minimal generalized template support for one Plant.tscn.
- WORLD/Runtime/Streaming/WorldChunkStream.cs::ApplyOne and WORLD/Generation/Placement/WorldSceneryPlacement.cs instantiate legacy PackedScene or tree template. Preserve both existing paths, add a small data-driven branch/contract for vegetation definitions. Avoid new separate plant generation algorithms or hardcoded biome names.
- SYSTEMS/Harvesting/Harvestable.cs, HarvestProfile.cs, SYSTEMS/Drops/DropTable.cs, WorldDropSpawner.cs and DroppedItem.tscn already exist. The existing tree harvesting H1–H3 forbids production World until H4 persistence is complete. **Do not duplicate harvesting, reward generation or pickup logic inside plants.**
- P3 is a coordinate-seeded streamed candidate generator; P4 checkpoint validity uses strict content/generator compatibility checks. Changing Oak biome content/density changes future generated placements. Never bypass save validation, silently regenerate old campaigns, or delete saves.
- Current Oak density per million world units squared is trees 26 + rocks 11 + grass 60 = **97**. CandidateCellSize is 96, and the current validation capacity is approximately **108.5**. Eight additional plant population densities must fit as a *sum*. Example only: trees 26 + rocks 11 + grass 45 + eight plant populations at 2.5 each = 102. The existing candidate model may ultimately need a separate bounded low-vegetation placement tier for truly lush areas; do NOT rewrite the generator for this task.

## Proposed ownership structure (minimal)

~~~text
WORLD/Content/Vegetation/
├── Shared/
│   ├── Seasonal/                         EXISTING
│   ├── Snow/                             EXISTING
│   ├── Wetness/                          EXISTING
│   ├── VegetationDefinition.cs           PROPOSED, shared SceneryDefinition subtype
│   └── VegetationInstance.cs             PROPOSED, sprite/anchor configuration only
│
├── Plants/
│   ├── README.md                         EXISTS
│   ├── PLANTS_IMPLEMENTATION_PLAN.md     THIS PLAN
│   ├── Plant.tscn                        ONE shared scene
│   ├── Definitions/
│   │   ├── Plant001.tres                 individual definition, PNG link
│   │   ├── Plant002.tres
│   │   ├── Plant003.tres
│   │   ├── Plant004.tres
│   │   ├── Plant005.tres
│   │   ├── Plant006.tres
│   │   ├── Plant007.tres
│   │   └── Plant008.tres
│   ├── Sprites/
│   │   ├── Plant001.png
│   │   ├── Plant002.png
│   │   ├── Plant003.png
│   │   ├── Plant004.png
│   │   ├── Plant005.png
│   │   ├── Plant006.png
│   │   ├── Plant007.png
│   │   └── Plant008.png
│   └── PlantWind.tres                    OPTIONAL reused vegetation shader settings
│
├── Grass/
│   ├── Grass.tres                        EXISTING, unchanged for now
│   └── Tuft01–Tuft04.tscn                EXISTING, unchanged for now
│
└── Trees/Shared/Tree.tscn                EXISTING, unchanged
~~~

These shared class names are suggested, not a requirement to introduce an unnecessary hierarchy. The smallest practical contract is a generic vegetation Resource (identity/Texture2D/ground anchor/spacing/shared template/optional future HarvestProfile) and a lightweight scene controller (apply Resource before AddChild, no idle loop). One definition may optionally use multiple cosmetic images later, but **different plant species MUST remain separately selectable by biome**.

A plant is not necessarily forever noninteractive. PL1–PL3 construct the data-driven scene/biome foundation; PL4 later opts individual definitions into the existing systems for harvesting and drops. No second plant-only Harvestable or Drops implementation.

## How the biome selects plants

Inside WORLD/Biomes/Definitions/OakForest/Biome.tres, add eight separate inline BiomePopulation subresources, each with a different Content reference:

~~~text
OakForest/Biome.tres
  Populations:
    Oak.tres                  existing
    Rock.tres                 existing
    Grass.tres                existing
    Plant001.tres             new: own count, streamed density, scale
    Plant002.tres             new: own count, streamed density, scale
    Plant003.tres             new: own count, streamed density, scale
    Plant004.tres             new: own count, streamed density, scale
    Plant005.tres             new: own count, streamed density, scale
    Plant006.tres             new: own count, streamed density, scale
    Plant007.tres             new: own count, streamed density, scale
    Plant008.tres             new: own count, streamed density, scale
~~~

Do NOT reference the new definitions from PinkMeadow/Biome.tres. When PineForest/Biome.tres exists, it can reference Plant001.tres (same exact Resource file) plus some other subset, choosing a new density for that biome. A volcano's dead plant definition only appears in the volcanic biome if the volcanic biome explicitly references it. Plant file names or folder layout never encode a hard restriction to one biome.

If each plant has its own harvested items/drops, each PlantXXX.tres can later have its own HarvestProfile/DropTable reference. If several plants share identical yield rules, they can reference one shared profile to avoid unnecessary Resources.

## PL0 — audit confirmation

**Objective:** trace current generation, plant artwork setup, biome population contract and save constraints. **Completed at documentation level:** current class/scene structure, density validator, H1–H3 harvesting state, wind/snow source, existing shared folders. **Still verify before coding:** actual image dimensions, transparent padding and rooted display anchors, old-save behavior and present HEAD. **Preserve:** original grass/trees/rocks/biome composition unless necessary to accommodate plant densities. **Pass:** eight unique IDs and total population density are established. **Assistant:** Regular Chat; Codex rechecks.

## PL1 — single scene, reusable visual definition

**Objective:** all plant species instantiate one Plant.tscn, configured by their respective definitions, not by separate scenes.

**Dependencies:** SceneryDefinition and both placement paths; generic tree precedent; existing Vegetation/Shared environment adapters.

**Implementation:** add smallest reusable visual definition/controller, ideally using a shared VegetationDefinition : SceneryDefinition with Texture2D, ground anchor or bottom-centre fallback, optional shared wind material, optional prototype/template PackedScene reference and existing inherited spacing/bounds. VegetationInstance : Node2D (or equivalent) configures its Sprite2D once before adding to the world, honoring scale/mirror, transparent padding and root orientation. Keep one Plant.tscn with shared node structure; no bespoke Plant001.cs. Only create more Resource classes when actual artwork variations or behavior need them.

**Out of scope:** enabling harvesting/collision, plant growth or new GPU effects. **Preserve:** Tree.tscn, Grass.tres, legacy PackedScene population behavior and existing wind/snow. **Performance:** no per-instance _Process or _PhysicsProcess; reuse materials and cache template references. **Pass:** Plant001.tres and Plant008.tres both instantiate the identical scene with different sprites, stable anchors and unique identities. **Assistant:** Codex.

## PL2 — individual plant definitions and Oak permissions

**Objective:** eight distinct Resources independently controllable by a biome.

**Dependencies:** PL1. **Files:** move all eight root PNGs into Plants/Sprites, create Plant001.tres–Plant008.tres in Plants/Definitions, connect each Texture2D with correct anchor, bounds and unique stable Id (plant.001 etc). Author one BiomePopulation per plant in OakForest/Biome.tres with appropriate per-plant density/scale; adjust existing grass density only as needed to remain within the existing validator. Do not change any Grass scenes, and do not add plants to PinkMeadow.

**Out of scope:** one aggregate Plants.tres, eight plant scenes, changing all biomes or adding harvest-drop mechanics. **Performance:** bounded total density and deterministic distribution. **Pass:** disable an individual plant population and only that specific plant stops appearing; all eight are separately editable in Inspector; no plant is selected outside the Oak biome. **Assistant:** Codex.

## PL3 — production world/streaming, weather and regression

**Objective:** seeded placement and instantiated appearance work in real SCENES/World/World.tscn (F6), not just a test fixture.

**Implementation:** extend minimal definition/template dispatch in ChunkSceneryData.cs (one image per definition or optional true cosmetic variants), WorldChunkStream.cs::ApplyOne and WorldSceneryPlacement.cs. Reuse the existing seeded selection, biome population filtering, stable world-content and instance IDs, spacing, water/feature exclusions, biome-edge check, scaled bounds, Y sort, P3 publication/unload and snow/wind registration. Avoid hardcoded switches on plant name, oak biome name or sprite file.

**Save:** changes to Oak's biome populations change content fingerprint and deterministic candidate outputs. Audit P4 compatibility; do not silently load/regenerate mismatched saved campaigns. **Tests:** Godot import/build, F6 production Oak/Pink comparison, no duplicate/incorrect plant type, fixed seed revisit and unload/reload, compatibility preview, wind/snow/off state, 1920x1080 and target Windows performance; clearly identify manual/unperformed checks. **Performance:** no per-item idle loops, no per-frame folder search, bounded chunk actors/materials; do not claim draw-call performance improvement from simply reducing scene count. **Pass:** one shared Plant scene, eight Resource-authored types, correct Oak eligibility and distinct per-plant density in real World. **Assistant:** Codex.

## PL4 — later shared harvesting and drops (separate permission)

When H4 persistent harvesting/drop state is implemented in the production World, enable selected plant definitions to opt in using existing SYSTEMS/Harvesting/HarvestProfile + Harvestable and SYSTEMS/Drops/DropTable + WorldDropSpawner. A plant could be picked by hand, cut with a sickle, or require a certain tool; each species can have its own Resource settings, or reuse shared profiles. Do not add new harvest logic or pickup scene per species. Use stable instance IDs so harvesting/removal and drops save across chunks; no duplicate rewards on reload. Keep dormant plants dormant. The original plant-only PL1–PL3 pass must not turn on production harvest before this persistence safety check.

## GR0 — later grass conversion (separate permission)

After PL1–PL3 are accepted, migrate existing Grass.tres with its FOUR cosmetic image variants to ONE Grass.tscn using the same shared vegetation definition/controller. This case differs from plants: Grass is currently one content type with four cosmetic textures, so **one Grass.tres holding those four images is correct**. Preserve stable grass content Id, biome populations, no-collision, wind, snow, scaling and save compatibility. Search all references before retiring Tuft01–04.tscn. Do not invent a second vegetation framework. **Not part of the current plants job.**

## Acceptance checklist

- [ ] Exactly ONE Plant.tscn and EIGHT individually selectable PlantXXX.tres Resources, each with its own PNG.
- [ ] OakForest/Biome.tres has eight separately tunable plant BiomePopulation entries; PinkMeadow has zero.
- [ ] Future Pine can share any Oak plant definition directly; a volcanic plant only spawns where its definition is explicitly referenced.
- [ ] One shared scene handles visuals and future harvesting wiring without per-species C# / scenes.
- [ ] Existing Seasonal/, Snow/, Wetness/ systems, Harvesting/ and Drops/ are reused, not duplicated.
- [ ] Grass remains four scenes during initial plants implementation, pending separate GR0.
- [ ] Generator bounds/density/water/reservations, chunk streaming, World F6 and legacy preview still work.
- [ ] P4 old campaign content compatibility is audited; no silent checkpoint corruption.
- [ ] Idle plants have no per-node process loops; document active/sleep/unload behavior and measured vs unmeasured performance.
- [ ] Codex records implementation commit, test results and follow-up; one commit for the authorized PL1–PL3 job.

**Suggested Codex instruction:** Read this file, root README.md and CodePreferences.md, then implement only PL1–PL3 as one production World vertical slice. Use ONE Plant.tscn shared by EIGHT independent Plant001.tres–Plant008.tres (each with its own PNG). Register eight separate BiomePopulation entries in OakForest/Biome.tres, none in PinkMeadow. Maintain extensibility for future Pine/Volcano definitions and existing shared Harvesting/Drops. Keep grass Tuft scenes unchanged; no production harvesting until H4. Respect density, stable IDs, P3, P4 save compatibility and performance requirements; test and commit once.
