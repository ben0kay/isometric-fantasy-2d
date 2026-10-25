using System.Text.Json.Serialization;
// Detached checkpoint values only: workers never touch live scene objects.
public sealed record SavedPlayer(float X, float Y, string Layer = "surface", int? Health = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] SavedInventory Inventory = null);
public sealed record SavedClock(float Hour, float CycleSeconds, bool Paused);
public sealed record SavedTestEntity(bool Exists, float X, float Y, int Health);
public sealed record ChunkEdit(string Id, bool Removed, float BurnProgress, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] float HarvestWork = 0, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Felled = false);
public sealed record SavedChunk(string WorldId, string Layer, int X, int Y, ChunkEdit[] Edits, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] SavedDrop[] Drops = null);
public sealed record ChunkReference(string Layer, int X, int Y, string Hash);
public sealed record CampaignSnapshot(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    string ContentFingerprint, string SavedUtc, WorldBlueprintManifest Blueprint, SavedPlayer Player,
    SavedClock Clock, SavedTestEntity TestEntity, SavedChunk[] Chunks)
{ public const int CurrentSchema = 3; }
public sealed record CampaignIndex(int SchemaVersion, string ProfileId, string RunId, string WorldId,
    string ContentFingerprint, string SavedUtc, string BlueprintHash, SavedPlayer Player,
    SavedClock Clock, SavedTestEntity TestEntity, ChunkReference[] Chunks);
public sealed record ActiveCampaign(string ProfileId, string RunId, string WorldId);
public sealed record CampaignLoad(CampaignSnapshot Snapshot, bool Recovered);

// Versioned detached item/shortcut/drop values; no Resources or live Nodes cross the save-worker boundary.
public sealed record SavedItemStack(int Slot,string ItemId,int Count,long Key);
public sealed record SavedInventory(int Slots,SavedItemStack[] Items,long[] HotbarKeys,int SelectedSlot,bool ShieldEquipped);
public sealed record SavedDrop(string Id,string SourceId,string ItemId,int Quantity,float X,float Y,float Rotation);
