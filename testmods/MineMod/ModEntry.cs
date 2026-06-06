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
        private const string OilItemId = "crude_oil";

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
            menu.AddChoiceOption(helper.ModManifest, () => T("config.mode.name", "Default mode"), () => T("config.mode.tooltip", "Electric mode consumes power and a small amount of fuel; fuel mode consumes fuel faster."), () => config.DefaultMode, value => config.DefaultMode = value, new[] { "electric", "fuel" });
            menu.AddNumberOption(helper.ModManifest, () => T("config.fuelCapacity.name", "Fuel capacity"), () => T("config.fuelCapacity.tooltip", "Internal DTMAPI fuel capacity for both mine modes."), () => config.FuelCapacity, value => config.FuelCapacity = (int)Math.Round(value), 7200, 99999, 100);
            menu.AddNumberOption(helper.ModManifest, () => T("config.fuelOnlyCost.name", "Fuel mode cost"), () => T("config.fuelOnlyCost.tooltip", "Fuel consumed per production cycle in pure fuel mode."), () => config.FuelOnlyFuelCostPerCycle, value => config.FuelOnlyFuelCostPerCycle = (int)Math.Round(value), 2, 1000, 1);
            menu.AddNumberOption(helper.ModManifest, () => T("config.electricFuelCost.name", "Electric mode fuel"), () => T("config.electricFuelCost.tooltip", "Fuel consumed per production cycle while also using power."), () => config.ElectricModeFuelCostPerCycle, value => config.ElectricModeFuelCostPerCycle = (int)Math.Round(value), 1, 1000, 1);
            menu.AddNumberOption(helper.ModManifest, () => T("config.electricPower.name", "Power per cycle"), () => T("config.electricPower.tooltip", "Electric power consumed per production cycle."), () => config.ElectricPowerCostPerCycle, value => config.ElectricPowerCostPerCycle = (int)Math.Round(value), 0, 100, 1);
            menu.AddBoolOption(helper.ModManifest, () => T("config.oilRecipe.name", "Use Oil recipe"), () => T("config.oilRecipe.tooltip", "When OilMod is loaded, replace the fallback coal recipe with the oil recipe. Restart or reload official mods before crafting if this changes."), () => config.UseOilRecipeReplacement, value => config.UseOilRecipeReplacement = value);
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
                AllowElectricMode = true,
                DefaultMode = config.DefaultMode,
                NativeTechNodeId = "dtmapi_mine",
                NativeTechNodeTitle = T("tech.mine.title", "矿井"),
                NativeTechNodeDescription = T("tech.mine.description", "解锁矿井制作配方。矿井可使用纯燃料模式，或使用耗电 10 且燃料消耗更低的耗电模式，并按游戏时间产出矿物到自己的储物格。"),
                NativeTechNodeParentId = "alloy_material",
                NativeTechNodeAboveTitleContains = "指挥官",
                FuelCapacity = config.FuelCapacity,
                FuelOnlyFuelCostPerCycle = config.FuelOnlyFuelCostPerCycle,
                ElectricModeFuelCostPerCycle = config.ElectricModeFuelCostPerCycle,
                ElectricModePowerCostPerCycle = config.ElectricPowerCostPerCycle,
                CycleMinutes = config.CycleMinutes,
                RecipeInputs = BuildRecipeInputs(),
                IncludeRuntimeModMinerals = config.IncludeRuntimeModMinerals,
                OutputRules = BuildOutputRules(),
                ProbabilityOverrides = BuildProbabilityOverrides(),
                VerboseLogging = config.VerboseLogging
            };
        }

        private IReadOnlyList<MachineOutputRule> BuildOutputRules()
        {
            var rules = new List<MachineOutputRule>
            {
                new MachineOutputRule { ItemId = "coal", DisplayName = T("output.coal", "Coal"), Weight = config.CoalWeight, MinCount = 1, MaxCount = 2, Source = "vanilla" },
                new MachineOutputRule { ItemId = "copper_ore", DisplayName = T("output.copper", "Copper ore"), Weight = config.CopperOreWeight, MinCount = 1, MaxCount = 2, Source = "vanilla" },
                new MachineOutputRule { ItemId = "iron_ore", DisplayName = T("output.iron", "Iron ore"), Weight = config.IronOreWeight, MinCount = 1, MaxCount = 1, Source = "vanilla" }
            };
            if (IsOilModAvailable())
                rules.Add(new MachineOutputRule { ItemId = OilItemId, DisplayName = T("output.oil", "Oil"), Weight = config.OilWeight, MinCount = 1, MaxCount = 1, Source = "DTMAPI.OilMod" });
            return rules;
        }

        private IReadOnlyDictionary<string, double> BuildProbabilityOverrides()
        {
            var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "coal", config.CoalWeight },
                { "copper_ore", config.CopperOreWeight },
                { "iron_ore", config.IronOreWeight }
            };
            if (IsOilModAvailable())
                weights[OilItemId] = config.OilWeight;
            return weights;
        }

        private IReadOnlyList<MachineRecipeInput> BuildRecipeInputs()
        {
            if (config.UseOilRecipeReplacement && IsOilModAvailable())
            {
                return new[]
                {
                    new MachineRecipeInput { ItemId = "metal_framework", Count = 10 },
                    new MachineRecipeInput { ItemId = "engine_core", Count = 5 },
                    new MachineRecipeInput { ItemId = "steel_ingot", Count = 20 },
                    new MachineRecipeInput { ItemId = OilItemId, Count = 10 }
                };
            }

            return new[]
            {
                new MachineRecipeInput { ItemId = "metal_framework", Count = 15 },
                new MachineRecipeInput { ItemId = "engine_core", Count = 10 },
                new MachineRecipeInput { ItemId = "steel_ingot", Count = 20 },
                new MachineRecipeInput { ItemId = "coal", Count = 100 }
            };
        }

        private bool IsOilModAvailable() => helper.ModRegistry.IsLoaded("DTMAPI.OilMod");

        private string BuildStatusText()
        {
            if (machineApi == null)
                return T("config.status.missing", "Machine API is not available.");

            MachineProductionState state = machineApi.GetState(helper.ModManifest.UniqueID);
            string mode = string.IsNullOrWhiteSpace(state.LastMode) ? state.DefaultMode : state.LastMode;
            string output = string.IsNullOrWhiteSpace(state.LastOutputItemId) ? "none" : state.LastOutputItemId;
            return string.Format(
                T("config.status", "Status={0}, registered={1}, placed={2}, mode={3}, cycle={4}m/{5}TU, electric={6}/cycle, recipe={7}, group={8}, scale={9:0.##}, last={10}x{11}, target={12}, storage={13}/{14}, fuel={15}/{16}, fuelModeCost={17}, electricFuelCost={18}."),
                state.Status,
                state.RegisteredMachineCount,
                state.PlacedMachineCount,
                string.IsNullOrWhiteSpace(mode) ? "unknown" : mode,
                state.CycleMinutes,
                state.CycleTUs,
                state.ElectricModePowerCostPerCycle,
                config.UseOilRecipeReplacement && IsOilModAvailable() ? "oil" : "coal",
                state.RecipeGroupId,
                state.VisualScale,
                output,
                state.LastOutputCount,
                string.IsNullOrWhiteSpace(state.LastOutputTarget) ? "none" : state.LastOutputTarget,
                state.LastStorageFilledSlots,
                state.LastStorageCapacity,
                state.RemainingFuel,
                state.FuelCapacity,
                state.FuelOnlyFuelCostPerCycle,
                state.ElectricModeFuelCostPerCycle);
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
            config.DefaultMode = string.Equals(config.DefaultMode, "fuel", StringComparison.OrdinalIgnoreCase) ? "fuel" : "electric";
            if (config.FuelCapacity < 7200)
                config.FuelCapacity = 7200;
            config.FuelCapacity = Math.Min(999999, config.FuelCapacity);
            if (config.FuelOnlyFuelCostPerCycle <= 1)
                config.FuelOnlyFuelCostPerCycle = 120;
            config.FuelOnlyFuelCostPerCycle = Math.Max(2, Math.Min(999999, config.FuelOnlyFuelCostPerCycle));
            if (config.ElectricModeFuelCostPerCycle <= 0 || config.ElectricModeFuelCostPerCycle >= config.FuelOnlyFuelCostPerCycle)
                config.ElectricModeFuelCostPerCycle = Math.Max(1, Math.Min(20, config.FuelOnlyFuelCostPerCycle - 1));
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
            [DataMember] public bool IncludeRuntimeModMinerals { get; set; } = true;
            [DataMember] public bool UseOilRecipeReplacement { get; set; } = true;
            [DataMember] public int CycleMinutes { get; set; } = 120;
            [DataMember] public string DefaultMode { get; set; } = "electric";
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
