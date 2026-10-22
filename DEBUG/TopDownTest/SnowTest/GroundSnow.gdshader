// Distributed local deposits; surface shading stays independent from coverage.
shader_type canvas_item;
uniform sampler2D snow_noise : repeat_enable, filter_linear;
uniform sampler2D snow_detail : repeat_enable, filter_linear;
uniform float snow_coverage = 0.0;
uniform vec4 snow_colour : source_color = vec4(0.87, 0.91, 0.94, 1.0);
varying vec2 snow_world;
void vertex() { snow_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    // Small overlapping patches distribute snow throughout the terrain.
    vec2 uv = snow_world / 128.0;
    vec2 warp = vec2(texture(snow_noise, uv * 0.71 + vec2(0.13, 0.47)).r,
        texture(snow_noise, uv * 0.71 + vec2(0.61, 0.29)).r) - vec2(0.5);
    float broad = texture(snow_noise, uv + warp * 0.08).r;
    float small = texture(snow_noise, uv * 2.0 + vec2(0.37, 0.69)).r;
    float deposit = broad * 0.60 + small * 0.40;
    // Grow each local deposit instead of reserving whole regions for late accumulation.
    float threshold = 1.0 - smoothstep(0.30, 0.70, deposit);
    float cover = 1.0 - smoothstep(snow_coverage - 0.035, snow_coverage + 0.035, threshold);
    if (snow_coverage <= 0.0) cover = 0.0;
    if (snow_coverage >= 1.0) cover = 1.0;
    float roll = texture(snow_detail, snow_world / 420.0 + vec2(0.21, 0.53)).r;
    float fine = texture(snow_detail, (floor(snow_world) + vec2(0.5)) / 34.0).r;
    vec3 surface = snow_colour.rgb * mix(vec3(0.965, 0.98, 1.0), vec3(1.01), roll);
    surface *= 0.985 + fine * 0.030;
    COLOR = vec4(surface, cover);
}
