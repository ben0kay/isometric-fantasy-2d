// Same analytical boundary as AdjacentBiomeLayout.EdgeAt; no per-tile nodes or texture uploads.
uniform bool adjacent_biome = false;
uniform float biome_boundary_x = 1760.0;
uniform float biome_curve_amplitude = 70.0;
uniform float biome_curve_scale = 340.0;
uniform float biome_blend_width = 240.0;
uniform vec4 secondary_base : source_color = vec4(0.2196, 0.2745, 0.2078, 1.0);
uniform vec4 secondary_shade : source_color = vec4(0.18, 0.245, 0.18, 1.0);
uniform vec4 secondary_fresh : source_color = vec4(0.28, 0.335, 0.23, 1.0);
uniform vec4 secondary_dry : source_color = vec4(0.32, 0.32, 0.22, 1.0);
uniform float secondary_variation = 0.7;
uniform float secondary_patch_size = 320.0;
uniform float secondary_detail_strength = 0.065;
float biome_weight(vec2 world) {
    if (!adjacent_biome) return 0.0;
    float y = world.y / max(100.0, biome_curve_scale);
    float edge = biome_boundary_x + biome_curve_amplitude * (sin(y) + 0.35 * sin(y * 2.17 + 1.3));
    float half_width = max(1.0, biome_blend_width) * 0.5;
    return smoothstep(-half_width, half_width, world.x - edge);
}
