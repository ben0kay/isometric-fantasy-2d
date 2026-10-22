// Startup-only actor discovery across explicit ownership containers; chunk sources register separately.
using Godot;
using System.Collections.Generic;
public static class WorldActorSources
{
    // Descend only marked ownership containers, never arbitrary component/effect or streamed chunk trees.
    // =========================================================
    public static IEnumerable<Node2D> Initial(Node2D root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is not Node2D source || source.HasMeta("world_chunk_id")) continue;
            if (source.HasMeta("world_actor_container"))
            { foreach (var actor in Initial(source)) yield return actor; }
            else yield return source;
        }
    }
}
