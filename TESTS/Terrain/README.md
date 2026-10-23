# Terrain validation

Run ValidateGroundStack.tscn headlessly after .NET build and Godot import. GS1 validates authored references, legacy fallback, stack precedence, an arbitrary third biome, rejection and serialization. See WORLD/Terrain/Ground/Surfaces/GS0_GS1_Validation.md for exact commands, baseline captures and save migration checks. Tests are authored but not executed in the implementation runner (Godot/.NET unavailable).
