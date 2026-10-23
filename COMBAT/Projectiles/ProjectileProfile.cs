// Straight point-arrow data; snapshots reuse canonical combat contact values without a melee executor.
using Godot;
using System;
[GlobalClass]
public partial class ProjectileProfile : Resource
{
    [Export] public AttackProfile Impact { get; set; }
    [Export(PropertyHint.Range,"1,2000,1")] public float Speed { get; set; } = 240;
    [Export(PropertyHint.Range,"1,4000,1")] public float MaximumDistance { get; set; } = 700;
    [Export(PropertyHint.Range,"0.05,10,0.05")] public float Lifetime { get; set; } = 4;
    [Export] public Color Colour { get; set; } = new(0.8f,0.65f,0.4f);
    [Export(PropertyHint.Range,"4,24,1")] public float VisualLength { get; set; } = 12;
    [Export(PropertyHint.Range,"0,32,1")] public float VisualHeight { get; set; } = 12;
    // Reject malformed authoring before creating any runtime/query resources.
    // =========================================================
    public ProjectileValues Snapshot(EffectiveStats stats)
    {
        EffectiveStats.Validate(stats);
        if(Impact==null || !float.IsFinite(Speed) || Speed<1 || Speed>2000 || !float.IsFinite(MaximumDistance) || MaximumDistance<1 || MaximumDistance>4000
            || !float.IsFinite(Lifetime) || Lifetime<0.05f || Lifetime>10 || !float.IsFinite(VisualLength) || VisualLength<4 || VisualLength>24
            || !float.IsFinite(VisualHeight) || VisualHeight<0 || VisualHeight>32 || (!float.IsFinite(Colour.R) || !float.IsFinite(Colour.G) || !float.IsFinite(Colour.B) || !float.IsFinite(Colour.A))) throw new ArgumentException("Invalid projectile profile.");
        var impact=Impact.Snapshot(stats.AttackSpeedMultiplier);
        if(impact.CollisionMask!=3) throw new ArgumentException("R0 arrows require world solids + actors (mask 3).");
        return new(Speed,MaximumDistance,Lifetime,VisualLength,VisualHeight,Colour,impact,stats);
    }
}
public readonly record struct ProjectileValues(float Speed,float Distance,float Lifetime,float Length,float Height,Color Colour,AttackValues Impact,EffectiveStats Stats);
