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
    data=np.array(colour,dtype=np.float32);texImage(0x0DE1,0,0x8814,1,1,0,0x1908,0x1406,data.ctypes.data)
    return ident.value
fbo=c.c_uint();gl('glGenFramebuffers',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(fbo));gl('glBindFramebuffer',None,[c.c_uint,c.c_uint])(0x8D40,fbo)
output=texture((0,0,0,0),7);gl('glFramebufferTexture2D',None,[c.c_uint,c.c_uint,c.c_uint,c.c_uint,c.c_int])(0x8D40,0x8CE0,0x0DE1,output,0)
vao=c.c_uint();gl('glGenVertexArrays',None,[c.c_int,c.POINTER(c.c_uint)])(1,c.byref(vao));gl('glBindVertexArray',None,[c.c_uint])(vao)
vec4(loc(prog,b'sprite_tint'),1,1,1,1);vec2(loc(prog,b'TEXTURE_PIXEL_SIZE'),1,1);scalar(loc(prog,b'strength_world_pixels'),0)
read=gl('glReadPixels',None,[c.c_int,c.c_int,c.c_int,c.c_int,c.c_uint,c.c_uint,c.c_void_p])
def render(rgb,mask=1,enabled=True,amount=1,healthy=True,wetness=0,snowfall=0,hasmask=True):
    for unit,(name,colour) in enumerate([('TEXTURE',(*rgb,0.7)),('foliage_mask',(mask,mask,mask,1)),('seasonal_state',(.89,.38,.08,amount)),('snow_noise',(1,1,1,1))]):
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
