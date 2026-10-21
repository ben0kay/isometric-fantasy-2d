# Disposable rain and shared wind preview

Everything here is a prototype inside `DEBUG/TopDownTest/`. Build C# and press F5/F6 as before. The **Debug rain / shared wind** panel sits below Atmosphere at the right.

## Live controls

- **Rain 0–100%:** starts at 55%, fades in/out over two seconds. Zero stops new drops and lets existing ones finish their short lifetime.
- **Wind -3 to +3:** negative blows left, zero is calm, positive blows right. Drives rain slant, canopy/silhouette wind, haze, cloud shade, pollen, leaves, smoke and embers. Rain stays nearly vertical in calm air. Mirrored trees have matching wind direction.
- **Ground splashes:** separate toggle for tiny ground rings and spray droplets.
- **Storm lightning:** automatic events while rain is at least 35%. Nearby illumination occurs every 8–18 real seconds; only 4% of events become visible ground strikes (about one per five minutes on average, not a guarantee).
- **Test nearby flash / Test visible strike:** trigger either immediately without waiting for rain. The master lightning toggle must be on; a 0.8-second cooldown prevents stacking.
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

## Lightning prototype

`DebugLightning.cs` is a separate child helper under Weather. A nearby event briefly blends actual CanvasModulate scene illumination toward a cool lightning tint; it is not a white overlay over the UI. A visible strike adds a world-anchored zigzag bolt with three branches, layered glow strokes and one reusable radial PointLight2D at the impact. The shape stays stable during each event. Two brief pulses fade completely within 0.65 seconds. UI remains unaffected, and disabling lightning immediately clears illumination, bolt and impact light.

Visible impacts use the splash helper's cached ground exclusions, choosing open ground in the middle/lower visible map. If no clear point fits after 40 attempts, the event becomes a nearby flash instead. There is no damage, fire, scorch persistence, thunder/audio, new lightning shadows or production dependency. The flash does not change the saved day/night settings or clock. Day/night lighting is recomputed normally every frame and restored after the transient blend.

Inspector settings in WeatherMasterConfig: Lightning, LightningIntervalMin/Max, VisibleStrikeChance, LightningRainThreshold, NearbyFlashStrength, StrikeFlashStrength and LightningTint. Automatic timers run only during sufficient rain, independently of the demo clock; entering rainy conditions starts a new interval. Manual preview buttons bypass the rain threshold. Visual strikes are intentionally rare; use the button to judge them.

Cost: a small fixed set of line segments and a single local light during visible strikes. Nearby events only alter the existing ambient colour. No per-frame geometry randomization or particle/node spawning. The glow is an inexpensive approximation, not volumetric lightning or HDR bloom.

Lightning validation: all debug scripts compile against GodotSharp 4.7.2 and with the Godot 4.5.1 source generators. A full-scene software OpenGL run rendered both events at night and asserted ambient brightening, impact-light activation only for visible strikes, exact restoration and immediate toggle cancellation. A headless timed simulation additionally checked automatic nearby events, automatic visible events with probability forced to 100% for testing, and suppression after rain stops. No Windows D3D12 or hardware performance validation is claimed.

## More visible ground splashes

Splashes sample uniformly across the **entire camera-visible world rectangle**, intersected with map bounds, including upper/middle/lower terrain. Prop artwork and player exclusions still apply; dense foliage therefore reduces accepted splashes. There is no bottom-of-screen restriction.

Defaults now use SplashOpacity 0.72, SplashSize 1.25 and SplashLifetime 0.38 seconds, with slightly thicker rings/spray and an unshaded material so night tint does not hide them. Rain intensity still controls spawn rate and visibility. These three settings are live Inspector controls in DefaultWeatherConfig.tres. The fixed 96-slot pool and bounded spawn attempts are retained. Exclusion margins cover the larger rings. This remains a disposable debug comparison.
