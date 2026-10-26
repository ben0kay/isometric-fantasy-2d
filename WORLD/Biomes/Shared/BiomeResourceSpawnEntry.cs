// Future static surface deposit candidate; item rewards belong to harvesting, not this biome entry.
using Godot;
using System;

[GlobalClass]
public partial class BiomeResourceSpawnEntry : Resource
{
    [ExportGroup("Canonical world content - never an inventory item")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public SceneryDefinition Definition { get; set; }
    [ExportGroup("Placement - per million square world units")]
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float InstancesPerMillionUnits { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public float MinimumSpacing { get; set; } = 32f;
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public float BiomeEdgeClearance { get; set; } = 32f;

    // BP2 must additionally verify harvestable/deposit capability before consuming this contract.
    // =========================================================
    public void Validate()
    {
        if (!Enabled) return;
        if (Definition == null || string.IsNullOrWhiteSpace(Definition.Id) || !float.IsFinite(InstancesPerMillionUnits) || InstancesPerMillionUnits < 0
            || !float.IsFinite(MinimumSpacing) || MinimumSpacing < 0 || !float.IsFinite(BiomeEdgeClearance) || BiomeEdgeClearance < 0)
            throw new ArgumentException("Invalid enabled biome resource spawn entry.");
    }
}
