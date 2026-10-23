// One small action gate: committed actions never overlap or queue; dodge may explicitly end a guard first.
public enum PlayerAction { Idle, Attack, Block, Dodge, Hop }
public sealed class PlayerActions
{
    public PlayerAction Current { get; private set; }
    // Only an idle actor can begin a committed action.
    // =========================================================
    public bool Begin(PlayerAction action)
    { if (action==PlayerAction.Idle || Current!=PlayerAction.Idle) return false; Current=action; return true; }
    // An old action's completion must never clear a newer action.
    // =========================================================
    public void End(PlayerAction action) { if (Current==action) Current=PlayerAction.Idle; }
    // Death, suspension and focus loss discard transient state without queuing a replacement.
    // =========================================================
    public void Reset() => Current=PlayerAction.Idle;
}
