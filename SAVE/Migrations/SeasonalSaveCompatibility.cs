// Exact appearance-only compatibility for shipped SE0/SE1/SE1b resource revisions.
// Reconstructed fingerprints still include every unrelated current dependency; no disk/schema edits.
using Godot;
using System.Collections.Generic;
using System.Text;

internal static class SeasonalSaveCompatibility
{
    private static readonly (string Path, string Hash)[] CurrentVisuals = {
        ("res://ENVIRONMENT/DefaultEnvironmentConfig.tres", "7D7F2ADFFA4F36ED010A0881AB03A0E2D1DD38F540C2601DF14B26D513CC798D"),
        ("res://ENVIRONMENT/Seasons/DefaultSeasonConfig.tres", "348E2B6950F7D3D4D589F61A888A0A0D288FA022E27E6DF15B5A87205AD50FC3"),
        ("res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres", "A770E744D9928ACB9C1A45BEBC580D2C199F42F15DEB525CE5F6193E936A7AC1"),
        ("res://WORLD/Content/Vegetation/Trees/Oak/OakSeasonalFoliage.tres", "C71676B8965BBB25C83697648390B827F1551DEC05E7D96DC96C6E3512F5C12B"),
    };

    // Future authoring changes disable this bridge until deliberately reviewed again.
    // =========================================================
    internal static bool CanMigrate(bool previousHarvest=false)
    {
        foreach (var item in CurrentVisuals)
        {
            if (!FileAccess.FileExists(item.Path)) return false;
            string source = FileAccess.GetFileAsString(previousHarvest && HarvestSaveCompatibility.Previous(item.Path) is string old ? old : item.Path).Replace("\r\n", "\n");
            if (CheckpointFiles.Digest(Encoding.UTF8.GetBytes(source)) != item.Hash) return false;
        }
        return true;
    }

    // Values identify frozen text files, not replacement Resources to load into the live world.
    // Each historical dependency graph is followed after substitution, excluding newly added references.
    // =========================================================
    internal static IEnumerable<IReadOnlyDictionary<string, string>> ReferenceSets()
    {
        const string prefix = "res://SAVE/Migrations/References/";
        const string oak = "res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres";
        const string season = "res://ENVIRONMENT/Seasons/DefaultSeasonConfig.tres";
        yield return new Dictionary<string, string> {
            [oak] = prefix + "SeasonsPreSE1Oak.txt",
            ["res://ENVIRONMENT/DefaultEnvironmentConfig.tres"] = prefix + "SeasonsPreSE0Environment.txt"
        };
        yield return new Dictionary<string, string> {
            [oak] = prefix + "SeasonsPreSE1Oak.txt", [season] = prefix + "SeasonsSE0Config.txt"
        };
        yield return new Dictionary<string, string> {
            [season] = prefix + "SeasonsSE0Config.txt",
            ["res://WORLD/Content/Vegetation/Trees/Oak/OakSeasonalFoliage.tres"] = prefix + "SeasonsSE1Palette.txt"
        };
    }
}
