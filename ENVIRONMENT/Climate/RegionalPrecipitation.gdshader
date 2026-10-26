shader_type canvas_item;
render_mode unshaded;
#include "res://ENVIRONMENT/Climate/RegionalWeather.gdshaderinc"
uniform float snow_particles=0.0;
uniform float atlas_frames=1.0;
varying vec2 particle_world;
varying vec4 particle_colour;
void vertex() {
    particle_world=(MODEL_MATRIX*vec4(VERTEX,0.0,1.0)).xy;
    particle_colour=COLOR;
    UV.x=(UV.x+floor(clamp(INSTANCE_CUSTOM.z,0.0,0.99999)*atlas_frames))/atlas_frames;
}
void fragment() {
    vec4 pixel=texture(TEXTURE,UV)*particle_colour;
    float mask=snow_particles>0.5 ? regional_snow(particle_world) : regional_rain(particle_world);
    COLOR=vec4(pixel.rgb,pixel.a*mask);
}
