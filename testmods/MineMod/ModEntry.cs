using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace MineMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string MineMachineId = "dtmapi.mine";
        private const string MineItemId = "dtmapi_mine";
        private const string OilItemId = "dtmapi_oil";

        private IDtmHelper helper = null!;
        private MineModConfig config = new MineModConfig();
        private IMachineProductionApi? machineApi;
        private MachineRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<MineModConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            BindMachineApi("Entry");
            helper.Events.Save.SaveLoaded += (_, e) => BindMachineApi("SaveLoaded slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Monitor.Log(T("mod.loaded", "DTMAPI Mine loaded; official JSON supplies the mine item/equipment/recipe and the experimental Machine API owns runtime behavior."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Mine"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.machine", "Mine machine"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Registers the DTMAPI mine machine definition."), () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.cycle.name", "Cycle minutes"), () => T("config.cycle.tooltip", "Game minutes per production cycle once runtime hooks are implemented."), () => config.CycleMinutes, value => config.CycleMinutes = (int)Math.Round(value), 5, 720, 5);
            menu.AddNumberOption(helper.ModManifest, () => T("config.fuelCapacity.name", "Fuel capacity"), () => T("config.fuelCapacity.tooltip", "Large machine fuel tank capacity."), () => config.FuelCapacity, value => config.FuelCapacity = (int)Math.Round(value), 100, 50000, 100);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.electric.name", "Electric mode"), () => T("config.electric.tooltip", "Electric mode should consume 10 power per cycle and less fuel."), () => config.AllowElectricMode, value => config.AllowElectricMode = value, () => config.ElectricPowerCostPerCycle, value => config.ElectricPowerCostPerCycle = (int)Math.Round(value), 0, 100, 1);
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.outputs", "Output weights"));
            menu.AddNumberOption(helper.ModManifest, () => T("config.weight.coal", "Coal weight"), () => T("config.weight.tooltip", "Default probability weight."), () => config.CoalWeight, value => config.CoalWeight = value, 0, 100, 0.5);
            menu.AddNumberOption(helper.ModManifest, () => T("config.weight.copper", "Copper ore weight"), () => T("config.weight.tooltip", "Default probability weight."), () => config.CopperOreWeight, value => config.CopperOreWeight = value, 0, 100, 0.5);
            menu.AddNumberOption(helper.ModManifest, () => T("config.weight.iron", "Iron ore weight"), () => T("config.weight.tooltip", "Default probability weight."), () => config.IronOreWeight, value => config.IronOreWeight = value, 0, 100, 0.5);
            menu.AddNumberOption(helper.ModManifest, () => T("config.weight.oil", "Oil weight"), () => T("config.weight.tooltip", "Default probability weight."), () => config.OilWeight, value => config.OilWeight = value, 0, 100, 0.5);
        }

        private void BindMachineApi(string reason)
        {
            machineApi = helper.ModRegistry.GetApi<IMachineProductionApi>("DTMAPI.GameBridge.DolocTown");
            if (machineApi == null)
            {
                helper.Monitor.Log(T("mod.machineApiMissing", "Machine API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastRegisterResult = machineApi.RegisterMachine(helper.ModManifest, BuildMachineDefinition());
            helper.Monitor.Log("Mine machine API register success=" + lastRegisterResult.Success + " reason=" + reason + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private MachineDefinition BuildMachineDefinition()
        {
            return new MachineDefinition
            {
                MachineId = MineMachineId,
                DisplayName = T("machine.name", "Mine"),
                ItemId = MineItemId,
                EquipmentId = MineItemId,
                RecipeId = MineItemId,
                RecipeGroupId = "equipment_workbench",
                VisualScale = 2,
                AllowFuelMode = true,
                AllowElectricMode = config.AllowElectricMode,
                DefaultMode = config.AllowElectricMode ? "electric" : "fuel",
                NativeTechNodeId = "dtmapi_mine",
                NativeTechNodeTitle = T("tech.mine.title", "矿井"),
                NativeTechNodeDescription = T("tech.mine.description", "解锁矿井制作配方。矿井消耗燃料或电力，并按游戏时间产出矿物到自己的储物格。"),
                NativeTechNodeParentId = "alloy_material",
                NativeTechNodeAboveTitleContains = "指挥官",
                FuelCapacity = config.FuelCapacity,
                FuelOnlyFuelCostPerCycle = config.FuelOnlyFuelCostPerCycle,
                ElectricModeFuelCostPerCycle = config.ElectricModeFuelCostPerCycle,
                ElectricModePowerCostPerCycle = config.ElectricPowerCostPerCycle,
                CycleMinutes = config.CycleMinutes,
                IncludeRuntimeModMinerals = config.IncludeRuntimeModMinerals,
                OutputRules = BuildOutputRules(),
                ProbabilityOverrides = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    { "coal", config.CoalWeight },
                    { "copper_ore", config.CopperOreWeight },
                    { "iron_ore", config.IronOreWeight },
                    { OilItemId, config.OilWeight }
                },
                VerboseLogging = config.VerboseLogging
            };
        }

        private IReadOnlyList<MachineOutputRule> BuildOutputRules()
        {
            return new[]
            {
                new MachineOutputRule { ItemId = "coal", DisplayName = T("output.coal", "Coal"), Weight = config.CoalWeight, MinCount = 1, MaxCount = 2, Source = "vanilla" },
                new MachineOutputRule { ItemId = "copper_ore", DisplayName = T("output.copper", "Copper ore"), Weight = config.CopperOreWeight, MinCount = 1, MaxCount = 2, Source = "vanilla" },
                new MachineOutputRule { ItemId = "iron_ore", DisplayName = T("output.iron", "Iron ore"), Weight = config.IronOreWeight, MinCount = 1, MaxCount = 1, Source = "vanilla" },
                new MachineOutputRule { ItemId = OilItemId, DisplayName = T("output.oil", "Oil"), Weight = config.OilWeight, MinCount = 1, MaxCount = 1, Source = "DTMAPI.OilMod" }
            };
        }

        private string BuildStatusText()
        {
            if (machineApi == null)
                return T("config.status.missing", "Machine API is not available.");

            MachineProductionState state = machineApi.GetState(helper.ModManifest.UniqueID);
            string mode = string.IsNullOrWhiteSpace(state.LastMode) ? state.DefaultMode : state.LastMode;
            string output = string.IsNullOrWhiteSpace(state.LastOutputItemId) ? "none" : state.LastOutputItemId;
            return string.Format(
                T("config.status", "Status={0}, registered={1}, placed={2}, mode={3}, fuel={4}/{5}, cycle={6}m/{7}TU, electric={8}/cycle, group={9}, scale={10:0.##}, last={11}x{12}, target={13}, storage={14}/{15}."),
                state.Status,
                state.RegisteredMachineCount,
                state.PlacedMachineCount,
                string.IsNullOrWhiteSpace(mode) ? "unknown" : mode,
                state.RemainingFuel,
                state.FuelCapacity,
                state.CycleMinutes,
                state.CycleTUs,
                state.ElectricModePowerCostPerCycle,
                state.RecipeGroupId,
                state.VisualScale,
                output,
                state.LastOutputCount,
                string.IsNullOrWhiteSpace(state.LastOutputTarget) ? "none" : state.LastOutputTarget,
                state.LastStorageFilledSlots,
                state.LastStorageCapacity);
        }

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            BindMachineApi("config saved");
        }

        private void ResetConfig()
        {
            config = new MineModConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.CycleMinutes = Math.Max(5, Math.Min(720, config.CycleMinutes));
            config.FuelCapacity = Math.Max(100, Math.Min(50000, config.FuelCapacity));
            config.FuelOnlyFuelCostPerCycle = Math.Max(0, config.FuelOnlyFuelCostPerCycle);
            config.ElectricModeFuelCostPerCycle = Math.Max(0, config.ElectricModeFuelCostPerCycle);
            config.ElectricPowerCostPerCycle = Math.Max(0, config.ElectricPowerCostPerCycle);
            config.CoalWeight = Math.Max(0, config.CoalWeight);
            config.CopperOreWeight = Math.Max(0, config.CopperOreWeight);
            config.IronOreWeight = Math.Max(0, config.IronOreWeight);
            config.OilWeight = Math.Max(0, config.OilWeight);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class MineModConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public bool AllowElectricMode { get; set; } = true;
            [DataMember] public bool IncludeRuntimeModMinerals { get; set; } = true;
            [DataMember] public int CycleMinutes { get; set; } = 120;
            [DataMember] public int FuelCapacity { get; set; } = 7200;
            [DataMember] public int FuelOnlyFuelCostPerCycle { get; set; } = 120;
            [DataMember] public int ElectricModeFuelCostPerCycle { get; set; } = 20;
            [DataMember] public int ElectricPowerCostPerCycle { get; set; } = 10;
            [DataMember] public double CoalWeight { get; set; } = 18;
            [DataMember] public double CopperOreWeight { get; set; } = 10;
            [DataMember] public double IronOreWeight { get; set; } = 6;
            [DataMember] public double OilWeight { get; set; } = 2;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
