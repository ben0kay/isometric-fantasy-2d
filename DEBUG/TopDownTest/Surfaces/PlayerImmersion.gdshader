// Clip the dipped player's lower artwork at a world-space waterline; do not move physics/camera.
shader_type canvas_item;
uniform float immersion = 0.0;
uniform float waterline_y = 0.0;
varying vec2 actor_world;
varying vec4 actor_tint;
void vertex() { actor_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; actor_tint = COLOR; }
void fragment() {
    vec4 pixel = texture(TEXTURE, UV) * actor_tint;
    if (immersion > 0.001) pixel.a *= 1.0 - smoothstep(waterline_y - 0.5, waterline_y + 0.5, actor_world.y);
    COLOR = pixel;
}
