"""Render the actual Oak mask with the production wind fragment under scale/mirror/wind variations.
Uses the existing software-GL harness; this is not Godot geometry/API/visual acceptance.
"""
from pathlib import Path
import runpy,math
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
n=runpy.run_path(str(ROOT/'TESTS/Seasons/check_foliage_shader_gl.py'))
gl=n['gl'];c=n['c'];shader=n['shader'];loc=n['loc'];one=n['one'];scalar=n['scalar'];vec2=n['vec2'];vec4=n['vec4']
fragment=n['code'].replace('const vec2 UV=vec2(0.5);','in vec2 UV;')
fragment=fragment.replace('    COLOR = pixel * sprite_tint * inside;','    COLOR = vec4(vec3(foliage_selection(original_foliage_rgb, sample_uv, 0.0, vec2(0.60, 0.79))), pixel.a) * inside;')
vertex=shader(0x8B31,'''#version 330 core
out vec2 UV; uniform vec2 frame_min; uniform vec2 frame_max; uniform bool mirrored;
void main(){vec2 p=vec2((gl_VertexID<<1)&2,gl_VertexID&2);gl_Position=vec4(p*2.-1.,0,1);UV=mix(frame_min,frame_max,p);if(mirrored)UV.x=1.-UV.x;}
''')
prog=n['createProgram']();n['attach'](prog,vertex);n['attach'](prog,shader(0x8B30,fragment));n['link'](prog)
ok=c.c_int();n['programStatus'](prog,0x8B82,c.byref(ok));assert ok.value
n['use'](prog)
art=np.asarray(Image.open(ROOT/'WORLD/Content/Vegetation/Trees/Oak/Sprites/tree_01.png').convert('RGBA'),dtype=np.float32)/255
mask=np.asarray(Image.open(ROOT/'WORLD/Content/Vegetation/Trees/Oak/Sprites/tree_01_fm.png').convert('RGBA'),dtype=np.float32)/255
for unit,(name,data) in enumerate([('TEXTURE',art),('foliage_mask',mask),('fire_burn_noise',np.ones((1,1,4),np.float32))]):
    n['texture'](data,unit);one(loc(prog,name.encode()),unit)
one(loc(prog,b'foliage_has_mask'),1);one(loc(prog,b'foliage_healthy'),1)
vec4(loc(prog,b'sprite_tint'),1,1,1,1);vec2(loc(prog,b'TEXTURE_PIXEL_SIZE'),1/art.shape[1],1/art.shape[0])
width,height=256,320
output=n['texture'](np.zeros((height,width,4),np.float32),7)
gl('glFramebufferTexture2D',None,[c.c_uint,c.c_uint,c.c_uint,c.c_uint,c.c_int])(0x8D40,0x8CE0,0x0DE1,output,0)
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
checks=0
for scale in [.3,.575,1.2]:
    for mirror in [False,True]:
        for wind in [-3,0,3]:
            padding=1.6*abs(wind)/scale/art.shape[1]*1.05
            vec2(loc(prog,b'frame_min'),-padding,0);vec2(loc(prog,b'frame_max'),1+padding,1)
            one(loc(prog,b'mirrored'),mirror);scalar(loc(prog,b'wind_orientation'),-1 if mirror else 1)
            scalar(loc(prog,b'source_pixels_per_world'),1/scale);scalar(loc(prog,b'wind_multiplier'),wind)
            scalar(loc(prog,b'TIME'),1.23)
            gl('glViewport',None,[c.c_int,c.c_int,c.c_int,c.c_int])(0,0,width,height)
            gl('glDrawArrays',None,[c.c_uint,c.c_int,c.c_int])(4,0,3)
            actual=np.zeros((height,width,4),np.float32);n['read'](0,0,width,height,0x1908,0x1406,actual.ctypes.data)
            y,x=np.mgrid[:height,:width];uvx=-padding+(x+.5)/width*(1+2*padding);uvy=(y+.5)/height
            if mirror:uvx=1-uvx
            t=1.23*.8;gust=.75+.25*math.sin(t*.37);sway=.75*math.sin(t)+.25*math.sin(t*1.83)
            distance=(.35+sway*gust*.65)*1.6*wind*(-1 if mirror else 1)*(1-smooth(.12,.7,uvy))
            samplex=uvx-distance/scale/art.shape[1]
            sx=np.minimum((np.clip(samplex,0,1)*art.shape[1]).astype(int),art.shape[1]-1)
            sy=np.minimum((uvy*art.shape[0]).astype(int),art.shape[0]-1)
            inside=(samplex>=0)&(samplex<=1)
            expected=mask[sy,sx,0]*inside;alpha=art[sy,sx,3]*inside
            # Ignore only numerical nearest-texel boundaries between float32 GL and float64 reference.
            frac=(samplex*art.shape[1])%1;stable=(frac>1e-4)&(frac<1-1e-4)
            assert np.max(abs(actual[:,:,0][stable]-expected[stable]))<2e-5,(scale,mirror,wind,'mask')
            assert np.max(abs(actual[:,:,3][stable]-alpha[stable]))<2e-5,(scale,mirror,wind,'alpha')
            if wind:
                unwarped=np.minimum((np.clip(uvx,0,1)*art.shape[1]).astype(int),art.shape[1]-1)
                assert np.sum(abs(expected-mask[sy,unwarped,0]*inside)>.05)>100,'Fixture would not catch unwarped mask sampling'
            checks+=1
print(f'PASS: {checks} actual-Oak cases; ±3/calm wind, 3 scales, both mirrors, fractional mask edges/source alpha and transparent wind margins')
