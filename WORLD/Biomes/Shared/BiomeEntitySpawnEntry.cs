// One eligible canonical creature and its relative share of the profile's candidate population.
using Godot;
using System;

[GlobalClass]
public partial class BiomeEntitySpawnEntry : Resource
{
    [ExportGroup("Canonical content")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public EntityDefinition Definition { get; set; }
    [ExportGroup("Selection - weight is not density")]
    [Export(PropertyHint.Range, "0,1000,0.1,or_greater")] public float SelectionWeight { get; set; } = 1f;
    [ExportGroup("Group size - individuals count toward the profile cap")]
    [Export(PropertyHint.Range, "1,100,1")] public int MinimumGroupSize { get; set; } = 1;
    [Export(PropertyHint.Range, "1,100,1")] public int MaximumGroupSize { get; set; } = 1;

    // Disabled entries may be incomplete while authoring; enabled entries require canonical identity.
    // =========================================================
    public void Validate()
    {
        if (!Enabled) return;
        if (Definition == null || string.IsNullOrWhiteSpace(Definition.Id) || !float.IsFinite(SelectionWeight) || SelectionWeight <= 0
            || MinimumGroupSize < 1 || MaximumGroupSize < MinimumGroupSize || MaximumGroupSize > 100)
            throw new ArgumentException("Invalid enabled biome entity spawn entry.");
    }
}
