// Soft, variably wide wet bank with full transparency before the field rectangle ends.
float liquid_bank(float distance, float width, float noise) {
    float bank_width = width * (0.55 + noise * 0.45);
    return width <= 0.0 ? 0.0 : 1.0 - smoothstep(bank_width * 0.35, max(0.1, bank_width), -distance);
}
