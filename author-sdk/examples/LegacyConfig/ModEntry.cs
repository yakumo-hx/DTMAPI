using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using DTMAPI.Abstractions;

namespace Samples.LegacyConfig
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            Settings settings = LegacySettings.Load(helper.Config, helper.ModManifest);
            helper.Monitor.Log("Config example: enabled=" + settings.Enabled + "; tool=" + settings.ToolEnabled + "; fill=" + settings.FillEnabled);
        }
    }

    public sealed class Settings
    {
        public int SchemaVersion { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public bool ToolEnabled { get; set; } = true;
        public bool FillEnabled { get; set; } = true;
        public double ToolMultiplier { get; set; } = 3;
        public double FillMultiplier { get; set; } = 3;
    }

    internal static class LegacySettings
    {
        internal static Settings Load(IConfigHelper config, IManifest owner)
        {
            string json;
            try { json = File.ReadAllText(config.GetConfigPath(owner), new UTF8Encoding(false, true)); }
            catch (FileNotFoundException) { return config.ReadConfig<Settings>(owner); }
            catch (DirectoryNotFoundException) { return config.ReadConfig<Settings>(owner); }
            Settings value = Read(json, out bool migrated);
            // Only a successful atomic write commits the migration. Failure propagates;
            // the Runtime preserves the old bytes and the next startup can retry.
            if (migrated) config.WriteConfig(owner, value);
            return value;
        }

        private static Settings Read(string json, out bool migrated)
        {
            Dictionary<string, object> fields;
            using (var input = new MemoryStream(Encoding.UTF8.GetBytes(json.TrimStart('\uFEFF'))))
                fields = (Dictionary<string, object>)new DataContractJsonSerializer(typeof(Dictionary<string, object>),
                    new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).ReadObject(input)!;
            if (fields == null) throw new InvalidDataException("Config must be an object.");
            bool legacy = !fields.ContainsKey("SchemaVersion");
            migrated = legacy;
            if (!migrated && (!decimal.TryParse(Convert.ToString(fields["SchemaVersion"], CultureInfo.InvariantCulture), out decimal schema) || schema != 1))
                throw new InvalidDataException("Unsupported config schema; original file preserved.");
            foreach (string field in new[] { "Enabled", "ToolEnabled", "ToolSpeedEnabled", "FillEnabled", "BottleFillSpeedEnabled", "ToolMultiplier", "FillMultiplier", "BottleFillMultiplier" })
                if (fields.TryGetValue(field, out object? present) && present == null) throw new InvalidDataException("Invalid null: " + field);

            object? Value(string current, string old) => fields.TryGetValue(current, out object? value) ? value :
                legacy && fields.TryGetValue(old, out value) ? value : null;
            bool Flag(string current, string old) => Value(current, old) is object value
                ? value is bool flag ? flag : throw new InvalidDataException("Invalid boolean: " + current)
                : true;
            double Factor(string current, string old)
            {
                object? value = Value(current, old);
                if (value == null) return 3;
                if (value is string || value is bool) throw new InvalidDataException("Invalid multiplier: " + current);
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsNaN(number) || double.IsInfinity(number) ? 3 : Math.Max(1, Math.Min(4, number));
            }
            return new Settings
            {
                Enabled = Flag("Enabled", "Enabled"),
                ToolEnabled = Flag("ToolEnabled", "ToolSpeedEnabled"),
                FillEnabled = Flag("FillEnabled", "BottleFillSpeedEnabled"),
                ToolMultiplier = Factor("ToolMultiplier", "ToolMultiplier"),
                FillMultiplier = Factor("FillMultiplier", "BottleFillMultiplier")
            };
        }
    }
}
