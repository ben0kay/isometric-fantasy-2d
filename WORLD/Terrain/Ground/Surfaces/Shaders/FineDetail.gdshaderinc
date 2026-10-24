// Small ground-level mottling only; quantized world coordinates preserve pixel texture.
uniform bool fine_detail = true;
uniform float preview_base_detail_strength = 1.0;
uniform float detail_strength = 0.065;

vec3 grass_fine_detail(vec3 surface, vec2 world) {
    if (!fine_detail || preview_base_detail_strength<=0.0) return surface;
    vec2 pixel = floor(world) + vec2(0.5);
    float detail = texture(ground_noise, pixel / 48.0 + vec2(0.23, 0.59)).r;
    float grain = texture(ground_noise, pixel / 13.0 + vec2(0.71, 0.19)).r;
    float fibres = smoothstep(0.48, 0.78, detail);
    return surface * (1.0 + ((fibres - 0.5) + (grain - 0.5) * 0.45) * mix(detail_strength, secondary_detail_strength, biome_weight(world))*preview_base_detail_strength);
}
