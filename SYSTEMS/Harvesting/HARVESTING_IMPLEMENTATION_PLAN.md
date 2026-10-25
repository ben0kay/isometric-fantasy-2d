# Harvesting, World Drops & Tree Felling — Implementation Plan

> **Status: H0 audit complete; H1–H3 implemented in disposable practice, awaiting Windows visual/performance acceptance; H4/H5 planned — 24 October 2026.** This file tracks approved implementation and remaining separately scoped work. See [H0 audit](H0_Audit.md) and [H0–H3 validation](H0_H3_Validation.md). Start with the root `README.md`, `CodePreferences.md`, `SYSTEMS/Harvesting/README.md`, `SYSTEMS/Inventory/README.md`, `SYSTEMS/Interaction/README.md`, `WORLD/Content/Vegetation/Trees/README.md`, `WORLD/Runtime/Streaming/README.md`, `SAVE/README.md`, and the **current source** for the touched scene/scripts. Preserve the author's simple, modular, data-driven and performance-first approach; one commit per authorized job.

## Goal

A player strikes an Oak with an axe. After **about four confirmed valid chops** (illustrative tuning), the Oak falls sideways away from the incoming hit direction, optionally leaves a stump, and **3–5 Wooden Logs** appear as real collectible world items. The logs scatter a short distance, hop vertically with a ground shadow, rotate to slight random landing angles and settle. The player collects them into the **existing inventory storage**; any items that cannot fit remain in the world. Later the **same drop/collection foundation** serves rocks, ore, bushes, enemy deaths, chests and crafted outputs. No new per-species code branches.

Keep **harvesting**, **reward selection**, **physical pickups** and **tree-only falling presentation** distinct. The world pickup is not a damage particle, and an enemy must not inherit tree-felling just to drop loot.

## Phase overview

| Phase | Goal | Recommended assistant | Status |
| --- | --- | --- | --- |
| **H0** | Audit confirmed hit path, tree lifecycle, streaming/save ownership; design shared drop contracts and a safe standalone test | Regular Chat for design; Codex if deep code audit needed | Complete: source audit |
| **H1** | Implement reusable physical dropped items, scatter/hop presentation and player collection in an isolated fixture | Codex | Implemented; visual/performance acceptance pending |
| **H2** | Implement reusable harvest-work/progress and Resource-driven reward definitions; test with a generic harvestable | Codex | Implemented; visual/performance acceptance pending |
| **H3** | Attach shared harvesting to Oak/Blossom-capable `Tree.tscn`, animate fall and spawn Wooden Logs in an isolated/disposable world test | Codex | Implemented; visual/performance acceptance pending |
| **H4** | Integrate tree state and live/uncollected pickups with P3 chunk lifecycle + P4 campaign saves; enable production loop safely | Codex | Planned |
| **H5** | Optional expansion to rocks, plants, enemy loot, containers and other harvestables | Regular Chat to plan; Codex to integrate | Planned |

**Critical release gate:** H1–H3 may be validated in disposable practice/F6 fixtures, but **do not enable destructible trees that spawn drops in saved campaigns until H4 persistence is complete**. Design stable identities and unload behavior in H0/H1 so H4 is an extension, not a rewrite.

## Pre-H0 foundations (historical audit; source inspected 24 October 2026)

