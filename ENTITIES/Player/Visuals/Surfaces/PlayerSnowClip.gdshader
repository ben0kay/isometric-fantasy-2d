// Clip shallow snow burial at ground level without moving the physics body or camera.
shader_type canvas_item;
uniform float burial = 0.0;
uniform float groundline_y = 0.0;
varying vec2 actor_world;
varying vec4 actor_tint;
void vertex() { actor_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; actor_tint = COLOR; }
void fragment() {
    vec4 pixel = texture(TEXTURE, UV) * actor_tint;
    if (burial > 0.001) pixel.a *= 1.0 - smoothstep(groundline_y - 0.5, groundline_y + 0.5, actor_world.y);
    COLOR = pixel;
}
