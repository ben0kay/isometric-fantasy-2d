"""Optional Linux Mesa formula/render checks. Requires libEGL/libGL and numpy.
This translates only the canvas fragment subset; it is NOT a Godot shader/import test.
Run from repository root: python TESTS/Terrain/check_ground_shader_gl.py
"""
import ctypes as c
E=c.CDLL('libEGL.so.1');E.eglGetProcAddress.argtypes=[c.c_char_p];E.eglGetProcAddress.restype=c.c_void_p
getdisplay=c.CFUNCTYPE(c.c_void_p,c.c_uint,c.c_void_p,c.POINTER(c.c_int))(E.eglGetProcAddress(b'eglGetPlatformDisplayEXT'))
display=getdisplay(0x31DD,None,None)
E.eglInitialize.argtypes=[c.c_void_p,c.POINTER(c.c_int),c.POINTER(c.c_int)];a=c.c_int();b=c.c_int();assert E.eglInitialize(display,c.byref(a),c.byref(b))
E.eglBindAPI(0x30A2)
E.eglChooseConfig.argtypes=[c.c_void_p,c.POINTER(c.c_int),c.POINTER(c.c_void_p),c.c_int,c.POINTER(c.c_int)]
attrs=(c.c_int*9)(0x3033,1,0x3040,8,0x3024,8,0x3025,8,0x3038);conf=c.c_void_p();num=c.c_int();assert E.eglChooseConfig(display,attrs,c.byref(conf),1,c.byref(num)) and num.value
E.eglCreateContext.argtypes=[c.c_void_p,c.c_void_p,c.c_void_p,c.POINTER(c.c_int)];E.eglCreateContext.restype=c.c_void_p
context=E.eglCreateContext(display,conf,None,(c.c_int*7)(0x3098,3,0x30FB,3,0x30FD,1,0x3038));assert context
E.eglMakeCurrent.argtypes=[c.c_void_p,c.c_void_p,c.c_void_p,c.c_void_p];assert E.eglMakeCurrent(display,None,None,context)
def gl(name,ret,args):return c.CFUNCTYPE(ret,*args)(E.eglGetProcAddress(name.encode()))
getstring=gl('glGetString',c.c_char_p,[c.c_uint]);print('Software GL:',getstring(0x1F02).decode(),getstring(0x1F01).decode())

import re
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
def load(path):
    source=Path(path).read_text()
    return re.sub(r'#include "res://([^"]+)"',lambda m:load(ROOT/m[1]),source)
def translate(path):
    source=load(path)
    source=re.sub(r'shader_type canvas_item;','',source)
    source=re.sub(r'void vertex\(\)\s*\{.*?\}','',source,flags=re.S)
    source=re.sub(r'(uniform sampler2D \w+)\s*:[^;]+;',r'\1;',source)
    source=source.replace('varying ','in ').replace('void fragment()', 'void main()')
    return '#version 330 core\nout vec4 COLOR;\n'+source
create=gl('glCreateShader',c.c_uint,[c.c_uint]);shaderSource=gl('glShaderSource',None,[c.c_uint,c.c_int,c.POINTER(c.c_char_p),c.POINTER(c.c_int)])
compileShader=gl('glCompileShader',None,[c.c_uint]);shaderStatus=gl('glGetShaderiv',None,[c.c_uint,c.c_uint,c.POINTER(c.c_int)])
shaderLog=gl('glGetShaderInfoLog',None,[c.c_uint,c.c_int,c.POINTER(c.c_int),c.c_char_p])
def shader(kind,source):
    obj=create(kind); ptr=c.c_char_p(source.encode());shaderSource(obj,1,c.byref(ptr),None);compileShader(obj)
    ok=c.c_int();shaderStatus(obj,0x8B81,c.byref(ok))
    if not ok.value:
        log=c.create_string_buffer(8192);shaderLog(obj,8192,None,log);raise AssertionError(log.value.decode())
    return obj
createProgram=gl('glCreateProgram',c.c_uint,[]);attach=gl('glAttachShader',None,[c.c_uint,c.c_uint]);link=gl('glLinkProgram',None,[c.c_uint]);programStatus=gl('glGetProgramiv',None,[c.c_uint,c.c_uint,c.POINTER(c.c_int)])
vertex=shader(0x8B31,'''#version 330 core
out vec2 ground_world; out vec4 ground_tint;
uniform vec2 frame_origin; uniform vec2 frame_size;
void main() {
vec2 p=vec2((gl_VertexID<<1)&2,gl_VertexID&2);
gl_Position=vec4(p*2.0-1.0,0.0,1.0);
ground_world=frame_origin+p*frame_size;ground_tint=vec4(1.0);
}''')
def program(path):
    obj=createProgram();attach(obj,vertex);attach(obj,shader(0x8B30,translate(path)));link(obj)
    ok=c.c_int();programStatus(obj,0x8B82,c.byref(ok));assert ok.value,'GL program link failed'
    return obj
