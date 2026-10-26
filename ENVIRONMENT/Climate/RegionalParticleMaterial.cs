// Shared shader factory retains existing emitters/motion while masking individual particle positions on the GPU.
using Godot;
public static class RegionalParticleMaterial
{
    // Atlas frames replace CanvasItemMaterial particle animation only; no particle CPU loop is introduced.
    // =========================================================
    public static ShaderMaterial Create(bool snow,int frames=1)
    {
        var material=new ShaderMaterial { Shader=GD.Load<Shader>("res://ENVIRONMENT/Climate/RegionalPrecipitation.gdshader") };
        material.SetShaderParameter("snow_particles",snow ? 1f : 0f); material.SetShaderParameter("atlas_frames",(float)frames);
        return material;
    }
}
