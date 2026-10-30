namespace Yuuka.DTMAPI.ActionSpeed;

public sealed class ActionConfig
{
    public int SchemaVersion { get; set; } = 1;
    public bool ToolEnabled { get; set; } = true;
    public double ToolMultiplier { get; set; } = 3;
    public bool FillEnabled { get; set; } = true;
    public double FillMultiplier { get; set; } = 3;
    public bool EatEnabled { get; set; } = true;
    public double EatMultiplier { get; set; } = 3;
    public bool InteractEnabled { get; set; } = true;
    public double InteractMultiplier { get; set; } = 3;
    public bool GatherEnabled { get; set; } = true;
    public double GatherMultiplier { get; set; } = 3;
    public bool PlantEnabled { get; set; } = true;
    public double PlantMultiplier { get; set; } = 3;
    public bool AutoFill { get; set; }
    public bool StrongAutoFill { get; set; }
    public bool ContinuousDrink { get; set; } = true;
    public double AutoFillInterval { get; set; } = 0.25;
    public double StrongAutoFillInterval { get; set; } = 0.12;

    internal float Factor(ActionKind kind) => kind switch
    {
        ActionKind.Tool => ToolEnabled ? Limit(ToolMultiplier) : 1,
        ActionKind.Fill => FillEnabled ? Limit(FillMultiplier) : 1,
        ActionKind.Eat => EatEnabled ? Limit(EatMultiplier) : 1,
        ActionKind.Interact => InteractEnabled ? Limit(InteractMultiplier) : 1,
        ActionKind.Gather => GatherEnabled ? Limit(GatherMultiplier) : 1,
        ActionKind.Plant => PlantEnabled ? Limit(PlantMultiplier) : 1,
        _ => 1
    };

    internal static float Limit(double value) => double.IsNaN(value) || double.IsInfinity(value)
        ? 3 : (float)System.Math.Max(1, System.Math.Min(4, value));
}
