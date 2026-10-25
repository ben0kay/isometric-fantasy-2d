// Disposable P5 water-first preview: one existing biome permits water, the other must form around it.
using Godot;
public partial class GeographyFirstPractice : RiverPractice
{
    // Deep-clone test authoring only; never modify canonical biome Resources or production generation defaults.
    // =========================================================
    public override void _EnterTree()
    {
        Config=(WorldMasterConfig)Config.Duplicate(true);
        for (int i=0;i<Config.Generation.Biomes.Count;i++)
            Config.Generation.Biomes[i].Hydrology=new BiomeHydrologyProfile { AllowFreshwaterLakes=i==0,AllowFreshwaterRivers=i==0 };
        base._EnterTree();
    }
    // Reuse river traversal and F7 map; only the cloned permission profiles distinguish this practice scene.
    // =========================================================
    public override void _Ready()
    {
        base._Ready(); if (StartupError!=null) return;
        var ui=new CanvasLayer(); AddChild(ui);
        ui.AddChild(new Label { Position=new Vector2(16,70),Text="P5 geography first | Only "+Config.Generation.Biomes[0].DisplayName+" permits water; other biome stays on dry land." });
    }
}
