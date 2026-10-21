// Pixel-sized cellular pebbles and fine dirt, independent of patch shape and lighting.
vec3 gravel_surface(vec2 world, sampler2D pebble_noise, sampler2D broad_noise, vec3 dark, vec3 light) {
    vec2 p = floor(world) + vec2(0.5);
    vec2 uv = p / 256.0;
    float stone = texture(pebble_noise, uv).r;
    float neighbour = texture(pebble_noise, uv + vec2(1.0 / 256.0, 0.0)).r;
    float seams = smoothstep(0.12, 0.40, abs(stone - neighbour));
    float dirt = texture(broad_noise, p / 37.0 + vec2(0.23, 0.51)).r;
    vec3 gravel = mix(dark, light, smoothstep(0.12, 0.85, stone));
    gravel *= 1.0 - seams * 0.18;
    return gravel * (0.93 + dirt * 0.14);
}
