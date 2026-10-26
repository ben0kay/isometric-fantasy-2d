#ifndef REGIONAL_WEATHER_INCLUDED
#define REGIONAL_WEATHER_INCLUDED
// One world-owned, camera-local field: R settled cover / G rain eligibility / B snow eligibility.
global uniform bool regional_weather_enabled;
global uniform sampler2D regional_weather_field : filter_linear, repeat_disable;
global uniform vec4 regional_weather_rect;
vec4 regional_weather_at(vec2 world) {
    vec2 uv=(world-regional_weather_rect.xy)/max(regional_weather_rect.zw,vec2(1.0));
    return texture(regional_weather_field,clamp(uv,vec2(0.0),vec2(1.0)));
}
float regional_cover(vec2 world,float fallback) {
    if(!regional_weather_enabled) return fallback;
    return regional_weather_at(world).r;
}
float regional_rain(vec2 world) {
    if(!regional_weather_enabled) return 1.0;
    return regional_weather_at(world).g;
}
float regional_snow(vec2 world) {
    if(!regional_weather_enabled) return 1.0;
    return regional_weather_at(world).b;
}
#endif
