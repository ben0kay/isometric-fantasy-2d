"""Seasonal foliage software-GL checks (not Godot import/runtime validation). Requires libEGL/libGL and numpy.
This translates only the canvas fragment subset; it is NOT a Godot shader/import test.
Run from repository root: python TESTS/Seasons/check_foliage_shader_gl.py
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
vertex=shader(0x8B31,"#version 330 core\nvoid main(){vec2 p=vec2((gl_VertexID<<1)&2,gl_VertexID&2);gl_Position=vec4(p*2.-1.,0,1);}")
base=load(ROOT/'ENVIRONMENT/Wind/Visuals/VegetationSway.gdshader')
base=base[:base.index('void vertex()')]+base[base.index('void fragment()'):]
base=base.replace('shader_type canvas_item;','').replace('instance uniform','uniform').replace('varying ','uniform ')
base=re.sub(r'(uniform [^;:=]+)\s*:[^;=]+',r'\1',base)
base=base.replace('void fragment()', 'void main()')
wet=load(ROOT/'WORLD/Content/Vegetation/Shared/Wetness/VegetationWetness.gdshaderinc')
snow=load(ROOT/'WORLD/Content/Vegetation/Shared/Snow/VegetationSnow.gdshaderinc')
anchor='    COLOR = pixel * sprite_tint * inside;'
code=base.replace('void main()',wet+'\n'+snow+'\nvoid main()')
code=code.replace(anchor,'    if (!shadow_only) pixel.rgb = wet_foliage_colour(pixel.rgb, original_foliage_rgb, sample_uv);\n    if (!shadow_only) pixel.rgb = tree_snow_colour(pixel.rgb, original_foliage_rgb, sample_uv);\n'+anchor)
code=re.sub(r'(uniform [^;:=]+)\s*:[^;=]+',r'\1',code)
code='#version 330 core\nout vec4 COLOR;\nuniform sampler2D TEXTURE;\nuniform vec2 TEXTURE_PIXEL_SIZE;\nuniform float TIME;\nconst vec2 UV=vec2(0.5);\n'+code
prog=createProgram();attach(prog,vertex);attach(prog,shader(0x8B30,code));link(prog)
ok=c.c_int();programStatus(prog,0x8B82,c.byref(ok));assert ok.value,'link failed'
use=gl('glUseProgram',None,[c.c_uint]);use(prog)
loc=gl('glGetUniformLocation',c.c_int,[c.c_uint,c.c_char_p]);one=gl('glUniform1i',None,[c.c_int,c.c_int]);scalar=gl('glUniform1f',None,[c.c_int,c.c_float])
vec2=gl('glUniform2f',None,[c.c_int,c.c_float,c.c_float]);vec4=gl('glUniform4f',None,[c.c_int,c.c_float,c.c_float,c.c_float,c.c_float])
gen=gl('glGenTextures',None,[c.c_int,c.POINTER(c.c_uint)]);bind=gl('glBindTexture',None,[c.c_uint,c.c_uint]);active=gl('glActiveTexture',None,[c.c_uint]);param=gl('glTexParameteri',None,[c.c_uint,c.c_uint,c.c_int])
texImage=gl('glTexImage2D',None,[c.c_uint,c.c_int,c.c_int,c.c_int,c.c_int,c.c_int,c.c_uint,c.c_uint,c.c_void_p])
def texture(colour,unit):
    active(0x84C0+unit);ident=c.c_uint();gen(1,c.byref(ident));bind(0x0DE1,ident)
    for p in [0x2801,0x2800]:param(0x0DE1,p,0x2600)
    for p in [0x2802,0x2803]:param(0x0DE1,p,0x812F)
    data=np.asarray(colour,dtype=np.float32)
    if data.ndim==1:data=data.reshape(1,1,4)
    data=np.ascontiguousarray(data);texImage(0x0DE1,0,0x8814,data.shape[1],data.shape[0],0,0x1908,0x1406,data.ctypes.data)
    return ident.value
fbo=c.c_uint();gl('glGenFramebuffers',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(fbo));gl('glBindFramebuffer',None,[c.c_uint,c.c_uint])(0x8D40,fbo)
output=texture((0,0,0,0),7);gl('glFramebufferTexture2D',None,[c.c_uint,c.c_uint,c.c_uint,c.c_uint,c.c_int])(0x8D40,0x8CE0,0x0DE1,output,0)
vao=c.c_uint();gl('glGenVertexArrays',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(vao));gl('glBindVertexArray',None,[c.c_uint])(vao)
vec4(loc(prog,b'sprite_tint'),1,1,1,1);vec2(loc(prog,b'TEXTURE_PIXEL_SIZE'),1,1);scalar(loc(prog,b'strength_world_pixels'),0)
read=gl('glReadPixels',None,[c.c_int,c.c_int,c.c_int,c.c_int,c.c_uint,c.c_uint,c.c_void_p])
def render(rgb,mask=1,enabled=True,amount=1,healthy=True,wetness=0,snowfall=0,hasmask=True,tone=(1,0),bucket=0,atlas=None):
    if atlas is None:
        atlas=np.zeros((2,16,4),np.float32);atlas[0,:]=(.89,.38,.08,amount);atlas[1,:]=(tone[0],tone[1],0,0)
    scalar(loc(prog,b'seasonal_bucket'),bucket)
    for unit,(name,colour) in enumerate([('TEXTURE',(*rgb,0.7)),('foliage_mask',(mask,mask,mask,1)),('seasonal_state',atlas),('snow_noise',(1,1,1,1))]):
        texture(colour,unit);one(loc(prog,name.encode()),unit)
    for name,value in [('foliage_has_mask',hasmask),('seasonal_profile_enabled',enabled),('foliage_healthy',healthy)]:one(loc(prog,name.encode()),value)
    for name,value in [('wet_coverage',wetness),('snow_coverage',snowfall)]:scalar(loc(prog,name.encode()),value)
    gl('glViewport',None,[c.c_int,c.c_int,c.c_int,c.c_int])(0,0,1,1);gl('glDrawArrays',None,[c.c_uint,c.c_int,c.c_int])(4,0,3)
    result=np.zeros(4,np.float32);read(0,0,1,1,0x1908,0x1406,result.ctypes.data)
    assert np.isfinite(result).all();assert abs(result[3]-.7)<1e-6
    return result[:3]
dark=(.094,.078,.145);green=(.15,.36,.26);bark=(.4,.2,.1)
for colour in [dark,green,bark]:
    assert np.max(abs(render(colour,enabled=False)-colour))<1e-6
    assert np.max(abs(render(colour,amount=0)-colour))<1e-6
    assert np.max(abs(render(colour,mask=0)-colour))<1e-6
    assert np.max(abs(render(colour,healthy=False)-colour))<1e-6
print('PASS: disabled/Summer identity, black-mask bark protection, burnt suppression, source alpha')
changed=render(dark);assert np.max(abs(changed-dark))>.01
assert np.max(abs(render(dark,hasmask=False)-dark))<1e-6
assert np.max(abs(render(green,hasmask=False)-green))>.01
assert np.max(abs(render(dark,mask=.5)-(np.array(dark)+changed)*.5))<1e-6
assert abs(np.dot(changed,[.3,.6,.1])-np.dot(dark,[.3,.6,.1]))<1e-6
print('PASS: dark leaves selected by mask, green heuristic fallback, fractional mask and luminance preservation')
assert np.max(abs(render(green,wetness=1)-render(green)*.85))<1e-6
assert np.max(abs(render(dark,snowfall=1)-render(dark)))>.1
assert np.max(abs(render(bark,mask=0,wetness=1,snowfall=1)-bark))<1e-6
assert np.max(abs(render(green,enabled=False,hasmask=False,wetness=1)-np.array(green)*.85))<1e-6
print('PASS: autumn rain darkening, masked dark-leaf snow, weather bark isolation and unmasked rain fallback')

# Real authored gradients exercise calendar remapping and all timing groups, beyond synthetic pixels.
resource=(ROOT/'WORLD/Content/Vegetation/Trees/Oak/OakSeasonalFoliage.tres').read_text()
arrays=re.findall(r'PackedFloat32Array\(([^)]+)\)|PackedColorArray\(([^)]+)\)',resource)
offsets=np.array([float(x) for x in arrays[0][0].split(',')])
colours=np.array([float(x) for x in arrays[1][1].split(',')]).reshape(-1,4)
tones=np.array([float(x) for x in arrays[3][1].split(',')]).reshape(-1,4)
def authored(year,variation=14,strength=1):
    result=np.zeros((2,16,4),np.float32)
    for b in range(16):
        y=(year+(b*2/15-1)*variation/365)%1
        result[0,b]=[np.interp(y,offsets,colours[:,i]) for i in range(4)]
        result[0,b,3]*=strength
        result[1,b,:2]=[np.interp(y,offsets,tones[:,i]) for i in range(2)]
    return result
assert np.max(abs(authored(0)-authored(1)))<1e-6
assert np.max(authored(.5)[0,:,3])==0
assert np.max(abs(authored(.76,variation=0)[0]-authored(.76,variation=0)[0,0]))<1e-6
assert np.max(abs(authored(.76)[0,0]-authored(.76)[0,-1]))>.1
assert np.max(abs(render(green,atlas=authored(.76,strength=0))-green))<1e-6
full=render(green,atlas=authored(.76,variation=0));half=render(green,atlas=authored(.76,variation=0,strength=.5))
assert np.max(abs(half-(full+green)*.5))<1e-6
winter=render(green,atlas=authored(0));assert np.dot(winter,[.3,.6,.1])>np.dot(green,[.3,.6,.1])+.05
assert winter[2]>winter[0] and winter[1]>winter[0]
assert np.max(abs(render(green,bucket=0,atlas=authored(.76))-render(green,bucket=15,atlas=authored(.76))))>.05
print('PASS: authored Jan/Dec wrap, Summer plateau, pale winter lift, strength, synchronized/varied timing and atlas group sampling')
