// Optional surface deposit configuration; no duplicate scenery generator or automatic respawn.
using Godot;
using System;

[GlobalClass]
public partial class BiomeResourceSpawnProfile : Resource
{
    [ExportGroup("Activation - configuration only in BP1")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public Godot.Collections.Array<BiomeResourceSpawnEntry> Entries { get; set; } = new();

    // Explicit authoring validation; disabled profiles do not require completed entries.
    // =========================================================
    public void Validate()
    {
        if (!Enabled) return;
        if (Entries == null) throw new ArgumentException("Biome resource entries cannot be null.");
        foreach (var entry in Entries) entry?.Validate();
    }
}
