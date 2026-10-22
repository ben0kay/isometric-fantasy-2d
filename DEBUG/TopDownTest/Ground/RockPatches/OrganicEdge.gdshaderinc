// Smooth, warped gravel bed with a long fade, transparent before its support rectangle ends.
float gravel_coverage(vec2 uv, vec2 world, sampler2D broad_noise) {
    vec2 p = uv * 2.0 - 1.0;
    vec2 warp = vec2(texture(broad_noise, world / 410.0 + vec2(0.17, 0.41)).r,
        texture(broad_noise, world / 410.0 + vec2(0.63, 0.29)).r) - vec2(0.5);
    float shape = texture(broad_noise, world / 270.0).r;
    float distance = length(p + warp * 0.23) + (shape - 0.5) * 0.13;
    float fade = 1.0 - smoothstep(0.28, 0.87, distance);
    float margin = 1.0 - smoothstep(0.88, 0.98, length(p));
    return fade * fade * margin;
}
