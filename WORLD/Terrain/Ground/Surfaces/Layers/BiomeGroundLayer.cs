// One biome-owned surface use; at most two optional streamed organic overlays.
using Godot;
using System;

[GlobalClass]
public partial class BiomeGroundLayer : Resource
{
    [ExportGroup("Identity and appearance")]
    [Export] public string Id { get; set; } = "";
    [Export] public string Label { get; set; } = "";
    [Export] public GroundSurfaceProfile Surface { get; set; }
    [ExportGroup("Biome tint - white preserves the surface")]
    [Export] public Color Tint { get; set; } = Colors.White;
    [ExportGroup("Optional overlay only")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public bool AllowSessionPreview { get; set; } = false;
    [Export(PropertyHint.Range,"0,1")] public float Coverage { get; set; } = 0.25f;
    [Export(PropertyHint.Range,"0,1")] public float Opacity { get; set; } = 1f;
    [Export(PropertyHint.Range,"80,8192")] public float PatchSize { get; set; } = 320f;
    [Export(PropertyHint.Range,"0,1")] public float EdgeSoftness { get; set; } = 0.15f;
    [Export(PropertyHint.Range,"0,1")] public float WarpStrength { get; set; } = 0.05f;
    [Export] public int SeedSalt { get; set; }

    // Reject malformed authored values rather than silently changing shared Resources.
    // =========================================================
    public void Validate(bool isBase)
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Label) || Surface == null)
            throw new ArgumentException("Ground layers require an ID, label and surface.");
        if ((Surface.Recipe==GroundMaterialRecipe.Artwork && Surface.Texture==null) || Surface.Material == null)
            throw new ArgumentException("Ground layer surfaces require texture and material.");
        Surface.Validate();
        RequireRange(Tint.R,0,1); RequireRange(Tint.G,0,1); RequireRange(Tint.B,0,1);
        if (!float.IsFinite(Tint.A) || Tint.A != 1f)
            throw new ArgumentException("Ground tint must be opaque; it changes colour, not ground coverage.");
        if(isBase && Surface.Recipe!=GroundMaterialRecipe.Artwork)
            throw new ArgumentException("Procedural recipes currently belong to extra patch layers; base ground requires artwork.");
        if (isBase) return; // Base always fills 100%; overlay fields never affect it.
        RequireRange(Coverage,0,1); RequireRange(Opacity,0,1); RequireRange(PatchSize,80,8192);
        RequireRange(EdgeSoftness,0,1); RequireRange(WarpStrength,0,1);

    }
    // Inspector hints do not protect programmatic/resource-file input.
    // =========================================================
    private static void RequireRange(float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentException("Ground overlay setting is outside its supported range.");
    }
}
