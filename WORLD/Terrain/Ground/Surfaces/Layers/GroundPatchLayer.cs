// Accompanying ground flavour, separate from the full-coverage base and the two extra surface slots.
using Godot;
using System;
[GlobalClass]
public partial class GroundPatchLayer : Resource
{
    [Export] public string Id { get; set; } = "accompanying";
    [Export] public string Label { get; set; } = "Accompanying patches";
    [Export] public bool Enabled { get; set; } = true;
    [ExportGroup("Patch colours")]
    [Export] public Color ShadeColour { get; set; } = new(0.18f,0.245f,0.18f);
    [Export] public Color FreshColour { get; set; } = new(0.28f,0.335f,0.23f);
    [Export] public Color DryColour { get; set; } = new(0.32f,0.32f,0.22f);
    [ExportGroup("Distribution")]
    [Export(PropertyHint.Range,"0,1")] public float Strength { get; set; } = 0.7f;
    [Export(PropertyHint.Range,"80,8192")] public float PatchSize { get; set; } = 320f;

    // Validate during setup, never by polling individual ground objects.
    // =========================================================
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Label)
            || !float.IsFinite(Strength) || Strength<0 || Strength>1
            || !float.IsFinite(PatchSize) || PatchSize<80 || PatchSize>8192)
            throw new ArgumentException("Invalid accompanying ground patch layer.");
        foreach(var colour in new[] {ShadeColour,FreshColour,DryColour})
            if(!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B) || !float.IsFinite(colour.A))
                throw new ArgumentException("Accompanying patch colours must be finite.");
    }
    // Legacy Surface-only assets keep their old appearance; explicit stacks never infer hidden patches.
    // =========================================================
    public static GroundPatchLayer FromLegacy(GroundSurfaceProfile profile) => new() {
        ShadeColour=profile.ShadeColour,FreshColour=profile.FreshColour,DryColour=profile.DryColour,
        Strength=profile.Variation,PatchSize=profile.PatchSize };
}
