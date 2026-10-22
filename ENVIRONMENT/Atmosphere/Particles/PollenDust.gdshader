// Sparse, softly fading screen dust. Motion is independent of camera, scenery, wind and daylight.
shader_type canvas_item;
render_mode unshaded;
uniform vec2 view_size=vec2(1920.0,1080.0);
varying float speck_alpha;
void vertex() {
    float index=round(INSTANCE_CUSTOM.r*100.0);
    float phase=index*2.39996;
    float t=TIME+phase;
    float cycle=floor(t/24.0);
    float age=mod(t,24.0);
    vec2 seed=fract(sin(vec2(index*12.9898+cycle*7.13,index*78.233+cycle*3.71))*43758.5453);
    vec2 origin=vec2(32.0)+seed*max(view_size-vec2(64.0),vec2(1.0));
    vec2 velocity=vec2(sin(phase)*3.0,cos(phase*0.7)*2.0+1.0);
    vec2 p=origin+velocity*age+vec2(sin(age*0.4+phase)*3.0,cos(age*0.3+phase)*2.0);
    float radius=mix(0.7,1.2,seed.x);
    VERTEX=VERTEX*radius+p-view_size*0.5;
    speck_alpha=smoothstep(0.0,2.0,age)*(1.0-smoothstep(5.0,8.0,age))*0.10;
}
void fragment() {
    float edge=1.0-smoothstep(0.65,1.0,length((UV-vec2(0.5))*2.0));
    COLOR=vec4(0.82,0.79,0.68,speck_alpha*edge);
}
