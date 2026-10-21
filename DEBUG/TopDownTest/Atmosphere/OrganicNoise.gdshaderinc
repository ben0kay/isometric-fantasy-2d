// Both overlays sample one shared, seamless FastNoiseLite texture.
// Linear filtering avoids shader-calculated cell/hash seams on different GPU backends.
uniform sampler2D atmosphere_noise_texture : repeat_enable, filter_linear;

// Gentle texture-space warp keeps broad patches organic without procedural cell arithmetic.
float atmosphere_field(vec2 p) {
    vec2 warp = vec2(
        texture(atmosphere_noise_texture, p * 0.09 + vec2(0.13, 0.47)).r,
        texture(atmosphere_noise_texture, p * 0.09 + vec2(0.61, 0.23)).r
    ) - vec2(0.5);
    vec2 uv = p * 0.17 + warp * 0.10;
    float broad = texture(atmosphere_noise_texture, uv).r;
    float detail = texture(atmosphere_noise_texture, uv * 1.91 + vec2(0.37, 0.69)).r;
    return mix(broad, detail, 0.20);
}
