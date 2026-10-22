// Detached checkpoint values only: workers never touch live scene objects.
public sealed record SavedPlayer(float X, float Y, string Layer = "surface");
public sealed record SavedClock(float Hour, float CycleSeconds, bool Paused);
public sealed record SavedTestEntity(bool Exists, float X, float Y, int Health);
public sealed record ChunkEdit(string Id, bool Removed, float BurnProgress);
public sealed record SavedChunk(string WorldId, string Layer, int X, int Y, ChunkEdit[] Edits);
public sealed record ChunkReference(string Layer, int X, int Y, string Hash);
public sealed record CampaignSnapshot(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    string ContentFingerprint, string SavedUtc, WorldBlueprintManifest Blueprint, SavedPlayer Player,
    SavedClock Clock, SavedTestEntity TestEntity, SavedChunk[] Chunks)
{ public const int CurrentSchema = 1; }
public sealed record CampaignIndex(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    string ContentFingerprint, string SavedUtc, string BlueprintHash, SavedPlayer Player,
    SavedClock Clock, SavedTestEntity TestEntity, ChunkReference[] Chunks);
public sealed record ActiveCampaign(string ProfileId, string RunId, string WorldId);
public sealed record CampaignLoad(CampaignSnapshot Snapshot, bool Recovered);
