// FIRE2: irregular healthy-foliage erosion injected after existing wind/snow shading.
// No new texture upload per frame: uses the shared world-noise texture on the GPU.
uniform sampler2D fire_burn_noise : repeat_enable, filter_linear;
uniform float fire_burn_progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 fire_char_colour : source_color = vec4(0.18, 0.105, 0.075, 1.0);

// Preserve the original trunk until late, but burn irregular holes through the crown.
// =========================================================
vec4 fire_erode_pixel(vec4 original, vec2 sample_uv, vec2 raw_uv) {
    float progress = clamp(fire_burn_progress, 0.0, 1.0);
    float crown = 1.0 - smoothstep(0.57, 0.81, raw_uv.y);
    float broad = texture(fire_burn_noise, sample_uv * vec2(3.0, 2.1)).r;
    float detail = texture(fire_burn_noise, sample_uv * vec2(8.4, 5.9) + vec2(0.29, 0.41)).r;
    float threshold = 0.12 + broad * 0.59 + detail * 0.21;

    float foliage_loss = smoothstep(threshold - 0.085, threshold + 0.085, progress);
    float trunk_loss = smoothstep(0.83, 1.0, progress);
    float erased = mix(trunk_loss, foliage_loss, crown);

    // Darken surviving leaf edges as the advancing burn reveals the charred underlay.
    float charring = clamp(progress * (0.75 + crown * 0.42)
        + max(0.0, progress - threshold + 0.12) * crown * 1.3, 0.0, 1.0);
    original.rgb = mix(original.rgb, original.rgb * fire_char_colour.rgb * 1.45,
                       charring * (0.28 + crown * 0.65));
    // A faint orange leading edge; applied only on surviving crown fragments.
    float glow = (1.0 - smoothstep(0.0, 0.11, abs(progress - threshold)))
        * crown * (1.0 - foliage_loss) * progress;
    original.rgb += vec3(0.32, 0.07, 0.01) * glow;
    original.a *= (1.0 - erased);
    return original;
}
