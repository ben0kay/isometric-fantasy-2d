# Data-driven World Content — One Scene per Category, One Definition per Type, Many Art Variants

> **Status: PL0 audited; PL1 implemented, awaiting native validation. PL2/PL3 remain planned.** This is the PLANTS implementation plan and the intended content-authoring contract for TREES, PLANTS, GRASS, ROCKS and future scenery types. The earlier proposal to combine eight *distinct* plants into one Plants.tres is superseded. **Implement PL1–PL3 for plants only when approved; category conversions for grass, rocks or trees are separate requests.** Read root README.md, CodePreferences.md, the owning README files and actual current production code before implementing. One commit per authorized job.

## Project-wide content authoring rule — mandatory design target

**The standard for every world content category is:**

1. **ONE shared `.tscn` per category/behavior family** — one `Plant.tscn` for plant species, one `Tree.tscn` for tree species, one `Grass.tscn` for grass, one `Rock.tscn` for rocks, etc. (Future genuinely different physical behavior can justify an explicit specialized family, not a scene per artwork.)
2. **ONE canonical `.tres` per distinct gameplay/content type** — e.g. `Plant001.tres`, `Plant002.tres`, `DeadVolcanicPlant.tres`, `Oak.tres`, `Pine.tres`, `Boulder.tres`. Each definition owns identity, reusable mechanic settings, and **one or more visual variants**.
3. **ONE OR MORE PNG/texture/atlas regions inside each definition** — **one artwork variant is the default**; optionally author several variants for the **same type**. At spawn time, the seeded generator chooses exactly one visual variant for each world instance. This changes appearance only, not the content type, biome eligibility, loot or definition ID.
4. **Biome.tres chooses types explicitly** — one `BiomePopulation` per allowed content definition, with its own density/scale/mirror chances. An unreferenced definition cannot spawn there. The exact same definition can be referenced by multiple biomes with different densities.
5. **Shared mechanics remain shared** — harvesting/rewards in existing SYSTEMS/Harvesting and SYSTEMS/Drops; weather/wind/snow use existing environment adapters. The .tscn composes these mechanics; the .tres supplies optional behavior settings. No species-specific script or scene.

| Category | Shared scene target | Separate definitions | Artwork *within* each definition |
| --- | --- | --- | --- |
| Plants | `Plant.tscn` | `Plant001.tres`, `Plant002.tres`, `DeadVolcanicPlant.tres` | 1 texture by default, optional several |
| Trees | existing shared `Tree.tscn` | `Oak.tres`, `PinkBlossom.tres`, future `Pine.tres` | already supports `TreeVisualVariant` |
| Grass | future `Grass.tscn` | current `Grass.tres`; new grass species get their own .tres | current 4 tuft sprites become 4 variants of one Grass type |
| Rocks | future shared `Rock.tscn` | current `Rock.tres`; additional distinct rocks get their own .tres | 1 or several texture/atlas variants |

**PNG vs atlas:** A visual variant can point to a normal `Texture2D` or a Godot `AtlasTexture`/equivalent authored region from an atlas. It must select the correct subtexture/region and root anchor, **not choose a whole atlas image as a sprite or treat atlas frames as separate plant species**. Atlas support can use the same texture-compatible slot if Godot's `AtlasTexture` works; add a region descriptor only when actually necessary. Artwork can supply optional variant-specific anchors, scale/bounds, mirror constraints, or selection weights where needed; equal weights and one texture per definition are sane defaults.

**Determinism/performance:** Select a visual-variant index with existing coordinate-seeded world generation, not `Randomize()` or node-instance RNG in `_Ready()`. Preserve the choice across chunk revisit, save/restore and order-of-loading changes. Apply texture and rooted transform **once at instantiation**; no per-instance idle update, extra `.tscn` or `.tres` per cosmetic variant. Keep authoritative identity `world_content_id` tied to the definition, while storing the selected variant separately. Do not force all categories to inherit the same heavy runtime scene tree or tree-specific scripts: **share the data contract and instantiation logic where practical; retain category-appropriate minimal nodes and behaviors.**

**Scope warning:** Trees already substantially follow this pattern. Grass and rocks still use PackedScene variant/individual scene paths today, so they are **future migrations**; this plan records their destination without authorizing their refactors during Plants PL1–PL3.

