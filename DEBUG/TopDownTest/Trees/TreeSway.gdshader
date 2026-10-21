// Row-weighted crown wind; lower trunk and roots remain stationary.
shader_type canvas_item;

uniform float strength_pixels : hint_range(0.0, 30.0, 0.5) = 14.0;
uniform float speed : hint_range(0.0, 3.0, 0.05) = 0.8;
uniform float canopy_end : hint_range(0.1, 0.95, 0.01) = 0.70;

varying float tree_phase;
varying vec4 sprite_tint;

// Vary the breeze by placement without per-tree CPU updates.
// =========================================================
void vertex() {
    tree_phase = dot(MODEL_MATRIX[3].xy, vec2(0.017, 0.011));
    sprite_tint = COLOR;
}

// Move upper rows independently so the lower trunk is not sheared.
// =========================================================
void fragment() {
    float t = TIME * speed;
    float gust = 0.75 + 0.25 * sin(t * 0.37);
    float sway = 0.75 * sin(t + tree_phase)
        + 0.25 * sin(t * 1.83 + tree_phase * 0.63);
    float crown_weight = 1.0 - smoothstep(0.12, canopy_end, UV.y);
    vec2 sample_uv = UV;
    sample_uv.x -= sway * gust * strength_pixels * TEXTURE_PIXEL_SIZE.x * crown_weight;
    vec4 pixel = texture(TEXTURE, clamp(sample_uv, vec2(0.0), vec2(1.0)));
    float inside = step(0.0, sample_uv.x) * step(sample_uv.x, 1.0);
    COLOR = pixel * sprite_tint * inside;
}