- `SYSTEMS/Harvesting/` contains only `README.md`: harvesting logic is **not implemented**. `SYSTEMS/Interaction/` similarly documents future reusable interactions.
- `SYSTEMS/Inventory/InventoryStorage.cs` implements catalog-backed bounded stack storage, `TryAdd`, `TryRemove`, `TryMove`, `Get`, `Changed` and on-demand weight. **TryAdd is all-or-nothing** for the requested quantity; a successful add must precede deleting the world pickup. `InventoryHotbar` is a separate shared model; neither UI nor physical drops should own another inventory implementation.
- `ITEMS/Definitions/ItemDefinition.cs` exposes stable `Id`, `Icon`, `MaxStack` and `WeightKg`; `ItemCatalog` validates the canonical definitions. `ITEMS/Resources/Wood/WoodenLog/WoodenLog.tres` exists with `Id = "wooden_log"`, `MaxStack = 40` and `WeightKg = 1`. The practice fixture uses the shared storage; normal campaign inventory/save ownership is still a separate integration.
- Both Oak and Pink Blossom production trees use `WORLD/Content/Vegetation/Trees/Shared/Tree.tscn`, `Tree.cs` and `TreeDefinition.cs`. The species Resource owns existing variants, leaf colour, hit response, fire and wind data. **Do not create an Oak-only Harvest script or duplicate tree scene.**
- `TreeHitResponse.cs` currently implements `IToolHitTarget.ReceiveHit(Vector2 direction)`, restarts wobble/leaves and increments `ReceivedHits` for accepted calls. `ToolHitQuery.cs` chooses the nearest eligible tool target by reach/angle and invokes `ReceiveHit`; it **does not currently communicate tool type or work strength**. The harvest implementation must extend or bridge the authoritative confirmed-hit contract carefully rather than counting every visual wobble or arbitrary contact as one valid axe chop.
- The tree root is `StaticBody2D`; `Sprite`, `TrunkFootprint`, `HitResponse`, `Burnable`, `FireAppearance` and local-light occluder are existing children. `TreeVisualVariant.SourceAnchor` already maps texture pixels to the trunk base; the fall animation must respect that anchor and must not rotate the entire collision/physics root.
- P3 owns seeded scenery with stable world-content IDs, per-chunk scene lifecycles and removed-object deltas. P4 persists touched chunk edits/campaign world identity; **it does not yet guarantee saving free-standing pickup items or intermediate harvest work**. Future saved drop records must be designed explicitly with SAVE. F6 direct sessions are disposable.
- The scene has `WorldActorsRoot` with `PersistentEntitiesRoot`, `PersistentPropsRoot` and `ChunkActorsRoot`; the exact parent/lifecycle for dropped world objects must be chosen during H0. Do not attach all pickups to one permanent always-loaded global node.

## Proposed folder structure — create incrementally, not all at once

```text
SYSTEMS/
├── Harvesting/                           # EXISTS: owns generic extraction/progress
│   ├── README.md                         # EXISTS
│   ├── HARVESTING_IMPLEMENTATION_PLAN.md # THIS FILE
│   ├── Harvestable.cs                    # PROPOSED: work state, completion event
│   └── HarvestProfile.cs                 # PROPOSED: work, tool/strength settings
│
├── Drops/                                # PROPOSED: generic rewards + world pickups
│   ├── README.md                         # add when folder is created
│   ├── DropTable.cs                      # PROPOSED: reusable reward entries
│   ├── DropEntry.cs                      # PROPOSED: ItemDefinition, range, chance
│   ├── WorldDropSpawner.cs               # PROPOSED: one world-scoped coordinator
│   ├── DroppedItem.tscn                   # PROPOSED: shared pickup presentation
│   ├── DroppedItem.cs                     # PROPOSED: item+quantity, collection, lifecycle
│   └── DropScatterProfile.cs             # PROPOSED: reusable arcade hop settings
│
├── Inventory/                            # EXISTS: canonical storage/hotbar backend
└── Interaction/                          # EXISTS: shared interaction rules

WORLD/Content/Vegetation/Trees/
├── Shared/
│   ├── Tree.tscn                         # EXISTS; add/compose harvest capability here
│   ├── Tree.cs                           # EXISTS
│   ├── TreeDefinition.cs                 # EXISTS; optional harvest/reward profile refs
│   ├── HitResponse/                      # EXISTS; preserve wobble/leaf behavior
│   └── TreeFall.cs                       # PROPOSED: tree-only falling state/presentation
├── Oak/Oak.tres                          # EXISTS; configure inline/reusable harvest data
└── PinkBlossom/                          # EXISTS; unchanged until configured

ITEMS/Resources/Wood/WoodenLog/
├── WoodenLog.tres                        # EXISTS; canonical reward definition
└── WoodenLog.svg                         # EXISTS; icon/fallback art

ENTITIES/Player/Interaction/              # EXISTS; player pickup request/detection
ENTITIES/Player/Inventory/                # EXISTS; adapter to authoritative storage
SCENES/World/                             # EXISTING world composition, no separate world clone
SAVE/                                     # EXISTING save coordinator and schema/migrations
TESTS/Harvesting/                         # PROPOSED focused headless + visual test fixtures
```

