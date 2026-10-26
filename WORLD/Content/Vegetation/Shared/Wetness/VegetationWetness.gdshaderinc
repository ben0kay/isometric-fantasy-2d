#include "res://ENVIRONMENT/Climate/RegionalWeather.gdshaderinc"
// Wet leaves/petals darken after seasonal colour; leaf identity comes from original RGB or mask.
// Inject into the live wind shader BEFORE snow coverage and fire erosion.
uniform float wet_coverage : hint_range(0.0, 1.0) = 0.0;
uniform float wet_darkening : hint_range(0.0, 0.35) = 0.15;
uniform float wet_foliage_profile = 0.0; // 0 = green leaves; 1 = pink blossoms.

vec3 wet_foliage_colour(vec3 original, vec3 source_rgb, vec2 uv) {
    if (wet_coverage <= 0.0001) return original;
    float foliage = foliage_selection(source_rgb, uv, wet_foliage_profile, vec2(0.60, 0.79));
    float shade = clamp(wet_coverage * wet_darkening * foliage * regional_rain(regional_foliage_world), 0.0, 0.35);
    return original * (1.0 - shade);
}
