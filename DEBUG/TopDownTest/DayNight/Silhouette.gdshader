// Flat alpha silhouette for props without foliage wind.
shader_type canvas_item;
varying vec4 tint;

void vertex() {
    tint = COLOR;
}

void fragment() {
    COLOR = vec4(0.055, 0.075, 0.09, texture(TEXTURE, UV).a) * tint;
}
