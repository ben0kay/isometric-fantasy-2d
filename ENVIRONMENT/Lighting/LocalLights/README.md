# Local Lights — Torch, Campfire, Lantern & Shadow Plan

> **Status: LL0–LL1 implemented.** Native PointLight2D shadows are enabled in the existing torch preset. Generic trees and the rock template own footprint-derived LightOccluder2D nodes. Two extra preview torches stand near spawn. LL2 common light-type framework and LL3 streaming/budgets remain planned.
>
> **Codex:** Read root `README.md` and `CodePreferences.md`, then this README, `ENVIRONMENT/EnvironmentRuntime.cs`, `ENVIRONMENT/Lighting/Shadows/README.md`, `WORLD/Content/Props/Torch/Torch.tscn`, relevant tree/rock scenes and their collision geometry, plus the current production world/biome and chunk-generation code before making changes. One GitHub commit per approved job.

## What works today

- `TorchLight.cs` caches the torch's existing `PointLight2D` and adds a two-frequency energy flicker.
- `TorchProfile.cs` / `TorchProfile.tres` configure whether it is visible, light energy and its offset from the world spawn.
- `WORLD/Content/Props/Torch/Torch.tscn` owns the **actual torch art, physics body and light node**. Do not duplicate this content asset in ENVIRONMENT.
- `EnvironmentRuntime.cs` instantiates **the original temporary development torch plus two configurable comparison torches** near the player's spawn, and adds the torch light controller. Disabling its presentation currently retains collision.
- The sunlight system is **different**: `ENVIRONMENT/Lighting/Sun/`, `DayNight/` and `Shadows/SunShadowRenderer.cs` own one shared day/night calculation and custom projected sun silhouettes. **Do not replace or couple sunlight to local-light occlusion**.

**Now implemented:** object-owned native occluders for generic trees (oak and pink blossom) and rocks, optional per-variant light outlines and independent torch energy/radius/shadow controls. No global shadow manager, per-object raycasts or per-frame polygon rebuilding.

**Still planned:** shared controllers for several light types, per-light budgets and chunk streaming registration.

## LL0–LL1 run / tweak / removal

Build C#, open SCENES/World/World.tscn and F6. The existing torch plus two additional torches are near the player spawn. In F9 controls, set Time near midnight and expand **Lighting and clouds**. Compare **Local torch shadows** on/off; **Torch energy** changes brightness, **Torch radius scale** changes range, and **Local shadow softness** adjusts PCF smoothing. ShadowFilter and Colour are also Inspector-editable TorchProfile fields.

TorchProfile.tres opts into CastShadows and specifies two AdditionalOffsets relative to player spawn. Clear AdditionalOffsets and reload to remove the extra lamps; at most two extra development lamps are instantiated. The original torch stays at OffsetFromSpawn. Set CastShadows=false to disable local shadow casting. The light texture, flame artwork/emissive material and flicker stay with the canonical torch; only the existing main torch emits the ambient smoke/embers for this contained comparison.

WORLD/Content/Shared/LocalLightOccluder.cs creates a closed, double-sided native polygon once from the configured physical footprint. The generic tree template and Rock01.tscn own it, so masks/geometry follow object scale, mirror and lifetime naturally. TreeDefinition.CastLocalLightShadows opts a tree species out. TreeVisualVariant.LocalLightPolygon can supply separate root-local geometry; empty uses TrunkFootprint. Rock's helper accepts an authored Outline override. Physics polygons never change for lighting. Native light/occluder masks remain the matching default bit 1.

The sun renderer/lighting formulas, seed placement, physics and weather are unchanged. No broad LL2 controller, LL3 budget, streaming or persistence work is included. The current preview torches remain temporary environment composition, not persistent inventory/fuel entities.

Validation: C# compiled against GodotSharp 4.5.1; full production scene imported/rendered in Godot .NET 4.5.1 Compatibility software OpenGL. On/off image probes verified darkness behind an oak with the same emitter on both sides; runtime checks cover all tree/rock footprints, mirrors, three lamps, shadow opt-out, independent energy/radius and retained sun cache. Tree integration and original DEBUG startup are checked separately. The repository SDK remains 4.7.2; your Windows renderer/performance needs the local check. See TESTS/Lighting.

## Goal

A data-driven, reusable **local point-light shadow system**. Torches, campfires, lanterns, candles, magical lights, glowing ores/crystals and suitable fires should illuminate nearby content and cast **directionally correct shadows away from each light**, based on the relative positions of light and obstruction.