old=program(ROOT/'TESTS/Terrain/References/PreGS2StreamedGround.txt')
new=program(ROOT/'WORLD/Terrain/Ground/Surfaces/Shaders/StreamedGround.gdshader')
use=gl('glUseProgram',None,[c.c_uint]);loc=gl('glGetUniformLocation',c.c_int,[c.c_uint,c.c_char_p])
one=gl('glUniform1i',None,[c.c_int,c.c_int]);scalar=gl('glUniform1f',None,[c.c_int,c.c_float]);vec=gl('glUniform2f',None,[c.c_int,c.c_float,c.c_float])
genTex=gl('glGenTextures',None,[c.c_int,c.POINTER(c.c_uint)]);bindTex=gl('glBindTexture',None,[c.c_uint,c.c_uint]);activeTex=gl('glActiveTexture',None,[c.c_uint])
texParam=gl('glTexParameteri',None,[c.c_uint,c.c_uint,c.c_int]);texImage=gl('glTexImage2D',None,[c.c_uint,c.c_int,c.c_int,c.c_int,c.c_int,c.c_int,c.c_uint,c.c_uint,c.c_void_p])
def texture(data,linear=False,repeat=False):
    data=np.ascontiguousarray(data,dtype=np.float32);ident=c.c_uint();genTex(1,c.byref(ident));bindTex(0x0DE1,ident)
    for pname in [0x2801,0x2800]:texParam(0x0DE1,pname,0x2601 if linear else 0x2600)
    for pname in [0x2802,0x2803]:texParam(0x0DE1,pname,0x2901 if repeat else 0x812F)
    texImage(0x0DE1,0,0x8814,data.shape[1],data.shape[0],0,0x1908,0x1406,data.ctypes.data)
    return ident.value
# Same synthetic texture data exercises both equations, independent of Godot noise generation.
y,x=np.mgrid[:64,:64];noise=np.zeros((64,64,4),np.float32);noise[:,:,:3]=(0.5+0.25*np.sin(x*.43)+0.24*np.cos(y*.27))[:,:,None];noise[:,:,3]=1
art=np.ones((64,64,4),np.float32)
for row,col,colour in [(0,0,(.3,.4,.2)),(0,1,(.6,.4,.3)),(1,0,(.2,.12,.08)),(1,1,(.4,.4,.5))]:
    art[row*32:(row+1)*32,col*32:(col+1)*32,:3]=colour
settings=np.zeros((4,4,4),np.float32);settings[:]=(.7,320,.065,0);settings[:,2:,3]=1
maps={}
for name,colour in [('map_base',(.22,.27,.2,1)),('map_shade',(.18,.24,.18,1)),('map_fresh',(.28,.33,.23,1)),('map_dry',(.32,.32,.22,1))]:
    maps[name]=texture(np.tile(colour,(4,4,1)),True)
maps['map_settings']=texture(settings,True);maps['ground_noise']=texture(noise,True,True);maps['ground_art']=texture(art)
maps['map_tint']=texture(np.ones((4,4,4),np.float32),True)
styles=np.tile((.22,0,0,1),(4,4,1));maps['map_styles']=texture(styles,True)
table=np.zeros((4,9,4),np.float32)
for row in [0,1]:
    table[row,0]=(.3,.25,.16,1);table[row,1]=(.18,.13,.1,1);table[row,2]=(.4,.3,.2,1);table[row,3]=(.35,.3,.2,1)
    table[row,4]=(.6,240,.1,.35);table[row,5]=(1,1,1,1);table[row,6]=(.65,.85,180 if row==0 else 80,.1)
    table[row,7]=(.4,.23+row*.12,.17+row*.15,row+1);table[row,8]=(row+2,1,0,0)
