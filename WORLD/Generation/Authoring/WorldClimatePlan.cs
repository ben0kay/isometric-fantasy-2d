// Immutable analytical macro field: continuous position queries, no world grid, Nodes or runtime weather work.
using Godot;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
public enum WorldClimateZone { Cold, Temperate, Hot, Enchanted }
public readonly record struct WorldClimateSample(float TemperatureCelsius,float RelativeHumidityPercent,WorldClimateZone Zone,float Enchantment);
public sealed record WorldClimateSnapshot(int Version,int Seed,float NorthTemperature,float SouthTemperature,float NorthHumidity,float SouthHumidity,
    float Warp,bool Enchanted,float AnchorX,float AnchorY,float ExtentX,float ExtentY,float Transition,string Hash);
public sealed class WorldClimatePlan
{
    public const int Version=1;
    private readonly WorldClimateSnapshot _data;
    private readonly int _fieldSeed;
    public Rect2 Bounds { get; }
    public string Hash => _data.Hash;
    // Domain/version keys isolate geography, climate and future sectors from order-sensitive RNG draws.
    // =========================================================
    public static int DeriveSeed(int seed,string domain)
    {
        byte[] bytes=SHA256.HashData(Encoding.UTF8.GetBytes($"world.author.v{Version}:{seed.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{domain}"));
        return System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }
    // Freeze scalar authoring values, not Resource references or mutable noise instances.
    // =========================================================
    public static WorldClimatePlan FromPreset(WorldAuthorPreset p,int seed,Rect2 bounds) => new(bounds,
        new(Version,seed,p.NorthTemperatureCelsius,p.SouthTemperatureCelsius,p.NorthHumidityPercent,p.SouthHumidityPercent,
            p.BoundaryWarp,p.EnchantedEnabled,p.EnchantedAnchor.X,p.EnchantedAnchor.Y,p.EnchantedExtent.X,p.EnchantedExtent.Y,p.EnchantedTransition,""),false);
    // Restore never reads current presets. A missing climate remains absent on old worlds.
    // =========================================================
    public static WorldClimatePlan Restore(WorldClimateSnapshot data,int seed,Rect2 bounds)
    {
        if(data==null) return null;
        if(data.Seed!=seed) throw new InvalidDataException("Climate/world seed mismatch.");
        return new(bounds,data,true);
    }
    // Hash explicit binary values and world bounds so JSON formatting and locale cannot change identity.
    // =========================================================
    private WorldClimatePlan(Rect2 bounds,WorldClimateSnapshot d,bool restoring)
    {
        if(d.Version!=Version || !bounds.Position.IsFinite() || !bounds.Size.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X<=0 || bounds.Size.Y<=0
            || !Range(d.NorthTemperature,-100,100) || !Range(d.SouthTemperature,-100,100) || d.NorthTemperature>=d.SouthTemperature
            || !Range(d.NorthHumidity,0,100) || !Range(d.SouthHumidity,0,100) || !Range(d.Warp,0,0.2f)
            || !Range(d.AnchorX,0,1) || !Range(d.AnchorY,0,1) || !Range(d.ExtentX,0.01f,1) || !Range(d.ExtentY,0.01f,1) || !Range(d.Transition,0.01f,1))
            throw new InvalidDataException("Invalid frozen climate field.");
        Bounds=bounds; _fieldSeed=DeriveSeed(d.Seed,"climate.field");
        using var stream=new MemoryStream(); using var writer=new BinaryWriter(stream);
        writer.Write(d.Version); writer.Write(d.Seed);
        foreach(float v in new[] {bounds.Position.X,bounds.Position.Y,bounds.Size.X,bounds.Size.Y,d.NorthTemperature,d.SouthTemperature,
            d.NorthHumidity,d.SouthHumidity,d.Warp,d.AnchorX,d.AnchorY,d.ExtentX,d.ExtentY,d.Transition}) writer.Write(v);
        writer.Write(d.Enchanted); writer.Flush();
        string hash=Convert.ToHexString(SHA256.HashData(stream.ToArray()));
        if(restoring && d.Hash!=hash) throw new InvalidDataException("Frozen climate hash mismatch.");
        _data=d with { Hash=hash };
    }
    // No query mutates caches or depends on exploration order; out-of-world calls fail explicitly.
    // =========================================================
    public WorldClimateSample Sample(Vector2 position)
    {
        if(!position.IsFinite() || !Bounds.HasPoint(position)) throw new ArgumentOutOfRangeException(nameof(position));
        Vector2 uv=(position-Bounds.Position)/Bounds.Size;
        float warp=Noise(uv*5)*_data.Warp;
        float latitude=Mathf.Clamp(uv.Y+warp*Mathf.Sin(Mathf.Pi*uv.Y),0,1);
        float temperature=Mathf.Lerp(_data.NorthTemperature,_data.SouthTemperature,latitude);
        float humidity=Mathf.Clamp(Mathf.Lerp(_data.NorthHumidity,_data.SouthHumidity,latitude)+Noise(uv*7+new Vector2(19,31))*8,0,100);
        float magic=0;
        if(_data.Enchanted)
        {
            float distance=((uv-new Vector2(_data.AnchorX,_data.AnchorY))/new Vector2(_data.ExtentX,_data.ExtentY)).Length();
            magic=1-Mathf.SmoothStep(1-_data.Transition,1+_data.Transition,distance+Noise(uv*11)*0.1f);
        }
        var zone=magic>=0.5f ? WorldClimateZone.Enchanted : temperature<2 ? WorldClimateZone.Cold : temperature>=24 ? WorldClimateZone.Hot : WorldClimateZone.Temperate;
        return new(temperature,humidity,zone,magic);
    }
    // Detached value snapshot; no caller can modify a live field through the save handoff.
    // =========================================================
    public WorldClimateSnapshot Snapshot() => _data with { };
    // Smooth bounded lattice noise avoids straight belts and sector seams without Resource noise ownership.
    // =========================================================
    private float Noise(Vector2 p)
    {
        int x=Mathf.FloorToInt(p.X),y=Mathf.FloorToInt(p.Y);
        float sx=Mathf.SmoothStep(0,1,p.X-x),sy=Mathf.SmoothStep(0,1,p.Y-y);
        return Mathf.Lerp(Mathf.Lerp(Lattice(x,y),Lattice(x+1,y),sx),Mathf.Lerp(Lattice(x,y+1),Lattice(x+1,y+1),sx),sy);
    }
    // Explicit integer mixing is stable across runtimes; no string.GetHashCode or shared RNG state.
    // =========================================================
    private float Lattice(int x,int y)
    {
        unchecked { uint v=(uint)_fieldSeed^(uint)x*0x9E3779B9u^(uint)y*0x85EBCA6Bu; v^=v>>16; v*=0x7FEB352Du; v^=v>>15; v*=0x846CA68Bu; return (v^(v>>16))/4294967295f*2-1; }
    }
    // Central finite-range check used for generated and disk-supplied field values.
    // =========================================================
    private static bool Range(float v,float min,float max) => float.IsFinite(v) && v>=min && v<=max;
}
