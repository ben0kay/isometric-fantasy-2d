# Production rain layers and ground splashes

The production weather controller (WorldWeather.cs) owns the smoothly changing rain intensity, overcast and visual lightning. RainVisualLayers.cs now owns the GPU rain presentation, using **three depth layers** by default:

- DistantRain: small, faint, slower streaks (30% of MaxDrops).
- MainRain: most of the visible droplets (remaining 51%).
- ForegroundRain: larger, sparser drops with independently adjustable strength (19%).

All share the existing WindController/WorldWeather.Rain; intensity is still controlled by the existing Rain slider. **MaxDrops is the combined particle cap**, not a per-layer cap. Each GPU particle has varied scale, initial opacity and fall speed; the generated narrow rain streak image is softened at its edges. No individual drop scene/CPU update or new weather simulator.

Settings reside in WeatherProfiles/WeatherProfile.cs and .tres: LayeredRain=true, DropSoftness=0.40, DropVariation=0.35 and ForegroundRainStrength=0.45, plus existing MaxDrops, FallSpeed, wind slant, intensity and transition. Layer toggle, generated texture softness and per-particle variation ranges are read when the controller starts; restart World to compare new defaults. ForegroundRainStrength and existing rain/wind intensity work live.

**Ground splashes remain in GroundRainSplashes.cs, here in ENVIRONMENT/Weather/Rain/**: the existing 96-impact pool, artwork/player exclusions and shared open-ground targeting reused by WorldLightning are not changed. Liquid rain hits are separately implemented in WORLD/Liquids/Interactions/WorldLiquidRainSplashes.cs.

Foliage wetness is a reusable tree-material effect owned by WORLD/Content/Vegetation/Shared/Wetness/. It responds to WorldWeather.Rain and combines with existing wind, snow and fire materials. Wetness options in WeatherProfile control leaf darkening and drying; F9 developer controls expose an immediate wetness on/off comparison and darkening slider.

**Future scope, NOT included:** camera lens/screen droplets (FEEDBACK/ScreenEffects/RainLens), new storm weather schedules, puddle accumulation, roof edge drips, physics wetness and seasons.

## Testing

1. Build in Godot .NET, run SCENES/World/World.tscn (F6) and open F9. Change Rain 0 to 1: verify distant/main/foreground streaks with softer varying size/opacity.
2. Wind -3 / 0 / +3 tilts all three groups consistently. Foreground density follows the single rain intensity. Set rain back to zero and verify emission stops.
3. Ground splashes, pond ripples, lightning and overcast should operate exactly as before; no changes were made to their behavior.
4. With rain high, oak foliage darkens gently, pink blossoms remain pink but slightly richer, trunks stay readable. Change Wet tree foliage and Leaf darkening live in F9; return rain to zero and confirm gradual drying.
5. Check snow toggling, firing/reset of trees, canopy wind sway and tree-occlusion fade: shaders must coexist without permanently replacing other materials. Future weather states normally choose snow OR rain.
6. Profile frame time and particle budget at 1920x1080, full rain and camera 25% zoomed out. Windows renderer/visual performance still needs checking locally.
