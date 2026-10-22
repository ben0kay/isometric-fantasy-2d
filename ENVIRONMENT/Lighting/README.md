# Lighting
Implemented: DayNight/ reads GameClock and weather; Sun/ owns daylight threshold math; Shadows/ caches ground projections/contact ovals; Colour/ owns fullscreen grading; LocalLights/ owns torch flicker. All settings come from typed environment profiles. Moon/ remains future work.


LL0–LL1 local-light shadows are implemented separately from sun projections: the existing torch preset uses native PointLight2D shadows against object-owned tree/rock occluders. Two removable preview torches share the same TorchProfile. See LocalLights/README.md for comparison settings and removal.
