// Local profile identity registry; durable campaigns reference these GUIDs without replacing this format.
using Godot;
using System;
using System.Collections.Generic;
public static class ProfileStore
{
    private static bool _fromBackup;
    private const string Path = "user://profiles.cfg";
    public sealed class Profile
    {
        public string Id { get; }
        public string Name { get; }
        public Profile(string id, string name) { Id = id; Name = name; }
    }
    private static readonly List<Profile> _profiles = new();
    public static IReadOnlyList<Profile> Profiles => _profiles;
    public static Profile Selected { get; private set; }
    public static string LastError { get; private set; } = "";
    // Read on entering boot; do not fabricate a profile if data is invalid.
    // =========================================================
    public static void Load()
    {
        _profiles.Clear(); Selected = null; LastError = ""; _fromBackup = false;
        ConfigFile file = new();
        Error error = file.Load(Path);
        if (error != Error.Ok && FileAccess.FileExists(Path + ".bak"))
        {
            file = new ConfigFile(); error = file.Load(Path + ".bak"); _fromBackup = error == Error.Ok;
        }
        if (error == Error.FileNotFound && !FileAccess.FileExists(Path)) return;
        if (error != Error.Ok) { LastError = "Profiles could not be read: " + error + ". Existing files were preserved."; return; }
        foreach (string id in file.GetSections())
        {
            if (!Guid.TryParse(id, out _)) continue;
            string name = file.GetValue(id, "name", "").AsString().Trim();
            if (name.Length > 0) _profiles.Add(new Profile(id, name));
        }
    }
    // Add a stable independent identity; never use the display name as the save key.
    // =========================================================
    public static Profile Create(string name)
    {
        if (!string.IsNullOrEmpty(LastError)) throw new InvalidOperationException(LastError);
        name = (name ?? "").Trim();
        if (name.Length < 2 || name.Length > 32) throw new ArgumentException("Profile name must be 2–32 characters.");
        foreach (var existing in _profiles)
            if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A profile with this name already exists.");
        var profile = new Profile(Guid.NewGuid().ToString("N"), name);
        _profiles.Add(profile);
        try { Save(); }
        catch { _profiles.Remove(profile); throw; }
        Selected = profile;
        return profile;
    }
    // =========================================================
    public static bool Select(string id)
    {
        Selected = _profiles.Find(p => p.Id == id);
        return Selected != null;
    }
    // Save via a temporary file; keep the previous registry if write fails.
    // =========================================================
    private static void Save()
    {
        ConfigFile config = new();
        foreach (Profile p in _profiles) config.SetValue(p.Id, "name", p.Name);
        Error error = config.Save(Path + ".tmp");
        if (error != Error.Ok) throw new InvalidOperationException("Could not save profile: " + error);
        string absolute = ProjectSettings.GlobalizePath(Path);
        string temporary = ProjectSettings.GlobalizePath(Path + ".tmp");
        // Preserve the original ConfigFile format and retain a recoverable previous registry.
        if (_fromBackup && System.IO.File.Exists(absolute))
            System.IO.File.Move(absolute,absolute + ".rejected-" + Guid.NewGuid().ToString("N"));
        if (System.IO.File.Exists(absolute)) System.IO.File.Replace(temporary,absolute,absolute + ".bak");
        else System.IO.File.Move(temporary,absolute);
        _fromBackup = false;
    }
}
