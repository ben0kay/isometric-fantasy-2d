// Crisp screen colour adjustment; no blur, chromatic aberration or pixel resampling.
shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float contrast = 1.10;
uniform float saturation = 1.08;
uniform float vignette = 0.08;

void fragment() {
    vec3 colour = texture(screen_texture, SCREEN_UV).rgb;
    float luma = dot(colour, vec3(0.2126, 0.7152, 0.0722));
    colour = mix(vec3(luma), colour, saturation);
    // Gentle S-curve preserves black and white, avoiding a crushed night-time floor.
    colour += (contrast - 1.0) * colour * (vec3(1.0) - colour) * (2.0 * colour - vec3(1.0));
    float edge = smoothstep(0.25, 0.75, length(SCREEN_UV - vec2(0.5)));
    COLOR = vec4(clamp(colour * (1.0 - vignette * edge), vec3(0.0), vec3(1.0)), 1.0);
}
