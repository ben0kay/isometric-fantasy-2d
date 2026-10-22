// Slow directional wave highlights only; the shoreline never moves.
float liquid_ripples(vec2 world, float time, float wind, float noise) {
    float first = sin(dot(world, vec2(0.075, 0.11)) - time * 0.7 + noise * 3.0);
    float second = sin(dot(world, vec2(-0.055, 0.095)) + time * 0.45);
    return smoothstep(0.70 - min(abs(wind), 3.0) * 0.025, 0.98, first * 0.60 + second * 0.40);
}
