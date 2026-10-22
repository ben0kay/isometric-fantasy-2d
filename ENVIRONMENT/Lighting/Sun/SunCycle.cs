// Art-directed daylight/projection calculations retained from the tested 2D sandbox.
using Godot;
public static class SunCycle
{
    // Smooth threshold mapping, not a value-interpolating smoothstep.
    // =========================================================
    public static float Edge(float low, float high, float value)
    {
        float t = Mathf.Clamp((value - low) / (high - low), 0f, 1f); return t * t * (3f - 2f * t);
    }
    // Sunrise at 06:00 and sunset at 18:00, without another advancing clock.
    // =========================================================
    public static float Altitude(float hour) => Mathf.Sin((hour - 6f) / 24f * Mathf.Tau);
}
