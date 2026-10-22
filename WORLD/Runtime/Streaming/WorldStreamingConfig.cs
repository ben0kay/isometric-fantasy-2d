// Bounded local streaming settings; biome region size remains independent in generation data.
using Godot;
[GlobalClass]
public partial class WorldStreamingConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int ChunkSize { get; set; } = 512;
    [Export] public int ActiveRadius { get; set; } = 2;
    [Export] public int PreloadRadius { get; set; } = 3;
    [Export] public int UnloadRadius { get; set; } = 4;
    [Export] public int NodesPerFrame { get; set; } = 12;
    [Export] public float ApplyBudgetMilliseconds { get; set; } = 3f;
    [Export] public int CandidateCellSize { get; set; } = 96;
    [Export] public float BiomeBlendWidth { get; set; } = 120f;
}
