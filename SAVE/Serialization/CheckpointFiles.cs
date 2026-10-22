// Checksummed immutable blobs and atomic checkpoint replacement; no Godot API calls.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
public static class CheckpointFiles
{
    private sealed record Envelope(int Version, string Hash, string Payload);
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = false };
    // Only canonical IDs/hashes are permitted in filesystem paths.
    // =========================================================
    public static string Id(string value) => Guid.TryParseExact(value, "N", out var id)
        ? id.ToString("N") : throw new InvalidDataException("Invalid save identity.");
    public static string Hash(string value) => value != null && value.Length == 64 && IsHex(value)
        ? value : throw new InvalidDataException("Invalid save blob hash.");
    private static bool IsHex(string text)
    { foreach (char c in text) if (!(c >= '0' && c <= '9') && !(c >= 'A' && c <= 'F')) return false; return true; }
    public static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    // Bound reads before allocating and reject incomplete/incompatible envelopes.
    // =========================================================
    public static T Read<T>(string path, int limit)
    {
        if (new FileInfo(path).Length > limit) throw new InvalidDataException("Save exceeds configured file limit.");
        var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(path), Json) ?? throw new InvalidDataException("Empty save.");
        if (envelope.Version != 1 || envelope.Payload == null || Digest(Encoding.UTF8.GetBytes(envelope.Payload)) != envelope.Hash)
            throw new InvalidDataException("Save integrity check failed.");
        return JsonSerializer.Deserialize<T>(envelope.Payload, Json) ?? throw new InvalidDataException("Empty payload.");
    }
    // Flush temp contents before atomic replacement, preserving the previous committed checkpoint.
    // =========================================================
    public static void Write<T>(string path, T value, int limit)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string payload = JsonSerializer.Serialize(value, Json);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(1, Digest(Encoding.UTF8.GetBytes(payload)), payload), Json);
        if (bytes.Length > limit) throw new InvalidDataException("Save exceeds configured file limit.");
        using (var file = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        { file.Write(bytes); file.Flush(true); }
        if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
        else File.Move(path + ".tmp", path);
    }
    // Publish immutable blobs before the index references them; unchanged blobs are reused.
    // =========================================================
    public static string Put<T>(string directory, T value, int limit)
    {
        string hash = Digest(JsonSerializer.SerializeToUtf8Bytes(value, Json));
        string path = Path.Combine(directory, hash + ".json");
        if (!File.Exists(path)) Write(path, value, limit);
        else Blob<T>(directory, hash, limit);
        return hash;
    }
    public static T Blob<T>(string directory, string hash, int limit)
    {
        T value = Read<T>(Path.Combine(directory, Hash(hash) + ".json"), limit);
        if (Digest(JsonSerializer.SerializeToUtf8Bytes(value, Json)) != hash) throw new InvalidDataException("Save blob identity mismatch.");
        return value;
    }
}
