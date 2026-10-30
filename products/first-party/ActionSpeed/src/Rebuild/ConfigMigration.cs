using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.ActionSpeed;

internal static class ConfigMigration
{
    internal static ActionConfig Load(IDtmHelper helper)
    {
        string path = helper.Config.GetConfigPath(helper.ModManifest);
        if (!File.Exists(path)) return helper.ReadConfig<ActionConfig>();
        // Read without adopting defaults on malformed/unknown input. The platform writer is atomic.
        var config = Read(File.ReadAllText(path, new UTF8Encoding(false, true)), out bool migrated);
        if (migrated)
        {
            helper.WriteConfig(config);
            helper.Monitor.Log("ActionSpeed configuration migrated: individual switches, multipliers and resolved auto-fill intervals preserved; settings use DTMAPI Mod Config Menu.");
        }
        return config;
    }

    internal static ActionConfig Read(string json, out bool migrated)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json.TrimStart('\uFEFF'));
        T Deserialize<T>()
        {
            using var stream = new MemoryStream(bytes);
            return (T)new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).ReadObject(stream)!;
        }
        var fields = Deserialize<Dictionary<string, object>>() ?? throw new InvalidDataException("ActionSpeed config must be an object.");
        migrated = !fields.ContainsKey(nameof(ActionConfig.SchemaVersion));
        if (!migrated && (!decimal.TryParse(Convert.ToString(fields[nameof(ActionConfig.SchemaVersion)], System.Globalization.CultureInfo.InvariantCulture), out decimal schema) || schema != 1))
            throw new InvalidDataException("Unsupported ActionSpeed config schema; original file preserved.");
        var value = Deserialize<ActionConfig>();
        if (migrated)
        {
            void Map(string oldName, string newName)
            {
                if (!fields.ContainsKey(newName) && fields.TryGetValue(oldName, out object? old))
                {
                    var property = typeof(ActionConfig).GetProperty(newName)!;
                    if (old == null || (property.PropertyType == typeof(bool) && !(old is bool)))
                        throw new InvalidDataException("Invalid ActionSpeed config field: " + oldName);
                    property.SetValue(value, Convert.ChangeType(old, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                }
            }
            Map("ToolSpeedEnabled", "ToolEnabled");
            Map("BottleFillSpeedEnabled", "FillEnabled"); Map("BottleFillMultiplier", "FillMultiplier");
            Map("EatDrinkSpeedEnabled", "EatEnabled"); Map("EatDrinkMultiplier", "EatMultiplier");
            Map("MachineAddSpeedEnabled", "InteractEnabled"); Map("MachineAddMultiplier", "InteractMultiplier");
            Map("HarvestSpeedEnabled", "GatherEnabled"); Map("HarvestMultiplier", "GatherMultiplier");
            Map("PlantSpeedEnabled", "PlantEnabled");
            Map("AutoFillBottle", "AutoFill"); Map("AutoFillStrong", "StrongAutoFill");
            Map("ContinuousDrinkWithRightClick", "ContinuousDrink");
            // The old Profile selected cooldowns, not separate execution owners.
            string profile = fields.TryGetValue("Profile", out object? p) ? Convert.ToString(p) ?? "Safe" : "Safe";
            if (!fields.ContainsKey("AutoFillInterval"))
            {
                Map("AutoActionCooldownSeconds", "AutoFillInterval");
                if (profile.Equals("Fast", StringComparison.OrdinalIgnoreCase)) value.AutoFillInterval = 0.18;
                if (profile.Equals("Debug", StringComparison.OrdinalIgnoreCase)) value.AutoFillInterval = 0.1;
            }
            if (!fields.ContainsKey("StrongAutoFillInterval"))
                value.StrongAutoFillInterval = Math.Min(value.AutoFillInterval, profile.Equals("Fast", StringComparison.OrdinalIgnoreCase) || profile.Equals("Debug", StringComparison.OrdinalIgnoreCase) ? 0.08 : 0.12);
        }
        value.AutoFillInterval = Interval(value.AutoFillInterval, 0.25);
        value.StrongAutoFillInterval = Interval(value.StrongAutoFillInterval, 0.12);
        return value;
    }

    private static double Interval(double value, double fallback) => double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(0.05, Math.Min(5, value));
}
