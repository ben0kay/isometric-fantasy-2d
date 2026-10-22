# Environment profiles
BiomeEnvironmentProfile references typed shared component settings. Null override components inherit world defaults; a supplied component replaces that component as a whole. The single clock lives in EnvironmentMasterConfig, outside biome appearance overrides.

Resolve explicitly duplicates each selected component, avoiding Godot external Resource references remaining shared after cloning a parent. Live tools change only these runtime copies. Adding a biome requires profile data, not copied simulation/render scripts. Profile resolution currently happens once at world startup; region transitions are not implemented.
