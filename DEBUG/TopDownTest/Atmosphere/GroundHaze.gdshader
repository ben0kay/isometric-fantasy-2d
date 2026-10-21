// Thin patchy ground mist; world-anchored noise leaves clear gaps.
shader_type canvas_item;
uniform float density = 0.22;
uniform float patch_size = 190.0;
uniform vec2 drift = vec2(0.0);
uniform float daylight = 1.0;
uniform vec4 haze_colour : source_color = vec4(0.74, 0.80, 0.79, 1.0);
varying vec2 world_position;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float mist_noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(cell), hash(cell + vec2(1.0, 0.0)), f.x),
        mix(hash(cell + vec2(0.0, 1.0)), hash(cell + vec2(1.0, 1.0)), f.x), f.y);
}
void vertex() { world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 p = (world_position + drift) / max(30.0, patch_size);
    p.y *= 2.3;
    float layers = 0.65 * mist_noise(p) + 0.35 * mist_noise(p * 1.8 + vec2(13.0));
    float wisps = smoothstep(0.40, 0.73, layers);
    float strength = mix(1.0, 0.32, daylight);
    // Match map edges softly instead of ending in an obvious rectangular sheet.
    vec2 edge = min(world_position, vec2(960.0, 640.0) - world_position);
    float border = smoothstep(0.0, 30.0, min(edge.x, edge.y));
    COLOR = vec4(haze_colour.rgb, wisps * density * strength * border * haze_colour.a);
}