## The three responsibilities — core design

1. **Plant.tscn**: exactly ONE reusable scene for the plant category. All plant types instantiate this scene, with the same basic node tree and reusable mechanics. No per-image, per-variant or per-biome plant scenes.
2. **One individual .tres per distinct plant type**: its stable ID, display name, **array of one or more visual textures/atlas regions** (one default), rooted anchors, placement and environment settings, and later optional harvest requirements and drops. The first eight separate types are Plant001.tres through Plant008.tres, **each initially with exactly one PNG**, but the PL1 contract must support multiple variants **from day one**. Do NOT combine distinct species in one Plants.tres; extra PNGs of the SAME species belong inside that species' existing .tres.
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
| PL0 | Verify content classes, biome ownership, streamer, assets, save compatibility | Regular Chat | Audited at `3673cb1`; see PL0_PL1_Validation.md |
| PL1 | Reusable one-scene vegetation definition/controller and **1-to-many visual-variant contract** | Codex | Implemented; awaiting native validation |
| PL2 | Move eight artwork files, author EIGHT plant Resources, register EIGHT Oak biome populations | Codex | Planned |
| PL3 | Wire production streamed World and compatibility-preview generation; test wind/snow/performance | Codex | Planned |
| PL4 | Later attach existing Harvesting/Drops capabilities and SAVE integration to plant types | Codex | Deferred; separate approval after H4 |
| GR0 | Later convert grass from four scenes to one Grass.tscn using same reusable setup | Regular Chat for small refactor; Codex if needed | Deferred; separate job |
| RK0 | Later convert rocks to one shared Rock.tscn + separate rock type Resources, each with visual variants | Codex/Regular Chat depending on collider/shadow scope | Deferred; separate job |

Codex may implement PL1–PL3 together as a coherent production feature with one commit when authorized. Do not implement PL4, GR0, RK0 or unrelated tree architecture work in that job.

## Current source state verified 24 October 2026

- New source art: Plant001.png through Plant008.png are currently in the **repository root**. The Plants folder has this plan and its README, but no plant scenes/definitions.
- WORLD/Content/Vegetation/Shared/ already contains Seasonal/, Snow/, Wetness/. They are existing shared adapters. **This planning commit does not add C# files there**; Codex should add only the minimum visual setup classes required.
- Existing grass uses one Grass.tres and four PackedScene Tufts; later GR0 consolidates them into **one Grass.tscn and four cosmetic variants in Grass.tres**. This stays unchanged during plant implementation.
- Existing trees already demonstrate single-template instancing using TreeDefinition, **an array of TreeVisualVariant entries per tree Resource**, and Trees/Shared/Tree.tscn; this is the desired multi-PNG precedent. Do not copy tree-specific fire/collision/falling mechanics to plants.
- Rocks currently use `WORLD/Content/Rocks/Rock.tres` pointing to `Rock01.tscn` and one sprite, with rock collision/light-occluder setup in that scene. A future RK0 moves per-rock artwork/footprint variations into each rock definition and uses **one Rock.tscn** without losing collision/shadow features. Not part of PL1–PL3.
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
│   │   ├── Plant001.tres                 individual definition, VisualVariants=[Plant001.png]
│   │   ├── Plant002.tres                 distinct definition, VisualVariants=[Plant002.png]
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

These shared class names are suggested, not a requirement to introduce an unnecessary hierarchy. The minimum **initial** contract is a generic vegetation Resource (**one-or-many Texture2D or atlas-region variants**, per-variant ground anchors/bounds when needed, spacing, shared template, optional future HarvestProfile) plus a lightweight scene controller (apply the seeded chosen variant before AddChild, no idle loop). Even though each first-generation Oak plant has one PNG, the implementation **must accept multiple PNG variants per plant definition now** without additional class/scene changes. Distinct plant types MUST remain separately selectable by biome.

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

## PL1 — single scene, reusable 1-to-many visual definition

**Objective:** all plant species instantiate one Plant.tscn, configured by their respective definitions, not by separate scenes.

**Dependencies:** SceneryDefinition and both placement paths; generic tree precedent; existing Vegetation/Shared environment adapters.

