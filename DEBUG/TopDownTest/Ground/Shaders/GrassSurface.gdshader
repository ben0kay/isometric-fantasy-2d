// Grass material composition only; normal CanvasItem lighting remains handled by Godot.
shader_type canvas_item;
#include "res://DEBUG/TopDownTest/Ground/Shaders/NoiseSampling.gdshaderinc"
#include "res://DEBUG/TopDownTest/Ground/Shaders/PatchColours.gdshaderinc"
#include "res://DEBUG/TopDownTest/Ground/Shaders/FineDetail.gdshaderinc"
uniform bool natural_grass = true;
varying vec2 ground_world;
varying vec4 ground_tint;

void vertex() {
    ground_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    ground_tint = COLOR;
}
void fragment() {
    vec4 artwork = texture(TEXTURE, UV);
    if (natural_grass) {
        vec3 base = vec3(0.2196, 0.2745, 0.2078);
        // Retain a little authored pixel detail while suppressing obvious 32-pixel repetition.
        vec3 surface = grass_patch_colour(ground_world) * mix(vec3(1.0), artwork.rgb / base, 0.22);
        artwork.rgb = grass_fine_detail(surface, ground_world);
    }
    COLOR = artwork * ground_tint;
}
