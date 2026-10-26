// Same version-1 lattice as MacroBorderRefinement.cs. It warps biome lookup, never water geometry or texture grain.
uniform float map_refinement_strength=0.0;
uniform int map_refinement_seed=0;
float macro_border_hash(ivec2 p,uint salt) {
    uint v=uint(p.x)*73856093u^uint(p.y)*19349663u^uint(map_refinement_seed)^salt;
    v=(v^(v>>16u))*0x7FEB352Du; v=(v^(v>>15u))*0x846CA68Bu;
    return float((v^(v>>16u))&65535u)/65535.0*2.0-1.0;
}
float macro_border_noise(ivec2 p,vec2 f,uint salt) {
    return mix(mix(macro_border_hash(p,salt),macro_border_hash(p+ivec2(1,0),salt),f.x),
        mix(macro_border_hash(p+ivec2(0,1),salt),macro_border_hash(p+ivec2(1,1),salt),f.x),f.y);
}
vec2 refined_macro_lookup(vec2 world) {
    vec2 raw=(world-map_origin)/(map_grid_size*map_cell_size);
    if(map_refinement_strength<=0.0) return raw;
    vec2 p=(world-map_origin)/(map_cell_size*2.0),f=fract(p); f=f*f*(3.0-2.0*f);
    vec2 offset=vec2(macro_border_noise(ivec2(floor(p)),f,17u),macro_border_noise(ivec2(floor(p)),f,97u))*map_cell_size*map_refinement_strength;
    vec2 warped=clamp((world-map_origin+offset)/(map_grid_size*map_cell_size),vec2(0.0),vec2(0.999999));
    // Ocean-centre cells retain their original identity; exact shore polygons remain independently authoritative.
    vec2 a=(floor(raw*map_grid_size)+vec2(0.5))/map_grid_size;
    vec2 b=(floor(warped*map_grid_size)+vec2(0.5))/map_grid_size;
    return texture(map_base,a).a>0.5 && texture(map_base,b).a>0.5 ? warped : raw;
}
