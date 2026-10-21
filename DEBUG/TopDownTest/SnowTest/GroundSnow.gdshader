// Independent world-space ground snow above grass/path/gravel, below shadows and actors.
shader_type canvas_item;
uniform sampler2D snow_noise : repeat_enable, filter_linear;
uniform float snow_coverage = 0.0;
uniform vec4 snow_colour : source_color = vec4(0.87, 0.91, 0.94, 1.0);
varying vec2 snow_world;
void vertex() { snow_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    float broad = texture(snow_noise, snow_world / 1500.0).r;
    float fine = texture(snow_noise, (floor(snow_world) + vec2(0.5)) / 46.0).r;
    float deposit = broad * 0.80 + fine * 0.20;
    float cover = smoothstep(1.0 - snow_coverage, 1.10 - snow_coverage, deposit);
    if (snow_coverage <= 0.0) cover = 0.0;
    COLOR = vec4(snow_colour.rgb * (0.91 + fine * 0.12), cover);
}
