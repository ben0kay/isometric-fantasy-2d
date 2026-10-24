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
uniform bool preview_accompanying_patches = true;
uniform bool preview_base_detail = true;
uniform bool preview_optional_layers = true;
uniform vec2 preview_overlay_visibility = vec2(1.0);
uniform vec2 preview_overlay_coverage = vec2(1.0);
uniform vec2 preview_overlay_opacity = vec2(1.0);
uniform bool blend_ground_art = false;
uniform bool has_surface_styles = false;
uniform sampler2D map_styles : filter_linear, repeat_disable;
uniform bool has_ground_overlays = false;
uniform sampler2D overlay_table : filter_nearest, repeat_disable;
uniform float overlay_rows = 1.0;
#include "res://WORLD/Terrain/Ground/Surfaces/Shaders/LayeredSurface.gdshaderinc"
varying vec2 ground_world;
varying vec4 ground_tint;
void vertex() { ground_world = (MODEL_MATRIX * vec4(VERTEX,0.0,1.0)).xy; ground_tint = COLOR; }
void fragment() {
    vec2 lookup = (ground_world - map_origin) / (map_grid_size * map_cell_size);
    vec3 settings = texture(map_settings,lookup).rgb;
    vec3 surface = texture(map_base,lookup).rgb;
    if (preview_accompanying_patches && settings.r > 0.0) {
        vec2 uv = ground_world / max(settings.g,80.0) * 0.18;
        vec2 warp = vec2(texture(ground_noise,uv*0.63+vec2(0.17,0.43)).r,texture(ground_noise,uv*0.63+vec2(0.71,0.29)).r)-vec2(0.5);
        float broad = texture(ground_noise,uv+warp*0.05).r;
        float dry = smoothstep(0.63,0.84,texture(ground_noise,uv*1.37+vec2(0.31,0.67)).r);
        vec3 patch = mix(texture(map_shade,lookup).rgb,texture(map_fresh,lookup).rgb,smoothstep(0.18,0.82,broad));
        patch = mix(patch,texture(map_dry,lookup).rgb,dry*0.45);
        surface = mix(texture(map_base,lookup).rgb,patch,clamp(settings.r,0.0,1.0));
    }
    vec2 nearest_lookup = (floor(lookup * map_grid_size)+vec2(0.5))/map_grid_size;
    float art_index = texture(map_settings,nearest_lookup).a;
    vec2 art_uv = (vec2(mod(art_index,art_columns),floor(art_index/art_columns))+fract(ground_world/32.0))/art_columns;
    vec4 artwork = texture(ground_art,art_uv);
    if (blend_ground_art) artwork = blended_ground_art(lookup,ground_world);
    float influence = has_surface_styles ? texture(map_styles,lookup).r : 0.22;
    surface = mix(surface,artwork.rgb,influence*artwork.a);
    if (preview_base_detail) {
        vec2 pixel = floor(ground_world)+vec2(0.5);
        float detail = texture(ground_noise,pixel/48.0+vec2(0.23,0.59)).r;
        float grain = texture(ground_noise,pixel/13.0+vec2(0.71,0.19)).r;
        float grain_value = (smoothstep(0.48,0.78,detail)-0.5)+(grain-0.5)*0.45;
        if (has_surface_styles) {
            vec2 styles = texture(map_styles,lookup).gb;
            grain_value = grain_value*(1.0-styles.r-styles.g)
                +material_grain(ground_world,1.0)*styles.r+material_grain(ground_world,2.0)*styles.g;
        }
        surface *= 1.0 + grain_value*settings.b;
    }
    if (has_ground_tint) surface *= texture(map_tint,lookup).rgb;
    if (has_ground_overlays && preview_optional_layers) surface = blended_ground_layers(surface,lookup,ground_world);
    COLOR = vec4(surface,1.0) * ground_tint;
}
