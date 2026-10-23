// Resolution-independent, row-weighted foliage sway with a wind-sized transparent draw margin.
// Crown pixels move in world units; roots stay steady and texture dimensions do not set wind strength.
shader_type canvas_item;

uniform float strength_world_pixels : hint_range(0.0, 20.0, 0.1) = 1.6;
uniform float speed : hint_range(0.0, 3.0, 0.05) = 0.8;
uniform float canopy_end : hint_range(0.1, 0.95, 0.01) = 0.70;

uniform float wind_multiplier : hint_range(-3.0, 3.0) = 1.0;
uniform float wind_orientation = 1.0;
uniform bool shadow_only = false;
uniform float softness_uv : hint_range(0.0, 0.025) = 0.0;

varying float tree_phase;
varying vec4 sprite_tint;
varying float source_pixels_per_world;

// Extend the quad only as far as the current wind can move foliage, on either side.
// Remap its UVs to the original texture (0..1) plus transparent margins; FlipH is supported.
// =========================================================
void vertex() {
    tree_phase = dot(MODEL_MATRIX[3].xy, vec2(0.017, 0.011));
    sprite_tint = COLOR;
    source_pixels_per_world = 1.0 / max(length(MODEL_MATRIX[0].xy), 0.001);

    float padding_local = strength_world_pixels * abs(wind_multiplier * wind_orientation)
        * source_pixels_per_world * 1.05;
    float padding_uv = padding_local * TEXTURE_PIXEL_SIZE.x;
    float side = (UV.x < 0.5 ? -1.0 : 1.0) * (wind_orientation < 0.0 ? -1.0 : 1.0);
    VERTEX.x += side * padding_local;
    UV.x = UV.x * (1.0 + 2.0 * padding_uv) - padding_uv;
}

// Shift source samples by world distance, not raw PNG pixels.
// Extended geometry allows foliage to render outside the original texture's rectangle.
// =========================================================
void fragment() {
    float t = TIME * speed;
    float gust = 0.75 + 0.25 * sin(t * 0.37);
    float sway = 0.75 * sin(t + tree_phase)
        + 0.25 * sin(t * 1.83 + tree_phase * 0.63);
    float crown_weight = 1.0 - smoothstep(0.12, canopy_end, UV.y);
    vec2 sample_uv = UV;
    float world_sway = (0.35 + sway * gust * 0.65) * strength_world_pixels
        * wind_multiplier * wind_orientation * crown_weight;
    sample_uv.x -= world_sway * source_pixels_per_world * TEXTURE_PIXEL_SIZE.x;

    float inside = step(0.0, sample_uv.x) * step(sample_uv.x, 1.0);
    vec4 pixel = texture(TEXTURE, clamp(sample_uv, vec2(0.0), vec2(1.0)));
    if (shadow_only) {
        pixel.rgb = vec3(0.055, 0.075, 0.09);
        float radius = softness_uv * (1.0 - smoothstep(0.12, 0.9, UV.y));
        float alpha = texture(TEXTURE, clamp(sample_uv, vec2(0.0), vec2(1.0))).a;
        if (radius > 0.00001) {
            vec2 d = vec2(radius);
            alpha *= 0.4;
            alpha += texture(TEXTURE, sample_uv + vec2(d.x, 0.0)).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + vec2(-d.x, 0.0)).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + vec2(0.0, d.y)).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + vec2(0.0, -d.y)).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + d * 0.707).a * 0.075;
            alpha += texture(TEXTURE, sample_uv - d * 0.707).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + vec2(d.x, -d.y) * 0.707).a * 0.075;
            alpha += texture(TEXTURE, sample_uv + vec2(-d.x, d.y) * 0.707).a * 0.075;
        }
        pixel.a = alpha;
    }
    COLOR = pixel * sprite_tint * inside;
}
