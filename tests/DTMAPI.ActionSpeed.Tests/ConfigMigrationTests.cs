using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Yuuka.DTMAPI.ActionSpeed;

namespace DTMAPI.UnitTests;

internal static class ConfigMigrationTests
{
    internal static int RunAll()
    {
        const string legacy = "{\"Enabled\":false,\"Profile\":\"Fast\",\"MenuKey\":\"F9\",\"ToolSpeedEnabled\":false,\"ToolMultiplier\":2,\"BottleFillSpeedEnabled\":false,\"BottleFillMultiplier\":4,\"EatDrinkSpeedEnabled\":false,\"EatDrinkMultiplier\":1.5,\"MachineAddSpeedEnabled\":false,\"MachineAddMultiplier\":2.5,\"HarvestSpeedEnabled\":false,\"HarvestMultiplier\":4,\"PlantSpeedEnabled\":false,\"PlantMultiplier\":1.5,\"AutoFillBottle\":true,\"AutoFillStrong\":true,\"ContinuousDrinkWithRightClick\":false}";
        var value = ConfigMigration.Read(legacy, out bool migrated);
        Require(migrated && !value.ToolEnabled && !value.FillEnabled && !value.EatEnabled && !value.InteractEnabled && !value.GatherEnabled && !value.PlantEnabled, "legacy disabled category choices retained");
        Require(value.ToolMultiplier == 2 && value.FillMultiplier == 4 && value.EatMultiplier == 1.5 && value.InteractMultiplier == 2.5 && value.GatherMultiplier == 4 && value.PlantMultiplier == 1.5, "six legacy multipliers retained");
        Require(value.AutoFill && value.StrongAutoFill && !value.ContinuousDrink, "automation choices retained from a legacy config containing the retired menu key");
        Require(value.AutoFillInterval == 0.18 && value.StrongAutoFillInterval == 0.08, "Fast profile converted to resolved intervals");
        value.ToolEnabled = true;
        Require(value.Factor(ActionKind.Tool) == 2 && value.Factor(ActionKind.Fill) == 1, "retired master disable cannot override enabled tool or disabled fill choices");
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(ActionConfig)).WriteObject(stream, value);
        var again = ConfigMigration.Read(Encoding.UTF8.GetString(stream.ToArray()), out migrated);
        Require(!migrated && again.ToolEnabled && again.FillMultiplier == 4, "second startup keeps migration and does not request rewrite");
        var oldCurrent = ConfigMigration.Read("{\"SchemaVersion\":1,\"MenuKey\":\"F9\",\"Enabled\":false,\"ToolMultiplier\":2,\"ContinuousDrink\":false}", out migrated);
        Require(!migrated && oldCurrent.Factor(ActionKind.Tool) == 2 && !oldCurrent.ContinuousDrink, "current config with retired master/key fields preserves individual controls without startup rewrite");
        var fresh = ConfigMigration.Read("{}", out _);
        Require(fresh.ToolEnabled && fresh.FillEnabled && fresh.ContinuousDrink && fresh.ToolMultiplier == 3 && !fresh.AutoFill, "new defaults remain independent");
        var mixed = ConfigMigration.Read("{\"FillEnabled\":false,\"BottleFillSpeedEnabled\":true,\"FillMultiplier\":2,\"BottleFillMultiplier\":4}", out _);
        Require(!mixed.FillEnabled && mixed.FillMultiplier == 2, "explicit new names win over legacy aliases");
        foreach (string bad in new[] { "{\"SchemaVersion\":2}", "{\"SchemaVersion\":null}", "{\"ToolSpeedEnabled\":\"false\"}", "{", "null" })
        {
            bool failed = false;
            try { ConfigMigration.Read(bad, out _); } catch { failed = true; }
            Require(failed, "unknown or damaged configuration is refused: " + bad);
        }
        return 14;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
