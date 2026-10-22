// Same curve as BiomeMasterConfig.EdgeAt: continuous world-space colour transition.
uniform float biome_boundary_x = 1760.0;
uniform float biome_curve_amplitude = 70.0;
uniform float biome_curve_scale = 340.0;
uniform float biome_blend_width = 120.0;
uniform vec4 west_base : source_color = vec4(0.2196, 0.2745, 0.2078, 1.0);
uniform vec4 east_base : source_color = vec4(0.43, 0.265, 0.35, 1.0);
uniform vec4 east_shade : source_color = vec4(0.32, 0.19, 0.28, 1.0);
uniform vec4 east_fresh : source_color = vec4(0.52, 0.34, 0.43, 1.0);
uniform vec4 east_dry : source_color = vec4(0.49, 0.365, 0.37, 1.0);
float biome_weight(vec2 world) {
    float y = world.y / max(100.0, biome_curve_scale);
    float edge = biome_boundary_x + biome_curve_amplitude * (sin(y) + 0.35 * sin(y * 2.17 + 1.3));
    float half_width = max(1.0, biome_blend_width) * 0.5;
    return smoothstep(-half_width, half_width, world.x - edge);
}
