# Disposable organic grass surface

Run the normal top-down debug scene. **Grass surface comparison** appears beneath the polish controls on the left. Toggle Natural grass off for the original repeated SVG; toggle colour patches/fine detail independently or change the colour variation slider. Default variation is subtle, with soft green shades and occasional muted dry patches. No new production dependencies.

## Separate responsibilities

- `GroundMasterConfig.cs` / `DefaultGroundConfig.tres`: grass appearance and comparison settings only. Inspector controls include palette colours, PatchSize, Variation and DetailStrength. Values update live; restarting restores saved settings. Inspector changes update the slider but not checkbox state.
- `DebugGroundSurface.cs`: applies those uniforms to a private grass material and provides debug controls. It does not own lighting, wind, fog or weather.
- `GrassMaterial.tres`: connects the grass shader to its dedicated noise resource.
- `Noise/GroundNoise.tres`: one shared 512×512 seamless FastNoiseLite texture, generated once. Its seed/noise settings are edited on the resource.
- `Shaders/NoiseSampling.gdshaderinc`: shared sampler declaration only; smooth linear sampling is independent of pixel-art scenery filtering.
- `Shaders/PatchColours.gdshaderinc`: broad warped colour regions and subtle dry areas only.
- `Shaders/FineDetail.gdshaderinc`: finer brightness/fibre mottling only, using snapped world coordinates to keep small detail pixel-like.
- `Shaders/GrassSurface.gdshader`: small composition entry point. Includes the three modules, preserves a little existing artwork grain and passes the result to normal Godot CanvasItem lighting.

The old `isometric-new-2d` project's GrassSurface.gdshaderinc and GroundSurfaceNoise.cs inspired the soft mottling and shared sampled-noise approach. Its combined world-surface/sunlight system was not imported. Broad palette variation, fine texture, resource setup and C# comparison UI remain separately editable here. Other terrain types should have their own material/composition, reusing only genuinely shared helpers.

## Coordinates and scope

Both patch and fine detail sample **world coordinates**, not screen or repeating tile UVs. Camera movement does not move the pattern, and the colour field crosses the original 32-pixel tile edges continuously. Smooth seamless texture sampling avoids analytical noise/hash cell boundaries. There is no TIME or wind animation in the ground material: moving shade still belongs to Polish, haze to Atmosphere, lighting to DayNight and rain to Weather.

Only `Ground/Grass` gets this material. The path, clearing, original small ground decorations, collisions, world scatter and existing effects retain their current behaviour. This is surface colour/detail, not terrain generation, grass blade geometry, biome blending or softened path edges. Existing path polygon edges remain authored and sharp.

Cost is one existing grass draw with up to six inexpensive noise samples per pixel; fine-detail off skips two, and Natural grass off returns the original artwork. There is no screen copy, per-frame noise generation or added grass node per tile. Profile actual target hardware before promotion.

## Validation

All debug C# scripts compiled against GodotSharp 4.7.2 / .NET 8 and with Godot 4.5.1 source generators. A full-scene software OpenGL Compatibility comparison loaded the new material, generated its seamless texture and rendered both organic and original grass. Images were inspected for colour continuity and retained scenery/lighting. This does not validate the Windows D3D12 backend or hardware performance.
