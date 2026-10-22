# SYSTEMS / WorldInteractions / Fire

**Status: production folder scaffold only — fire is NOT yet active in `SCENES/World/World.tscn`.**

This is the canonical future home for **generic fire mechanics** affecting trees, grass, buildings, enemies, campfires and other burnable objects. Tree-specific artwork and visual transformation belong with each tree species, and lightning/wind/weather stay in ENVIRONMENT.

The working reference is [DEBUG/TopDownTest/Fire](../../../DEBUG/TopDownTest/Fire/README.md), where FIRE1 and FIRE2 already implement the oak lifecycle, charred transformation, GPU flames, smoke, embers, shared-wind response and a canopy-alpha emission mask. Do **not** delete or rename that test.

## Planned internal layout (not created yet)

```text
SYSTEMS/WorldInteractions/Fire/
├── README.md                       [created now]
├── FireMasterConfig.cs             [future shared runtime/effects limits]
├── DefaultFireConfig.tres          [future]
├── Definitions/
│   └── FireProfile.cs              [future, per-object ignition & burn settings]
├── Simulation/
│   ├── Burnable.cs                 [future, generic healthy / burning / burnt lifecycle]
│   └── BurnState.cs                [future]
├── Visuals/
│   ├── BurningEffect.cs            [future reusable GPU emitters]
│   ├── FireErosionAdapter.cs       [generic layer only, if appropriate]
│   └── Shaders/                   [shared flame/smoke/burn effects]
└── Interactions/
    └── FireIgnition.cs             [future external ignition requests]
```

Keep the actual physical folder count modest. Create modules only when migrating working code. A separate `FireRuntime` / active-fire registry is optional: add it only if capped effects, target queries or lifecycle registration need centralized ownership.

## Exact DEBUG → production ownership

| Current source | Intended production owner / adaptation |
| --- | --- |
| `Fire/Simulation/BurnableTree.cs` | `SYSTEMS/WorldInteractions/Fire/Simulation/` as a generic burnable component; preserve `TryIgnite`, burn states/signals/reset and process-only-while-active. Rename to avoid collision with the compiled DEBUG C# class. |
| `Fire/FireMasterConfig.cs`, `DefaultFireConfig.tres` | Split shared fire effects/limits into `SYSTEMS/WorldInteractions/Fire/` and oak art/anchors/burn attributes into the species' `WORLD/Content/Vegetation/Trees/Oak/OakFireProfile.tres`. Production class names must not duplicate DEBUG global C# types. |
| `Fire/Visuals/BurningTreeEffect.cs` | `SYSTEMS/WorldInteractions/Fire/Visuals/`: reusable GPU flame/smoke/ember presentation, bounded per-active-fire emitters, light and cleanup. Replace `DebugWeather` with the production `ENVIRONMENT/Wind/Simulation/WindController.cs` wind state. |
| `Fire/Visuals/CanopyFireEmissionMask.cs` | `WORLD/Content/Vegetation/Trees/Shared/Fire/` or a content-aware reusable fire visual helper; keep alpha-derived shape per artwork, cache masks and retain a no-mask fallback. |
| `Fire/Visuals/FireErosion.gdshaderinc`, `FireErosionAdapter.cs` | Generic visual core in `SYSTEMS/WorldInteractions/Fire/Visuals/` if practical; tree/canopy-specific erosion and material integration under `WORLD/Content/Vegetation/Trees/Shared/Fire/` so a burning building never needs a foliage shader. |
| `Fire/Artwork/oak_charred_source.png` | `WORLD/Content/Vegetation/Trees/Oak/Sprites/`, with source-to-world anchor alignment and charred variant referenced by the oak's data. No duplicating charred artwork in SYSTEMS or biome folders. |
| `Fire/Debug/FireTestController.cs`, `FireTest.tscn` | Developer-only `DEBUG/UI/Fire/` after extraction; **do not** make the production feature depend on that panel. |
| Later FIRE3 lightning-targeted ignition | `ENVIRONMENT/Weather/Storms/WorldLightning.cs` remains owner of strike visuals/selection; invoke the shared ignition contract on the actual impacted object. No separate weather simulation inside Fire. |

## Recommended separate implementation stages

1. **Production FIRE1:** create the generic burnable lifecycle; data profile; import the charred oak into canonical `Oak/Sprites/`; wire one actual production Oak instance; test ignition, art transition and reset using a removable `DEBUG/UI/Fire/` control. Preserve the root `StaticBody2D` and `TrunkFootprint`.
2. **Production FIRE2:** adapt current GPU flames, smoke, embers, warm flickering light, capped visuals and cached alpha-mask emission. Change DEBUG-specific weather, noise and shader references to **production** paths/controllers.
3. **Integration:** coordinate vegetation wind/snow material ownership, update the existing production `ENVIRONMENT/Lighting/Shadows/SunShadowRenderer.cs` cached texture when the tree becomes charred, and stop healthy leaf feedback on burnt trees. Keep runtime effects only on actively burning objects.
4. **Production FIRE3 (later):** allow `WorldLightning` to select an eligible target and ignite the specific receiver, without breaking its existing ground strikes. Rain extinguishing, fire spread, heat/damage, collapsing trees and persistent burnt-tree state are later independent features.

## Current production dependencies to respect

- `WORLD/Content/Vegetation/Trees/Shared/TreeDefinition.cs` extends `SceneryDefinition`; reuse per-content data, not duplicated tree classes.
- `WORLD/Content/Vegetation/Trees/Oak/Oak.tscn` currently has `Sprite`, `TrunkFootprint`, and `HitResponse`; it has **no production Burnable node** yet.
- `ENVIRONMENT/EnvironmentRuntime.cs` already instantiates `WindController`, `WorldWeather`, `SunShadowRenderer` and `WorldSnow` after placement; integrate with those existing instances, not copies from DEBUG.
- `ENVIRONMENT/Lighting/Shadows/SunShadowRenderer.cs` caches sprite textures at startup and currently does not expose a targeted art refresh method; add it during integration so charred trees don't cast leafy sun shadows.
- Production snow/vegetation wind adapters own existing live shader materials. Fire must not clobber shared material identities or lose restoration after completion/reset.
- `ENVIRONMENT/Weather/Storms/WorldLightning.cs` is currently visual-only and uses open-ground strike selection. No tree ignition is connected today.

**Migration difficulty:** Moderate. The ignition state machine and fire GPU visuals are mostly reusable. The nontrivial parts are replacing DebugWeather/scene-path dependencies, separating shared fire config from oak texture/anchor data, Godot C# naming collisions, and coordinating runtime snow/wind/shadow material ownership. Do not report success until the Godot .NET build, asset/scene loading and live tree ignition/reset are tested.

**Scope of this scaffold commit:** only two READMEs and a link in `SYSTEMS/README.md`. No runtime scripts, configs, scenes, images or world references are modified. The production implementation roadmap in `Notes/Production_Implementation.md` remains a record of the previous, completed phases.
