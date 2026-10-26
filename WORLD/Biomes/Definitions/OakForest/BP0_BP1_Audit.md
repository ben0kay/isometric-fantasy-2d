# BP0 / BP1 — biome configuration foundation

BP0 audit complete; BP1 implemented, awaiting local engine/visual validation. No new spawn service, live climate, gameplay deposits or species are implemented.

## BP0 findings and decisions

| Existing owner | Confirmed contract / decision |
| --- | --- |
| `WORLD/Biomes/Shared/WorldBiomeDefinition.cs`, `BiomePopulation.cs` | Keep static scenery in Populations. Oak currently has trees 26, rocks 11, grass 45 and eight plants at 2.5 each per million square world units. The earlier proposal's three entries/grass 60 predates PL3. Preview Count/clusters are separate. |
| `WORLD/Generation/Placement/ChunkSceneryData.cs` | Global coordinate-keyed lattice, priority thinning, reservation/liquid rejection, stable surface IDs and generator v2. BP2 should reuse candidate/spacing principles through this owner, with a separate deposit ID namespace; do not route another independent overlapping rock scatter through WorldScene. No generator change in BP1. |
| `WorldEntityPlacement.cs`, `ENTITIES/Shared/EntityDefinition.cs`, `Entity.tscn` | Existing authored actor plus disposable practice markers; not a biome population manager. BP3 must preserve these, share entity/motor/nav contracts, and add bounded chunk/player-area scheduling separately. |
| `ENVIRONMENT/Weather/Rain/WorldWeather.cs`, Snow, `GameClock`, `SeasonController` | Rain/snow are visual weather state; clock supplies hours, seasons are a manual preview. No current temperature/RH simulation or gameplay consumers found. Environment selects startup biome; it does not follow every region. |
| `SYSTEMS/Harvesting`, `WORLD/Runtime/Persistence`, `SAVE` | H4 supplies real tree harvesting, inventory and drops. Depleted deposits and resident populations do not yet have a generic durable actor registry. Authored test actor persistence is not a reusable resident-population save contract. BP2/BP3 must add explicit records/IDs and death/depletion semantics. |
| `SAVE/WorldState/CampaignSession.cs` | Hashes recursive text `.tres`/`.tscn` dependencies, not C# script bodies. Simply adding linked profiles would change the content fingerprint. BP1 adds an exact, hash-guarded normalization of the shipped Oak/profile texts to the pre-BP1 Oak dependency graph. Changed profile or unrelated Oak data is not normalized. No schema change or broad compatibility bypass. |

### Climate

`BiomeClimateProfile` stores baseline temperature in degrees Celsius (-100..100) and baseline relative humidity in percent (0..100), validated explicitly with `Validate()`. Oak authors placeholder 15°C / 65% RH. These values are neither current rain nor soil wetness. Missing Climate means unspecified baseline; future consumers should use `DefaultTemperatureCelsius` / `DefaultRelativeHumidityPercent` (15/65) until another explicit default is chosen. BP1 has no consumers and therefore cannot alter weather or exposure. Other biomes retain null links.

Future dynamic state belongs to one shared climate service, sampled deterministically by world position and updated on relevant region/time/weather changes with bounded edge smoothing. Do not put runtime current values in these authored Resources. Seasonal curves, daily offsets, weather scheduling and exposure remain deferred.

### Entity profile

`BiomeEntitySpawnProfile`: disabled master, typed Entries, total **IndividualsPerMillionUnits**, MaximumActiveNearby, MinimumPlayerDistance and BiomeEdgeClearance in world units. Entries reference canonical `EntityDefinition`, have independent Enabled, relative SelectionWeight and inclusive minimum/maximum group sizes. Weight distributes the profile's candidate population; it never multiplies density. Group members count toward the cap. Both profile and entry must eventually be enabled; BP1 consumes neither, even when enabled in Inspector.

Initial future policy: surface biome membership with authored edge clearance, persistent residents whose deaths do not reset at chunk reentry. No respawn enum/timers until BP3 specifies saved resident IDs and lifecycle; off-screen roaming remains BP5/P6. Density is a pre-rejection target, not a guaranteed count. Placement/navigation/reservation checks remain required.

### Resource profile

`BiomeResourceSpawnProfile`: disabled master and typed Entries. Entries reference canonical world `SceneryDefinition` (the existing world-content base, not ItemDefinition), with Enabled, individual area density, spacing and edge clearance. Independent density avoids a redundant weight/total-density system for static deposit types. BP2 must verify actual harvest/deposit capability or introduce a derived canonical deposit definition before enabling content. No ore definition currently exists; none was invented for BP1.

Initial future scope: static surface deposits, persistent depletion, no automatic respawn. Existing decorative rocks, trees and plants remain exclusively in Populations; conversion/deduplication requires an explicit migration. Herbs, veins/depth/caves/geology, habitat overlap, time/weather gates and roaming/encounter policies stay deferred. Do not duplicate canonical assets under a biome.

## BP1 ownership and performance

Reusable Resource types live in `WORLD/Biomes/Shared`; Oak owns only authored Climate/Spawning `.tres` data. `WorldBiomeDefinition` exposes optional Climate, EntitySpawns and ResourceSpawns references in Inspector. Profiles/entries default disabled, lists empty. Explicit validation methods reject non-finite/range-invalid active settings; they run only when called by authoring tools or a future service, not per frame. Disabled drafts can be incomplete.

These are Resources, with no nodes, callbacks, signals, timers, caches or chunk registration. Loading Oak loads three small Resources once; there is no active or idle simulation work and no shutdown work. Existing scenery/environment/actor systems do not inspect these fields. No new MasterConfig is needed: the profiles themselves own their tweakable data under the existing WORLD/Biomes module.

Before BP2/BP3 makes profiles affect gameplay, retire/version the BP1 normalization and define fingerprint/schema migration deliberately. It only preserves worlds compatible with the immediately preceding content; it does not repair unrelated older incompatibilities such as pre-PL3 generator layouts.

## Brief validation and local handoff

Source checks: exact pre-BP1 Oak reconstruction; full recursive dependency-graph equality with the previous git revision after normalization; guards reject changes to each linked Resource; unrelated Oak population edits reject; all new `res://` references exist; `git diff --check`. Godot and dotnet are absent here, so no build/import/runtime pass is claimed. Extended suites/profiling were intentionally deferred per CodePreferences.

Locally run `dotnet build`, then open/import the project in the matching Godot Mono version. Inspect Oak Biome.tres: Climate 15/65, spawn profiles disabled/empty; Pink links unset. F6 normal World should retain current scenery, weather, plants and authored skeletons. Continue an otherwise compatible pre-BP1 campaign and verify player/inventory/tree edits survive; save/reload it. Profile edits currently have no gameplay effect but deliberately change strict compatibility. No new F9 controls. Existing P3/P4 suites are optional follow-up only if local failures suggest a regression. Visuals, disk Continue and engine validation remain pending.
