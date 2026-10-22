// Wet leaves/petals darken gently, while the trunk remains mostly unchanged.
// Inject into the live wind shader BEFORE snow coverage and fire erosion.
uniform float wet_coverage : hint_range(0.0, 1.0) = 0.0;
uniform float wet_darkening : hint_range(0.0, 0.35) = 0.15;
uniform float wet_foliage_profile = 0.0; // 0 = green leaves; 1 = pink blossoms.

vec3 wet_foliage_colour(vec3 original, vec2 uv) {
    if (wet_coverage <= 0.0001) return original;
    float green = smoothstep(0.005, 0.065, original.g - max(original.r * 0.88, original.b));
    float pink = smoothstep(0.025, 0.11, original.r - original.g)
        * smoothstep(-0.015, 0.09, original.b - original.g * 0.85);
    float foliage = mix(green, pink, step(0.5, wet_foliage_profile));
    float crown = 1.0 - smoothstep(0.60, 0.79, uv.y);
    float shade = clamp(wet_coverage * wet_darkening * foliage * crown, 0.0, 0.35);
    return original * (1.0 - shade);
}
