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
    public static string ContentFingerprint()
    {
        var files = new SortedDictionary<string,string>(StringComparer.Ordinal);
        void Visit(string path)
        {
            if (files.ContainsKey(path)) return;
            if (!Godot.FileAccess.FileExists(path)) throw new InvalidDataException("Missing campaign dependency: " + path);
            string source = Godot.FileAccess.GetFileAsString(path).Replace("\r\n","\n");
            string normalized = string.Join("\n",source.Split('\n').Where(line => !line.StartsWith("RandomSeedOnLoad = ",StringComparison.Ordinal) && !line.StartsWith("FixedSeed = ",StringComparison.Ordinal)));
            files.Add(path,normalized);
            foreach (Match match in Regex.Matches(source,"path=\"(res://[^\"]+\\.(?:tres|tscn))\"")) Visit(match.Groups[1].Value);
        }
        Visit("res://WORLD/DefaultWorldConfig.tres"); Visit("res://SCENES/World/World.tscn");
        var text = new StringBuilder();
        foreach (var pair in files) text.Append(pair.Key).Append('\n').Append(pair.Value).Append('\n');
        return CheckpointFiles.Digest(Encoding.UTF8.GetBytes(text.ToString()));
    }
    // Reject changed content rather than moving old trees or rerolling geography behind the player's back.
    // =========================================================
    public static void RequireContent(CampaignSnapshot snapshot)
    {
        if (snapshot.ContentFingerprint != ContentFingerprint())
            throw new InvalidDataException("Campaign content/settings have changed. A deliberate migration is required; the saved world was preserved.");
    }
}
