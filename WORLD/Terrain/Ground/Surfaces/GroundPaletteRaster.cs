// Setup-only packed RGBA32F writer avoids a native Image.SetPixel call for every large macro-palette texel.
using Godot;
using System;
internal sealed class GroundPaletteRaster
{
    private readonly int _width,_height;
    private readonly float[] _values;
    private readonly byte[] _bytes;
    // One reusable managed buffer pair serves all channels; Godot copies each completed image before reuse.
    // =========================================================
    internal GroundPaletteRaster(int width,int height)
    {
        _width=width; _height=height; _values=new float[checked(width*height*4)]; _bytes=new byte[checked(_values.Length*4)];
    }
    internal void Set(int x,int y,Color colour)
    {
        int i=(y*_width+x)*4; _values[i]=colour.R; _values[i+1]=colour.G; _values[i+2]=colour.B; _values[i+3]=colour.A;
    }
    // Native floating-point RGBA preserves the original shader parameter values and texture format.
    // =========================================================
    internal Image CreateImage()
    {
        Buffer.BlockCopy(_values,0,_bytes,0,_bytes.Length);
        return Image.CreateFromData(_width,_height,false,Image.Format.Rgbaf,_bytes);
    }
}