Names are proposed **ownership labels**, not a requirement to create a C# class or .tres for every conceptual boundary. If `DroppedItem` can own its bounded hop state without another helper script, keep it there. Only add `WorldDropSpawner` when coordination requires it; avoid a second inventory or world-streaming manager. Do **not** add speculative `MasterConfig`s on top of existing owning configuration; expose frequently edited shared scatter defaults once in `SYSTEMS/Drops/` if justified.

## Cross-system contracts

### 1. Confirmed hits and harvesting

- A confirmed hit carries **who struck, tool/item category or capability, strength/work contribution, hit direction and target identity** as needed; choose the smallest extension compatible with existing combat and `IToolHitTarget`. Do not treat repeated visual hit callbacks, leaves or attack animation ticks as independent chopping credit.
- A generic `Harvestable` accepts work **only** when enabled and eligible, tracks current vs required work, and emits completion **once**. Repeated hits after completion return no work/rewards.
- Suggested first Oak tuning: `RequiredWork = 4`, valid axe hit contributes `1`; configurable per definition. No requirement to expose a separate hitpoints/damage system or invent an unneeded harvesting stat framework.
- Preserve ordinary TreeHitResponse wobble/leaf burst on nonfinal hits; the final chop transitions to exclusive falling behavior. Nonharvestable objects keep current hit behavior.
- Fire/burn/dead/removed states need an explicit precedence decision. Do not let an already-felled, incinerated or unloaded tree accept another reward-generating hit.

### 2. Reward data and item ownership

- `DropTable` has entries referring to **canonical `ItemDefinition`** Resources, with minimum/maximum quantity and optional independent chances/weights. Example: Wooden Log **3–5 total units** after one Oak felling; choose whether this becomes a few separate visible pickups or a single stack according to visual and performance budgets.
- Seed reward rolls by stable **world/harvest source ID and completion event**, not global random state or Node instance ID. Store the final selected rewards/identities so revisit/reload cannot reroll or multiply them.
- Do not embed runtime counts, roll seeds, depleted flags or collection state inside shared `.tres` definitions. Those belong to instance/world records.
- Support an optional separate `WorldSprite` (or a future visual profile) on the canonical item definition; initially `Icon` may be used as fallback. Keep item data and scene artwork separate.

### 3. Arcade-style physical drops

- World pickups are **actual collectible item stacks**, not ephemeral feedback particles. One reusable `DroppedItem.tscn` displays icon/world sprite, a stable ground shadow and a visual item Sprite whose Y offset follows a brief hop arc (e.g. parabola); optional tumble and slightly randomized landing angle. The physics/collection root stays at **ground position**, respecting top-down coordinate space and Y sorting.
- Spawn at/near the tree's grounded trunk anchor, scatter along deterministic bounded offsets and test for valid landing positions (water, obstacles, navigation, loaded chunk boundaries). Avoid landing inside trunks or uncollectible scenery.
- Expose a shared `DropScatterProfile`: horizontal spread, initial/upward hop height, duration, spin/landing angle variance, and optional settle/fade presentation. These are **cosmetic**, independent of the item quantities.
- Animate only while airborne/settling; when settled disable `_Process`/`_PhysicsProcess`, redraw and per-item material updates. Reuse simple shared art/materials; cap burst quantity and world pickup count; coalesce rewards into stacks when appropriate.

### 4. Collection and inventory

