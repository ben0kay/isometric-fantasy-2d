// Grass material composition only; normal CanvasItem lighting remains handled by Godot.
shader_type canvas_item;
#include "res://WORLD/Terrain/Ground/Surfaces/Shaders/NoiseSampling.gdshaderinc"
#include "res://WORLD/Terrain/Transitions/AdjacentBiomeBlend.gdshaderinc"
#include "res://WORLD/Terrain/Ground/Surfaces/Shaders/PatchColours.gdshaderinc"
#include "res://WORLD/Terrain/Ground/Surfaces/Shaders/FineDetail.gdshaderinc"
uniform bool natural_grass = true;
uniform vec4 surface_tint = vec4(1.0);
uniform vec4 secondary_surface_tint = vec4(1.0);
varying vec2 ground_world;
varying vec4 ground_tint;

void vertex() {
    ground_world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    ground_tint = COLOR;
}
void fragment() {
    vec4 artwork = texture(TEXTURE, UV);
    vec3 authored_base = vec3(0.2196, 0.2745, 0.2078);
    if (natural_grass) {
        vec3 base = vec3(0.2196, 0.2745, 0.2078);
        // Retain a little authored pixel detail while suppressing obvious 32-pixel repetition.
        vec3 surface = grass_patch_colour(ground_world) * mix(vec3(1.0), artwork.rgb / base, 0.22);
        artwork.rgb = grass_fine_detail(surface, ground_world);
    }
    else artwork.rgb *= mix(base_colour.rgb, secondary_base.rgb, biome_weight(ground_world)) / authored_base;
    artwork.rgb *= mix(surface_tint.rgb,secondary_surface_tint.rgb,biome_weight(ground_world));
    COLOR = artwork * ground_tint;
}
