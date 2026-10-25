# Vegetation contract fixture

Run ValidateVegetation.tscn with the Godot .NET editor or the headless command in the [PL0/PL1 handoff](../../WORLD/Content/Vegetation/Plants/PL0_PL1_Validation.md). It uses the existing staged PNGs without moving/registering production content. Two in-memory definitions share Plant.tscn; one gains an inline AtlasTexture variant. It checks repeatable seeded selection, identity, roots/mirroring and dormant callbacks. This is a contract test, not a World integration or performance acceptance test.

PL3 adds ValidatePlantWorld.tscn against normal World publication, real resources and both placement paths. See [PL3 handoff](../../WORLD/Content/Vegetation/Plants/PL3_Validation.md). Neither fixture has been executed in the remote environment.