- Only an eligible player interaction or overlap requests pickup; prefer existing player interaction/Area2D events and bounded nearby queries over one physics poll per idle dropped item.
- On collection, request `InventoryStorage.TryAdd(item,count)` from the **authoritative player inventory instance**. If `false`, the pickup remains unchanged. On `true`, mark the world pickup consumed exactly once, record its removal and remove the scene instance. This is separate from cosmetic landing.
- Existing `TryAdd` is all-or-nothing: **do not delete the object after a partially accepted amount**; partial collection is a later explicit inventory API if wanted. Reject out-of-range/empty/corrupt quantities and missing catalog IDs.
- UI inventory/hotbar observe existing storage change events. No UI code decides world pickup rewards, and no grant happens on every save/load or every scene entry.

### 5. Streaming and persistence — design from H0, finish before production use

- Distinguish **source state** (intact/progress/felled/burned/removed) from **drop state** (identity, item ID, quantity, settled position/visual state, collected flag). Stable source ID should reuse the generated candidate ID when available; drop IDs derive from `WorldId + sourceId + rewardIndex` (or equivalent stable key).
- The source's felling transition and its chosen rewards form **one logical event**. Ensure retries, chunk unload, death, travel away during fall and save mid-animation cannot regenerate rewards or lose existing pickups. Record durable logical outcome before scene visuals or reconcile atomically at save boundaries.
- Existing P3 removed-prop tombstones **alone are insufficient**: a removed tree with no saved drops silently loses timber; a restored drop without a felled source duplicates it. Extend established P4 SAVE owner/schema carefully; choose one authoritative snapshot representation, with tests and old-campaign compatibility strategy.
- Keep the logical fall outcome deterministic; saving an in-progress **visual animation** is optional, but reload must resolve to an intact/progress or **felled + exactly one set of remaining rewards** state with no ambiguity.
- Stream pickups by settled position/owner chunk and unload them with that chunk; preserve their logical records in bounded world/region deltas, not per-pickup always-on nodes. Handle pickup landing across chunk boundaries and player collecting during or immediately after the hop.
- Fire, wind, wet foliage, shadows, snow, occlusion, collision/nav and tree visual registries must unregister/update correctly when a tree stops standing. Existing world/campaign content fingerprint, generator version and save migration contracts remain authoritative.

## Detailed phase handoff

### H0 — audit, data and safety contract
**Objective/dependencies:** trace confirmed axe hit through PlayerInput/attack into ToolHitQuery/TreeHitResponse, shared Tree definition/variant, P3 candidate/removal identity, player inventory ownership, P4 SAVE hooks and world root lifecycles. Check whether the current combat hit already exposes tool class/attack contact elsewhere before widening `IToolHitTarget`.

**Scope and files:** review above modules, decide minimal work/context struct or adapter, reward table and drop identity, pickup scene parent/stream lifecycle, where new save delta state belongs, fixed vs generated tree behavior, old-save compatibility and interaction key/range.

**Out of scope:** new mechanics or visuals. **Preserve:** existing wobble/leaves, active axes/combat, fire behavior, streamed world performance. **Config:** choose embedded/reused profiles, no duplicate per-tree resources. **Performance:** identify event-driven wake/sleep and bounded drop counts. **Pass:** precise interface/ownership notes and a test fixture approach that will not pollute production saves. **Assistant:** Regular Chat for audit notes, Codex for complex source tracing.

### H1 — shared drops and collecting (isolated fixture)
**Objective:** manually spawn a few `WoodenLog` pickups (using the existing catalog) in a dedicated practice fixture, hop/spin/land, and collect into the real shared `InventoryStorage`.

**Scope and files:** `SYSTEMS/Drops/`, `TESTS/Harvesting/`, targeted `ENTITIES/Player/Interaction/` adapter and existing inventory fixture hookup where possible. Reuse ItemDefinition icons before new sprites. Do not wire tree felling yet.