maps['overlay_table']=texture(table)
fbo=c.c_uint();gl('glGenFramebuffers',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(fbo));gl('glBindFramebuffer',None,[c.c_uint,c.c_uint])(0x8D40,fbo)
vao=c.c_uint();gl('glGenVertexArrays',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(vao));gl('glBindVertexArray',None,[c.c_uint])(vao)
read=gl('glReadPixels',None,[c.c_int,c.c_int,c.c_int,c.c_int,c.c_uint,c.c_uint,c.c_void_p])
def render(prog,flags=None,origin=(-1024,-1024),size=(2048,2048),resolution=128,vectors=None):
    output=texture(np.zeros((resolution,resolution,4),np.float32))
    gl('glFramebufferTexture2D',None,[c.c_uint,c.c_uint,c.c_uint,c.c_uint,c.c_int])(0x8D40,0x8CE0,0x0DE1,output,0)
    use(prog)
    for unit,(name,ident) in enumerate(maps.items()):
        activeTex(0x84C0+unit);bindTex(0x0DE1,ident);one(loc(prog,name.encode()),unit)
    for name,value in {'has_ground_tint':False,'blend_ground_art':False,'has_ground_overlays':False,'has_surface_styles':False,'preview_accompanying_patches':True,'preview_base_detail':True,'preview_optional_layers':True,**(flags or {})}.items():one(loc(prog,name.encode()),value)
    for name,value in [('art_columns',2),('map_cell_size',512),('overlay_rows',4)]:scalar(loc(prog,name.encode()),value)
    for name,value in [('map_origin',(-1024,-1024)),('map_grid_size',(4,4)),('frame_origin',origin),('frame_size',size)]:vec(loc(prog,name.encode()),*value)
    for name,value in {'preview_overlay_visibility':(1,1),'preview_overlay_coverage':(1,1),'preview_overlay_opacity':(1,1),**(vectors or {})}.items():vec(loc(prog,name.encode()),*value)
    gl('glViewport',None,[c.c_int,c.c_int,c.c_int,c.c_int])(0,0,resolution,resolution)
    gl('glDrawArrays',None,[c.c_uint,c.c_int,c.c_int])(4,0,3)
    result=np.zeros((resolution,resolution,4),np.float32);read(0,0,resolution,resolution,0x1908,0x1406,result.ctypes.data)
    assert np.isfinite(result).all(),'Nonfinite rendered output'
    return result
before=render(old);after=render(new)
error=float(np.max(np.abs(before-after)));assert error<1e-6,error
print('PASS: pre-GS2 neutral formula parity; max absolute error',error)
flags={'has_ground_overlays':True,'blend_ground_art':True}
base=render(new,{'blend_ground_art':True});both=render(new,flags)
assert np.max(np.abs(both-base))>.01,'Enabled overlays had no visible effect'
assert np.array_equal(both,render(new,flags)),'Repeated render changed'
full=both
for iy in range(4):
    for ix in range(4):
        piece=render(new,flags,(-1024+ix*512,-1024+iy*512),(512,512),32)
        assert np.max(np.abs(piece-full[iy*32:(iy+1)*32,ix*32:(ix+1)*32]))<2e-5,'Chunk-relative seams'
print('PASS: enabled overlays, deterministic repeat, negative coordinates and sixteen separately rendered 512-unit chunks')
saved=table.copy()
table[0,8,1]=0;maps['overlay_table']=texture(table);only_b=render(new,flags)
table[:]=saved;table[1,8,1]=0;maps['overlay_table']=texture(table);only_a=render(new,flags)
assert np.max(np.abs(only_b-both))>.001 and np.max(np.abs(only_a-both))>.001,'Independent layer toggle failed'
table[:]=saved;table[:,6,1]=0;maps['overlay_table']=texture(table);zero=render(new,flags)
assert np.max(np.abs(zero-base))<1e-6,'Zero opacity changed base'
print('PASS: separate A/B toggles and all-zero opacity base parity')
# Session controls must bypass overlays without mutating their packed authored table.
table[:]=saved;maps['overlay_table']=texture(table)
master_off=render(new,{**flags,'preview_optional_layers':False})
assert np.max(np.abs(master_off-base))<1e-6,'Preview master did not restore base'
opacity_off=render(new,flags,vectors={'preview_overlay_opacity':(0,0)})
coverage_off=render(new,flags,vectors={'preview_overlay_coverage':(0,0)})
assert np.max(np.abs(opacity_off-base))<1e-6 and np.max(np.abs(coverage_off-base))<1e-6,'Zero preview multipliers changed base'
a_hidden=render(new,flags,vectors={'preview_overlay_visibility':(0,1)})
b_hidden=render(new,flags,vectors={'preview_overlay_visibility':(1,0)})
assert np.max(np.abs(a_hidden-only_b))<1e-6 and np.max(np.abs(b_hidden-only_a))<1e-6,'Preview slot controls disagree with authored toggles'
base_flat=render(new,{'blend_ground_art':True,'preview_accompanying_patches':False})
base_no_detail=render(new,{'blend_ground_art':True,'preview_base_detail':False})
assert np.max(np.abs(base_flat-base))>.001 and np.max(np.abs(base_no_detail-base))>.00001,'Base comparison controls had no effect'
assert np.max(np.abs(render(new,flags)-both))<1e-6,'Uniform reset did not restore authored rendering'
print('PASS: preview master, independent slots, coverage/opacity multipliers, base comparisons and neutral reset')
print('These software-GL checks do not validate Godot import, C# compilation, Windows rendering or target performance.')
