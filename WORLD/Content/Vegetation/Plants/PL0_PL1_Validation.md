# PL0 audit / PL1 implementation

Base inspected: `3673cb1`. PL0 source/art audit complete. PL1 code implemented; native compilation/runtime checks pending. PL2/PL3/PL4/GR0/RK0 not implemented by this job.

## PL0 findings

SceneryDefinition owns identity/spacing/environment settings; BiomePopulation explicitly grants eligibility and density. ChunkSceneryData currently dispatches tree visual variants or legacy PackedScenes. WorldChunkStream.ApplyOne and WorldSceneryPlacement likewise require PL3 template dispatch. Preserve their coordinate keys, RNG draw sequence for existing content, bounds, halo, feature/water exclusions, publication metadata and unload cleanup. Snow adapter is opt-in via SnowResponse; shared materials/wind remain existing owners.

All root plants have alpha channels. Alpha bounding boxes below use exclusive right/bottom coordinates. Proposed roots use alpha-box bottom centre; artistic grounding still requires visual acceptance.

| Art | Image size | Alpha bounds | Proposed anchor |
| --- | --- | --- | --- |
| Plant001 | 91×91 | 0,8–91,91 | 45.5,91 |
| Plant002 | 91×91 | 7,13–84,91 | 45.5,91 |
| Plant003 | 91×91 | 6,28–85,91 | 45.5,91 |
| Plant004 | 91×91 | 8,8–83,91 | 45.5,91 |
| Plant005 | 110×100 | 3,9–107,100 | 55,100 |
| Plant006 | 110×100 | 0,0–110,100 | 55,100 |
| Plant007 | 110×100 | 3,17–107,100 | 55,100 |
| Plant008 | 110×100 | 8,14–101,100 | 54.5,100 |

Subsequent asset staging at `5ac3875` moved sprites to Plants/Sprites/plant_oak_XXX.png and created staged SceneryDefinition Resources with IDs plant.oak.001 through plant.oak.008. Preserve those IDs during PL2 conversion; they are not production-spawnable yet. The fixture now uses the renamed sprite paths. Oak currently totals 97 density (26 trees + 11 rocks + 60 grass); capacity at cell size 96 is 108.5069. Proposed PL2 budget: 26 + 11 + 45 grass + eight plants at 2.5 = 102; this fits without rewriting placement. No density edits in PL1.

CampaignSession.BuildContentFingerprint walks referenced .tres/.tscn dependencies starting at DefaultWorldConfig and World.tscn. This job adds unreferenced foundation/fixture files and changes documentation only; existing dependency bytes, generation code/version and save validation remain unchanged. No migration/bypass added. PL2 Oak edits will change this fingerprint and require a deliberate compatibility decision/new campaign, never silent regeneration. Disk Continue was not executed here.

## PL1 behavior / performance

Three small shared classes and one Plant.tscn. SelectVariant consumes the planner's seeded unit sample; no instance randomness or new generator. Configure applies a validated index, normal/atlas texture, source anchor, mirror, positive sprite scale and shared material before AddChild. Population scale stays on the root. Inline artwork bounds/radius are ready for PL3; they are authored conservatively relative to the root, not computed from opaque pixels at spawn.

Activation: explicit Configure at instantiation. Shutdown: configuration ends immediately; process and physics callbacks stay disabled. Idle: one static sprite draw, no per-node CPU update or shader writes. Unload: normal node lifetime; no event subscriptions, global cache or private materials to release. Active wind will use shared shader work when authored/wired in PL2/PL3. No measured performance claims.

## Checks and local handoff

Executed: Pillow dimensions/alpha-box audit; source review of both generation paths, density and save fingerprint contracts; git diff --check. Neither dotnet nor Godot is installed here, so no compilation, engine fixture, import, visual or profiling pass is claimed.

With Godot 4.7.2 .NET and .NET 8 available, from repo root (substitute your Godot .NET executable for godot):

```sh
dotnet build
godot --headless --editor --path . --import --quit
godot --headless --path . TESTS/Vegetation/ValidateVegetation.tscn
```

Expected fixture: PL1 vegetation contract PASS, exit 0. It checks Plant001/Plant008 using the same template, then one definition with two inline normal/atlas variants, repeatable key-seeded samples, both selections, roots under mirroring/scaling and disabled callbacks. It intentionally does not modify production definitions or candidate dispatch. Full planner determinism, serialized production .tres authoring, weather lifecycle, production World F6 and compatibility preview belong to PL2/PL3. Native import/serialization and visual atlas/wind parity remain unverified.

Publication reconciliation: rebased onto asset-staging commit `5ac3875`, preserved all eight staged definitions/sprites, merged Plants README and corrected fixture asset paths. Earlier root-path findings above describe the original audit revision.