**Implementation:** add the smallest reusable visual definition/controller, ideally a shared VegetationDefinition : SceneryDefinition with a **nonempty collection of visual variants** (`Texture2D`, including AtlasTexture references where suitable), per-variant rooted anchor/bounds when needed, optional shared wind material, optional template PackedScene reference and inherited placement settings. A definition with one PNG simply has a one-element list. Use a small inline per-variant Resource/class only where per-image anchors/bounds or weights justify it; avoid separate `.tres` files for variants. `VegetationInstance : Node2D` (or equivalent) configures its Sprite2D **once using the generator-selected variant index** before adding it to the world, honoring mirror, size, alpha padding and root orientation. Keep one Plant.tscn with shared node structure; no bespoke Plant001.cs, no per-artwork scene or scene-specific variant script. Spawn randomness lives in the deterministic world planner, not on the instance.

**Out of scope:** enabling harvesting/collision, plant growth or new GPU effects. **Preserve:** Tree.tscn, Grass.tres, legacy PackedScene population behavior and existing wind/snow. **Performance:** no per-instance _Process or _PhysicsProcess; reuse materials and cache template references. **Pass:** Plant001.tres and Plant008.tres instantiate the identical scene with different sprites, stable anchors and unique identities; a temporary PL1 fixture also verifies that **a single plant .tres containing two textures/atlas regions** can instantiate either variant deterministically under different seeded candidate keys **without creating more scenes or plant definitions**. Remove/keep this fixture only as appropriate; don't turn the two art samples into distinct plant types. **Assistant:** Codex.

## PL2 — individual plant definitions and Oak permissions

**Objective:** eight distinct Resources independently controllable by a biome.

**Dependencies:** PL1. **Files:** move all eight root PNGs into Plants/Sprites, create Plant001.tres–Plant008.tres in Plants/Definitions, initialize each definition's **VisualVariants collection with exactly one matching PNG** (ready to append more later), with correct anchor, bounds and unique stable Id (plant.001 etc). Author one BiomePopulation per plant in OakForest/Biome.tres with appropriate per-plant density/scale; adjust existing grass density only as needed to remain within the existing validator. Do not change any Grass scenes, and do not add plants to PinkMeadow.

**Out of scope:** one aggregate Plants.tres, eight plant scenes, changing all biomes or adding harvest-drop mechanics. **Performance:** bounded total density and deterministic distribution. **Pass:** disable an individual plant population and only that specific plant stops appearing; all eight are separately editable in Inspector; no plant is selected outside the Oak biome. **Assistant:** Codex.

## PL3 — production world/streaming, weather and regression

**Objective:** seeded placement and instantiated appearance work in real SCENES/World/World.tscn (F6), not just a test fixture.

**Implementation:** extend minimal definition/template dispatch in ChunkSceneryData.cs (**1-to-many variants per definition supported immediately; choose a deterministic index, with the one-image case selecting index 0**), WorldChunkStream.cs::ApplyOne and WorldSceneryPlacement.cs. Reuse the existing seeded selection, biome population filtering, stable world-content and instance IDs, spacing, water/feature exclusions, biome-edge check, scaled bounds, Y sort, P3 publication/unload and snow/wind registration. Avoid hardcoded switches on plant name, oak biome name or sprite file.

**Save:** changes to Oak's biome populations change content fingerprint and deterministic candidate outputs. Audit P4 compatibility; do not silently load/regenerate mismatched saved campaigns. **Tests:** Godot import/build, F6 production Oak/Pink comparison, no duplicate/incorrect plant type, fixed seed revisit and unload/reload, compatibility preview, wind/snow/off state, 1920x1080 and target Windows performance; clearly identify manual/unperformed checks. **Performance:** no per-item idle loops, no per-frame folder search, bounded chunk actors/materials; do not claim draw-call performance improvement from simply reducing scene count. **Pass:** one shared Plant scene, eight independent Resource-authored types, correct Oak eligibility and distinct per-plant density in real World; deterministic multi-image selection is covered in an explicit fixture even though production Oak definitions initially have one image each. **Assistant:** Codex.

## PL4 — later shared harvesting and drops (separate permission)

