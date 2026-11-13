using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private static readonly TimeSpan FeatureStatusPublishHeartbeat = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan FeatureFailureSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly string[] UiContextBlockingStateTypeNames =
        {
            "DolocTown.ModUiState, Assembly-CSharp",
            "DolocTown.SettingPanelUiState, Assembly-CSharp",
            "DolocTown.GameDataUiState, Assembly-CSharp",
            "DolocTown.ModChangeListUiState, Assembly-CSharp",
            "DolocTown.MainMenuUiState, Assembly-CSharp",
            "DolocTown.SystemMenuUiState, Assembly-CSharp",
            "DolocTown.SmallTextMenuUiState, Assembly-CSharp",
            "DolocTown.ConfirmUiState, Assembly-CSharp",
            "DolocTown.InputNameUiState, Assembly-CSharp",
            "DolocTown.HomePageUiState, Assembly-CSharp"
        };
        private const int FeatureFailureShortLogLimit = 3;
        private const int FeatureFailureRecoverySuccessThreshold = 3;
        private readonly DtmApiRuntime runtime;
        private DateTimeOffset initializedAt;
        private HarmonyPatcher? patcher;
        private AudioStartupPolicies? audioStartupPolicies;
        private Type? uiContextDolocApiType;
        private Func<bool>? uiContextIsNormalStateGetter;
        private Func<object?>? uiContextUserInputGetter;
        private Func<object, object?>? uiContextCurrentStateGetter;
        private Type? uiContextUserInputType;
        private Type[] uiContextBlockingStateTypes = Array.Empty<Type>();
        private readonly HookInstallScheduler hookInstallScheduler = new HookInstallScheduler();
        private bool shutdownCleanupRan;
        private bool saveLoadedPatched;
        private bool saveLoadedEventSubscribed;
        private bool loadRequestedPatched;
        private bool loadReturnedPatched;
        private bool nativeGameFramePatched;
        private bool saveSavingPatched;
        private bool saveSavedPatched;
        private bool returnHomeRequestedPatched;
        private bool returnHomePatched;
        private bool workshopReloadPatched;
        private bool workshopModUiRegisterPatched;
        private bool workshopModUiHidePatched;
        private bool workshopModManagerSavePatched;
        private bool workshopModUiCloseTransactionPatched;
        private bool hookResolutionDiagnosticLogged;
        private bool uiContextDiagnosticLogged;
        private Delegate? saveLoadedUnityEventDelegate;
        private readonly List<IGameBridgeFeature> features = new List<IGameBridgeFeature>();
        private readonly Dictionary<string, GameBridgeFeatureStatus> featureStatuses = new Dictionary<string, GameBridgeFeatureStatus>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameBridgeFeatureFailureState> featureFailures = new Dictionary<string, GameBridgeFeatureFailureState>(StringComparer.OrdinalIgnoreCase);
        private bool experimentalApisRegistered;
        private ItemDisplayNameFeature? itemDisplayNameFeature;
        private CropHarvestingFeature? cropHarvestingFeature;

        public DolocTownGameBridge(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            this.runtime.ConfigureLanguageProvider(DolocTownLanguageProvider.GetCurrentLanguage);
            RegisterExperimentalApis();
            runtime.RegisterModOwnerCleanupParticipant(new GameBridgeModOwnerCleanupParticipant(this));
        }




        internal CropHarvestingService? CropHarvestingService => cropHarvestingFeature?.Service;

        public void Initialize()
        {
            initializedAt = DateTimeOffset.Now;
            try { audioStartupPolicies = new AudioStartupPolicies(runtime, runtime.AppendBgmGroups); }
            catch (Exception ex) { runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Audio startup policy unavailable.", ex.ToString()); }
            DolocTownHookCallbacks.Runtime = runtime;
            DolocTownHookCallbacks.Bridge = this;
            PublishGameBridgeFeatureHookStatuses();
            runtime.SetHookStatus("GameLoop.UpdateTicked", "verified", "NormalGameState.OnUpdate / Unity PlayerLoop", "DTMAPI dispatches UpdateTicked once per Unity frame.");
            runtime.SetHookStatus("GameLoop.OneSecondUpdateTicked", "verified", "DTMAPI.Core.Update", "DTMAPI dispatches the one-second event from the main-thread frame update.");
            hookInstallScheduler.StartRetry();
            hookInstallScheduler.Request();
        }

        private void RegisterExperimentalApis()
        {
            if (experimentalApisRegistered)
                return;
            EnsureGameBridgeFeatures();
            var manifest = new ManifestModel
            {
                Name = "DTMAPI Doloc Town GameBridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            RegisterGameBridgeFeatureApis(manifest);
            experimentalApisRegistered = true;
        }

        internal void UpdateRuntimeAutomation()
        {
            if (shutdownCleanupRan)
                return;
            ProcessPendingHookInstallRequests();
            RefreshUiContext();
        }

        internal void NotifyGameBridgeFeaturesSaveLoaded(bool isNewGame)
        {
            DispatchGameBridgeFeatures("SaveLoaded", feature => feature.SaveLoaded(isNewGame));
        }

        internal void NotifyGameBridgeFeaturesReturnedToTitle()
        {
            DispatchGameBridgeFeatures("ReturnedToTitle", feature => feature.ReturnedToTitle());
        }

        private void EnsureGameBridgeFeatures()
        {
            itemDisplayNameFeature ??= new ItemDisplayNameFeature(runtime);
            if (!features.Contains(itemDisplayNameFeature)) features.Add(itemDisplayNameFeature);
            cropHarvestingFeature ??= new CropHarvestingFeature(runtime);
            if (!features.Contains(cropHarvestingFeature)) features.Add(cropHarvestingFeature);
        }

        private void RefreshUiContext()
        {
            try
            {
                patcher ??= new HarmonyPatcher(runtime);
                if (uiContextDolocApiType == null)
                {
                    uiContextDolocApiType = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                    if (uiContextDolocApiType != null)
                        BuildUiContextNativeCache(uiContextDolocApiType);
                }
                Type? dolocApi = uiContextDolocApiType;
                if (dolocApi == null)
                {
                    runtime.UI.SetUiContext("Unknown", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI not visible yet.");
                    return;
                }

                if (uiContextIsNormalStateGetter?.Invoke() == true)
                {
                    runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI.IsNormalState.");
                    return;
                }

                // HomePageUiState can remain in the state stack behind official title-page
                // panels. Prefer blocking panels first so the DTMAPI title button hides
                // when Doloc Town owns the visible UI.
                string context = FirstActiveUiState() ?? "Gameplay";

                bool gameplayHotkeysAllowed = context.Equals("Gameplay", StringComparison.OrdinalIgnoreCase);
                runtime.UI.SetUiContext(context, canDrawOverlay: true, gameplayHotkeysAllowed: gameplayHotkeysAllowed, reason: "Active Doloc Town UI state.");
            }
            catch (Exception ex)
            {
                if (!uiContextDiagnosticLogged)
                {
                    uiContextDiagnosticLogged = true;
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to refresh UI input context.", ex.ToString());
                }
                runtime.UI.SetUiContext("Unknown", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "UI context query failed once; see diagnostics.");
            }
        }

        private void BuildUiContextNativeCache(Type dolocApi)
        {
            uiContextIsNormalStateGetter = GameBridgeNativeAccessors.CreateStaticBoolGetter(
                GameBridgeNativeAccessors.FindMember(dolocApi, "IsNormalState", isStatic: true));
            uiContextUserInputGetter = GameBridgeNativeAccessors.CreateStaticObjectGetter(
                GameBridgeNativeAccessors.FindMember(dolocApi, "userInput", isStatic: true));
            uiContextBlockingStateTypes = UiContextBlockingStateTypeNames
                .Select(typeName => patcher?.ResolveType(typeName))
                .Where(type => type != null)
                .Cast<Type>()
                .ToArray();
            uiContextCurrentStateGetter = null;
            uiContextUserInputType = null;
        }

        private string? FirstActiveUiState()
        {
            object? userInput = uiContextUserInputGetter?.Invoke();
            if (userInput == null)
                return null;

            Type userInputType = userInput.GetType();
            if (!ReferenceEquals(uiContextUserInputType, userInputType))
            {
                uiContextUserInputType = userInputType;
                uiContextCurrentStateGetter = GameBridgeNativeAccessors.CreateObjectGetter(
                    GameBridgeNativeAccessors.FindMember(userInputType, "CurrentState"));
            }

            object? currentState = uiContextCurrentStateGetter?.Invoke(userInput);
            if (currentState == null)
                return null;
            for (int index = 0; index < uiContextBlockingStateTypes.Length; index++)
            {
                Type stateType = uiContextBlockingStateTypes[index];
                if (stateType.IsInstanceOfType(currentState))
                    return stateType.Name;
            }
            return null;
        }

        private static object? GetExistingUiState(Type dolocApi, Type stateType)
        {
            PropertyInfo? gameUiStatesProperty = dolocApi.GetProperty("gameUiStates", BindingFlags.Public | BindingFlags.Static);
            object? gameUiStates = gameUiStatesProperty?.GetValue(null);
            if (gameUiStates == null)
                return null;

            MethodInfo? hasState = gameUiStates.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "HasState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (hasState == null)
                return null;

            object? result = hasState.MakeGenericMethod(stateType).Invoke(gameUiStates, null);
            if (!(result is bool active) || !active)
                return null;

            MethodInfo? getState = gameUiStates.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            return getState?.MakeGenericMethod(stateType).Invoke(gameUiStates, null);
        }

        private bool TrySubscribeSaveLoadedUnityEvent(HarmonyPatcher patcher)
        {
            try
            {
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                FieldInfo? field = dolocApi?.GetField("OnAfterLoadArchiveData", BindingFlags.Public | BindingFlags.Static);
                object? unityEvent = field?.GetValue(null);
                if (unityEvent == null)
                    return false;

                MethodInfo? addListener = unityEvent.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "AddListener" && m.GetParameters().Length == 1);
                MethodInfo? callback = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AfterLoadArchiveDataPostfix), BindingFlags.Public | BindingFlags.Static);
                Type? delegateType = addListener?.GetParameters()[0].ParameterType;
                if (addListener == null || callback == null || delegateType == null)
                    return false;

                saveLoadedUnityEventDelegate = Delegate.CreateDelegate(delegateType, callback);
                addListener.Invoke(unityEvent, new object[] { saveLoadedUnityEventDelegate });
                runtime.RuntimeMonitor.Log("Subscribed to DolocAPI.OnAfterLoadArchiveData for SaveLoaded evidence.");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to subscribe DolocAPI.OnAfterLoadArchiveData.", ex.ToString());
                return false;
            }
        }

        private void TryUnsubscribeSaveLoadedUnityEvent(string reason)
        {
            if (saveLoadedUnityEventDelegate == null || patcher == null)
                return;

            Delegate listener = saveLoadedUnityEventDelegate;
            try
            {
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                FieldInfo? field = dolocApi?.GetField("OnAfterLoadArchiveData", BindingFlags.Public | BindingFlags.Static);
                object? unityEvent = field?.GetValue(null);
                if (unityEvent == null)
                    return;

                MethodInfo? removeListener = unityEvent.GetType().GetMethods()
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "RemoveListener")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(listener.GetType());
                    });
                removeListener?.Invoke(unityEvent, new object[] { listener });
                runtime.RuntimeMonitor.Log("Unsubscribed DTMAPI SaveLoaded UnityEvent listener reason=" + (reason ?? string.Empty) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordWarning("DTMAPI.GameBridge", "Failed to unsubscribe DolocAPI.OnAfterLoadArchiveData.", ex.ToString());
            }
            finally
            {
                saveLoadedUnityEventDelegate = null;
                saveLoadedEventSubscribed = false;
            }
        }

    }
}

