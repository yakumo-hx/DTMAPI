using System;
using System.Reflection;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingNativeEnergyGate
    {
        private Type? dolocApiType;
        private Func<object?>? getGlobalParameter;
        private Func<int, bool>? hasEnoughEnergy;
        private Type? globalParameterType;
        private Type? unavailableGlobalParameterType;
        private Func<object, int>? getFishingEnergyCost;
        private bool staticAccessorsUnavailable;
        private int accessorBuilds;
        private int accessorRebuilds;
        private int accessorBuildFailures;
        private int accessorInvocationFailures;

        internal int AccessorBuilds => accessorBuilds;
        internal int AccessorRebuilds => accessorRebuilds;
        internal int AccessorBuildFailures => accessorBuildFailures;
        internal int AccessorInvocationFailures => accessorInvocationFailures;

        internal bool TryCheck(Type? currentDolocApiType, out bool sufficient, out int fishingEnergyCost, out string failure)
        {
            sufficient = false;
            fishingEnergyCost = 0;
            failure = string.Empty;
            if (currentDolocApiType == null)
            {
                failure = "DolocAPI was unavailable for the native fishing energy gate.";
                return false;
            }
            if (!EnsureStaticAccessors(currentDolocApiType, out failure))
                return false;

            try
            {
                object? globalParameter = getGlobalParameter!();
                if (globalParameter == null)
                {
                    failure = "DolocAPI.GlobalParameter returned null.";
                    return false;
                }
                if (!EnsureGlobalParameterAccessor(globalParameter.GetType(), out failure))
                    return false;
                fishingEnergyCost = getFishingEnergyCost!(globalParameter);
                if (fishingEnergyCost <= 0)
                {
                    failure = "DolocAPI.GlobalParameter.FishingEnergyCost was not positive.";
                    return false;
                }
                sufficient = hasEnoughEnergy!(fishingEnergyCost);
                return true;
            }
            catch (Exception ex)
            {
                accessorInvocationFailures++;
                Exception actual = ex is TargetInvocationException invocation && invocation.InnerException != null
                    ? invocation.InnerException
                    : ex;
                failure = actual.GetType().Name + ": " + actual.Message;
                return false;
            }
        }

        private bool EnsureStaticAccessors(Type currentDolocApiType, out string failure)
        {
            failure = string.Empty;
            if (dolocApiType == currentDolocApiType && getGlobalParameter != null && hasEnoughEnergy != null)
                return true;
            if (staticAccessorsUnavailable && dolocApiType == currentDolocApiType)
            {
                failure = "DolocAPI.GlobalParameter or HasEnoughEnergy(int) was unavailable.";
                return false;
            }

            try
            {
                if (dolocApiType != null && dolocApiType != currentDolocApiType)
                    accessorRebuilds++;
                dolocApiType = currentDolocApiType;
                getGlobalParameter = FishingNativeAccessors.CreateStaticObjectGetter(
                    FishingNativeAccessors.FindMember(currentDolocApiType, "GlobalParameter", isStatic: true));
                MethodInfo? hasEnoughEnergyMethod = currentDolocApiType.GetMethod(
                    "HasEnoughEnergy",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(int) },
                    modifiers: null);
                hasEnoughEnergy = FishingNativeAccessors.CreateStaticBoolIntMethod(hasEnoughEnergyMethod);
                accessorBuilds++;
                staticAccessorsUnavailable = getGlobalParameter == null || hasEnoughEnergy == null;
                if (staticAccessorsUnavailable)
                {
                    accessorBuildFailures++;
                    failure = "DolocAPI.GlobalParameter or HasEnoughEnergy(int) was unavailable.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                accessorBuilds++;
                accessorBuildFailures++;
                staticAccessorsUnavailable = true;
                failure = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool EnsureGlobalParameterAccessor(Type currentType, out string failure)
        {
            failure = string.Empty;
            if (globalParameterType == currentType && getFishingEnergyCost != null)
                return true;
            if (unavailableGlobalParameterType == currentType)
            {
                failure = "DolocAPI.GlobalParameter.FishingEnergyCost was unavailable.";
                return false;
            }
            try
            {
                if (globalParameterType != null && globalParameterType != currentType)
                    accessorRebuilds++;
                globalParameterType = currentType;
                getFishingEnergyCost = FishingNativeAccessors.CreateIntGetter(
                    FishingNativeAccessors.FindMember(currentType, "FishingEnergyCost"));
                accessorBuilds++;
                if (getFishingEnergyCost == null)
                {
                    unavailableGlobalParameterType = currentType;
                    accessorBuildFailures++;
                    failure = "DolocAPI.GlobalParameter.FishingEnergyCost was unavailable.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                unavailableGlobalParameterType = currentType;
                accessorBuilds++;
                accessorBuildFailures++;
                failure = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }
    }
}
