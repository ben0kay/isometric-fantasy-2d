// Player adapter for explicit nearby collection. Storage and world drop authority stay with their shared owners.
using Godot;
public partial class PlayerPickupInteraction : Node
{
    private PlayerMovement _player;
    private PlayerInput _input;
    private InventoryStorage _storage;
    private WorldDropSpawner _drops;
    // Bind once in a disposable fixture; H4 must supply real persistent player ownership before campaign activation.
    // =========================================================
    public void Initialize(PlayerMovement player,InventoryStorage storage,WorldDropSpawner drops)
    {
        _player=player; _storage=storage; _drops=drops; _input=player.GetNode<PlayerInput>("Input");
        _input.PickupInteractionEnabled=true; _input.PickupRequested+=Collect;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // No automatic retries or range polling: one input request makes one all-or-nothing transaction.
    // =========================================================
    private void Collect()
    { if (!_input.InventoryOpen && GodotObject.IsInstanceValid(_drops)) _drops.TryCollectNearest(_player,_storage); }
    // Input subscriptions cannot retain an unloaded collector or storage model.
    // =========================================================
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_input)) { _input.PickupRequested-=Collect; _input.PickupInteractionEnabled=false; } }
}
