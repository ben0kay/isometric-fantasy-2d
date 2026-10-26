// Generation-only climate weights, cached once per cell/biome and discarded after macro growth.
using Godot;
using System;
internal sealed class ClimateBiomeRules
{
    private readonly float[][] _preferences;
    // Bound temporary memory explicitly; current four-biome map uses about 0.9 MB of weights.
    // =========================================================
    internal ClimateBiomeRules(WorldClimatePlan climate,float cellSize,int columns,int rows,WorldBiomeDefinition[] definitions)
    {
        int count=checked(columns*rows);
        if((long)count*definitions.Length>16777216) throw new ArgumentException("Climate preference cache exceeds 16M entries; reduce macro resolution/content count.");
        _preferences=new float[definitions.Length][];
        for(int b=0;b<definitions.Length;b++)
        {
            definitions[b].PlacementClimate?.Validate();
            _preferences[b]=new float[count];
        }
        for(int y=0;y<rows;y++) for(int x=0;x<columns;x++)
        {
            float left=x*cellSize,top=y*cellSize;
            var position=climate.Bounds.Position+new Vector2(left+Mathf.Min(cellSize,climate.Bounds.Size.X-left)*0.5f,top+Mathf.Min(cellSize,climate.Bounds.Size.Y-top)*0.5f);
            var sample=climate.Sample(position);
            for(int b=0;b<definitions.Length;b++) _preferences[b][y*columns+x]=definitions[b].PlacementClimate?.Preference(sample) ?? 1f;
        }
    }
    // Shared by initial site choice, every growth edge and constrained-component fallback.
    // =========================================================
    internal float Preference(int cell,int biome) => _preferences[biome][cell];
}
