# Disposable rain and shared wind preview

Everything here is a prototype inside `DEBUG/TopDownTest/`. Build C# and press F5/F6 as before. The **Debug rain / shared wind** panel sits below Atmosphere at the right.

## Live controls

- **Rain 0–100%:** starts at 55%, fades in/out over two seconds. Zero stops new drops and lets existing ones finish their short lifetime.
- **Wind -3 to +3:** negative blows left, zero is calm, positive blows right. Drives rain slant, canopy/silhouette wind, haze, cloud shade, pollen, leaves, smoke and embers. Rain stays nearly vertical in calm air. Mirrored trees have matching wind direction.
- **Ground splashes:** separate toggle for tiny ground rings and spray droplets.
- **Rain overcast lighting:** cooler/dimmer daylight and weaker sun shadows as rain intensity builds. Night tint remains readable. Disable to compare rain with the original lighting.

Weather keeps moving when the 30-second demonstration clock is paused. Sliders do not take keyboard focus, so arrow-key movement continues after dragging them. Restart restores saved settings. Changes through the Remote Inspector update slider values; checkbox state only follows the UI.

## Files and settings

- `WeatherMasterConfig.cs` / `DefaultWeatherConfig.tres`: signed WindStrength, DriftPerStrength, RainIntensity, TransitionSeconds, startup MaxDrops (100–4000), FallSpeed, SlantPerStrength, splash rate/toggle and overcast tint/toggle.
- `DebugWeather.cs`: one coordinator, C# **GpuParticles2D** rain, shared accumulated displacement, and comparison UI. `WeatherDemo.tscn` groups its generated helpers.
- `DebugRainSplashes.cs`: a reusable 96-slot splash pool. Decorative splashes are sampled independently of the falling GPU drops; they are not physical impact events.

The previous separate DayNight WindStrengthMultiplier, Atmosphere DriftVelocity and Polish CloudVelocity settings were removed. Weather is now the single wind source. Each effect retains its appropriate response: tree materials retain authored sway strength/speed; fog/clouds/pollen use integrated world drift, and short-lived emissions integrate drift within their lifetimes. Changing wind does not teleport these patterns. Rain direction updates on new particle emission, settling within its 0.65-second lifetime.

## Ground-only splashes

Splashes draw at world Z=-4, above terrain and below all actors. Static tree/rock sprite rectangles are transformed into world bounds once after seeded scenery generation, including scale/mirroring. Polygon-authored torch artwork is included too. Spawn samples reject these conservative rectangles plus clearance and the moving player's current sprite bounds. Thus splashes do not appear on trunks, canopies, rocks, torch or player. Full rectangles intentionally exclude some transparent gaps around artwork. Small rings stay within the padded exclusion margin.

This is for the current static debug props: moving/replacing scenery would require rebuilding cached exclusions. Rain streaks are foreground weather and can pass in front of props; there is no roof/shelter mask, accumulation, puddles, wet reflections, sound or production weather simulation.

## Rendering and cost

A single GPU emitter uses world coordinates and follows the actual camera canvas view (including zoom/clamping). Its emission rectangle extends beyond every screen edge by the maximum particle travel plus a camera margin. Old particles stay in world space while the player moves. The narrow generated streak texture aligns with particle velocity. Rain renders before fullscreen grading and below UI.

Splash work is bounded: 96 reusable slots, at most eight spawn requests per frame with six placement attempts each, cached static bounds and no per-drop physics queries or scene-node creation. No world-wide rain simulation or per-frame noise texture uploads. GPU budget remains allocated at MaxDrops even with a lower intensity, so profile before promotion to production.

## Validation

All debug C# scripts compile against **GodotSharp 4.7.2 / .NET 8**. They also compile with Godot 4.5.1 source generators, and the full debug scene starts in Godot .NET 4.5.1 with all 58 trees and 24 rocks, without script errors.

An actual full-scene software OpenGL Compatibility run rendered calm, left-wind and right-wind rain. Runtime assertions checked tree wind values, GPU rain direction, identical cloud/haze displacement, frozen displacement in calm wind, and stopping rain after its fade. Screenshots were inspected for rain angle, comparison panel placement and ground-level splash presentation. This validates the prototype on that renderer, not the project's Windows D3D12 backend or hardware performance; test locally before moving anything into production.
