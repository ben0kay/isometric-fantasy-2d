// Liquid palette and shallow-to-deep transition; no gameplay rules or boundary generation.
vec3 liquid_colour(float distance, float depth_range, float noise, vec3 shallow, vec3 deep) {
    float depth = smoothstep(0.0, depth_range, distance);
    return mix(shallow, deep, depth) * (0.94 + noise * 0.12);
}
