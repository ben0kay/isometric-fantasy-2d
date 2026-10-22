# SYSTEMS / WorldInteractions / Fire

**Implemented: production FIRE1 and FIRE2 in SCENES/World/World.tscn.** The independent DEBUG/TopDownTest/Fire experiment is preserved.

## Ownership

- `Definitions/FireProfile.cs`: reusable ignition delay, burn duration and re-ignition rules.
- `Simulation/Burnable.cs`, `BurnState.cs`: generic healthy → burning → burnt lifecycle, signals, TryIgnite and ResetBurn. Idle receivers do not process.
- `Simulation/FireRuntime.cs`: world-scoped event wiring, capped temporary effects and appearance/shadow hooks; initialized after scenery and ENVIRONMENT.
- `FireMasterConfig.cs`, `DefaultFireConfig.tres`: shared particle limits, intensities, wind response, smoke tail and warm flickering light.
- `Visuals/BurningEffect.cs`: temporary C# GpuParticles2D flame/smoke/ember emitters. Reads the existing production WindController. Unsupported erosion materials fall back to crossfade.
- `WORLD/Content/Vegetation/Trees/Shared/Fire/`: tree-specific profile, charred alignment/crossfade, erosion shader composition and cached canopy-alpha emission points.
- `WORLD/Content/Vegetation/Trees/Oak/OakFireProfile.tres`, `Oak/Sprites/oak_charred.png`: canonical oak artwork/anchors and lifecycle settings. The generic Shared/Tree.tscn carries Burnable and FireAppearance; TreeDefinition configures them for all opted-in species. Pink Blossom ignites and emits shared GPU flames without charred art.
- `DEBUG/UI/Fire/`: removable ignition/reset buttons. Production ignition/effects/shadow refresh do not depend on the panel.

C# types use `FantasyGame.WorldInteractions.Fire` to coexist with DEBUG names without Production prefixes. Config resources reference their scripts explicitly.

## Run and tweak

Pull, build C#, open `SCENES/World/World.tscn` and press **F6**. Walk within 320 pixels of an oak; use **Ignite nearest tree** at the top centre. **Reset burnt / burning trees** restores the world oaks. F5 still opens the independent debug sandbox.

Tune shared effects in DefaultFireConfig; tune duration, charred texture, source-pixel ground anchors and canopy band in OakFireProfile. Other tree species opt in through TreeDefinition Resources; the generic template installs the common nodes. No inventory/tool binding is required. All player input remains owned by PlayerInput; this feature adds no player input polling.

The original root StaticBody2D/TrunkFootprint persists. Burning/burnt oaks suppress healthy axe-hit leaves; reset restores them. SunShadowRenderer refreshes only the changed caster's texture/offset/flip. Fire composes with the live wind/snow material and restores the current source on burnout/reset; no shared material asset is edited. An effect cap limits presentation, while every accepted ignition still completes its lifecycle.

## Validation and scope

GodotSharp C# compilation and Godot .NET 4.5.1 headless + rendered Compatibility checks passed for normal/mirrored anchors, fixed colliders, left/right wind, snow enable/disable during burning, charred/reset shadow refresh, material restoration, optional UI removal and bounded effects. Rendered testing used the full production scenery density with software OpenGL. Windows renderer/performance still needs the local check.

The migration also repairs an existing Player.tscn resource declaration placed after a sub_resource, which otherwise prevented World from loading.

FIRE3 lightning ignition, spread, rain extinguishing, heat/damage, collapse and persistence remain future work. Existing lightning stays visual-only. See TESTS/Fire/README.md for the repeatable production integration check.
