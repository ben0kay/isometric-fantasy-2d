// Future shared population-service input; this Resource never creates or updates actors.
using Godot;
using System;

[GlobalClass]
public partial class BiomeEntitySpawnProfile : Resource
{
    [ExportGroup("Activation - configuration only in BP1")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public Godot.Collections.Array<BiomeEntitySpawnEntry> Entries { get; set; } = new();
    [ExportGroup("Population budget - individuals, before placement rejection")]
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float IndividualsPerMillionUnits { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,1000,1")] public int MaximumActiveNearby { get; set; } = 0;
    [ExportGroup("Placement - world units")]
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public float MinimumPlayerDistance { get; set; } = 256f;
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public float BiomeEdgeClearance { get; set; } = 32f;

    // Validate on explicit authoring/future service setup, never from an idle callback.
    // =========================================================
    public void Validate()
    {
        if (!Enabled) return;
        if (!float.IsFinite(IndividualsPerMillionUnits) || IndividualsPerMillionUnits < 0 || MaximumActiveNearby < 0 || MaximumActiveNearby > 1000
            || !float.IsFinite(MinimumPlayerDistance) || MinimumPlayerDistance < 0 || !float.IsFinite(BiomeEdgeClearance) || BiomeEdgeClearance < 0 || Entries == null)
            throw new ArgumentException("Invalid biome entity population budget or placement settings.");
        foreach (var entry in Entries) entry?.Validate();
    }
}
