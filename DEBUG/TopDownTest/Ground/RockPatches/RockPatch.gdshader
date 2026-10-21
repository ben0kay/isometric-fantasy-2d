// Ground decal composition only; CanvasModulate, torchlight and shadows remain independent.
shader_type canvas_item;
#include "res://DEBUG/TopDownTest/Ground/RockPatches/OrganicEdge.gdshaderinc"
#include "res://DEBUG/TopDownTest/Ground/RockPatches/GravelDetail.gdshaderinc"
uniform sampler2D broad_noise : repeat_enable, filter_linear;
uniform sampler2D pebble_noise : repeat_enable, filter_nearest;
uniform float opacity = 0.85;
uniform vec4 dark_gravel : source_color = vec4(0.24, 0.255, 0.23, 1.0);
uniform vec4 light_gravel : source_color = vec4(0.43, 0.435, 0.39, 1.0);
varying vec2 gravel_world;
varying vec4 gravel_tint;
void vertex() {
    gravel_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    gravel_tint = COLOR;
}
void fragment() {
    float coverage = gravel_coverage(UV, gravel_world, broad_noise);
    vec3 surface = gravel_surface(gravel_world, pebble_noise, broad_noise, dark_gravel.rgb, light_gravel.rgb);
    COLOR = vec4(surface, coverage * opacity) * gravel_tint;
}
