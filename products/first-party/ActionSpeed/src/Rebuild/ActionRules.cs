namespace Yuuka.DTMAPI.ActionSpeed;

internal enum ActionKind { None, Tool, Fill, Eat, Interact, Gather, Plant }
internal enum NativeAction
{
    Other, ToolSwing, ManualWater, BottleFill, CanRefill, WellFill, Food,
    Fuel, Feed, Pet, Milk, Electrical, CropInteract, Forage, Resin,
    PlantSeed, PlantTree, Fertilize, Film, FarmingGun
}

internal static class ActionRules
{
    internal static ActionKind Classify(NativeAction action, bool canHarvest = false) => action switch
    {
        NativeAction.ToolSwing or NativeAction.ManualWater => ActionKind.Tool,
        NativeAction.BottleFill or NativeAction.CanRefill or NativeAction.WellFill => ActionKind.Fill,
        NativeAction.Food => ActionKind.Eat,
        NativeAction.Fuel or NativeAction.Feed or NativeAction.Pet or NativeAction.Milk or NativeAction.Electrical => ActionKind.Interact,
        NativeAction.CropInteract => canHarvest ? ActionKind.Gather : ActionKind.None,
        NativeAction.Forage or NativeAction.Resin => ActionKind.Gather,
        NativeAction.PlantSeed or NativeAction.PlantTree or NativeAction.Fertilize or NativeAction.Film or NativeAction.FarmingGun => ActionKind.Plant,
        _ => ActionKind.None
    };

    internal static float DrinkClockFactor(bool enabled, bool bottledWater) => enabled && bottledWater ? 3 : 1;

    internal static bool CanAutoFill(bool enabled, bool normal, bool inWater, bool emptyBottle,
        bool supportsAction, bool enoughEnergy, bool bufferFull, bool overUi) =>
        enabled && normal && inWater && emptyBottle && supportsAction && enoughEnergy && !bufferFull && !overUi;
}

// Synchronous native entry scopes unwind even if a native action rejects or throws.
internal sealed class ActionScope
{
    private long sequence;
    private long currentToken;
    internal ActionKind Current { get; private set; }
    internal Receipt Push(ActionKind next)
    {
        var token = checked(++sequence);
        var receipt = new Receipt(this, token, currentToken, Current);
        currentToken = token;
        Current = next;
        return receipt;
    }
    internal void Restore(Receipt receipt)
    {
        if (!ReferenceEquals(receipt.Owner, this) || receipt.Token != currentToken) return;
        currentToken = receipt.ParentToken;
        Current = receipt.Previous;
    }
    internal void Reset() { currentToken = 0; Current = ActionKind.None; }

    internal readonly struct Receipt
    {
        internal Receipt(ActionScope owner, long token, long parentToken, ActionKind previous)
        { Owner = owner; Token = token; ParentToken = parentToken; Previous = previous; }
        internal ActionScope? Owner { get; }
        internal long Token { get; }
        internal long ParentToken { get; }
        internal ActionKind Previous { get; }
        internal void Restore() => Owner?.Restore(this);
    }
}

internal sealed class FillSchedule
{
    private bool started;
    private float elapsed;
    internal bool Tick(float dt, bool eligible, bool strong) => Tick(dt, eligible, strong ? 0.12 : 0.25);
    internal bool Tick(float dt, bool eligible, double interval)
    {
        if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0) return false;
        elapsed += dt;
        if (!eligible) return false;
        if (started && elapsed + 0.000001f < interval) return false;
        started = true;
        elapsed = 0;
        return true;
    }
    internal void Reset() { started = false; elapsed = 0; }
}
