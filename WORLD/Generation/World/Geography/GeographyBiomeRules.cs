// Temporary macro planner rules: hard freshwater exclusions plus soft biome preferences, never a gameplay service.
using System;

internal sealed class GeographyBiomeRules
{
    private readonly GeographyWaterKind[] _mask;
    private readonly ClimateBiomeRules _climate;
    private readonly bool[] _biased=new bool[4];
    private readonly float[][] _preferences=new float[4][];
    // Cache the four lake/river combinations once instead of inspecting Resources in every growth edge.
    // =========================================================
    internal int GenerationVersion { get; private set; }
    internal int LandCells { get; private set; }
    internal bool IsLand(int cell) => (_mask[cell]&GeographyWaterKind.Ocean)==0;
    internal GeographyBiomeRules(WorldGeographyPlan geography,float cellSize,int columns,int rows,WorldBiomeDefinition[] definitions,WorldClimatePlan climate=null)
    {
        GenerationVersion=climate!=null ? MacroWorldMap.ClimateGeneratorVersion : geography?.PlanVersion==1 ? MacroWorldMap.GeographyGeneratorVersion : MacroWorldMap.OceanGeneratorVersion;
        _mask=geography?.BuildWaterMask(cellSize,columns,rows) ?? new GeographyWaterKind[columns*rows];
        _climate=climate==null ? null : new ClimateBiomeRules(climate,cellSize,columns,rows,definitions);
        for (int kind=0;kind<4;kind++)
        {
            _preferences[kind]=new float[definitions.Length];
            for (int b=0;b<definitions.Length;b++)
            {
                var h=definitions[b].Hydrology;
                if (h!=null && (!float.IsFinite(h.LakeSuitability) || h.LakeSuitability<0 || h.LakeSuitability>1
                    || !float.IsFinite(h.RiverSuitability) || h.RiverSuitability<0 || h.RiverSuitability>1))
                    throw new ArgumentException($"Biome {definitions[b].Id}: freshwater suitability must be finite within 0–1.");
                bool lake=(kind&1)!=0,river=(kind&2)!=0;
                if (h!=null && ((lake && !h.AllowFreshwaterLakes) || (river && !h.AllowFreshwaterRivers))) { _biased[kind]=true; continue; }
                // Zero suitability stays a soft preference: it never erases water or makes an allowed biome impossible.
                float preference=1;
                if (h!=null && lake) preference*=0.1f+0.9f*h.LakeSuitability;
                if (h!=null && river) preference*=0.1f+0.9f*h.RiverSuitability;
                _preferences[kind][b]=preference; if (preference!=1f) _biased[kind]=true;
            }
        }
        for (int cell=0;cell<_mask.Length;cell++)
        {
            if (!IsLand(cell)) continue;
            LandCells++;
            bool eligible=false;
            for (int b=0;b<definitions.Length;b++)
                if (definitions[b].GenerationWeight>0 && Preference(cell,b)>0) { eligible=true; break; }
            if (!eligible) throw new ArgumentException($"No positive-weight biome is compatible with geography/climate at macro cell ({cell%columns},{cell/columns}), water={_mask[cell]}. Major water was not removed.");
        }
        if (LandCells==0) throw new ArgumentException("Geography leaves no land for biome regions/start.");
    }
    internal bool NeedsSelection(int cell) => _climate!=null || _biased[(int)_mask[cell]&3];
    internal float Preference(int cell,int biome) => IsLand(cell) ? _preferences[(int)_mask[cell]&3][biome]*(_climate?.Preference(cell,biome) ?? 1f) : 0f;
    // Stable weighted fallback for constrained cells/components; never silently claim them with a forbidden biome.
    // =========================================================
    internal int Select(int cell,WorldBiomeDefinition[] definitions,double unit)
    {
        double total=0;
        for (int b=0;b<definitions.Length;b++) total+=definitions[b].GenerationWeight*Preference(cell,b);
        double choice=unit*total; int last=-1;
        for (int b=0;b<definitions.Length;b++)
        {
            double weight=definitions[b].GenerationWeight*Preference(cell,b); if (weight<=0) continue;
            last=b; choice-=weight; if (choice<0) return b;
        }
        if (last<0) throw new ArgumentException("No eligible biome for geography cell.");
        return last;
    }
}
