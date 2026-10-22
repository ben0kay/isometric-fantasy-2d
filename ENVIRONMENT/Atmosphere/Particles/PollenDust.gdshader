// Same bounded seeded drift, colour and daylight response as the former C# pollen circles.
shader_type canvas_item;
render_mode unshaded;
uniform vec2 view_size = vec2(1920.0,1080.0);
uniform vec2 drift = vec2(0.0);
uniform float daylight = 1.0;
varying float speck_alpha;
void vertex() {
    float index = round(INSTANCE_CUSTOM.r * 100.0);
    float phase = index * 2.39996;
    vec2 area = max(view_size,vec2(1.0));
    vec2 p = mod(vec2(index * 137.4,index * 83.7) + drift
        + vec2(sin(TIME * 0.4 + phase) * 6.0,sin(TIME * 0.3 + phase) * 4.0),area);
    float radius = mod(index,4.0) < 0.5 ? 0.8 : 0.5;
    VERTEX = VERTEX * radius + p - area * 0.5;
    speck_alpha = (0.10 + 0.35 * daylight) * (0.6 + 0.4 * sin(TIME + phase));
}
void fragment() {
    float edge = 1.0 - smoothstep(0.75,1.0,length((UV - vec2(0.5)) * 2.0));
    COLOR = vec4(0.82,0.79,0.57,speck_alpha * edge);
}
