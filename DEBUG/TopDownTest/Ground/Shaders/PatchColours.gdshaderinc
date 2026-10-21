// Broad, softly warped colour patches only. No weather, lighting or fine texture here.
uniform bool colour_patches = true;
uniform float variation = 0.7;
uniform float patch_size = 320.0;
uniform vec4 shade_colour : source_color = vec4(0.18, 0.245, 0.18, 1.0);
uniform vec4 fresh_colour : source_color = vec4(0.28, 0.335, 0.23, 1.0);
uniform vec4 dry_colour : source_color = vec4(0.32, 0.32, 0.22, 1.0);

vec3 grass_patch_colour(vec2 world) {
    vec2 uv = world / max(patch_size, 80.0) * 0.18;
    vec2 warp = vec2(texture(ground_noise, uv * 0.63 + vec2(0.17, 0.43)).r,
        texture(ground_noise, uv * 0.63 + vec2(0.71, 0.29)).r) - vec2(0.5);
    float broad = texture(ground_noise, uv + warp * 0.05).r;
    float dry = smoothstep(0.63, 0.84, texture(ground_noise, uv * 1.37 + vec2(0.31, 0.67)).r);
    vec3 patches = mix(shade_colour.rgb, fresh_colour.rgb, smoothstep(0.18, 0.82, broad));
    patches = mix(patches, dry_colour.rgb, dry * 0.45);
    return mix(vec3(0.2196, 0.2745, 0.2078), patches, colour_patches ? clamp(variation, 0.0, 1.0) : 0.0);
}