Examples:
- A torch beside an oak trunk blocks some torchlight and throws a shadow away from the flame.
- Moving the light to the opposite side moves the shadow direction without modifying the tree.
- Stronger/brighter or larger-range lights may reveal more of the scene, but shadow visibility depends on other illumination (especially daylight) as well.
- Dim light should cover less visually or be less noticeable. **Energy is brightness, not range by itself**: the effective radius follows the light texture / texture scale; falloff follows the gradient/profile. Offer a configurable strength-to-range mapping only if it proves useful.
- Torches, fires and other lights can optionally cast shadows; unimportant decorative glows can remain non-shadow-casting.
- Multiple nearby lights can contribute independently. **An occluder blocks that local light's contribution, not the global sun or every other light.**

## Suggested folder ownership (PROPOSAL — do not create empty modules yet)

```text
ENVIRONMENT/
└── Lighting/
    ├── Sun/                       # Existing shared sunlight calculation
    ├── DayNight/                  # Existing global ambient lighting
    ├── Shadows/                   # Existing custom sun projections
    └── LocalLights/
        ├── README.md              # This document
        ├── TorchLight.cs          # Existing temporary torch flicker
        ├── TorchProfile.cs        # Existing torch-specific settings
        ├── TorchProfile.tres       # Existing torch profile
        ├── Shared/                # PROPOSED when reuse is justified
        │   ├── LocalLightProfile.cs
        │   └── LocalLightController.cs
        └── Occlusion/             # PROPOSED if a coordinator proves useful
            ├── LightOcclusionController.cs
            └── README.md

WORLD/Content/Props/Torch/Torch.tscn           # Canonical torch art + light
WORLD/Content/Vegetation/Trees/...             # Own each tree's occluder/shape
WORLD/Content/Rocks/...                        # Own each rock's occluder/shape
WORLD/Content/Structures/...                   # Future walls / buildings
```

**Important simplification:** Godot already performs native local-light shadows. A separate `LightOcclusionController` is **optional**, not a requirement. Prefer adding `LightOccluder2D` to participating content scenes and enabling shadows on the existing `PointLight2D`; add a shared registration/budget controller only when chunk streaming or many lights justify it. Avoid duplicating Godot's rendering engine or making a scripted raycast per object per frame.

## Proposed shared settings / resource fields

Consider extending `TorchProfile` with an optional reusable `LocalLightProfile`, not replacing working torch settings prematurely:

| Control | Meaning |
| --- | --- |
| Enabled | Light on/off; decide independently whether artwork and collision remain |
| CastShadows | Enable native `PointLight2D.ShadowEnabled` for this emitter |
| Energy | Light brightness, possibly flickered around a base value |
| Range/TextureScale | Reach of the emission texture; not interchangeable with energy |
| Falloff/LightTexture | Gradient/shape of illumination |
| Colour | Light colour (warm fire, cool moonlike magic, green crystal) |
| ShadowColour | Tint/transparency of the shadow contribution, if appropriate |
| ShadowFilter/Softness | Crisp or softened native 2D occlusion; test quality/performance |
| Update/Flicker | Optional flicker parameters; no unnecessary per-frame work for steady lights |
| ShadowBudgetPriority | Optional later setting for capped active shadow-casting lights |

Put common controls in shared data, light-type-specific presets in their respective own profiles, and canonical content scenes in WORLD/Content. Keep the current `EnvironmentMasterConfig` / biome environment overrides as appropriate rather than inventing another unrelated global master system.

## Geometry and how shadows should look

1. **Godot path:** For the first test enable `ShadowEnabled` on the torch's actual `PointLight2D`. Put `LightOccluder2D` children with `OccluderPolygon2D` shapes on selected tree and rock content scenes.
2. A tree's **solid trunk/footprint** may make a more believable torch occluder than its whole large leafy canopy (which otherwise can block an unrealistically huge wedge of light). Compare trunk-only, appropriately enlarged trunk and optional canopy approximations before deciding.
3. Do **not** assume `CollisionPolygon2D` automatically casts light shadows. Occluder geometry is separate; it may be derived from the same canonical footprint where useful rather than authored twice. Never force identical physics and light shape if the art calls for differences.
4. Preserve feet anchoring, object rotation/scale/flip and existing collision/navigation. Static light-occluder polygons should follow the object transform; don't introduce a separate independent scene per light-object combination.
5. Confirm appropriate `Light2D` item masks and `LightOccluder2D` occluder masks. Ensure the shadow actually appears on receiving canvas items in Godot and doesn't inadvertently obscure the flame.
6. Native occluders **do not reproduce** the existing sun silhouette projection or 3D height/soft volumetric shadows; use them for plausible local light blocking. Keep sunlight's art-directed system unchanged.

