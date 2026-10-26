// Optional placement affinity; authored climate baseline and live weather remain separate contracts.
using Godot;
using System;
[GlobalClass]
public partial class BiomePlacementClimate : Resource
{
    [ExportGroup("Preferred baseline ranges - Celsius / percent RH")]
    [Export] public Vector2 TemperatureRange { get; set; } = new(0,24);
    [Export] public Vector2 HumidityRange { get; set; } = new(30,85);
    [Export] public float TemperatureFalloff { get; set; } = 5;
    [Export] public float HumidityFalloff { get; set; } = 15;
    [ExportGroup("Optional hard requirements")]
    [Export] public bool RequireTemperatureRange { get; set; }
    [Export] public bool RequireHumidityRange { get; set; }
    [ExportGroup("Intentional enchanted exceptions")]
    [Export] public bool EnchantedOnly { get; set; }
    [Export] public bool AllowEnchantedException { get; set; }

    // Validate once before building planner-only lookup tables, never during active gameplay.
    // =========================================================
    public void Validate()
    {
        if(!ValidRange(TemperatureRange,-100,100) || !ValidRange(HumidityRange,0,100)
            || !float.IsFinite(TemperatureFalloff) || TemperatureFalloff<0.1f || TemperatureFalloff>100
            || !float.IsFinite(HumidityFalloff) || HumidityFalloff<0.1f || HumidityFalloff>100)
            throw new ArgumentException("Invalid biome placement climate ranges/falloff.");
    }
    // Soft mismatches remain eligible for sparse content collections; explicit hard requirements never relax.
    // =========================================================
    public float Preference(WorldClimateSample sample)
    {
        bool enchanted=sample.Enchantment>=0.5f;
        if(EnchantedOnly && !enchanted) return 0;
        if(AllowEnchantedException && enchanted) return 1;
        float temperature=Distance(sample.TemperatureCelsius,TemperatureRange),humidity=Distance(sample.RelativeHumidityPercent,HumidityRange);
        if((RequireTemperatureRange && temperature>0) || (RequireHumidityRange && humidity>0)) return 0;
        float mismatch=temperature/TemperatureFalloff; float moisture=humidity/HumidityFalloff;
        return Mathf.Max(0.001f,Mathf.Exp(-0.5f*(mismatch*mismatch+moisture*moisture)));
    }
    // Distance outside a closed authored range; values inside carry full preference.
    // =========================================================
    private static float Distance(float value,Vector2 range) => Mathf.Max(range.X-value,Mathf.Max(0,value-range.Y));
    // Unit-aware finite ordered ranges prevent malformed Resources from poisoning selection weights.
    // =========================================================
    private static bool ValidRange(Vector2 range,float min,float max) => range.IsFinite() && range.X>=min && range.Y<=max && range.X<=range.Y;
}
