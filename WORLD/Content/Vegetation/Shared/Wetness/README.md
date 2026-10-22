# Wet tree foliage (production)

Rain wetness darkens generic oak and pink-blossom leaves with a subtle GPU tint, while leaving rooted trunks readable. Wetness is visual only: no surface physics, temperature, saturation save state or separate weather simulation.

- VegetationWetness.cs attaches to the shared wind-variant materials in EnvironmentRuntime AFTER VegetationWind.Attach and BEFORE WorldSnow's material adapter is created. It creates one reusable tinted shader variant per wind source and foliage profile, NOT one shader/processor for every tree.
- VegetationWetness.gdshaderinc masks foliage using original sprite colour and upper-canopy UVs. It works for generic trees with foliage profile 0 (green) or 1 (pink), and intentionally does not darken grass or rock materials. Future species can opt in with these existing shared metadata/profiles; extend data profiles for other leaf colours.
- Rain intensity is read from the existing WorldWeather.Rain. Colour transitions saturate over WetnessBuildSeconds and dry over WetnessDrySeconds; shader materials are updated only when their parameters change enough.
- The existing snow adapter clones wet-compatible wind materials and mirrors changing wetness uniforms, applying snow OVER the tinted leaves. FIRE2's temporary erosion adapter mirrors those uniforms too; original materials and charred transitions remain owned by their existing modules.
- Tune WetFoliage, WetFoliageDarkening (default 0.15), WetnessBuildSeconds and WetnessDrySeconds via ENVIRONMENT/Weather/WeatherProfiles/WeatherProfile.cs and WeatherProfile.tres. No per-species scripts or rain-only tree scenes.

TEST: Build C# and run SCENES/World/World.tscn. Increase rain from zero to one in F9; oak and pink leaves should subtly darken and then dry when rain returns to zero. Also check wind, snow toggle, fire ignition/reset and occlusion fades. The future intended weather scheduling will normally prevent rain and snow at the same time; both materials are still composable now.
