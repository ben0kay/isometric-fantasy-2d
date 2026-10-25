// Persistent player adapter over shared inventory/hotbar models; UI/pickups never own a second copy.
using Godot;
using System;
using System.IO;
public partial class PlayerInventory : Node
{
    public InventoryStorage Storage { get; private set; }
    public InventoryHotbar Hotbar { get; private set; }
    private EquippedTool _equipment;
    private PlayerInput _input;
    // Restore exact state or apply the authored loadout once to a new/legacy session.
    // =========================================================
    public void Initialize(PlayerMovement player,SavedInventory saved)
    {
        var profile=GD.Load<PlayerInventoryProfile>("res://ENTITIES/Player/Inventory/DefaultPlayerInventory.tres");
        var catalog=profile.Items.Catalog; catalog.Initialize();
        Storage=new InventoryStorage(catalog,profile.Storage.StorageSlots);
        _equipment=player.GetNode<EquippedTool>("EquippedAxe"); _input=player.GetNode<PlayerInput>("Input");
        Hotbar=new InventoryHotbar(Storage,9) { CanSelect=Equip };
        Hotbar.SelectionInvalidated+=_equipment.ClearInventoryEquipment;
        if (saved!=null)
        {
            if (saved.Slots!=Storage.SlotCount) throw new InvalidDataException("Saved backpack capacity differs from authored inventory.");
            Storage.Restore(saved.Items); Hotbar.Restore(saved.HotbarKeys,saved.SelectedSlot); _equipment.RestoreShield(saved.ShieldEquipped);
        }
        else
        {
            if (profile.StartingItems==null) throw new InvalidDataException("Missing starting loadout.");
            foreach(var item in profile.StartingItems) if (!Storage.TryAdd(item,1)) throw new InvalidDataException("Invalid/overflowing starting loadout.");
            int shortcut=0; for(int i=0;i<Storage.SlotCount && shortcut<Hotbar.SlotCount;i++) if(!Storage.Get(i).IsEmpty) Hotbar.Bind(shortcut++,i);
            Hotbar.Select(0);
        }
        _input.HotbarSelectionEnabled=true; _input.ShieldToggleEnabled=true; _input.HotbarSelectionRequested+=Select;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // Ordinary weapon switching preserves the separate temporary shield flag until a real shield item exists.
    // =========================================================
    private bool Equip(ItemDefinition item)
    {
        bool shield=_equipment.ShieldEquipped;
        if (!_equipment.TryEquipInventoryItem(item)) return false;
        _equipment.RestoreShield(shield); return true;
    }
    // Central input emits selection intent; the equipment adapter rejects busy switches without queueing.
    // =========================================================
    private void Select(int slot) => Hotbar.Select(slot);
    // Capture all individual items, shortcut identities and temporary guard-equipment state in one detached value.
    // =========================================================
    public SavedInventory Capture() => new(Storage.SlotCount,Storage.Capture(),Hotbar.CaptureKeys(),Hotbar.SelectedSlot,_equipment.ShieldEquipped);
    // Release references when the player/world leaves; no global inventory survives a profile switch.
    // =========================================================
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) { _input.HotbarSelectionRequested-=Select; _input.HotbarSelectionEnabled=false; _input.ShieldToggleEnabled=false; }
        Hotbar?.Dispose();
    }
}
