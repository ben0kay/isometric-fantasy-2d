# B0 extended snowfall / storm state

Implemented on base c3a88dc. Native/runtime validation pending. B1–B4 remain planned; no heavy flakes, lighting, haze, veil or frost added.

## Controls / integration

Existing Snowfall intensity is 0–3 in Inspector and F9 Environment → Snow. 0–1 preserves normal snowfall; the existing 1300-particle emitter still clamps rate at 1. Storm target maps 1→0, 2→0.5, 3→1. Coverage remains 0–1 and independent. Snow.Enabled and Snowfall=false immediately clear the storm, preserving the requested rate for reactivation. Rate zero also shuts down; returning to rate 1 smoothly fades severity. Positive transitions take real seconds at full-range speed (3s from 0 to 1 by default). F9 shows current severity in live status.

One WorldBlizzard is composed by EnvironmentRuntime alongside Snow in normal World. BlizzardProfile.tres provides default transition tuning and is cloned before F9 edits. It is loaded at runtime, not inserted into authored biome/default-world resource graphs. Existing snow profiles were already runtime clones. No campaign serialization, save schema/fingerprint migration or weather scheduling changes. New worlds start as before with snow disabled and normal-range rate. No preview command changes global wind, settled coverage or master toggles.

F9 Blizzard transition subsection has transition duration/reset-to-normal (0.65 rate, 3s transition). Main enable/falling toggles are the only ON/OFF controls. No second severity slider. Future rain can reuse the same numeric convention/changed-state interface; B0 introduces no generic storm manager or rain refactor. B1/B2 consumers can subscribe to IntensityChanged and read current Intensity once on binding.

## Lifecycle / performance

Snow/Profile Changed wakes one bounded intensity transition. Only actual intensity changes notify consumers. Steady 0, 0.5 or 1 sleeps with SetProcess(false). Snow master/falling OFF clears immediately, including shutdown notification. Exit disconnects Snow and Config signals and clears subscribers. No timers, new GPU emitters, overlays or per-chunk/per-plant loops. Existing ordinary snowfall camera/wind callbacks continue while snow is enabled; F9 retains its existing visible-only refresh. No measured performance claim.

## Executed / deferred

Source review and git diff --check performed. Checked existing snowfall's explicit 0–1 emission clamp and runtime-only environment cloning path. Neither dotnet nor Godot is installed, so compilation/import, new fixture, baseline tests and F6 checks are NOT claimed passed.

Run locally from repository root with the project Godot .NET version:

```sh
dotnet build -p:NuGetAudit=false
godot --headless --editor --path . --import --quit
godot --headless --path . TESTS/Generation/ValidateBlizzardState.tscn
godot --headless --path . TESTS/Generation/ValidateSnowSleeping.tscn
godot --headless --path . TESTS/Generation/ValidateRainSleeping.tscn
godot --headless --path . TESTS/Generation/ValidateCloudWindCaching.tscn
```

New fixture expects B0 PASS and exit 0. It covers mapping, changed-only inputs/events, real-time ramp, both sleep endpoints, independent coverage, master/falling/zero gates, reactivation, runtime clone, bounded invalid input and teardown. It drives transition callbacks explicitly; this is not a GPU/visual acceptance test.

F6 normal World: F9 → Environment → Snow, enable snow/falling; try 0/0.65/1/2/3. The status should smoothly reach 0/0/0/50%/100%; 1–3 visibly remains today's maximum normal snowfall in B0. Change transition seconds and reset. Disable falling and snow separately; coverage can remain with falling disabled. Confirm wind/rain controls remain independent. Leave/reopen World: preview settings must not persist. Compile/run the fixtures without edits and report exact failures before considering this accepted.
