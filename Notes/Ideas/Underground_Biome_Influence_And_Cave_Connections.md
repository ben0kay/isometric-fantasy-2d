# Underground Biome Influence & Connected Cave Networks

> **Status:** Agreed high-level design direction, recorded for future implementation (2026-10-21).
> **Project:** Fully top-down 2D fantasy survival sandbox in Godot/C#.
> **Implementation:** Planning only. No production or DEBUG code should be changed by this note.

## The central rule — IMPORTANT

**Underground contents depend on BOTH the underground cave biome and the surface biome geographically above that part of the cave.** Underground depth and other local conditions can modify the result further.

The cave biome describes what the underground environment *is* (ravine, limestone cave, crystal cavern, etc.). The overworld biome contributes contextual spawn/resource rules. These are independent dimensions; do **not** make separate copies such as `OakForestRavine`, `DesertRavine`, etc.

### Canonical example: emeralds

- **Surface: Oak Forest. Underground: Ravine Cave.** Emerald deposits MAY spawn because the location satisfies the Oak Forest surface condition.
- **Surface: Desert. Underground: Ravine Cave.** Emerald deposits do NOT spawn because the location lacks the required Oak Forest surface condition.
- Both are the SAME `Ravine Cave` biome, which can otherwise share the same terrain/layout rules.
- Additional requirements (e.g. permitted depth, deposit frequency, geology) may apply; eligibility does not guarantee that an emerald actually spawns.

The emerald/Oak Forest relationship is a **concrete desired example**. Actual spawn chances and depths remain to be decided.

## A cave can cross beneath multiple surface biomes

This is **critical** to the concept. A player may enter a cave in an Oak Forest, travel far underground and exit through a second entrance in a Desert or Mountains.

**Do not assign the entire cave the biome of its first entrance.** Evaluate the surface-biome influence **at the underground position being generated**.

Illustration:

```text
SURFACE (depth 0)
Oak Forest  --------------------------- Desert
Entrance A                                  Exit B
    |                                         |
    v                                         ^
UNDERGROUND
Ravine Cave (emeralds permitted) -- Ravine Cave (no emeralds)
                one connected underground network
```

If the cave extends from under the Oak Forest into the Desert, the emerald rule changes when the surface influence changes. An organic transition zone near the boundary is acceptable; resources must remain consistent with their eligible locations.

## Geographic alignment, despite separate 2D scenes

Prefer a shared **logical horizontal world-coordinate system** for the surface and underground layers. A cave scene may use its own local tile/sprite coordinates and be loaded separately, but its underground positions must map deterministically to surface-world positions (e.g. shared X/Y with a layer/depth identifier or a stable coordinate transform).

This lets a generator answer: `What surface biome is above underground world position (X, Y, depth)?` **without loading/rendering the surface scene**.

The mapping does not demand identical visual detail or identical playable tunnel distances to the overworld. However, exits, resource influences and geographical relationships must be consistent and stable. Avoid purely abstract, unrelated underground coordinates if they prevent local surface-biome lookup.

## Multiple entrances and exits

A cave network may have two or more surface entrances/exits, possibly in different biomes or many chunks apart.

Use a **central, persistent world connection registry** with stable identifiers, not direct references from one scene to another.

Each entrance or internal stair/shaft transition should specify:
- Stable connection/portal ID
- Source environment ID, depth and location
- Target environment ID, depth and arrival location
- Return connection/portal ID where two-way traversal is possible
- Any restrictions (one-way, blocked, unlock conditions) if added later

Example: `Surface_Oak_A -> Cave_041_A` and `Cave_041_A -> Surface_Oak_A`; `Surface_Desert_B -> Cave_041_B` and `Cave_041_B -> Surface_Desert_B`.

A player walking into Entrance A can eventually emerge at Exit B, in a distant surface biome, and re-enter through B to the correct portion of the same persistent cave. **Never simply return to whichever entrance they originally used.**

Procedural generation should establish/resolve entrance locations and connections at the macro level before individual distant surface chunks are rendered. Validate/reserve usable terrain at exits to prevent portals embedded in walls, underwater, or inside unrelated structures.

