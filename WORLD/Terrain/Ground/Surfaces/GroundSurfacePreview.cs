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
    public bool ShowOverlay(int slot) { RequireSlot(slot); return _visible[slot]>0; }
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
        if(!float.IsFinite(value) || value<0 || value>2) throw new ArgumentOutOfRangeException(nameof(value));
        if(!SupportsOverlays || values[slot]==value) return;
        values[slot]=value; _material.SetShaderParameter(uniform,new Vector2(values[0],values[1]));
    }
    // Reject invalid slots rather than writing outside the bounded two-layer contract.
    // =========================================================
    private static void RequireSlot(int slot)
    { if(slot<0 || slot>1) throw new ArgumentOutOfRangeException(nameof(slot)); }
    // Restore authored base flags and neutral overlay multipliers; hiding F9 never calls this.
    // =========================================================
    public void Reset()
    {
        ShowBaseVariation=_originalVariation; ShowBaseDetail=_originalDetail; ShowOptionalLayers=true;
        for(int i=0;i<2;i++) { SetShowOverlay(i,true); SetCoverage(i,1); SetOpacity(i,1); }
    }
}
