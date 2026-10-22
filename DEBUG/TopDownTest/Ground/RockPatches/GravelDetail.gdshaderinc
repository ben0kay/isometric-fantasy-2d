// Fine restrained stone grain; pebble colour never determines patch coverage or its outer edge.
vec3 gravel_surface(vec2 world, sampler2D pebble_noise, sampler2D broad_noise, vec3 dark, vec3 light) {
    vec2 p = floor(world) + vec2(0.5);
    float stone = texture(pebble_noise, p / 360.0).r;
    float dirt = texture(broad_noise, world / 120.0 + vec2(0.23, 0.51)).r;
    vec3 gravel = mix(dark, light, mix(0.22, 0.72, stone));
    return gravel * (0.96 + dirt * 0.08);
}
