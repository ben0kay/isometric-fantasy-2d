// Organic world-anchored ground mist with soft, irregular edges.
shader_type canvas_item;
#include "res://DEBUG/TopDownTest/Atmosphere/OrganicNoise.gdshaderinc"

uniform float density = 0.22;
uniform float patch_size = 190.0;
uniform vec2 drift = vec2(0.0);
uniform float daylight = 1.0;
uniform vec2 world_min = vec2(-480.0, -320.0);
uniform vec2 world_max = vec2(1440.0, 960.0);
uniform vec4 haze_colour : source_color = vec4(0.74, 0.80, 0.79, 1.0);
varying vec2 world_position;

void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 p = (world_position + drift) / max(30.0, patch_size);
    p.y *= 1.8;
    float wisps = smoothstep(0.30, 0.78, atmosphere_field(p));
    float strength = mix(1.0, 0.32, daylight);
    vec2 edge = min(world_position - world_min, world_max - world_position);
    float border = smoothstep(0.0, 30.0, min(edge.x, edge.y));
    COLOR = vec4(haze_colour.rgb, wisps * density * strength * border * haze_colour.a);
}
