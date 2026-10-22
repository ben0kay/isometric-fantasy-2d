// Generic liquid composition, using a precomputed polygon distance field and regular 2D lighting.
shader_type canvas_item;
#include "res://DEBUG/TopDownTest/Liquids/Shaders/LiquidMovement.gdshaderinc"
#include "res://DEBUG/TopDownTest/Liquids/Shaders/LiquidColour.gdshaderinc"
#include "res://DEBUG/TopDownTest/Liquids/Shaders/ShoreEdge.gdshaderinc"
uniform sampler2D distance_field : repeat_disable, filter_linear;
uniform sampler2D surface_noise : repeat_enable, filter_linear;
uniform vec4 shallow_colour : source_color = vec4(0.22, 0.36, 0.32, 1.0);
uniform vec4 deep_colour : source_color = vec4(0.075, 0.19, 0.22, 1.0);
uniform vec4 highlight_colour : source_color = vec4(0.52, 0.67, 0.59, 1.0);
uniform vec4 bank_colour : source_color = vec4(0.30, 0.28, 0.22, 1.0);
uniform float depth_distance = 65.0;
uniform float shore_width = 18.0;
uniform vec2 wind_drift = vec2(0.0);
uniform float surface_time = 0.0;
uniform float wind_strength = 1.0;
uniform float ripples = 0.35;
varying vec2 liquid_world;
void vertex() { liquid_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    float distance = (texture(distance_field, vec2(UV.x, 1.0 - UV.y)).r - 0.5) * 512.0;
    float noise = texture(surface_noise, liquid_world / 350.0).r;
    vec3 water = liquid_colour(distance, depth_distance, noise, shallow_colour.rgb, deep_colour.rgb);
    float wave = liquid_ripples(liquid_world - wind_drift, surface_time, wind_strength, noise);
    water += highlight_colour.rgb * wave * clamp(ripples, 0.0, 1.0) * 0.12;
    float lap = (1.0 - smoothstep(1.0, 5.0, distance)) * smoothstep(-0.5, 1.0, distance);
    water = mix(water, highlight_colour.rgb, lap * (0.08 + wave * 0.12));
    float wet = smoothstep(-0.7, 0.7, distance);
    float bank = liquid_bank(distance, shore_width, noise) * 0.80;
    COLOR = vec4(mix(bank_colour.rgb * (0.90 + noise * 0.18), water, wet), max(wet, bank));
}
