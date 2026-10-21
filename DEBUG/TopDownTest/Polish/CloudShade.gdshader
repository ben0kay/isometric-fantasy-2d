// Broad world-anchored moving cloud cover, over both scenery and ground.
shader_type canvas_item;
render_mode unshaded;
uniform float opacity = 0.12;
uniform float daylight = 1.0;
uniform vec2 velocity = vec2(5.0, 2.0);
varying vec2 world_position;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(cell), hash(cell + vec2(1.0, 0.0)), f.x),
        mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), f.x), f.y);
}
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 p = (world_position + TIME * velocity) / 210.0;
    float cloud = 0.7 * noise(p) + 0.3 * noise(p * 1.9 + vec2(9.0));
    float shade = smoothstep(0.35, 0.72, cloud);
    COLOR = vec4(0.05, 0.075, 0.11, shade * opacity * daylight);
}
