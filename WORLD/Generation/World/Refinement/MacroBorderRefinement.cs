// Stateless coordinate-keyed refinement of a frozen coarse blueprint; no sectors, caches or exploration-order RNG.
using Godot;
public static class MacroBorderRefinement
{
    // CPU and shader use the same small integer lattice and smooth interpolation contract (version 1).
    // =========================================================
    public static Vector2 Offset(Vector2 local,float cellSize,int seed,float strength)
    {
        Vector2 p=local/(cellSize*2f);
        int x=Mathf.FloorToInt(p.X),y=Mathf.FloorToInt(p.Y);
        float fx=p.X-x,fy=p.Y-y; fx=fx*fx*(3-2*fx); fy=fy*fy*(3-2*fy);
        float Channel(uint salt) => Mathf.Lerp(Mathf.Lerp(Hash(x,y,seed,salt),Hash(x+1,y,seed,salt),fx),Mathf.Lerp(Hash(x,y+1,seed,salt),Hash(x+1,y+1,seed,salt),fx),fy);
        return new Vector2(Channel(17),Channel(97))*(cellSize*strength);
    }
    // Low 16 bits map exactly to single-precision shader inputs; runtime string hashing is never used.
    // =========================================================
    private static float Hash(int x,int y,int seed,uint salt)
    {
        unchecked { uint v=(uint)x*73856093u^(uint)y*19349663u^(uint)(seed&65535)^salt;
            v=(v^(v>>16))*0x7FEB352Du; v=(v^(v>>15))*0x846CA68Bu; return ((v^(v>>16))&65535u)/65535f*2-1; }
    }
}