**Out of scope:** tree hits, permanent campaign pickups, full item UI, enemy drops, partial stack acceptance. **Preserve:** existing inventory event/stack behavior. **Performance:** pickup animates only during flight; settled is dormant, no global polling. **Pass:** varied but reproducible scatter, rooted shadow, clean idle callbacks, successful collection increases count exactly once, full/invalid inventory leaves item, repeated input cannot duplicate, small-window visual check. **Assistant:** Codex; local Godot/Copilot validation.

### H2 — generic harvesting progress and reward plans
**Objective:** reuse harvesting work for trees, ore and bushes later without branching by species.

**Scope:** `SYSTEMS/Harvesting/Harvestable.cs`, `HarvestProfile.cs`, `SYSTEMS/Drops/DropTable.cs`/`DropEntry.cs` if not added in H1, minimal confirmed-hit adapter and focused tests. Start with a disposable generic harvest target, not a new species scene.

**Out of scope:** tree fall visuals, real world persistent extraction, skill XP or crafting. **Preserve:** current attacks and TreeHitResponse hit registration; only valid configured tool hits add work. **Performance:** idle harvestable has no ticking callback; processes only on accepted hit. **Pass:** four eligible hits complete example harvest exactly once, wrong tools do not advance, resource changes affect authored definitions without new classes, independent deterministic reward outcome. **Assistant:** Codex.

### H3 — tree felling vertical slice (disposable/playtest world only)
**Objective:** Oak can take repeated axe chops, fall away from hit, and scatter 3–5 log units that the player collects. One shared tree scene supports any future species with optional configuration.

**Scope:** add optional harvest settings to `TreeDefinition` (prefer inline Resource with shared fallback), compose generic Harvestable with `Tree.tscn`/`Tree.cs`, add tree-owned `TreeFall.cs` presentation and configured rewards; connect it to H1 pickup service.

**Out of scope:** production campaign release before H4, second tree species-specific behavior, full tree stump/regrowth lifecycle unless explicitly approved. **Preserve:** trunk-anchored art, mirrored variants, hit wobble/leaves, fire, snow, shadows, wind, occlusion, collision/Y-sort and nonharvest trees. **Performance:** fall begins once, visual animator sleeps when complete, disable standing-tree effects/registrations cleanly, no additional healthy-tree physics loops. **Pass:** repeated chops correctly count, falling direction reacts to hit, no invisible sprite clipping/offset, leaves don't fight rotation, log landing reads well, collision/nav correct after fall, no rewards duplicated. **Assistant:** Codex; user visual acceptance of feel.

### H4 — chunk streaming, save/restore and campaign integration
**Objective:** make harvested trees and uncollected pickups persist correctly during chunk unload/reload, travel and campaign Continue, including save during scatter/fall.

**Scope:** extend P3/P4's **existing** scene-state/delta/save contract for source progress/felled outcome and independent pickup records; restore logical state before rendering or allowing further hits; version and migration strategy; durable player inventory connection if collection is offered in production; bounded chunk drop materialization/unload and collision/nav updates.

**Out of scope:** new separate global save manager, offscreen pickup physics, complete item economy. **Preserve:** existing valid campaigns and scene ownership; never double-grant logs after Continue. **Performance:** save snapshots aggregate changed records at save boundaries, not every frame; offscreen items are records rather than Nodes. **Pass:** multiple chunks, leave/return, save/reload before completion, mid-fall, after collection, full inventory, source removed/felled/ablaze, legacy saved worlds and content validation. **Assistant:** Codex, with Copilot local save regression tests. **Only after this phase is accepted enable in production campaigns.**

### H5 — optional reuse and expansion
**Objective:** confirm shared contract works for mining, foraging, enemy death loot and containers without copying drop code.

**Scope:** adapt existing individual content definition/interaction owners to shared DropTable and WorldDropSpawner. Rock deposits use extraction behavior; enemies use death event and bypass Harvestable; chests use interaction opening and shared inventory transfer.