## Data-driven underground content rules

Resources, plants, structures and species should be defined once, then selected through reusable conditions:

- Allowed / excluded **surface biomes** or biome tags
- Allowed / excluded **cave biomes** or cave tags
- Minimum/maximum depth or geological band
- Optional geological, temperature, moisture, corruption or fantasy modifiers
- Spawn weight/chance, density, cluster/vein shape and rarity

Conceptual example (NOT final .tres schema):

```yaml
Resource: Emerald
AllowedSurfaceBiomes: [OakForest]
AllowedCaveBiomes: [RavineCave, CrystalCavern]
DepthRange: [2, 5]
SpawnWeight: 0.08  # illustrative only
```

The same multi-factor approach may eventually govern underground enemies, cave plants, special features and loot. Keep it compatible with a future sci-fi/alien world, but **focus on medieval fantasy first**.

## Underground depth/layers

The surface is logical depth 0. Cave floors or regions can occupy depth 1, 2, 3, ... with distinct cave biomes, creatures, ores and threats. Passages, shafts and stairs link depths using the same connection model.

At every depth, content may consider the surface biome **plus** the underground cave biome at that position, not just depth or the entrance used. A single cave network can contain multiple cave biomes.

Cave floors can be separate 2D scenes or chunks; the player and camera can remain the same across a faded scene transition.

## Determinism, updates and performance

- Given the same world seed, cave ID, coordinate, biome configuration version and saved edits, generation should give stable results.
- Cache or query the **coarse surface-biome map**, not instantiate the full overworld to decide underground resources.
- Preserve stable cave/portal IDs, multiple exits, mined deposits, player buildings and other edits across saves.
- Plan for content/generator versioning when new surface or cave biomes are added.
- Generate/load only active cave areas in detail; keep distant underground networks as lightweight metadata.
- Validate connections across procedural chunk borders and ensure entrances/exits remain reachable.

## Future DEBUG proof of concept (not yet authorized implementation)

Build only a tiny isolated test when ready:
1. Surface map with adjacent **Oak Forest** and **Desert** regions.
2. Two surface entrances, A (Oak) and B (Desert), connected to one Ravine Cave network.
3. Cave sections are logically aligned to the surface: emeralds are eligible **only under Oak**, despite the identical Ravine Cave biome on both sides.
4. Travel A -> underground -> B -> re-enter at B -> underground -> A without incorrect teleporting.
5. Add a second cave depth and confirm depth requirements work.
6. Revisit after leaving and verify stable seeds, entrances and mined/resource state.
7. Keep prototype confined to DEBUG until deliberately promoted; do not silently add production code.

## Decisions fixed vs still open

**Agreed concept**
- Surface biome and cave biome independently determine underground content.
- Geographical location above a cave section matters, not merely the entrance biome.
- Cave networks can connect distant surface entrances/exits.
- Depth/layer contributes additional generation conditions.
- No need for per-surface-biome copies of each cave biome.

**Implementation choices still open**
- How cave world coordinates map to surface coordinates (shared grid versus deterministic offsets/transforms).
- The exact entrance-network topology generator and how macro connections are reserved.
- How sharply surface influence changes at biome boundaries.
- Per-resource spawn weights, eligible depths, geological modifiers and underground biome catalogue.
- Cave streaming, scene boundaries, save formats and whether all layers are contiguous.

## Existing repository homes (currently scaffolding)

- `WORLD/Biomes/` — surface biome definitions and influence profiles.
- `WORLD/Caves/` — cave biome and depth definitions, entrances/features.
- `WORLD/Generation/Caves/` — layout, content selection and connection generation.
- `WORLD/Runtime/` — chunk lifecycle and persistence.
- `WORLD/Locations/` — special authored ruins, mines, dungeons, etc.
- `DEBUG/` — disposable validation scenes before implementation is promoted.

**One-sentence rule:** "A Ravine Cave underneath Oak Forest can have emeralds; the same Ravine Cave underneath Desert does not — even when the two sections are part of one connected, multi-entrance cave network."