## Performance & future large finite world/chunk streaming

- Avoid full-world light sources, full-world scans and CPU raycasting for every tree against every fire.
- Only keep nearby chunks' physical objects/occluders active. Content scenes should load/unload with their owning chunk; no static global list of every tree's occluder.
- Limit simultaneously **shadow-casting** point lights by visibility/distance/importance if profiling shows a problem; non-shadow-casting distant/decorative lights may still glow.
- Where needed, rate-limit updates to *management* or flicker settings, but don't manually update shadow polygons every frame when Godot can do it automatically.
- Use Godot's light range/culling, occluder/light masks, suitable shadow-filter quality and profiler measurements. High shadow-filter settings, many overlapping lights and lots of occluders cost GPU time.
- Existing sun shadow cache currently assumes startup-scattered content: any future streaming refactor must register/unregister sunlight casters separately. **Don't entangle that existing issue with the first local-light test.**
- A torch left in an unloaded chunk must not continue running `TorchLight._Process` or maintain a live physics/light node unless explicitly needed.
- Save persistent fire/light-source *state* as world/chunk changes (lit, extinguished, owner, fuel when implemented); derived light shadows themselves need no save data.

## Codex staged implementation plan

### LL0 — Inspect and isolate a safe test (completed)

- Inspect current torch scene and its `PointLight2D`, existing oak / pink blossom tree / rock scenes, and Godot engine version.
- Confirm current object materials and render layers receive PointLight2D illumination; inspect whether any objects already have native occluders or unusual masks.
- Use a contained production comparison with the existing torch or a removable test fixture. Preserve sun shadows, rain, snow, fog, wind, day/night and current DEBUG.
- Identify minimum resource/config changes; **no new global shadow manager in this pass**.

### LL1 — Minimal working torch shadows (completed)

- Make the existing torch's shadow casting opt-in through its profile and `PointLight2D`.
- Add **reusable, object-owned** occluder geometry to at least one oak tree and one rock (or a small isolated test if safer).
- Tune enough occluder geometry to visually verify sensible shadow direction at night, with a nearby torch on either side of a tree; test different lamp energy, radius and shadow filter settings.
- Confirm Godot-native shadows coexist with custom sunlight shadows and work with sprite flips / generated tree scales.
- Avoid changing other tree collision, biome definitions, generation or sun rendering.
- **Pass:** Night torch shows a shadow away from the light, moving/relocating emitter updates that direction, disabling shadows restores the existing torch look, and other existing systems remain unaffected.

### LL2 — Reuse across several local light types

- Only if LL1 proves useful, extract common light profile / controller functionality with stable Inspector-editable settings.
- Let the current torch and a second example (e.g. lantern or campfire when it exists) use the same base lighting/shadow logic without separate bespoke shadow implementations.
- Preserve custom torch flicker as a small behaviour layered on top; steady lights should not need a `_Process` loop.
- **Pass:** Two distinct light presets show different range/colour/strength/shadow softness with one reusable implementation.

### LL3 — Streaming, persistence and performance follow-up

- Ensure light emitters and their object-owned occluders load/unload correctly with streamed chunks; inactive lights have no live update cost.
- Measure multiple lit sources and dense forest occluders with Godot profiler and real target hardware; configure a budget / LOD strategy only if justified.
- Ensure dynamic tree removal or construction automatically removes/adds appropriate occluders with their content scenes.
- **Pass:** No ghost shadows, stale registrations, unexpected duplicate occluders or unacceptable frame-time spikes when entering/leaving lit chunks.

## Validation / do-not-break checklist

- [x] Same sun direction, ambient day/night system, sun-shadow silhouettes, wind and colour grading as before.
- [x] Existing torch remains spawned, visible and flickering with its current default settings when `CastShadows` is off.
- [x] At night, an occluding tree or rock blocks **only that torch's light**, while other light sources and daylight still contribute.
- [x] Shadow direction is correct relative to lamp/object positions; light range and light energy work as separate settings.
- [x] Occluder shapes look plausible with pixel-art assets and don't require per-object CPU collision queries every frame.
- [x] Godot build/import/scene validation and actual visual screenshot testing pass; no broken asset paths or editor errors.
- [x] Existing DEBUG tests and future production chunk streaming plans remain isolated.
- [x] Update this README to distinguish implemented functionality from planned functionality after each pass; **one commit per job**.

**Recommendation:** Start with **LL0–LL1 only**. Godot's built-in `PointLight2D` + `LightOccluder2D` is enough for the first working test. Don't prebuild a complex shadow architecture until two or more light types actually need it.
