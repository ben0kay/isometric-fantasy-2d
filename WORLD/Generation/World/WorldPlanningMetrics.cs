// Read-only planning evidence, collected only during creation; never a runtime profiler or SAVE dependency.
public sealed record WorldPlanningMetrics(double FeaturesMilliseconds,double WaterMilliseconds,double ClimateMilliseconds,double BiomesMilliseconds,double StartMilliseconds,
    int Cells,int Regions,long EstimatedFrozenBytes,long EstimatedPaletteBytes,int PeakGrowthQueue,long GrowthEnqueues)
{
    public double TotalMilliseconds => FeaturesMilliseconds+WaterMilliseconds+ClimateMilliseconds+BiomesMilliseconds+StartMilliseconds;
}
public sealed class MacroBuildCounters
{
    public int PeakQueue { get; internal set; }
    public long Enqueues { get; internal set; }
}
