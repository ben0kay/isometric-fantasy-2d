# Codex Implementation Plan — Generic Resource-Driven Trees

> **Status: PLANNED, NOT IMPLEMENTED.** This is a handoff specification, not permission to assume any files described as future work already exist.
>
> **Goal:** One reusable, internally managed tree scene/controller for all tree species. Adding a species or sprite variant should require only Resources (.tres), artwork and biome data — **no species-specific scene editing or new C# scripts**. A future external **Avalonia toolbox** must be able to create/edit these definitions, without authoring Godot scenes.
>
> Read repository root `README.md` and `CodePreferences.md` first; inspect **latest** production code, relevant README files, actual DEBUG references and the current FIRE implementation before changes. Follow modular design, performance-friendly shared helpers, comments above methods with separator, appropriate regions, and one commit for this implementation job.

## Problem to solve

At present, `Oak.tscn` and `PinkBlossom.tscn` are separate PackedScenes referenced by `TreeDefinition`'s inherited `SceneryDefinition.Variants`. Shared effects are attached manually. This caused Pink Blossom to initially miss `TreeHitResponse`, and the fire-related `Burnable` / `TreeFireAppearance` components currently live directly inside the Oak scene. Scaling to many species would demand repetitive scene editing, which is incompatible with the intended Avalonia authoring workflow.

Refactor the **tree content path only** so that species configuration becomes Resource-driven; **do not** unnecessarily convert all rocks, decorative grass or general scenery to the new representation.

## Proposed ownership

```text
WORLD/Content/Vegetation/Trees/
├── Shared/
│   ├── Tree.tscn                   # ONE reusable tree template; internal runtime scene
│   ├── Tree.cs                     # instantiate/configure from TreeDefinition and selected variant
│   ├── TreeDefinition.cs           # editable per-species Resource class, existing
│   ├── HitResponse/
│   │   ├── TreeHitResponse.cs      # existing shared hit receiver
│   │   └── TreeHitConfig.cs        # existing tweakable impact/foliage parameters
│   ├── Fire/
│   │   ├── TreeFireAppearance.cs   # existing shared appearance behavior
│   │   └── TreeFireProfile.cs      # existing reusable profile
│   └── Visuals/                    # optional shared support where truly necessary
├── Oak/
│   ├── Oak.tres                    # tree species and variants, no required Oak.tscn
│   ├── OakFireProfile.tres         # charred artwork and matching anchors
│   └── Sprites/
└── PinkBlossom/
    ├── PinkBlossom.tres            # tree species and variants, no required PinkBlossom.tscn
    ├── PinkBlossomHitConfig.tres   # petal/foliage response
    └── Sprites/
```

Paths are targets; consolidate/retain existing working shared code wherever possible. Do not introduce duplicate ownership or unused wrapper layers. Individual resource filenames can remain stable if already referenced.

## Scene and resource contract

The **single generic** `Shared/Tree.tscn` is a `StaticBody2D` with consistent child paths for existing consumers:

```text
Tree (StaticBody2D)
├── Sprite (Sprite2D)
├── TrunkFootprint (CollisionPolygon2D)
├── HitResponse (shared)
├── Burnable (shared existing fire simulation)
└── FireAppearance (shared)
```

- Keep optional modules from breaking trees when no species-specific asset is supplied.
- `TreeDefinition` must expose a stable ID/name, **one or more visual variants** and their texture, sprite offset/scale/anchor and corresponding trunk-collision footprint. Preserve existing spacing, footprint, environment metadata, wind/snow/shadow parameters, visual bounds and biome restrictions.
- Include per-tree hit-response config (wobble, leaf/petal colour/count), wind/sway profile, burnable/fire profile and **optional** charred texture/anchor. Use Resources rather than C# switches on named species.
- If multiple artwork variants have different visible roots or silhouettes, keep per-variant sprite dimensions/footprints/anchors; placement and hit detection must use the *selected* variant's transformed footprint.
- Species definitions are **data**. Biomes reference them, with count/weights/density and placement constraints; do not put duplicate Oak/Pink assets inside biome folders.
- A tree's **species and chosen variant** should remain stable under the world's deterministic seed. Avoid changing output randomly on scene reload from an unrelated RNG.
- Any scene-derived IDs/paths in general scenery definitions should not be accidentally assumed to exist for the generic tree; preserve existing interfaces or add a narrowly scoped tree-generation branch.

## Current systems that MUST continue working

