// Shared presentation data. Defaults reproduce tree-hit leaves; callers snapshot once per burst.
using Godot;
public enum ParticleShape { Leaf, Shard, Droplet, Spark }
[GlobalClass]
public partial class ParticleProfile : Resource
{
    [ExportGroup("Motion")]
    [Export] public Vector2 VelocityMin { get; set; } = new(-12,18);
    [Export] public Vector2 VelocityMax { get; set; } = new(12,29);
    [Export] public Vector2 Acceleration { get; set; }
    [Export] public float DirectionSpeed { get; set; } = 5;
    [Export] public float SpreadDegrees { get; set; }
    [Export] public float WindMultiplier { get; set; } = 1;
    [Export] public float FlutterAmplitude { get; set; } = 8;
    [Export] public float FlutterFrequency { get; set; } = 5;
    [ExportGroup("Lifetime and shape")]
    [Export] public float LifetimeMin { get; set; } = 2.4f;
    [Export] public float LifetimeMax { get; set; } = 3.8f;
    [Export] public float FadeSeconds { get; set; } = 0.7f;
    [Export] public ParticleShape Shape { get; set; }
    [Export] public float Size { get; set; } = 1;
    [Export] public float Spin { get; set; } = 3;
    [Export] public float StretchMin { get; set; } = 0.4f;
    [Export] public float StretchFrequency { get; set; } = 4;

    // Clamp authored extremes; non-finite values use safe defaults without mutating the shared Resource.
    // =========================================================
    public ParticleValues Snapshot()
    {
        Vector2 min=SafeVector(VelocityMin,new(-12,18),1000), max=SafeVector(VelocityMax,new(12,29),1000);
        float lifeMin=Bound(LifetimeMin,2.4f,0.01f,10), lifeMax=Bound(LifetimeMax,3.8f,0.01f,10);
        return new(new(Mathf.Min(min.X,max.X),Mathf.Min(min.Y,max.Y)),new(Mathf.Max(min.X,max.X),Mathf.Max(min.Y,max.Y)),
            SafeVector(Acceleration,Vector2.Zero,2000),Bound(DirectionSpeed,5,0,1000),Bound(WindMultiplier,1,-10,10),
            Bound(FlutterAmplitude,8,0,100),Bound(FlutterFrequency,5,0,100),Mathf.Min(lifeMin,lifeMax),Mathf.Max(lifeMin,lifeMax),
            Bound(FadeSeconds,0.7f,0.001f,10),System.Enum.IsDefined(Shape) ? Shape : ParticleShape.Leaf,
            Bound(Size,1,0.01f,20),Bound(Spin,3,-100,100),Bound(StretchMin,0.4f,0.01f,1),Bound(StretchFrequency,4,0,100),Bound(SpreadDegrees,0,0,180));
    }
    // =========================================================
    private static float Bound(float value,float fallback,float min,float max) => Mathf.Clamp(float.IsFinite(value) ? value : fallback,min,max);
    // =========================================================
    private static Vector2 SafeVector(Vector2 value,Vector2 fallback,float bound) => value.IsFinite() ? value.Clamp(Vector2.One*-bound,Vector2.One*bound) : fallback;
}
public readonly record struct ParticleValues(Vector2 VelocityMin,Vector2 VelocityMax,Vector2 Acceleration,float DirectionSpeed,
    float WindMultiplier,float FlutterAmplitude,float FlutterFrequency,float LifetimeMin,float LifetimeMax,float FadeSeconds,
    ParticleShape Shape,float Size,float Spin,float StretchMin,float StretchFrequency,float SpreadDegrees=0)
{
    // Also guard public value-struct callers (including default values) at the emission boundary.
    public bool IsValid => VelocityMin.IsFinite() && VelocityMax.IsFinite() && Acceleration.IsFinite()
        && float.IsFinite(DirectionSpeed) && float.IsFinite(WindMultiplier) && float.IsFinite(FlutterAmplitude)
        && float.IsFinite(FlutterFrequency) && float.IsFinite(LifetimeMin) && LifetimeMin>0 && LifetimeMin<=10
        && float.IsFinite(LifetimeMax) && LifetimeMax>=LifetimeMin && LifetimeMax<=10
        && float.IsFinite(FadeSeconds) && FadeSeconds>0 && float.IsFinite(Size) && Size>0
        && float.IsFinite(Spin) && float.IsFinite(StretchMin) && StretchMin>0 && StretchMin<=1
        && float.IsFinite(StretchFrequency) && float.IsFinite(SpreadDegrees);
}
