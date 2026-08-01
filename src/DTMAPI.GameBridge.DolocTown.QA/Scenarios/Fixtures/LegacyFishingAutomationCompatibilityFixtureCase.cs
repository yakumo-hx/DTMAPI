#pragma warning disable CS0618 // This case exclusively verifies the frozen compatibility island.
using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private LegacyFishingAutomationCompatibilitySmokeCase? legacyFishingAutomationCompatibilitySmokeCase;

        private FixtureAttemptResult TryExerciseLegacyFishingAutomationCompatibilityForFixture()
        {
            legacyFishingAutomationCompatibilitySmokeCase ??= new LegacyFishingAutomationCompatibilitySmokeCase(this);
            return legacyFishingAutomationCompatibilitySmokeCase.Exercise().Attempt;
        }

        private sealed class LegacyFishingAutomationCompatibilitySmokeCase
        {
            private const string OwnerId = "DTMAPI.Smoke.LegacyFishingCompatibility";
            private readonly QaScenarioController bridge;

            internal LegacyFishingAutomationCompatibilitySmokeCase(QaScenarioController bridge)
            {
                this.bridge = bridge;
            }

            internal FishingSmokeCaseResult Exercise()
            {
                IManifest owner = new ManifestModel
                {
                    Name = "Legacy Fishing Compatibility Smoke",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = OwnerId,
                    Type = "CodeMod"
                };
                IFishingAutomationApi? api = null;
                bool cleanupComplete = false;
                try
                {
                    FishingAutomationCompatibilityFeature feature = bridge.fishingCompatibilityFeature
                        ?? throw new InvalidOperationException("FishingAutomationCompatibilityFeature was unavailable for compatibility smoke.");
                    if (feature.CountOwnerResources(OwnerId) != 0)
                        throw new InvalidOperationException("Legacy compatibility smoke synthetic owner was not clean at entry.");

                    api = bridge.runtime.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")
                        ?? throw new InvalidOperationException("Frozen IFishingAutomationApi was not registered.");
                    api.Configure(owner, new FishingAutomationOptions());
                    if (feature.Service != null)
                        throw new InvalidOperationException("Compatibility Configure activated LegacyFishingAutomationService before SetEnabled(true).");

                    api.SetEnabled(owner, true, "compatibility-smoke-enable");
                    if (feature.Service is not global::DTMAPI.GameBridge.DolocTown.LegacyFishingAutomationService || feature.CallbackRuntime == null)
                        throw new InvalidOperationException("Deprecated compatibility SetEnabled(true) did not activate the isolated legacy runtime.");

                    api.SetEnabled(owner, false, "compatibility-smoke-disable");
                    if (feature.Service != null || feature.CallbackRuntime != null)
                        throw new InvalidOperationException("Compatibility SetEnabled(false) retained its legacy executor or callback runtime.");
                    CompatibilityHostBroker broker = CompatibilityHostBroker.For(bridge.runtime);
                    if (!broker.TryGetService("FishingAutomation", out object? residentBackend) || residentBackend == null)
                        throw new InvalidOperationException("Compatibility Host did not retain the already loaded Fishing backend for resident-state cleanup inspection.");
                    int residentOwnerOptions = CompatibilityHostBroker.ReadProperty(residentBackend, "ConfiguredOwnerCount", -1);
                    int residentOwnerStates = CompatibilityHostBroker.ReadProperty(residentBackend, "OwnerStateCount", -1);
                    if (residentOwnerOptions != 0 || residentOwnerStates != 0)
                        throw new InvalidOperationException("Compatibility Host retained Fishing backend owner dictionaries after deactivation: ownerOptions=" + residentOwnerOptions + ", ownerStates=" + residentOwnerStates + ".");
                    feature.RemoveOwner(OwnerId, "compatibility-smoke-cleanup");
                    if (feature.CountOwnerResources(OwnerId) != 0)
                        throw new InvalidOperationException("Legacy fishing compatibility cleanup retained resources for the synthetic owner.");
                    cleanupComplete = true;
                    string summary = "configure=policy-only, enable=compatibility-runtime, disable=detached, residentOwnerOptions=0, residentOwnerStates=0, syntheticOwnerResources=0, productNativeRoots=not-applicable";
                    bridge.runtime.RuntimeMonitor.Log("Smoke exercise LegacyFishingAutomationCompatibility OK " + summary);
                    bridge.runtime.SetHookStatus("Smoke.LegacyFishingAutomationCompatibility", "verified", "obsolete IFishingAutomationApi.SetEnabled", summary);
                    return new FishingSmokeCaseResult("legacy-compatibility", FixtureAttemptResult.Succeeded);
                }
                catch (Exception ex)
                {
                    bridge.runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Legacy fishing compatibility smoke failed.", ex.ToString());
                    bridge.runtime.SetHookStatus("Smoke.LegacyFishingAutomationCompatibility", "failed", "obsolete IFishingAutomationApi.SetEnabled", ex.GetType().Name + ": " + ex.Message);
                    return new FishingSmokeCaseResult("legacy-compatibility", FixtureAttemptResult.Failed);
                }
                finally
                {
                    if (!cleanupComplete)
                    {
                        try
                        {
                            api?.SetEnabled(owner, false, "compatibility-smoke-finally");
                            bridge.fishingCompatibilityFeature?.RemoveOwner(OwnerId, "compatibility-smoke-finally");
                        }
                        catch (Exception cleanupError)
                        {
                            bridge.runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Legacy fishing compatibility smoke cleanup failed.", cleanupError.ToString());
                        }
                    }
                }
            }
        }
    }
}
