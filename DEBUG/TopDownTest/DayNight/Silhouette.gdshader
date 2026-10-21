// Flat alpha silhouette for props without foliage wind.
shader_type canvas_item;
uniform float softness_uv : hint_range(0.0, 0.025) = 0.0;
varying vec4 tint;




void vertex() {
    tint = COLOR;
}

void fragment() {
float radius = softness_uv * (1.0 - UV.y);
    float alpha = texture(TEXTURE, UV).a;
    if (radius > 0.00001) {
        vec2 d = vec2(radius);
        alpha *= 0.4;
        alpha += texture(TEXTURE, UV + vec2(d.x, 0.0)).a * 0.075;
        alpha += texture(TEXTURE, UV + vec2(-d.x, 0.0)).a * 0.075;
        alpha += texture(TEXTURE, UV + vec2(0.0, d.y)).a * 0.075;
        alpha += texture(TEXTURE, UV + vec2(0.0, -d.y)).a * 0.075;
        alpha += texture(TEXTURE, UV + d * 0.707).a * 0.075;
        alpha += texture(TEXTURE, UV + -d * 0.707).a * 0.075;
        alpha += texture(TEXTURE, UV + vec2(d.x, -d.y) * 0.707).a * 0.075;
        alpha += texture(TEXTURE, UV + vec2(-d.x, d.y) * 0.707).a * 0.075;
    }
    COLOR = vec4(0.055, 0.075, 0.09, alpha) * tint;
}
