// Shared continuous simplex noise for the two disposable atmosphere overlays.
// Triangular kernels fade smoothly to zero, avoiding square lattice silhouettes.
vec2 atmosphere_gradient(vec2 cell) {
    float angle = fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453) * 6.2831853;
    return vec2(cos(angle), sin(angle));
}

float atmosphere_corner(vec2 cell, vec2 offset) {
    float weight = max(0.5 - dot(offset, offset), 0.0);
    weight *= weight;
    return weight * weight * dot(atmosphere_gradient(cell), offset);
}

float atmosphere_noise(vec2 p) {
    const float skew = 0.366025404;
    const float unskew = 0.211324865;
    vec2 cell = floor(p + (p.x + p.y) * skew);
    vec2 first = p - cell + (cell.x + cell.y) * unskew;
    vec2 middle = first.x > first.y ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    vec2 second = first - middle + unskew;
    vec2 third = first - vec2(1.0) + 2.0 * unskew;
    return 70.0 * (atmosphere_corner(cell, first)
        + atmosphere_corner(cell + middle, second)
        + atmosphere_corner(cell + vec2(1.0), third));
}

// Domain warping and rotated octaves break up straight bands and repeated cell shapes.
float atmosphere_field(vec2 p) {
    vec2 warp = vec2(atmosphere_noise(p * 0.55 + vec2(8.1, 3.7)),
        atmosphere_noise(p * 0.55 + vec2(-2.6, 12.4)));
    p += warp * 0.45;
    mat2 turn = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
    float value = atmosphere_noise(p) * 0.65;
    p = turn * p * 1.97 + vec2(17.2, 5.3);
    value += atmosphere_noise(p) * 0.25;
    p = turn * p * 2.03 + vec2(4.8, 11.6);
    value += atmosphere_noise(p) * 0.10;
    return clamp(0.5 + value * 0.5, 0.0, 1.0);
}
