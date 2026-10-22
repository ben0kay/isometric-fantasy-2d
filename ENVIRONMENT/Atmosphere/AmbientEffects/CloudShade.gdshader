// Continuous, softly warped world-space cloud cover.
shader_type canvas_item;
render_mode unshaded;
#include "res://ENVIRONMENT/Atmosphere/AtmosphereProfiles/OrganicNoise.gdshaderinc"

uniform float opacity = 0.12;
uniform float daylight = 1.0;
uniform vec2 drift = vec2(0.0);
varying vec2 world_position;

void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 p = (world_position + drift) / 260.0;
    float shade = smoothstep(0.28, 0.76, atmosphere_field(p));
    COLOR = vec4(0.05, 0.075, 0.11, shade * opacity * daylight);
}
