// Scene-owned comparison overrides; writes only changed uniforms on an existing private ground material.
using Godot;
using System;
public sealed class GroundSurfacePreview
{
    private readonly ShaderMaterial _material;
    private readonly string _variationUniform, _detailUniform;
    private bool _variation=true, _detail=true, _optional=true;
    private readonly float[] _visible={1,1}, _coverage={1,1}, _opacity={1,1};
    private readonly bool _originalVariation, _originalDetail;
    private readonly float[] _originalVisibility={1,1};
    private float _detailStrength=1, _patchStrength=1;
    public float BaseDetailStrength { get=>_detailStrength; set=>SetStrength(ref _detailStrength,value,"preview_base_detail_strength"); }
    public float AccompanyingStrength { get=>_patchStrength; set=>SetStrength(ref _patchStrength,value,"preview_accompanying_strength"); }
    public bool SupportsOverlays { get; }
    public bool ShowAccompanyingPatches { get=>_variation; set=>SetFlag(ref _variation,value,_variationUniform); }
    public bool ShowBaseVariation { get=>ShowAccompanyingPatches; set=>ShowAccompanyingPatches=value; }
    public bool ShowBaseDetail { get=>_detail; set=>SetFlag(ref _detail,value,_detailUniform); }
    public bool ShowOptionalLayers { get=>_optional; set { if(SupportsOverlays) SetFlag(ref _optional,value,"preview_optional_layers"); } }

    // No node, callbacks, clones, authored Resource writes or texture rebuilds are needed.
    // =========================================================
    public GroundSurfacePreview(ShaderMaterial material,bool streamed)
    {
        _material=material ?? throw new ArgumentNullException(nameof(material)); SupportsOverlays=streamed;
        _variationUniform=streamed ? "preview_accompanying_patches" : "colour_patches";
        _detailUniform=streamed ? "preview_base_detail" : "fine_detail";
        _originalVariation=_variation=OriginalFlag(_variationUniform);
        _originalDetail=_detail=OriginalFlag(_detailUniform);
        var visibility=material.GetShaderParameter("preview_overlay_visibility");
        if(streamed && visibility.VariantType!=Variant.Type.Nil)
        {
            var defaults=visibility.AsVector2();
            _originalVisibility[0]=_visible[0]=defaults.X; _originalVisibility[1]=_visible[1]=defaults.Y;
        }
    }
    // Preserve explicit material overrides; unassigned uniforms use the shared shader's true default.
    // =========================================================
    private bool OriginalFlag(string name)
    { var value=_material.GetShaderParameter(name); return value.VariantType==Variant.Type.Nil || value.AsBool(); }
    // Repeated widget refresh never causes repeated GPU writes.
    // =========================================================
    private void SetFlag(ref bool stored,bool value,string name)
    { if(stored==value) return; stored=value; _material.SetShaderParameter(name,value); }
    // One slot comparison does not change the other slot or its authored state.
    // =========================================================
    public bool ShowOverlay(int slot) { RequireSlot(slot); return _visible[slot]!=0; }
    public void SetShowOverlay(int slot,bool value) => SetValue(_visible,slot,value ? 1f : 0f,"preview_overlay_visibility");
    public float Coverage(int slot) { RequireSlot(slot); return _coverage[slot]; }
    public void SetCoverage(int slot,float value) => SetValue(_coverage,slot,value,"preview_overlay_coverage");
    public float Opacity(int slot) { RequireSlot(slot); return _opacity[slot]; }
    public void SetOpacity(int slot,float value) => SetValue(_opacity,slot,value,"preview_overlay_opacity");
    // Multipliers apply globally to compiled active layers; absent/disabled authored slots remain absent.
    // =========================================================
    private void SetValue(float[] values,int slot,float value,string uniform)
    {
        RequireSlot(slot);
        if(!float.IsFinite(value) || value<(uniform=="preview_overlay_visibility" ? -1 : 0) || value>2) throw new ArgumentOutOfRangeException(nameof(value));
        if(!SupportsOverlays || values[slot]==value) return;
        values[slot]=value; _material.SetShaderParameter(uniform,new Vector2(values[0],values[1]));
    }
    // Base/companion sliders are simple changed-only scalar uniforms.
    // =========================================================
    private void SetStrength(ref float stored,float value,string uniform)
    {
        if(!float.IsFinite(value) || value<0 || value>2) throw new ArgumentOutOfRangeException(nameof(value));
        if(stored==value) return; stored=value; _material.SetShaderParameter(uniform,value);
    }
    // Reject invalid slots rather than writing outside the bounded two-layer contract.
    // =========================================================
    private static void RequireSlot(int slot)
    { if(slot<0 || slot>1) throw new ArgumentOutOfRangeException(nameof(slot)); }
    // Restore authored base flags and neutral overlay multipliers; hiding F9 never calls this.
    // =========================================================
    public void Reset()
    {
        BaseDetailStrength=1; AccompanyingStrength=1;
        ShowBaseVariation=_originalVariation; ShowBaseDetail=_originalDetail; ShowOptionalLayers=true;
        for(int i=0;i<2;i++) { SetValue(_visible,i,_originalVisibility[i],"preview_overlay_visibility"); SetCoverage(i,1); SetOpacity(i,1); }
    }
}
