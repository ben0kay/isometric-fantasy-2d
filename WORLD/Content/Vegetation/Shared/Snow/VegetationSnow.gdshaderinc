// Snow follows warped artwork UVs; explicit masks or original-colour selection protect bark.
uniform vec2 snow_crown_range = vec2(0.60, 0.77);
uniform float snow_coverage = 0.0;
uniform float snow_foliage_profile = 0.0; // 0 = green vegetation, 1 = pink blossoms.
uniform vec4 snow_colour : source_color = vec4(0.87, 0.91, 0.94, 1.0);
uniform sampler2D snow_noise : repeat_enable, filter_linear;
vec3 tree_snow_colour(vec3 original, vec3 source_rgb, vec2 uv) {
    if (snow_coverage <= 0.0) return original;
    float foliage = foliage_selection(source_rgb, uv, snow_foliage_profile, snow_crown_range);
    float light = dot(original, vec3(0.3, 0.6, 0.1));
    float noise = texture(snow_noise, uv * 3.2).r;
    float deposit = noise * 0.75 + light * 0.25;
    float mask = smoothstep(1.0 - snow_coverage, 1.12 - snow_coverage, deposit);
    vec3 snow = snow_colour.rgb * (0.78 + light * 0.35 + noise * 0.12);
    return mix(original, snow, mask * foliage);
}