| System | Preservation/validation requirement |
| --- | --- |
| Biome generation | Oak Forest and Pink Meadow still populate from their biome definitions; deterministic seeds, spacing, transitions, foot exclusion, world-liquid exclusion, size variation and horizontal mirroring remain correct. |
| Collision | Tree trunk footprints remain solid and match actual selected sprite art after mirroring/scaling. Only sprite changes for a hit wobble, not physics body. |
| Axe/tool hits | Centralized input stays exclusively in `ENTITIES/Player/Input/PlayerInput.cs`; generic shared `TreeHitResponse` registers all configured trees for the existing tool-hit query. |
| Hit foliage | Oak drops appropriate leaves, Pink Blossom drops pink petals; no per-species hit script. Configurable count/colour/strength and leafless cases. |
| Wind | Existing shared production wind simulation, shared GPU sway, orientation/mirror handling and species amplitude settings retained. Do not add per-tree idle `_Process` just for wind. |
| Snow | Existing vegetation snow opt-in/material adapter works on generic trees (including Pink Blossom foliage profile), with coverage, reset and visual restoration unchanged. |
| Lighting/shadows | Projected sun/contact shadows and color grading still work with generic tree sprites and transforms. |
| Fire ignition/spread | Existing production `Burnable`, fire manager, fire spread and fire visuals operate on **all opted-in tree species**, not Oak specifically; do not change fire simulation semantics while extracting. |
| Charred variant | Oak retains existing charred sprite, alignment, FIRE1/FIRE2 transition and material/wind/snow compatibility. |
| No charred sprite yet | Pink Blossom **still ignites, burns, spreads fire and shows shared flame effects** without a custom charred sprite; no Oak texture substitution. Its appearance transition may be absent until art exists. |
| Lifecycle/performance | Initialization order enables wind/snow/shadow/fire caches to discover trees *after* configuration; runtime registration will be needed later for dynamic spawn/despawn/streaming. Idle tree hit receivers should not continuously process. |

## Fire-specific requirements

Current Oak scene explicitly wires `SYSTEMS/WorldInteractions/Fire/Simulation/Burnable.cs` and `WORLD/Content/Vegetation/Trees/Shared/Fire/TreeFireAppearance.cs`, with `OakFireProfile.tres`. Make this a generic opt-in capability configured through the tree definition, preserving shared fire simulation and flame silhouette behavior. Do not add species-name tests like `if (tree.Id == "oak")`.

**Separate flammability from optional burnt artwork:** `CanBurn`/burn settings control ignition, burn progress, spreading and damage/heat hooks, while `BurntTexture` controls a *visual* charred transition. The current appearance code returns early when `BurntTexture` is null; ensure this does **not** block the simulated burn or extinguish the shared flames. It is acceptable for Pink Blossom to keep its original sprite until charred art is supplied. Do not invent burned Pink Blossom art.

Audit existing fire consumers, type checks, metadata, source material caches and registration logic for hardcoded `Oak`/single-species references. Keep all existing production fire effects intact.

## Suggested implementation sequence

1. Inspect current `TreeDefinition.cs`, base `SceneryDefinition`, both tree scenes/resources, `WorldSceneryPlacement.cs`, production Wind/Snow/DayNight/shadow adapters, `TreeHitResponse.cs`, fire manager/Burnable/TreeFireAppearance and relevant README documentation.
2. Design minimal Resource types for species and optional per-variant data (sprite texture, offsets, scale, shape/anchors, presentation profiles). Minimize serialization changes and keep definitions friendly to the eventual Avalonia editor.
3. Build/configure the one shared tree scene and controller. Set up **all** shared modules based on Resource capabilities without hardcoded species.
4. Migrate Oak and Pink Blossom Resources to configure the generic scene with their existing sprites, collision footprints, hit feedback, wind, snow, shadows and fire settings.
5. Update tree spawning in `WORLD/Generation/Placement/` while preserving the existing `SceneryDefinition` path for non-tree content. Apply seeded variant selection, mirroring and scale consistently.
6. Validate initialization and metadata registration with production environment/fire systems, and confirm safe optional absence of charred artwork.
7. Verify both production biome regions and both DEBUG and production scene loading. Keep the original DEBUG code/scenes intact. Update tree/folder READMEs and clearly mark implementation status.

**Compatibility / cleanup:** Prefer not to delete the old Oak/PinkBlossom scenes until all references are verified and the generic equivalent passes validation. Then remove or deprecate obsolete production scene references deliberately. Keep DEBUG experiments unchanged.

## Acceptance checks

- [ ] The world instantiates every tree species from a **single** shared `Tree.tscn`.
- [ ] Adding a third tree species/variant requires only `.tres` definition(s), artwork and biome-reference data, not a new `.tscn` or species-specific C#.
- [ ] Oak and Pink Blossom keep their existing appearance, trunk collisions, scaling, mirroring and seeded placement in the appropriate biome.
- [ ] Axe input remains centralized; both species wobble and emit their configured leaves/petals.
- [ ] Both species respond to shared wind, snow and sun/contact shadow systems.
- [ ] Both species can burn through the shared FIRE system; Oak retains charred appearance and Pink Blossom burns with **no required charred image**.
- [ ] Fire response does not accidentally alter/chop trees or implement unrelated damage/loot/inventory mechanics.
- [ ] GPU/material/cache ownership remains bounded and efficient; no per-tree idle loops or unnecessary shader copies.
- [ ] Godot C# build and `SCENES/World/World.tscn` run successfully. Both DEBUG and production remain loadable and functional.
- [ ] Documentation updated; a **single GitHub commit** for the implementation job.

## Out of scope

No Avalonia toolbox implementation yet, no additional tree species/art, no inventory/items/harvest/loot, no full tree-health/death system, no new weather/fire mechanics, no overhaul to caves, world chunk streaming or other scenery categories.

**End state:** The future Avalonia editor authors a Resource such as `Willow.tres` and its image references; the shared runtime tree system handles placement, wind, snow, shadows, axe response and optional burning with no new scene files.
