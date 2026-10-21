// Soft irregular footprint only; fully transparent before reaching the supporting quad edges.
float gravel_coverage(vec2 uv, vec2 world, sampler2D broad_noise) {
    vec2 p = uv * 2.0 - 1.0;
    float shape = texture(broad_noise, world / 170.0).r;
    float broken = texture(broad_noise, world / 48.0 + vec2(0.37, 0.61)).r;
    float distance = length(p) + (shape - 0.5) * 0.30;
    float edge = 1.0 - smoothstep(0.53, 0.87, distance);
    // Fade from a continuous gravel bed to sparse scattered grains at its outskirts.
    float scatter = smoothstep(0.22, 0.65, broken);
    float margin = 1.0 - smoothstep(0.88, 0.98, length(p));
    return margin * edge * mix(scatter, 1.0, 1.0 - smoothstep(0.25, 0.60, distance));
}
