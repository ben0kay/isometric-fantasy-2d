// Continuous shallow drifts; separate from coverage so full snow never reopens green holes.
uniform float drift_scale = 220.0;
uniform float drift_contrast = 0.065;
float snow_depth_field(vec2 world) {
    vec2 p = world / max(60.0, drift_scale);
    return clamp(0.64 + 0.20 * sin(p.x + 0.42 * sin(p.y * 0.83))
        + 0.16 * sin(p.y * 1.31 + p.x * 0.37), 0.0, 1.0);
}
