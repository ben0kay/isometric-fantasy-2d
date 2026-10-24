// Adapted from ben0kay/isometric-new-2d GroundSun + BasaltSurface/DustSurface.
// Coordinates are quantized world pixels scaled to the old material's tile coordinate family.
vec3 procedural_ground(float recipe,vec2 world,float feature_size,vec2 salt,vec3 low,vec3 high) {
    vec2 p=(floor(world)+vec2(0.5))/max(feature_size,1.0)+salt*256.0;
    float strata=texture(ground_noise,p*0.035+vec2(0.31,0.74)).r;
    float detail=texture(ground_noise,p*0.22+vec2(0.53,0.11)).r;
    float grain=texture(ground_noise,p*2.7+vec2(0.09,0.62)).r;
    vec3 surface=mix(low,high,strata);
    if(recipe<1.5) {
        surface+=vec3((detail-0.5)*0.009);
        surface+=vec3((grain-0.5)*0.012);
    } else {
        float fracture=1.0-smoothstep(0.008,0.026,abs(detail-0.49));
        fracture*=smoothstep(0.59,0.77,strata);
        surface*=1.0-fracture*0.24;
        surface+=vec3((detail-0.5)*0.014);
        surface+=vec3((grain-0.5)*0.018);
    }
    return surface;
}
