// One optional material for snow resting on the upper face of 2D rock sprites.
// Uses the same settled coverage, tint and noise as trees/grass; never edits sprite alpha.
shader_type canvas_item;
#include "res://ENVIRONMENT/Climate/RegionalWeather.gdshaderinc"

uniform sampler2D snow_noise : repeat_enable, filter_linear;
uniform float snow_coverage : hint_range(0.0, 1.0) = 0.0;
uniform float cap_depth : hint_range(0.25, 0.85) = 0.58;
uniform vec4 snow_colour : source_color = vec4(0.87, 0.91, 0.94, 1.0);

varying vec2 regional_rock_world;
varying vec4 sprite_tint;
varying float placement_phase;

// A phase per world object varies the deposit pattern without per-rock uniforms.
// =========================================================
void vertex() {
    regional_rock_world=MODEL_MATRIX[3].xy;
    sprite_tint = COLOR;
    placement_phase = dot(MODEL_MATRIX[3].xy, vec2(0.007, 0.011));
}

// A soft irregular cap approximates snow resting on top-facing rock facets.
// The lower/front face remains mostly bare even at full ground accumulation.
// =========================================================
void fragment() {
    float local_coverage=regional_cover(regional_rock_world,snow_coverage);
    vec4 rock = texture(TEXTURE, UV);
    // At zero coverage the final mix weight is already zero; no early return
    // is needed (Godot shader fragment functions do not support return).
    vec2 seed_offset = vec2(fract(placement_phase), fract(placement_phase * 0.613));
    float broad = texture(snow_noise, UV * vec2(2.6, 1.9) + seed_offset).r;
    float fine = texture(snow_noise, UV * vec2(8.3, 5.4) + seed_offset * 1.3).r;
    float luminance = dot(rock.rgb, vec3(0.26, 0.62, 0.12));

    // Normalized sprite Y is a cheap proxy for top surfaces, not a true 3D normal.
    float irregular_y = UV.y + (broad - 0.5) * 0.14 + (fine - 0.5) * 0.035;
    float top_face = 1.0 - smoothstep(cap_depth - 0.17, cap_depth + 0.10, irregular_y);
    float facet = mix(0.72, 1.0, smoothstep(0.16, 0.58, luminance));
    float deposit = broad * 0.53 + fine * 0.27 + (1.0 - UV.y) * 0.15 + luminance * 0.05;
    float settled = smoothstep(0.70 - local_coverage * 0.73,
                               0.88 - local_coverage * 0.62, deposit);
    float coverage = clamp(local_coverage * 2.5, 0.0, 1.0) * top_face * facet * settled;

    vec3 powder = snow_colour.rgb * (0.79 + broad * 0.15 + luminance * 0.11);
    COLOR = vec4(mix(rock.rgb, powder, coverage), rock.a) * sprite_tint;
}
