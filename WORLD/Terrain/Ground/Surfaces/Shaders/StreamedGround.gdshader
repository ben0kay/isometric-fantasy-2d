// Shared world-coordinate detail with bilinear coarse biome palettes; chunk borders share every sample.
shader_type canvas_item;
#include "res://WORLD/Terrain/Ground/Surfaces/Shaders/NoiseSampling.gdshaderinc"
uniform sampler2D ground_art : filter_nearest, repeat_disable;
uniform float art_columns;
uniform sampler2D map_base : filter_linear, repeat_disable;
uniform sampler2D map_shade : filter_linear, repeat_disable;
uniform sampler2D map_fresh : filter_linear, repeat_disable;
uniform sampler2D map_dry : filter_linear, repeat_disable;
uniform sampler2D map_settings : filter_linear, repeat_disable;
uniform bool has_ground_tint = false;
uniform sampler2D map_tint : filter_linear, repeat_disable;
uniform vec2 map_origin;
uniform vec2 map_grid_size;
uniform float map_cell_size;
varying vec2 ground_world;
varying vec4 ground_tint;
void vertex() { ground_world = (MODEL_MATRIX * vec4(VERTEX,0.0,1.0)).xy; ground_tint = COLOR; }
void fragment() {
    vec2 lookup = (ground_world - map_origin) / (map_grid_size * map_cell_size);
    vec3 settings = texture(map_settings,lookup).rgb;
    vec2 uv = ground_world / max(settings.g,80.0) * 0.18;
    vec2 warp = vec2(texture(ground_noise,uv*0.63+vec2(0.17,0.43)).r,texture(ground_noise,uv*0.63+vec2(0.71,0.29)).r)-vec2(0.5);
    float broad = texture(ground_noise,uv+warp*0.05).r;
    float dry = smoothstep(0.63,0.84,texture(ground_noise,uv*1.37+vec2(0.31,0.67)).r);
    vec3 patch = mix(texture(map_shade,lookup).rgb,texture(map_fresh,lookup).rgb,smoothstep(0.18,0.82,broad));
    patch = mix(patch,texture(map_dry,lookup).rgb,dry*0.45);
    vec3 surface = mix(texture(map_base,lookup).rgb,patch,clamp(settings.r,0.0,1.0));
    vec2 nearest_lookup = (floor(lookup * map_grid_size)+vec2(0.5))/map_grid_size;
    float art_index = texture(map_settings,nearest_lookup).a;
    vec2 art_uv = (vec2(mod(art_index,art_columns),floor(art_index/art_columns))+fract(ground_world/32.0))/art_columns;
    vec4 artwork = texture(ground_art,art_uv);
    surface = mix(surface,artwork.rgb,0.22*artwork.a);
    vec2 pixel = floor(ground_world)+vec2(0.5);
    float detail = texture(ground_noise,pixel/48.0+vec2(0.23,0.59)).r;
    float grain = texture(ground_noise,pixel/13.0+vec2(0.71,0.19)).r;
    surface *= 1.0 + ((smoothstep(0.48,0.78,detail)-0.5)+(grain-0.5)*0.45)*settings.b;
    if (has_ground_tint) surface *= texture(map_tint,lookup).rgb;
    COLOR = vec4(surface,1.0) * ground_tint;
}