When H4 persistent harvesting/drop state is implemented in the production World, enable selected plant definitions to opt in using existing SYSTEMS/Harvesting/HarvestProfile + Harvestable and SYSTEMS/Drops/DropTable + WorldDropSpawner. A plant could be picked by hand, cut with a sickle, or require a certain tool; each species can have its own Resource settings, or reuse shared profiles. Do not add new harvest logic or pickup scene per species. Use stable instance IDs so harvesting/removal and drops save across chunks; no duplicate rewards on reload. Keep dormant plants dormant. The original plant-only PL1–PL3 pass must not turn on production harvest before this persistence safety check.

## Project-wide migration path (future separate jobs; no automatic refactoring)

**Trees:** already have `Tree.tscn` + one species `.tres` + `VisualVariants` arrays, so **do not refactor their mechanics just for naming consistency**. Preserve tree-specific collision, hit responses, fire, fall, snow and rooted art. Only address shared variant API compatibility when genuinely needed.

**Grass (GR0):** migrate the **one current grass content type** to `Grass.tscn`, keeping **one Grass.tres with four PNG variants**. If a future meadow has a different grass type, that distinct type gets its own .tres, even though it also uses Grass.tscn. This migration is user-requested follow-up after PL1–PL3.

**Rocks (RK0):** move the `Rock01.tscn` behavior/physics/light occluder to one reusable `Rock.tscn`, and store texture variants and any **variant-dependent collider, grounded anchor and light occlusion geometry** with each distinct Rock type's .tres (e.g. existing `Rock.tres` and future volcanic rock). Preserve solid collisions and shadows. Different rock species get different definitions but **not scenes**. Auditing existing rock and streaming/save references is required before implementation.

**Everything else:** treat this as an **authoring principle** for content that shares a real behavior family (bushes, mushrooms, ores, props when appropriate), not an order to force unrelated objects into one Godot scene or to create hundreds of speculative base scripts. Data-driven species and optional visual variants, explicit biome permissions, shared mechanics where practical.

## GR0 — later grass conversion (separate permission)

After PL1–PL3 are accepted, migrate existing Grass.tres with its FOUR cosmetic image variants to ONE Grass.tscn using the same shared vegetation definition/controller. This case differs from the **eight distinct initial plant types**, not from the architecture: Grass is currently one content type with four cosmetic textures, so **one Grass.tres holding those four image variants is correct**. Preserve stable grass content Id, biome populations, no-collision, wind, snow, scaling and save compatibility. Search all references before retiring Tuft01–04.tscn. Do not invent a second vegetation framework. **Not part of the current plants job.**

## Acceptance checklist

- [ ] The contract is **one .tscn per content category**, **one .tres per distinct type**, **one or many image/atlas variants inside each .tres**, and explicit per-biome allowed content references.
- [ ] The first PL1 implementation supports `VisualVariants.Count >= 1` with deterministic choice, even though Plant001–Plant008 start with exactly one image per definition.
- [ ] A two-image/atlas-region fixture demonstrates that multiple artworks can live under **one** definition and spawn as variations of **one** species, without more .tres files or scenes.
- [ ] Tree/grass/rock migration boundaries are documented; no premature refactoring outside plants PL1–PL3.

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

**Suggested Codex instruction:** Read this file, root README.md and CodePreferences.md, then implement only PL1–PL3 as one production World vertical slice. Follow the **project-wide content contract: one scene per category, one Resource per distinct species/type, one or multiple textures/atlas regions as variants inside that Resource, deterministic seeded variant selection**. Use ONE Plant.tscn shared by EIGHT independent Plant001.tres–Plant008.tres (each initially with one PNG), and confirm the system supports two or more variants on one definition without extra scenes. Register eight separate BiomePopulation entries in OakForest/Biome.tres, none in PinkMeadow. Maintain extensibility for future Pine/Volcano definitions and existing shared Harvesting/Drops. Keep grass Tuft scenes and rock scenes unchanged (future GR0/RK0); keep existing Tree.tscn as the precedent; no production harvesting until H4. Respect density, stable IDs, P3, P4 save compatibility and performance requirements; test and commit once.

## PL0/PL1 implementation handoff

See [audit, implementation and validation](PL0_PL1_Validation.md). Only PL0/PL1 were authorized. No artwork moves, production plant definitions, biome changes or generator dispatch are included. PL3 must wire the existing candidate sample into SelectVariant, use per-variant bounds/radius, instantiate Template and Configure before AddChild in both placement paths, then reuse environment lifecycle registration.
