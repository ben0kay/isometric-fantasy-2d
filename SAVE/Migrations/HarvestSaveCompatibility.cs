// Exact pre-H4 resource introduction only. All unrelated old dependencies still participate in the reconstructed fingerprint.
using Godot;
using System.Text;
internal static class HarvestSaveCompatibility
{
    private static readonly (string Path,string Hash)[] Current = {
        ("res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres","6FB0024B21562290847421F513AB9D26150E07674A90D0C5DDD01FC45CC3A402"),
        ("res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossom.tres","4AF2345A799CA3CD984214CFDEC01DD26C8F3773CC44D1CE5A1A8594671099F1"),
        ("res://ITEMS/Tools/Axes/StarterAxe.tres","E9F0A5987F52744FF5DF3A5A5E0FA781A0CD2DF7E38C5E516A2ABB40ED7A9742"),
        ("res://ITEMS/DefaultItemConfig.tres","A2227F5327FB3DD7386579109E002DD1B9FC4E97F7C888DB27893C3288F9AE8D"),
        ("res://ITEMS/DefaultItemCatalog.tres","F6D3EFE58E6B08252ACD6BDA30B42B20FED8E6EA9E05EF8592031CF592A05C10"),
        ("res://SYSTEMS/Inventory/DefaultInventoryConfig.tres","E41EE65153B41777889AAA4D815946DCF80FD603FCB1783F5BF59974C725E998"),
        ("res://ITEMS/Tools/Axes/StarterAxeItem.tres","F36123650C7538F5F723C1154BDEAE47AD62FA660FF8DD2EA4525502D43A3B58"),
        ("res://ITEMS/Weapons/Swords/Starter/StarterSwordItem.tres","7CDEA80A0CCFA44ADBD10719FAD78EDFCAFD6DFA69F9ACB03A0B0B25EBD2C23A"),
        ("res://ITEMS/Resources/Wood/WoodenLog/WoodenLog.tres","DD551BB73B6D46EE50D412DBD8FE6CAE2D4553139FAC57A0FB732AE9D7FD9895"),
        ("res://ENTITIES/Player/Inventory/DefaultPlayerInventory.tres","1354E0CD4EEE56293BFDC97F6559A451E4B070D52C9EFF03E416DD91D6B2D4E6"),
        ("res://SYSTEMS/Drops/DefaultDropScatter.tres","39D1362289CFE16EFE8E8500FDD1DAE43C5B8CE70527EA7BDB3E8B5AEE6824CB"),
        ("res://SYSTEMS/Harvesting/DefaultTreeHarvest.tres","874C198793FB89B60341A1046223F5ED85520DC809E84228741A9C703F8D7217"),
    };
    // Authoring changes disable this bridge instead of silently moving old geography or altering stored items.
    // =========================================================
    internal static bool CanMigrate()
    {
        foreach(var pair in Current)
            if(!FileAccess.FileExists(pair.Path) || CheckpointFiles.Digest(Encoding.UTF8.GetBytes(FileAccess.GetFileAsString(pair.Path).Replace("\r\n","\n")))!=pair.Hash) return false;
        return true;
    }
    // Substitute only those production resources deliberately extended by this introduction.
    // =========================================================
    internal static string Previous(string path) => path switch {
        "res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres"=>"res://SAVE/Migrations/References/HarvestBeforeOak.txt",
        "res://WORLD/Content/Vegetation/Trees/PinkBlossom/PinkBlossom.tres"=>"res://SAVE/Migrations/References/HarvestBeforeBlossom.txt",
        "res://ITEMS/Tools/Axes/StarterAxe.tres"=>"res://SAVE/Migrations/References/HarvestBeforeAxe.txt",
        _=>null
    };
}
