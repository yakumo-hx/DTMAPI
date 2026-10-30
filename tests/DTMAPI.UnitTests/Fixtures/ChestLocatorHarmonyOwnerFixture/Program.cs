using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.ChestLocatorEnhancer;
using HarmonyLib;

namespace DTMAPI.ChestLocatorHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ProductOwner =
            ChestLocatorHookOwnership.HarmonyOwner;
        private const string OtherOwner = ProductOwner + ".Observer";
        private static readonly MethodInfo Target =
            typeof(Program).GetMethod(
                nameof(ExactTarget),
                BindingFlags.NonPublic |
                BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(Program).FullName,
                nameof(ExactTarget));
        private static readonly HarmonyMethod Postfix =
            new HarmonyMethod(
                typeof(Program).GetMethod(
                    nameof(ExactTargetPostfix),
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                ?? throw new MissingMethodException(
                    typeof(Program).FullName,
                    nameof(ExactTargetPostfix)));

        private static int Main()
        {
            try
            {
                ExactOwnerIsolationAndDuplicateRejection();
                DisableRestartAndResidualObservation();
                Console.WriteLine(
                    "ChestLocatorHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Cleanup(ProductOwner);
                Cleanup(OtherOwner);
                return 1;
            }
        }

        private static void ExactOwnerIsolationAndDuplicateRejection()
        {
            Cleanup(ProductOwner);
            Cleanup(OtherOwner);
            Install(ProductOwner);
            Assert(
                ExactTarget(1) == 11,
                "The V2 product Harmony Postfix did not execute.");
            Assert(
                ChestLocatorHookOwnership.DecideInstall(false, HasOwner(ProductOwner)) ==
                    ChestLocatorInstallDecision.RejectDuplicateProductOwner,
                "The physically present V2 owner must reject a duplicate installation.");
            Install(OtherOwner);
            Assert(
                ExactTarget(1) == 111,
                "The unrelated owner control did not execute alongside the product.");
            Cleanup(ProductOwner);
            Assert(
                !HasOwner(ProductOwner) && HasOwner(OtherOwner) && ExactTarget(1) == 101,
                "Exact-owner cleanup must preserve a different owner with the same prefix.");
            Cleanup(OtherOwner);
            Assert(
                !HasOwner(OtherOwner) && ExactTarget(1) == 1,
                "The fixture did not return to the original function.");
        }

        private static void DisableRestartAndResidualObservation()
        {
            Action install = () => Install(ProductOwner);
            Action unpatch = () => Cleanup(ProductOwner);
            ChestLocatorHookOwnership.ApplyEnabledState(
                enabled: true,
                install,
                unpatch);
            Assert(
                HasOwner(ProductOwner),
                "Configuration enable did not install the real product owner.");
            ChestLocatorHookOwnership.ApplyEnabledState(
                enabled: false,
                install,
                unpatch);
            Assert(
                !HasOwner(ProductOwner),
                "Configuration disable did not remove the real product owner.");
            ChestLocatorHookOwnership.ApplyEnabledState(
                enabled: true,
                install,
                unpatch);
            Assert(
                OwnerCount(ProductOwner) == 1,
                "Same-process restart did not restore exactly one product owner.");

            AssertThrows<InvalidOperationException>(
                () => ChestLocatorHookOwnership.ApplyEnabledState(
                    enabled: false,
                    install,
                    () => throw new InvalidOperationException(
                        "fixture exact-owner cleanup failure")),
                "A failed unpatch did not surface.");
            ChestLocatorObservedHookState residual =
                ChestLocatorHookOwnership
                    .FromExactOwnerObservation(
                        HasOwner(ProductOwner));
            Assert(
                residual.IsInstalled &&
                residual.InstalledPatchCount == 1,
                "Failed-unpatch residual did not retain real owner installed/count=1.");
            Cleanup(ProductOwner);
        }

        private static void Install(string owner)
        {
            Assert(
                !HasOwner(owner),
                "Fixture attempted to install duplicate owner " +
                owner +
                ".");
            new Harmony(owner).Patch(
                Target,
                postfix: owner == OtherOwner
                    ? new HarmonyMethod(typeof(Program), nameof(OtherTargetPostfix))
                    : Postfix);
            Assert(
                OwnerCount(owner) == 1,
                "Harmony did not publish exactly one owner " +
                owner +
                ".");
        }

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static bool HasOwner(string owner) =>
            OwnerCount(owner) > 0;

        private static int OwnerCount(string owner)
        {
            Patches? info = Harmony.GetPatchInfo(Target);
            return info == null
                ? 0
                : info.Owners.Count(
                    current =>
                        current.Equals(
                            owner,
                            StringComparison.Ordinal));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ExactTarget(int value) =>
            value;

        private static void ExactTargetPostfix(
            ref int __result) =>
            __result += 10;

        private static void OtherTargetPostfix(ref int __result) =>
            __result += 100;

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
