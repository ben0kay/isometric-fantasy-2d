// World-coordinate sampling and bounded two-slot composition; no chunk-local offsets or TIME.
vec4 surface_art(float index,vec2 world) {
    vec2 tile=vec2(mod(index,art_columns),floor(index/art_columns));
    // Atlas is nearest-filtered, no mipmaps: repeating inside one tile never samples adjacent art.
    return texture(ground_art,(tile+fract(world/32.0))/art_columns);
}
float biome_index_at(vec2 cell) {
    vec2 uv=(clamp(cell,vec2(0.0),map_grid_size-vec2(1.0))+vec2(0.5))/map_grid_size;
    return texture(map_settings,uv).a;
}
float material_grain(vec2 world,float style) {
    vec2 pixel=floor(world)+vec2(0.5);
    float detail=texture(ground_noise,pixel/48.0+vec2(0.23,0.59)).r;
    float grain=texture(ground_noise,pixel/13.0+vec2(0.71,0.19)).r;
    if(style<0.5) return (smoothstep(0.48,0.78,detail)-0.5)+(grain-0.5)*0.45;
    if(style<1.5) return (grain-0.5)*1.25+(detail-0.5)*0.25;
    return (abs(detail-0.5)*2.0-0.5)*0.8+(grain-0.5)*0.2;
}
vec4 layer_data(float row,float column) {
    return texture(overlay_table,vec2((column+0.5)/9.0,(row+0.5)/overlay_rows));
}
vec3 apply_ground_layer(vec3 below,float row,vec2 world) {
    float slot=mod(row,2.0);
    float visibility=slot<0.5 ? preview_overlay_visibility.x : preview_overlay_visibility.y;
    if(visibility==0.0) return below;
    vec4 info=layer_data(row,8.0);
    if(info.g<0.5 || (visibility<0.0 && info.g>1.5)) return below;
    vec4 mask=layer_data(row,6.0);
    mask.r=clamp(mask.r*(slot<0.5 ? preview_overlay_coverage.x : preview_overlay_coverage.y),0.0,1.0);
    mask.g=clamp(mask.g*(slot<0.5 ? preview_overlay_opacity.x : preview_overlay_opacity.y),0.0,1.0);
    if(mask.r<=0.0 || mask.g<=0.0) return below;
    vec4 noise=layer_data(row,7.0);
    // Independent authored salt and slot offset remain deterministic at negative coordinates.
    vec2 uv=world/mask.b*0.18+noise.gb+vec2(0.137,0.293)*mod(row,2.0);
    vec2 warp=vec2(texture(ground_noise,uv*0.63+vec2(0.17,0.43)).r,
        texture(ground_noise,uv*0.63+vec2(0.71,0.29)).r)-vec2(0.5);
    float n=texture(ground_noise,uv+warp*noise.r).r;
    float threshold=1.0-mask.r;
    float soft=max(mask.a,0.0001);
    float coverage=mask.r>=1.0 ? 1.0 : smoothstep(threshold-soft,threshold+soft,n);
    float amount=coverage*mask.g;
    if(amount<=0.0) return below;
    vec4 settings=layer_data(row,4.0);
    vec2 appearance=world/max(settings.g,80.0)*0.18+noise.gb;
    float broad=texture(ground_noise,appearance+warp*0.05).r;
    float dry=smoothstep(0.63,0.84,texture(ground_noise,appearance*1.37+vec2(0.31,0.67)).r);
    vec3 patch=mix(layer_data(row,1.0).rgb,layer_data(row,2.0).rgb,smoothstep(0.18,0.82,broad));
    patch=mix(patch,layer_data(row,3.0).rgb,dry*0.45);
    vec3 colour=mix(layer_data(row,0.0).rgb,patch,settings.r);
    vec4 artwork=surface_art(info.r,world);
    colour=mix(colour,artwork.rgb,settings.a*artwork.a);
    colour*=1.0+material_grain(world,noise.a)*settings.b;
    colour*=layer_data(row,5.0).rgb;
    return mix(below,colour,amount);
}
vec3 biome_layers(vec3 below,float biome,vec2 world) {
    vec3 first=apply_ground_layer(below,biome*2.0,world);
    return apply_ground_layer(first,biome*2.0+1.0,world);
}
vec3 blended_ground_layers(vec3 below,vec2 lookup,vec2 world) {
    vec2 cell=lookup*map_grid_size-vec2(0.5);
    vec2 origin=floor(cell), weight=fract(cell);
    float a=biome_index_at(origin), b=biome_index_at(origin+vec2(1.0,0.0));
    float c=biome_index_at(origin+vec2(0.0,1.0)), d=biome_index_at(origin+vec2(1.0,1.0));
    vec3 ca=biome_layers(below,a,world);
    if(a==b && a==c && a==d) return ca;
    vec3 cb=b==a ? ca : biome_layers(below,b,world);
    vec3 cc=c==a ? ca : (c==b ? cb : biome_layers(below,c,world));
    vec3 cd=d==a ? ca : (d==b ? cb : (d==c ? cc : biome_layers(below,d,world)));
    return mix(mix(ca,cb,weight.x),mix(cc,cd,weight.x),weight.y);
}
vec4 blended_ground_art(vec2 lookup,vec2 world) {
    vec2 cell=lookup*map_grid_size-vec2(0.5);
    vec2 origin=floor(cell), weight=fract(cell);
    vec4 a=surface_art(biome_index_at(origin),world);
    vec4 b=surface_art(biome_index_at(origin+vec2(1.0,0.0)),world);
    vec4 c=surface_art(biome_index_at(origin+vec2(0.0,1.0)),world);
    vec4 d=surface_art(biome_index_at(origin+vec2(1.0,1.0)),world);
    a.rgb*=a.a; b.rgb*=b.a; c.rgb*=c.a; d.rgb*=d.a;
    vec4 artwork=mix(mix(a,b,weight.x),mix(c,d,weight.x),weight.y);
    if(artwork.a>0.0) artwork.rgb/=artwork.a;
    return artwork;
}
