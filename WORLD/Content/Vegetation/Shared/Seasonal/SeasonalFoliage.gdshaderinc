// One artwork mask serves seasons and weather. Grayscale is data, never source_color.
uniform bool foliage_has_mask = false;
uniform sampler2D foliage_mask : repeat_disable, filter_nearest;
uniform bool seasonal_profile_enabled = false;
uniform sampler2D seasonal_state : repeat_disable, filter_nearest;
instance uniform bool foliage_healthy = true;
instance uniform float seasonal_bucket = 0.0;

// Explicit masks replace heuristics; source alpha remains owned by the final sprite output.
// =========================================================
float foliage_selection(vec3 original, vec2 uv, float profile, vec2 crown_range) {
    if (!foliage_healthy) return 0.0;
    if (foliage_has_mask) return texture(foliage_mask, clamp(uv, vec2(0.0), vec2(1.0))).r;
    float green = smoothstep(0.005, 0.065, original.g - max(original.r * 0.88, original.b));
    float pink = smoothstep(0.025, 0.11, original.r - original.g)
        * smoothstep(-0.015, 0.09, original.b - original.g * 0.85);
    return mix(green, pink, step(0.5, profile))
        * (1.0 - smoothstep(crown_range.x, crown_range.y, uv.y));
}

// Palette controls colour, saturation and optional winter lift while retaining cluster shading.
// Summer/disabled alpha zero returns the exact original RGB, with no tint arithmetic.
// =========================================================
vec3 seasonal_foliage_colour(vec3 original, vec2 uv) {
    if (!seasonal_profile_enabled || !foliage_healthy) return original;
    float bucket_u = (clamp(floor(seasonal_bucket + 0.5), 0.0, 15.0) + 0.5) / 16.0;
    vec4 state = texture(seasonal_state, vec2(bucket_u, 0.25));
    if (state.a <= 0.0) return original;
    float selection = foliage_selection(original, uv, 0.0, vec2(0.60, 0.79));
    float light = dot(original, vec3(0.3, 0.6, 0.1));
    vec2 tone = texture(seasonal_state, vec2(bucket_u, 0.75)).rg;
    float target_light = max(dot(state.rgb, vec3(0.3, 0.6, 0.1)), 0.001);
    vec3 target = mix(vec3(target_light), state.rgb, tone.x);
    // Lift dark foliage gently without making true black pixels white or erasing shading.
    float winter_light = light + tone.y * sqrt(max(light, 0.0)) * (1.0 - light);
    vec3 recoloured = clamp(target * winter_light / target_light, vec3(0.0), vec3(1.0));
    return mix(original, recoloured, selection * state.a);
}
