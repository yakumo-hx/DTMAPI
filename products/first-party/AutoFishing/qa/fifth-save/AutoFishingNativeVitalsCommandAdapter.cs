using System;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AutoFishingNativeVitalsReceipt
    {
        internal const string OfficialCommandSource =
            "DolocAPI.GetCommandFunction(compose_energy,compose_spirit,get_energy_percent,get_spirit_percent)";
        internal const string OfficialComposeEnergyDelegateIdentity = "DolocAPI.Command_ComposeEnergy";
        internal const string OfficialComposeSpiritDelegateIdentity = "DolocAPI.Command_ComposeSpirit";
        internal const string OfficialGetEnergyPercentDelegateIdentity = "DolocTown.FunctionDefines.GetCurrentEnergyPercent";
        internal const string OfficialGetSpiritPercentDelegateIdentity = "DolocTown.FunctionDefines.GetCurrentSpiritPercent";
        internal const string InitialSaveWorkloadPhase = "initial-save";
        internal const string PostReloadWorkloadPhase = "post-reload";
        internal const string OfficialL4RecoveryCheckpointContext = "Batch5 AutoFishing L4 product-disabled native recovery";

        internal string Source { get; set; } = OfficialCommandSource;
        internal string Context { get; set; } = string.Empty;
        internal bool WorkloadStart { get; set; }
        internal bool MaintenanceRefill { get; set; }
        internal bool L4RecoveryCheckpoint { get; set; }
        internal bool FinalObservation { get; set; }
        internal int SaveLoadOrdinal { get; set; }
        internal string WorkloadPhase { get; set; } = string.Empty;
        internal string ComposeEnergyDelegateIdentity { get; set; } = string.Empty;
        internal string ComposeSpiritDelegateIdentity { get; set; } = string.Empty;
        internal string GetEnergyPercentDelegateIdentity { get; set; } = string.Empty;
        internal string GetSpiritPercentDelegateIdentity { get; set; } = string.Empty;
        internal bool EnergyCommandInvoked { get; set; }
        internal bool SpiritCommandInvoked { get; set; }
        internal bool NativeEnergyInsufficientObserved { get; set; }
        internal bool NativeEnergyReserveLowObserved { get; set; }
        internal bool NativeSpiritLowObserved { get; set; }
        internal bool NativeEnergySufficientAfter { get; set; }
        internal bool NativeEnergyReserveSufficientAfter { get; set; }
        internal int FishingEnergyCost { get; set; }
        internal float EnergyPercentBefore { get; set; }
        internal float EnergyPercentAfter { get; set; }
        internal float SpiritPercentBefore { get; set; }
        internal float SpiritPercentAfter { get; set; }
    }

    internal sealed class AutoFishingNativeVitalsCommandAdapter
    {
        private const int ComposeAmount = 100000;
        private const float FullPercentMinimum = 0.999f;
        private const float SpiritLowWatermark = 0.25f;

        private readonly Action<int> composeEnergy;
        private readonly Action<int> composeSpirit;
        private readonly Func<float> getEnergyPercent;
        private readonly Func<float> getSpiritPercent;
        private readonly Func<int, bool> hasEnoughEnergy;
        private readonly int fishingEnergyCost;
        private readonly int fishingEnergyReserve;
        private readonly string composeEnergyDelegateIdentity;
        private readonly string composeSpiritDelegateIdentity;
        private readonly string getEnergyPercentDelegateIdentity;
        private readonly string getSpiritPercentDelegateIdentity;

        private AutoFishingNativeVitalsCommandAdapter(
            Action<int> composeEnergy,
            Action<int> composeSpirit,
            Func<float> getEnergyPercent,
            Func<float> getSpiritPercent,
            Func<int, bool> hasEnoughEnergy,
            int fishingEnergyCost,
            string composeEnergyDelegateIdentity,
            string composeSpiritDelegateIdentity,
            string getEnergyPercentDelegateIdentity,
            string getSpiritPercentDelegateIdentity)
        {
            this.composeEnergy = composeEnergy;
            this.composeSpirit = composeSpirit;
            this.getEnergyPercent = getEnergyPercent;
            this.getSpiritPercent = getSpiritPercent;
            this.hasEnoughEnergy = hasEnoughEnergy;
            this.fishingEnergyCost = fishingEnergyCost;
            this.composeEnergyDelegateIdentity = composeEnergyDelegateIdentity;
            this.composeSpiritDelegateIdentity = composeSpiritDelegateIdentity;
            this.getEnergyPercentDelegateIdentity = getEnergyPercentDelegateIdentity;
            this.getSpiritPercentDelegateIdentity = getSpiritPercentDelegateIdentity;
            fishingEnergyReserve = fishingEnergyCost <= int.MaxValue / 2
                ? fishingEnergyCost * 2
                : fishingEnergyCost;
        }

        internal int FishingEnergyCost => fishingEnergyCost;

        internal static AutoFishingNativeVitalsCommandAdapter Create(Type dolocApi) => CreateCore(dolocApi, requireOfficialDelegateIdentities: true);

        internal static AutoFishingNativeVitalsCommandAdapter CreateForTests(Type dolocApi) => CreateCore(dolocApi, requireOfficialDelegateIdentities: false);

        private static AutoFishingNativeVitalsCommandAdapter CreateCore(Type dolocApi, bool requireOfficialDelegateIdentities)
        {
            if (dolocApi == null)
                throw new ArgumentNullException(nameof(dolocApi));

            MethodInfo getCommandFunction = dolocApi.GetMethod(
                "GetCommandFunction",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
                ?? throw new MissingMethodException(dolocApi.FullName, "GetCommandFunction(string)");
            if (!typeof(Delegate).IsAssignableFrom(getCommandFunction.ReturnType))
                throw new InvalidOperationException("DolocAPI.GetCommandFunction(string) did not return System.Delegate.");

            Action<int> energyCommand = ResolveCommand<Action<int>>(getCommandFunction, "compose_energy");
            Action<int> spiritCommand = ResolveCommand<Action<int>>(getCommandFunction, "compose_spirit");
            Func<float> energyPercent = ResolveCommand<Func<float>>(getCommandFunction, "get_energy_percent");
            Func<float> spiritPercent = ResolveCommand<Func<float>>(getCommandFunction, "get_spirit_percent");
            string energyCommandIdentity = GetDelegateIdentity(energyCommand);
            string spiritCommandIdentity = GetDelegateIdentity(spiritCommand);
            string energyPercentIdentity = GetDelegateIdentity(energyPercent);
            string spiritPercentIdentity = GetDelegateIdentity(spiritPercent);
            if (requireOfficialDelegateIdentities)
            {
                RequireOfficialDelegateIdentity("compose_energy", energyCommandIdentity, AutoFishingNativeVitalsReceipt.OfficialComposeEnergyDelegateIdentity);
                RequireOfficialDelegateIdentity("compose_spirit", spiritCommandIdentity, AutoFishingNativeVitalsReceipt.OfficialComposeSpiritDelegateIdentity);
                RequireOfficialDelegateIdentity("get_energy_percent", energyPercentIdentity, AutoFishingNativeVitalsReceipt.OfficialGetEnergyPercentDelegateIdentity);
                RequireOfficialDelegateIdentity("get_spirit_percent", spiritPercentIdentity, AutoFishingNativeVitalsReceipt.OfficialGetSpiritPercentDelegateIdentity);
            }

            MethodInfo hasEnoughEnergyMethod = dolocApi.GetMethod(
                "HasEnoughEnergy",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null)
                ?? throw new MissingMethodException(dolocApi.FullName, "HasEnoughEnergy(int)");
            if (hasEnoughEnergyMethod.ReturnType != typeof(bool))
                throw new InvalidOperationException("DolocAPI.HasEnoughEnergy(int) did not return System.Boolean.");
            var enoughEnergy = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), hasEnoughEnergyMethod);

            PropertyInfo globalParameterProperty = dolocApi.GetProperty("GlobalParameter", BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMemberException(dolocApi.FullName, "GlobalParameter");
            object globalParameter = globalParameterProperty.GetValue(null, null)
                ?? throw new InvalidOperationException("DolocAPI.GlobalParameter returned null.");
            PropertyInfo fishingEnergyCostProperty = globalParameter.GetType().GetProperty("FishingEnergyCost", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMemberException(globalParameter.GetType().FullName, "FishingEnergyCost");
            object rawFishingEnergyCost = fishingEnergyCostProperty.GetValue(globalParameter, null)
                ?? throw new InvalidOperationException("DolocAPI.GlobalParameter.FishingEnergyCost returned null.");
            if (!(rawFishingEnergyCost is int cost) || cost <= 0)
                throw new InvalidOperationException("DolocAPI.GlobalParameter.FishingEnergyCost must be a positive System.Int32.");

            return new AutoFishingNativeVitalsCommandAdapter(
                energyCommand,
                spiritCommand,
                energyPercent,
                spiritPercent,
                enoughEnergy,
                cost,
                energyCommandIdentity,
                spiritCommandIdentity,
                energyPercentIdentity,
                spiritPercentIdentity);
        }

        internal AutoFishingNativeVitalsReceipt PrepareWorkload(string context, int saveLoadOrdinal, string workloadPhase)
        {
            if (saveLoadOrdinal <= 0)
                throw new ArgumentOutOfRangeException(nameof(saveLoadOrdinal), "AutoFishing workload preparation requires a positive save-load ordinal.");
            if (!string.Equals(workloadPhase, AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase, StringComparison.Ordinal) &&
                !string.Equals(workloadPhase, AutoFishingNativeVitalsReceipt.PostReloadWorkloadPhase, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("AutoFishing workload preparation requires the exact initial-save or post-reload phase identity.");
            }

            AutoFishingNativeVitalsReceipt receipt = PrepareFullRefill(context);
            receipt.WorkloadStart = true;
            receipt.SaveLoadOrdinal = saveLoadOrdinal;
            receipt.WorkloadPhase = workloadPhase;
            return receipt;
        }

        private AutoFishingNativeVitalsReceipt PrepareFullRefill(string context)
        {
            float energyBefore = ReadPercent(getEnergyPercent, "get_energy_percent");
            float spiritBefore = ReadPercent(getSpiritPercent, "get_spirit_percent");

            composeEnergy(ComposeAmount);
            composeSpirit(ComposeAmount);

            float energyAfter = ReadPercent(getEnergyPercent, "get_energy_percent");
            float spiritAfter = ReadPercent(getSpiritPercent, "get_spirit_percent");
            bool sufficientAfter = hasEnoughEnergy(fishingEnergyCost);
            bool reserveAfter = hasEnoughEnergy(fishingEnergyReserve);
            if (!IsFull(energyAfter) || !IsFull(spiritAfter) || !sufficientAfter || !reserveAfter)
            {
                throw new InvalidOperationException(
                    "Official AutoFishing workload-start vitals commands did not produce a full verified native state. " +
                    "energyPercent=" + energyAfter + "; spiritPercent=" + spiritAfter +
                    "; enoughForCast=" + sufficientAfter + "; enoughForReserve=" + reserveAfter + ".");
            }

            return new AutoFishingNativeVitalsReceipt
            {
                Context = context ?? string.Empty,
                EnergyCommandInvoked = true,
                SpiritCommandInvoked = true,
                NativeEnergySufficientAfter = sufficientAfter,
                NativeEnergyReserveSufficientAfter = reserveAfter,
                FishingEnergyCost = fishingEnergyCost,
                EnergyPercentBefore = energyBefore,
                EnergyPercentAfter = energyAfter,
                SpiritPercentBefore = spiritBefore,
                SpiritPercentAfter = spiritAfter,
                ComposeEnergyDelegateIdentity = composeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = composeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = getEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = getSpiritPercentDelegateIdentity
            };
        }

        internal AutoFishingNativeVitalsReceipt PrepareL4RecoveryCheckpoint(string context)
        {
            AutoFishingNativeVitalsReceipt receipt = PrepareFullRefill(context);
            receipt.L4RecoveryCheckpoint = true;
            return receipt;
        }

        internal AutoFishingNativeVitalsReceipt ObserveMeasurementEnd(string context)
        {
            float energyPercent = ReadPercent(getEnergyPercent, "get_energy_percent");
            float spiritPercent = ReadPercent(getSpiritPercent, "get_spirit_percent");
            bool sufficient = hasEnoughEnergy(fishingEnergyCost);
            bool reserveSufficient = hasEnoughEnergy(fishingEnergyReserve);
            return new AutoFishingNativeVitalsReceipt
            {
                Context = context ?? string.Empty,
                FinalObservation = true,
                NativeEnergyInsufficientObserved = !sufficient,
                NativeEnergyReserveLowObserved = !reserveSufficient,
                NativeEnergySufficientAfter = sufficient,
                NativeEnergyReserveSufficientAfter = reserveSufficient,
                FishingEnergyCost = fishingEnergyCost,
                EnergyPercentBefore = energyPercent,
                EnergyPercentAfter = energyPercent,
                SpiritPercentBefore = spiritPercent,
                SpiritPercentAfter = spiritPercent,
                ComposeEnergyDelegateIdentity = composeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = composeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = getEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = getSpiritPercentDelegateIdentity
            };
        }

        internal bool TryMaintainWorkload(string context, out AutoFishingNativeVitalsReceipt? receipt)
        {
            float energyBefore = ReadPercent(getEnergyPercent, "get_energy_percent");
            float spiritBefore = ReadPercent(getSpiritPercent, "get_spirit_percent");
            bool energySufficient = hasEnoughEnergy(fishingEnergyCost);
            bool energyReserveSufficient = hasEnoughEnergy(fishingEnergyReserve);
            bool refillEnergy = !energyReserveSufficient;
            bool refillSpirit = spiritBefore <= SpiritLowWatermark;
            if (!refillEnergy && !refillSpirit)
            {
                receipt = null;
                return false;
            }

            if (refillEnergy)
                composeEnergy(ComposeAmount);
            if (refillSpirit)
                composeSpirit(ComposeAmount);

            float energyAfter = ReadPercent(getEnergyPercent, "get_energy_percent");
            float spiritAfter = ReadPercent(getSpiritPercent, "get_spirit_percent");
            bool sufficientAfter = hasEnoughEnergy(fishingEnergyCost);
            bool reserveAfter = hasEnoughEnergy(fishingEnergyReserve);
            if ((refillEnergy && (!IsFull(energyAfter) || !sufficientAfter || !reserveAfter)) ||
                (refillSpirit && !IsFull(spiritAfter)))
            {
                throw new InvalidOperationException(
                    "Official AutoFishing maintenance vitals command did not pass native readback. " +
                    "energyCommand=" + refillEnergy + "; spiritCommand=" + refillSpirit +
                    "; energyPercent=" + energyAfter + "; spiritPercent=" + spiritAfter +
                    "; enoughForCast=" + sufficientAfter + "; enoughForReserve=" + reserveAfter + ".");
            }

            receipt = new AutoFishingNativeVitalsReceipt
            {
                Context = context ?? string.Empty,
                MaintenanceRefill = true,
                EnergyCommandInvoked = refillEnergy,
                SpiritCommandInvoked = refillSpirit,
                NativeEnergyInsufficientObserved = !energySufficient,
                NativeEnergyReserveLowObserved = !energyReserveSufficient,
                NativeSpiritLowObserved = refillSpirit,
                NativeEnergySufficientAfter = sufficientAfter,
                NativeEnergyReserveSufficientAfter = reserveAfter,
                FishingEnergyCost = fishingEnergyCost,
                EnergyPercentBefore = energyBefore,
                EnergyPercentAfter = energyAfter,
                SpiritPercentBefore = spiritBefore,
                SpiritPercentAfter = spiritAfter,
                ComposeEnergyDelegateIdentity = composeEnergyDelegateIdentity,
                ComposeSpiritDelegateIdentity = composeSpiritDelegateIdentity,
                GetEnergyPercentDelegateIdentity = getEnergyPercentDelegateIdentity,
                GetSpiritPercentDelegateIdentity = getSpiritPercentDelegateIdentity
            };
            return true;
        }

        internal static bool IsValidPercent(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1.001f;

        internal static bool IsFull(float value) => IsValidPercent(value) && value >= FullPercentMinimum;

        private static string GetDelegateIdentity(Delegate command)
        {
            Type? declaringType = command.Method.DeclaringType;
            string declaringTypeName = declaringType?.FullName ?? string.Empty;
            return declaringTypeName.Length == 0 ? command.Method.Name : declaringTypeName + "." + command.Method.Name;
        }

        private static void RequireOfficialDelegateIdentity(string commandName, string actual, string expected)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "DolocAPI command '" + commandName + "' resolved unexpected delegate identity '" + actual +
                    "'; expected current official identity '" + expected + "'.");
            }
        }

        private static TDelegate ResolveCommand<TDelegate>(MethodInfo getCommandFunction, string commandName)
            where TDelegate : class
        {
            Delegate command;
            try
            {
                command = getCommandFunction.Invoke(null, new object[] { commandName }) as Delegate
                    ?? throw new MissingMemberException("DolocAPI command registry did not expose '" + commandName + "'.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new InvalidOperationException("DolocAPI.GetCommandFunction failed for '" + commandName + "'.", ex.InnerException);
            }

            TDelegate typed = command as TDelegate
                ?? throw new InvalidOperationException(
                    "DolocAPI command '" + commandName + "' returned delegate type '" + command.GetType().FullName +
                    "' instead of '" + typeof(TDelegate).FullName + "'.");
            return typed;
        }

        private static float ReadPercent(Func<float> reader, string commandName)
        {
            float value = reader();
            if (!IsValidPercent(value))
                throw new InvalidOperationException("DolocAPI command '" + commandName + "' returned invalid percent " + value + ".");
            return value;
        }
    }
}
