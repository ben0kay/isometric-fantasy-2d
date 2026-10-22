// Profile/run/world storage: immutable chunk blobs and one atomic index form a coherent checkpoint.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
public sealed class CampaignStore
{
    private readonly string _root;
    private readonly int _limit, _maxChunks;
    public CampaignStore(string root, int maximumMegabytes = 32, int maximumChunks = 32768)
    { _root = root; _limit = Math.Clamp(maximumMegabytes, 1, 128)*1024*1024; _maxChunks = Math.Clamp(maximumChunks, 1, 65536); }
    public string ProfilePath(string profile) => Path.Combine(_root, CheckpointFiles.Id(profile));
    public string RunPath(ActiveCampaign key) => Path.Combine(ProfilePath(key.ProfileId), "runs", CheckpointFiles.Id(key.RunId), CheckpointFiles.Id(key.WorldId));
    public bool HasCampaign(string profile) => File.Exists(Path.Combine(ProfilePath(profile), "active.json")) || File.Exists(Path.Combine(ProfilePath(profile), "active.json.bak"));
    #region Recovery and commit
    // Recover committed checkpoints only; uncommitted temporary files are never loaded.
    // =========================================================
    public CampaignLoad Load(string profile)
    {
        profile = CheckpointFiles.Id(profile); var errors = new List<string>();
        string active = Path.Combine(ProfilePath(profile), "active.json");
        foreach (string pointer in new[] { active, active + ".bak" })
        {
            if (!File.Exists(pointer)) continue;
            try
            {
                var key = CheckpointFiles.Read<ActiveCampaign>(pointer, _limit);
                if (key.ProfileId != profile) throw new InvalidDataException("Profile ownership mismatch.");
                string run = RunPath(key), path = Path.Combine(run, "checkpoint.json");
                foreach (string candidate in new[] { path, path + ".bak" })
                {
                    if (!File.Exists(candidate)) continue;
                    try
                    {
                        var snapshot = ReadCheckpoint(key,candidate);
                        return new(snapshot, pointer != active || candidate != path);
                    }
                    catch (Exception error) when (Recoverable(error)) { errors.Add(error.Message); }
                }
            }
            catch (Exception error) when (Recoverable(error)) { errors.Add(error.Message); }
        }
        throw new InvalidDataException(errors.Count == 0 ? "No saved campaign." : "Cannot load campaign: " + string.Join(" | ",errors));
    }
    // Validate the complete referenced transaction, not just its outer JSON checksum.
    // =========================================================
    private CampaignSnapshot ReadCheckpoint(ActiveCampaign key, string path)
    {
        var index = CheckpointFiles.Read<CampaignIndex>(path,_limit);
        if (index.ProfileId != key.ProfileId || index.RunId != key.RunId || index.WorldId != key.WorldId)
            throw new InvalidDataException("Campaign ownership mismatch.");
        string blobs = Path.Combine(RunPath(key),"blobs");
        var blueprint = CheckpointFiles.Blob<WorldBlueprintManifest>(blobs,index.BlueprintHash,_limit);
        if (index.Chunks == null || index.Chunks.Length > _maxChunks) throw new InvalidDataException("Invalid chunk count.");
        var chunks = index.Chunks.Select(c =>
        {
            if (c == null) throw new InvalidDataException("Empty chunk reference.");
            var chunk = CheckpointFiles.Blob<SavedChunk>(blobs,c.Hash,_limit);
            if (chunk.WorldId != key.WorldId || chunk.Layer != c.Layer || chunk.X != c.X || chunk.Y != c.Y)
                throw new InvalidDataException("Chunk ownership mismatch.");
            return chunk;
        }).ToArray();
        var result = new CampaignSnapshot(index.SchemaVersion,index.ProfileId,index.RunId,index.WorldId,index.ContentFingerprint,
            index.SavedUtc,blueprint,index.Player,index.Clock,index.TestEntity,chunks);
        Validate(result); return result;
    }
    // Preserve corrupt primaries rather than letting replacement overwrite a known good backup.
    // =========================================================
    private void PreserveInvalid(string path, Action validate)
    {
        if (!File.Exists(path)) return;
        try { validate(); }
        catch (Exception error) when (Recoverable(error)) { File.Move(path,path + ".rejected-" + Guid.NewGuid().ToString("N")); }
    }
    // Called by the coordinator's sole worker; new-run activation is the final write.
    // =========================================================
    public void Save(CampaignSnapshot s, bool activate = false)
    {
        Validate(s); var key = new ActiveCampaign(s.ProfileId,s.RunId,s.WorldId);
        string run = RunPath(key), blobs = Path.Combine(run,"blobs");
        string blueprint = CheckpointFiles.Put(blobs,s.Blueprint,_limit);
        var chunks = s.Chunks.OrderBy(c => c.Y).ThenBy(c => c.X).Select(c =>
            new ChunkReference(c.Layer,c.X,c.Y,CheckpointFiles.Put(blobs,c,_limit))).ToArray();
        var index = new CampaignIndex(s.SchemaVersion,s.ProfileId,s.RunId,s.WorldId,s.ContentFingerprint,s.SavedUtc,
            blueprint,s.Player,s.Clock,s.TestEntity,chunks);
        string checkpoint = Path.Combine(run,"checkpoint.json");
        PreserveInvalid(checkpoint,() => ReadCheckpoint(key,checkpoint));
        CheckpointFiles.Write(checkpoint,index,_limit);
        if (activate)
        {
            string active = Path.Combine(ProfilePath(s.ProfileId),"active.json");
            PreserveInvalid(active,() =>
            {
                var previous = CheckpointFiles.Read<ActiveCampaign>(active,_limit);
                if (previous.ProfileId != s.ProfileId) throw new InvalidDataException("Profile pointer mismatch.");
                RunPath(previous);
            });
            CheckpointFiles.Write(active,key,_limit);
        }
    }
    private static bool Recoverable(Exception e) => e is IOException || e is InvalidDataException || e is System.Text.Json.JsonException || e is ArgumentException || e is OverflowException;
    #endregion
    // Validate detached data before commit/materialization; IDs are never inferred from the seed.
    // =========================================================
    public void Validate(CampaignSnapshot s)
    {
        if (s == null) throw new InvalidDataException("Empty campaign.");
        CheckpointFiles.Id(s.ProfileId); CheckpointFiles.Id(s.RunId); CheckpointFiles.Id(s.WorldId);
        CheckpointFiles.Hash(s.ContentFingerprint); SaveCompatibility.RequireSupported(s.SchemaVersion,s.Blueprint);
        var b = s.Blueprint;
        if (b.ProfileId != s.ProfileId || b.RunId != s.RunId || b.WorldId != s.WorldId) throw new InvalidDataException("Blueprint ownership mismatch.");
        if (s.Player == null || s.Clock == null || s.Chunks == null || s.TestEntity == null || s.Chunks.Length > _maxChunks
            || s.Player.Layer != "surface" || !float.IsFinite(s.Player.X) || !float.IsFinite(s.Player.Y)
            || !float.IsFinite(s.Clock.Hour) || s.Clock.Hour < 0 || s.Clock.Hour >= 24 || !float.IsFinite(s.Clock.CycleSeconds) || s.Clock.CycleSeconds < 1)
            throw new InvalidDataException("Invalid player, layer, clock or chunks.");
        if ((s.SchemaVersion >= 2 && s.Player.Health == null) || s.Player.Health is < 0 or > 1000000)
            throw new InvalidDataException("Invalid saved player health.");
        if (s.TestEntity.Exists && (!float.IsFinite(s.TestEntity.X) || !float.IsFinite(s.TestEntity.Y) || s.TestEntity.Health < 1)) throw new InvalidDataException("Invalid test actor.");
        var map = WorldBlueprintRestore.Map(b); WorldBlueprintRestore.Features(b);
        if (s.TestEntity.Exists && !map.Bounds.HasPoint(new Godot.Vector2(s.TestEntity.X,s.TestEntity.Y))) throw new InvalidDataException("Test actor outside world bounds.");
        if (!map.Bounds.HasPoint(new Godot.Vector2(s.Player.X,s.Player.Y))) throw new InvalidDataException("Player outside world bounds.");
        var coords = new HashSet<(int,int)>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var chunk in s.Chunks)
        {
            if (chunk == null || chunk.WorldId != s.WorldId || chunk.Layer != "surface" || chunk.Edits == null
                || Math.Abs((long)chunk.X)*b.ChunkSize > int.MaxValue || Math.Abs((long)chunk.Y)*b.ChunkSize > int.MaxValue
                || !coords.Add((chunk.X,chunk.Y)) || !new ChunkCoord(chunk.X,chunk.Y).Bounds(b.ChunkSize).Intersects(map.Bounds)) throw new InvalidDataException("Invalid chunk identity.");
            foreach (var edit in chunk.Edits)
            {
                if (edit == null || string.IsNullOrWhiteSpace(edit.Id) || !ids.Add(edit.Id) || !float.IsFinite(edit.BurnProgress) || edit.BurnProgress < 0 || edit.BurnProgress > 1)
                    throw new InvalidDataException("Invalid or duplicated edit.");
                var parts = edit.Id.Split('/');
                if (parts.Length != 4 || parts[0] != "surface" || !int.TryParse(parts[1],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out int x)
                    || !int.TryParse(parts[2],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out int y)) throw new InvalidDataException("Invalid scenery ID.");
                var cell = new Godot.Rect2((float)((double)x*b.CandidateCellSize),(float)((double)y*b.CandidateCellSize),b.CandidateCellSize,b.CandidateCellSize);
                if (!cell.Intersects(new ChunkCoord(chunk.X,chunk.Y).Bounds(b.ChunkSize))) throw new InvalidDataException("Edit belongs to another chunk.");
            }
        }
    }
}