**Out of scope:** unrelated AI/population/crafting implementation, wide content pass before first Oak playtest. **Preserve:** existing scene/item folder ownership. **Performance:** bounded event-driven calls, pooled effects where justified; no new idle update scripts. **Pass:** one new harvestable and one nonharvest reward source use identical pickup/collection logic; saved records remain unique. **Assistant:** Regular Chat design + Codex implementation only when requested.

## Example inspector setup (proposed, not existing fields)

```text
Oak.tres  (TreeDefinition)
  Harvesting [optional embedded HarvestProfile]
    Enabled = true
    RequiredWork = 4
    AcceptedTool = Axe                 # confirm actual item/tool-category representation
    FallDurationSeconds = ...          # may be tree fall profile or shared default
    Rewards [shared DropTable]
      Item = WoodenLog.tres
      Minimum = 3
      Maximum = 5

Shared DropScatterProfile  (one profile for most pickups)
  ScatterRadius = ...
  HopHeight = ...
  FlightSeconds = ...
  RotationVariance = ...
```

These are **illustrative labels**, not already-implemented exports or mandated numeric defaults. Keep `TreeDefinition` small: reference/inline a reusable harvest Resource, and configure fall presentation at the tree owner (one shared default, optional species override). No unnecessary `OakHit.tres`, `OakFall.tres` or per-item script.

## Testing and approval checklist

1. **Hit integrity:** valid tool, confirmed contact, four chops example, no extra credit from hit wobble or attack animation frames; original combat/hit feedback still works.
2. **Presentation:** tree pivots around actual rooted sprite anchor; fall goes away from impact; no wind/wobble/fire/snow/shadow incompatibility or invisible clipping; logs briefly pop, cast grounded shadows, settle at slight angles.
3. **Inventory integrity:** only registered WoodenLog definition, correct count/weight, catalog mismatch rejected, full inventory leaves all logs, double pickup cannot grant twice.
4. **World lifecycle:** independent IDs, cross-chunk scatter, unload mid-flight, leave/return, save/Continue, felled source/remaining pickups consistent; F6 disposable world unchanged on restart.
5. **Performance:** healthy tree and settled item have no idle callbacks, no per-frame world-group scans or new physics body for every static icon, bounded pooled/cached visuals, measured startup/active/idle vs baseline; profile on target Windows renderer.
6. **Regression:** P3 chunk and nav, P4 SAVE (including old checkpoints), fire, snow, wind, generic tree, C2 combat, existing inventory practice tests and producer/consumer signal disconnects.

## Optional ideas — not approved scope

- Hold-to-gather vs repeated chops; tool durability and required tool quality.
- Falling trunk damages enemies or collides briefly, with separate safety/physics design.
- A configurable stump, regrowth timer and replanting for renewable forests.
- Magnet/auto-pickup radius for nearby materials, with limits and no repeated expensive queries.
- Pooled pickup sprites, optional item sparkle, subtle pickup sound and inventory toast.
- Harvest bonus from character skills, tool rarity, or biome effects without duplicating drop tables.
- Crafting/building consumes the same authoritative inventory items after collection.
- Future rare drops, regional loot modifiers and independent enemy reward tables.

## Implementation workflow

Each approved phase should update this plan's state and relevant owning README after code work, record commit SHA, exact changed files, tests performed vs deferred, and the activation/sleep/scaling contract. Keep **one GitHub commit per authorized job**. H0–H3 were subsequently explicitly authorized and implemented as one job. H4/H5 remain separately authorized work; no production campaign enablement is included.

## Current H0–H3 handoff

Play `TESTS/Harvesting/HarvestingPractice.tscn` with F6. Shared drops, generic work/reward plans and Oak/mirrored Blossom felling are implemented through runtime fixture copies; production species/axe tuning remains unchanged. Four LMB axe chops reserve 3–5 logs, which hop/settle and collect via E into real storage. H0–H3 native checks and existing combat/inventory/UI/tree/fire regressions pass; Windows visuals/profiling remain pending. See [validation](H0_H3_Validation.md) for commands, limits, identity/persistence handoff and exact phase distinction.
