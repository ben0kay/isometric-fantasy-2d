// Exact BP1 configuration-only additions preserve the pre-BP1 dependency graph; unrelated edits stay strict.
using Godot;
using System.Text;

public static class BiomeProfileSaveCompatibility
{
    // Once profiles gain runtime meaning, remove/version this bridge before consuming them.
    // =========================================================
    public static string Normalize(string path, string source)
    {
        if (path == "res://WORLD/Biomes/Definitions/OakForest/Biome.tres"
            && Matches("res://WORLD/Biomes/Definitions/OakForest/Biome.tres", "31D13AB87A036F28E3573D0E8114D91C915DFBA5F0F3AD5FB8AAFE142FE9F054")
            && Matches("res://WORLD/Biomes/Definitions/OakForest/Climate.tres", "E8F04B373B4D3DCA4E82E29789E1C0C6333B1584EBFC1D32596E65EB05A38D07")
            && Matches("res://WORLD/Biomes/Definitions/OakForest/Spawning/Entities.tres", "B5F4D8E7970E5885B45A3A0C3945B0D07D16FCB6D956F662C5AC1C202F64492E")
            && Matches("res://WORLD/Biomes/Definitions/OakForest/Spawning/Resources.tres", "3879A7378661BC838EE77980E8000903660BE5181EB706C0F6EC125803098B49"))
            return Godot.FileAccess.GetFileAsString("res://SAVE/Migrations/References/BP1OakBefore.txt").Replace("\r\n", "\n");
        return source;
    }

    // Only byte-equivalent LF-normalized shipped Resources may omit their inert dependency additions.
    // =========================================================
    private static bool Matches(string path, string digest) => Godot.FileAccess.FileExists(path)
        && CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString(path).Replace("\r\n", "\n"))) == digest;
}
