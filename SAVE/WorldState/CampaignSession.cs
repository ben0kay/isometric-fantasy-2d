// Explicit one-shot launch request and shared IO configuration; no world nodes or mutable chunk caches.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
public sealed record CampaignLaunch(string ProfileId, string RunId, string WorldId, int Seed, CampaignSnapshot Restore);
public static class CampaignSession
{
    private static CampaignLaunch _pending;
    internal static string TestStorageRoot { get; set; }
    public static SaveMasterConfig Settings() => GD.Load<SaveMasterConfig>("res://SAVE/DefaultSaveConfig.tres");
    public static CampaignStore Store()
    {
        var config = Settings();
        return new CampaignStore(TestStorageRoot ?? ProjectSettings.GlobalizePath("user://saves"),config.MaximumFileMegabytes,config.MaximumChangedChunks);
    }
    // Capture IDs before scene transition; selecting a profile alone never creates a world.
    // =========================================================
    public static void New(string profileId, int? seed = null)
    {
        profileId = CheckpointFiles.Id(profileId);
        _pending = new(profileId,Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),seed ?? Random.Shared.Next(),null);
    }
    public static void Continue(CampaignSnapshot snapshot) => _pending = new(snapshot.ProfileId,snapshot.RunId,snapshot.WorldId,snapshot.Blueprint.Seed,snapshot);
    public static CampaignLaunch Consume() { var request = _pending; _pending = null; return request; }
    public static void Clear() => _pending = null;

    // Hash text scene/resource dependencies, excluding seed policy. Textures stay external; generation versions gate algorithms.
    // =========================================================
    public static string ContentFingerprint() => BuildContentFingerprint(null);
    // Narrow SO2 compatibility bridge: only the previously shipped scene text is substituted.
    // =========================================================
    internal static string LegacySO1ContentFingerprint() => BuildContentFingerprint("res://SAVE/Migrations/References/SO1WorldScene.txt");
    // Explicit GS1 migration fingerprint retains every unrelated dependency.
    // =========================================================
    internal static string LegacyGS1ContentFingerprint() => BuildContentFingerprint(null,true);
    // Compare the full dependency set with only explicitly requested frozen substitutions.
    // =========================================================
    private static string BuildContentFingerprint(string previousWorldScene, bool previousGround = false, bool previousPatches = false, bool previousOakExtras = false, bool previousRecipes = false, bool previousPng = false, bool previousDirtEdges = false, IReadOnlyDictionary<string, string> seasonalReferences = null, bool previousIdentity = false, bool previousFineGround = false)
    {
        if (previousWorldScene != null && !Godot.FileAccess.FileExists(previousWorldScene))
            throw new InvalidDataException("Missing explicit SO1 layout migration reference.");
        bool fineGround=(previousFineGround || previousIdentity || previousDirtEdges || previousPng || previousRecipes || previousOakExtras || previousPatches || previousGround) && CanMigrateFineGround();
        bool identity=(previousIdentity || previousDirtEdges || previousPng || previousRecipes || previousOakExtras || previousPatches || previousGround) && CanMigrateIdentity();
        var files = new SortedDictionary<string,string>(StringComparer.Ordinal);
        void Visit(string path)
        {
            if (files.ContainsKey(path)) return;
            if (!Godot.FileAccess.FileExists(path)) throw new InvalidDataException("Missing campaign dependency: " + path);
            string source = Godot.FileAccess.GetFileAsString(path == "res://SCENES/World/World.tscn" && previousWorldScene != null ? previousWorldScene : path).Replace("\r\n","\n");
            if(fineGround)
            {
                string reference=path switch {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/FineGroundOakBefore.txt",
                    "res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres" => "res://SAVE/Migrations/References/FineGroundDirtBefore.txt",
                    "res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres" => "res://SAVE/Migrations/References/FineGroundRockBefore.txt",
                    _ => null };
                if(reference!=null) source=Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            // Remove this identity-only schema change before reconstructing any older terrain layout.
            if(identity)
            {
                string reference=path switch {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/IdentityOakBefore.txt",
                    "res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres" => "res://SAVE/Migrations/References/IdentityPinkBefore.txt",
                    _ => null };
                if(reference!=null) source=Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            if(previousDirtEdges && path=="res://WORLD/Biomes/Definitions/OakForest/Biome.tres")
                source=Godot.FileAccess.GetFileAsString("res://SAVE/Migrations/References/DirtEdgesOakBefore.txt").Replace("\r\n","\n");
            if(previousPng)
            {
                string reference=path switch {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/PngDirtOakBefore.txt",
                    "res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres" => "res://SAVE/Migrations/References/PngDirtProfileBefore.txt",
                    _ => null };
                if(reference!=null) source=Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            if(previousRecipes)
            {
                string reference=path switch {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/ProceduralOakBefore.txt",
                    "res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres" => "res://SAVE/Migrations/References/ProceduralDirtBefore.txt",
                    "res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres" => "res://SAVE/Migrations/References/ProceduralRockBefore.txt",
                    _ => null };
                if(reference!=null) source=Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            if(previousOakExtras && path=="res://WORLD/Biomes/Definitions/OakForest/Biome.tres")
                source=Godot.FileAccess.GetFileAsString("res://SAVE/Migrations/References/GS5OakBeforeExtras.txt").Replace("\r\n","\n");
            if (previousPatches)
            {
                string reference = path switch
                {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/PatchLayerOakForest.txt",
                    "res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres" => "res://SAVE/Migrations/References/PatchLayerPinkMeadow.txt",
                    _ => null
                };
                if(reference!=null) source=Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            if (previousGround)
            {
                string reference = path switch
                {
                    "res://WORLD/Biomes/Definitions/OakForest/Biome.tres" => "res://SAVE/Migrations/References/GS1OakForest.txt",
                    "res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres" => "res://SAVE/Migrations/References/GS1PinkMeadow.txt",
                    _ => null
                };
                if (reference != null) source = Godot.FileAccess.GetFileAsString(reference).Replace("\r\n","\n");
            }
            if (seasonalReferences != null && seasonalReferences.TryGetValue(path, out string seasonalReference))
            {
                if (!Godot.FileAccess.FileExists(seasonalReference)) throw new InvalidDataException("Missing seasonal migration reference: " + seasonalReference);
                source = Godot.FileAccess.GetFileAsString(seasonalReference).Replace("\r\n", "\n");
            }
            string normalized = string.Join("\n",source.Split('\n').Where(line => !line.StartsWith("RandomSeedOnLoad = ",StringComparison.Ordinal) && !line.StartsWith("FixedSeed = ",StringComparison.Ordinal)));
            files.Add(path,normalized);
            foreach (Match match in Regex.Matches(source,"path=\"(res://[^\"]+\\.(?:tres|tscn))\"")) Visit(match.Groups[1].Value);
        }
        Visit("res://WORLD/DefaultWorldConfig.tres"); Visit("res://SCENES/World/World.tscn");
        var text = new StringBuilder();
        foreach (var pair in files) text.Append(pair.Key).Append('\n').Append(pair.Value).Append('\n');
        return CheckpointFiles.Digest(Encoding.UTF8.GetBytes(text.ToString()));
    }
    // Only the exact shipped GS1 or accompanying extraction may use frozen pre-GS1 resource text.
    // =========================================================
    internal static bool CanMigrateGround() => CanMigratePatches() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "A351B0A6BEB3A2E5EEC862993F5DD80A6A1F1F16B11823AA86DC1A98FBF49FBD" &&
            CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "76D21299E1AE93C37A37E50F567AFA89F1EA09950CB2E0A16035F2C544C942E4";

    // Exact accompanying-layer extraction only, with all unrelated dependency text still compared.
    // =========================================================
    internal static string LegacyPatchLayerFingerprint() => BuildContentFingerprint(null,false,true);
    internal static bool CanMigratePatches() => CanMigrateOakExtras() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "16E0DDE1492A9FE91043A34B350BAA252370457905044EAFF26A9728ABFAAFB0" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "AD426D1F72246B1B4711AB78F32FC494D2D4A539E43D0CC43567CD84A60453C8";

    // Exact optional Oak content addition, preserving all unrelated dependency checks.
    // =========================================================
    internal static string LegacyOakExtrasFingerprint() => BuildContentFingerprint(null,false,false,true);
    internal static bool CanMigrateOakExtras() => CanMigrateRecipes() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "D7702F421654910023F887B9E1F037E5843D168EE5E94FE04B0BB3F96158006C" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "AD426D1F72246B1B4711AB78F32FC494D2D4A539E43D0CC43567CD84A60453C8" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "74F226EA5363C4F19B97DE64A56BC69A3213CA276AC7C83B6AB62F352CC757E6" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "5D785BE17D1D388209D9E653E4630D1CFE776CA970733F6B24028987E050D000";

    // Exact appearance-only recipe replacement; unrelated dependency changes still reject.
    // =========================================================
    internal static string LegacyRecipeFingerprint() => BuildContentFingerprint(null,false,false,false,true);
    internal static bool CanMigrateRecipes() => CanMigrateDirtPng() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "F1EF5B922EFB75957F52D7D023ACE85A383E97950A613F116EF0AB5D6FD84A4E" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "AD426D1F72246B1B4711AB78F32FC494D2D4A539E43D0CC43567CD84A60453C8" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "46048C1C9CE04AB9C6296F52D14358BD7B8FD6ED8D71D6F6E5CB09BCAEDC80FF" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "5451A796CE25C205BC2FAFE45FBF2B778A7516B098C96BF52030EF0AC21558C4";

    // Only this shipped PNG/tint conversion may reconstruct the old full content fingerprint.
    // =========================================================
    internal static string LegacyDirtPngFingerprint() => BuildContentFingerprint(null,false,false,false,false,true);
    internal static bool CanMigrateDirtPng() => CanMigrateDirtEdges() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "98C31D4D61AB2AFDD68BBBD01151B77C4942C2C3063F5ACEFB67408B67958071" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "420883184A259F63C067AF1207BD8585FCA6FB67C78BB4D1DB455E9EAE8D6288" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "AD426D1F72246B1B4711AB78F32FC494D2D4A539E43D0CC43567CD84A60453C8" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "5451A796CE25C205BC2FAFE45FBF2B778A7516B098C96BF52030EF0AC21558C4";

    // Exact authored opacity revision only; older full dependency fingerprints remain strict.
    // =========================================================
    internal static string LegacyDirtEdgesFingerprint() => BuildContentFingerprint(null,false,false,false,false,false,true);
    internal static bool CanMigrateDirtEdges() => CanMigrateIdentity() || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "510AE25586F58BC052AB57B27273FBA6F1ADFB33A31830C2C30A8A88150562DB" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "AD426D1F72246B1B4711AB78F32FC494D2D4A539E43D0CC43567CD84A60453C8" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "420883184A259F63C067AF1207BD8585FCA6FB67C78BB4D1DB455E9EAE8D6288" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "5451A796CE25C205BC2FAFE45FBF2B778A7516B098C96BF52030EF0AC21558C4";

    // Exact shipped colour relocation only; all other dependency text still participates.
    // =========================================================
    internal static string LegacyIdentityFingerprint() => BuildContentFingerprint(null,previousIdentity:true);
    internal static bool CanMigrateIdentity() => CanMigrateFineGround() ||
        IdentitySourceMatches("OakForest","IdentityOakBefore.txt","OverviewColour = Color(0.22, 0.49, 0.25, 1)","Colour = Color(0.2196, 0.2745, 0.2078, 1)") &&
        IdentitySourceMatches("PinkMeadow","IdentityPinkBefore.txt","OverviewColour = Color(0.85, 0.48, 0.66, 1)","Colour = Color(0.43, 0.265, 0.35, 1)") &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "420883184A259F63C067AF1207BD8585FCA6FB67C78BB4D1DB455E9EAE8D6288" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "5451A796CE25C205BC2FAFE45FBF2B778A7516B098C96BF52030EF0AC21558C4";

    // Compare the complete resource source, ignoring only the two explicitly known colour lines.
    // =========================================================
    private static bool IdentitySourceMatches(string biome,string reference,string previous,string current)
    {
        string source=Godot.FileAccess.GetFileAsString($"res://WORLD/Biomes/Definitions/{biome}/Biome.tres").Replace("\r\n","\n");
        string before=Godot.FileAccess.GetFileAsString("res://SAVE/Migrations/References/"+reference).Replace("\r\n","\n");
        return source.Contains(current+"\n",StringComparison.Ordinal) &&
            source.Replace(current+"\n","")==before.Replace(previous+"\n","");
    }

    // Exact appearance tuning only; preserve all unrelated campaign dependencies.
    // =========================================================
    internal static string LegacyFineGroundFingerprint() => BuildContentFingerprint(null,previousFineGround:true);
    internal static bool CanMigrateFineGround() => CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/OakForest/Biome.tres").Replace("\r\n","\n"))) == "7CCF1D5DC9F6C6AD1C3676ABED637ADB8ADA9FA50FAF04F054CA6C2A41160BF4" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres").Replace("\r\n","\n"))) == "E8427A7BB1C375788721C0F4045C79F75219598CACFF1AE11BB0FC716955A5E6" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Terrain/Ground/Surfaces/Profiles/Rock.tres").Replace("\r\n","\n"))) == "31DE6B2CD89001DAB8358592A25E4ECC3372D30EFDA5DE3D098ADE1693B08CF4" &&
        CheckpointFiles.Digest(Encoding.UTF8.GetBytes(Godot.FileAccess.GetFileAsString("res://WORLD/Biomes/Definitions/PinkMeadow/Biome.tres").Replace("\r\n","\n"))) == "2514FAF9C8428FD29DC24A73D0BFA24B797D45EF2E4566230EEDA2B49FEFD838";

    // Compose exact seasonal substitutions with already gated terrain/layout bridges.
    // This work runs only while opening incompatible campaigns, never during world simulation.
    // =========================================================
    internal static IEnumerable<string> SeasonalContentFingerprints()
    {
        if (!SeasonalSaveCompatibility.CanMigrate()) yield break;
        foreach (var references in SeasonalSaveCompatibility.ReferenceSets())
        {
            yield return BuildContentFingerprint(null, seasonalReferences: references);
            if (CanMigrateFineGround()) yield return BuildContentFingerprint(null, seasonalReferences: references, previousFineGround:true);
            if (CanMigrateIdentity()) yield return BuildContentFingerprint(null, seasonalReferences: references, previousIdentity:true);
            if (CanMigrateDirtEdges()) yield return BuildContentFingerprint(null, previousDirtEdges: true, seasonalReferences: references);
            if (CanMigrateDirtPng()) yield return BuildContentFingerprint(null, previousPng: true, seasonalReferences: references);
            if (CanMigrateRecipes()) yield return BuildContentFingerprint(null, previousRecipes: true, seasonalReferences: references);
            if (CanMigrateOakExtras()) yield return BuildContentFingerprint(null, previousOakExtras: true, seasonalReferences: references);
            if (CanMigratePatches()) yield return BuildContentFingerprint(null, previousPatches: true, seasonalReferences: references);
            if (CanMigrateGround()) yield return BuildContentFingerprint(null, previousGround: true, seasonalReferences: references);
            string scene = Godot.FileAccess.GetFileAsString("res://SCENES/World/World.tscn").Replace("\r\n", "\n");
            if (CheckpointFiles.Digest(Encoding.UTF8.GetBytes(scene)) == SaveCompatibility.SO2SceneSourceHash)
                yield return BuildContentFingerprint("res://SAVE/Migrations/References/SO1WorldScene.txt", seasonalReferences: references);
        }
    }

    // Reject changed content rather than moving old trees or rerolling geography behind the player's back.
    // =========================================================
    public static void RequireContent(CampaignSnapshot snapshot)
    {
        if (snapshot.ContentFingerprint == ContentFingerprint()) return;
        if(CanMigrateFineGround() && snapshot.ContentFingerprint==LegacyFineGroundFingerprint()) return;
        if(CanMigrateIdentity() && snapshot.ContentFingerprint==LegacyIdentityFingerprint()) return;
        if(CanMigrateDirtEdges() && snapshot.ContentFingerprint==LegacyDirtEdgesFingerprint()) return;
        if(CanMigrateDirtPng() && snapshot.ContentFingerprint==LegacyDirtPngFingerprint()) return;
        if(CanMigrateRecipes() && snapshot.ContentFingerprint==LegacyRecipeFingerprint()) return;
        if(CanMigrateOakExtras() && snapshot.ContentFingerprint==LegacyOakExtrasFingerprint()) return;
        if (CanMigratePatches() && snapshot.ContentFingerprint == LegacyPatchLayerFingerprint()) return;
        // Exact GS1 data conversion only; all other current dependency text still participates.
        if (CanMigrateGround() && snapshot.ContentFingerprint == LegacyGS1ContentFingerprint()) return;
        // This bridge is valid only for the exact shipped SO2 layout, not arbitrary future scene edits.
        string scene = Godot.FileAccess.GetFileAsString("res://SCENES/World/World.tscn").Replace("\r\n","\n");
        if (CheckpointFiles.Digest(Encoding.UTF8.GetBytes(scene)) == SaveCompatibility.SO2SceneSourceHash
            && snapshot.ContentFingerprint == LegacySO1ContentFingerprint()) return;
        foreach (string fingerprint in SeasonalContentFingerprints())
            if (snapshot.ContentFingerprint == fingerprint) return;
        throw new InvalidDataException("Campaign content/settings have changed. A deliberate migration is required; the saved world was preserved.");
    }
}
