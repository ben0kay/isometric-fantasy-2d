// Smooth coverage and subtle drift shading are separate from fine snow surface detail.
shader_type canvas_item;
uniform sampler2D snow_noise : repeat_enable, filter_linear;
uniform sampler2D snow_detail : repeat_enable, filter_linear;
uniform float snow_coverage = 0.0;
uniform vec4 snow_colour : source_color = vec4(0.87, 0.91, 0.94, 1.0);
varying vec2 snow_world;
void vertex() { snow_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 uv = snow_world / 1800.0;
    vec2 warp = vec2(texture(snow_noise, uv * 0.71 + vec2(0.13, 0.47)).r,
        texture(snow_noise, uv * 0.71 + vec2(0.61, 0.29)).r) - vec2(0.5);
    float deposit = texture(snow_noise, uv + warp * 0.16).r;
    float cover = smoothstep(1.0 - snow_coverage, 1.025 - snow_coverage, deposit);
    if (snow_coverage <= 0.0) cover = 0.0;
    // Broad, pale-blue roll shadows give settled snow volume; microtexture never tears its edge.
    float roll = texture(snow_detail, snow_world / 420.0 + vec2(0.21, 0.53)).r;
    float fine = texture(snow_detail, (floor(snow_world) + vec2(0.5)) / 34.0).r;
    vec3 surface = snow_colour.rgb * mix(vec3(0.965, 0.98, 1.0), vec3(1.01), roll);
    surface *= 0.985 + fine * 0.030;
    COLOR = vec4(surface, cover);
}
