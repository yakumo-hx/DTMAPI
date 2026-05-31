using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class DolocTownExperimentalBridgeApi : IActionCompletionApi, IFishingAutomationApi, IActionSpeedApi, IItemTooltipApi, IAnimalViewerApi
    {
        private readonly DTMAPI.Core.Runtime.DtmApiRuntime runtime;
        private readonly Dictionary<string, ActionCompletionOptions> actionOptions = new Dictionary<string, ActionCompletionOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationOptions> fishingOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> fishingStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ActionSpeedOptions> actionSpeedOptions = new Dictionary<string, ActionSpeedOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishRoeTooltipOptions> fishRoeOptions = new Dictionary<string, FishRoeTooltipOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<string, FishRoeDisplayInfo?>> fishRoeLookups = new Dictionary<string, Func<string, FishRoeDisplayInfo?>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AnimalHusbandryProgressOptions> animalOptions = new Dictionary<string, AnimalHusbandryProgressOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> husbandryThresholdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> itemTitleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishingPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionSpeedApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishRoeApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedAnimalApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private bool actionHooksInstalled;
        private bool fishingHooksInstalled;
        private bool actionSpeedToolHooksInstalled;
        private bool actionSpeedInteractionHooksInstalled;
        private bool fishRoeHooksInstalled;
        private bool animalViewerHookInstalled;
        private bool animalViewerUiEvidenceRecorded;
        private bool animalPanelUiProbeLogged;
        private bool animalViewerUiDelayedScreenshotRecorded;
        private string? latestAnimalViewerEvidenceDir;

        public DolocTownExperimentalBridgeApi(DTMAPI.Core.Runtime.DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal int OneActionApplicationCount { get; private set; }

        internal string LastOneActionApplicationSummary { get; private set; } = string.Empty;

        internal int FishingAutomationApplicationCount { get; private set; }

        internal string LastFishingAutomationApplicationSummary { get; private set; } = string.Empty;

        internal int ActionSpeedApplicationCount { get; private set; }

        internal string LastActionSpeedApplicationSummary { get; private set; } = string.Empty;

        internal int ActionSpeedContinuousUseApplicationCount { get; private set; }

        internal string LastActionSpeedContinuousUseSummary { get; private set; } = string.Empty;

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus("Actions.OneActionComplete", "pending", "DTMAPI.GameBridge.DolocTown API", "Resource-hit, wrong-tool, and fuel/feed evidence exists in ONEACTION-001/002; vegetation/dandelion is recorded as the native VegetationRenderer.OnFell exception path in ONEACTION-003; waiting for ToolCollider/interact hooks in this run.");
            runtime.SetHookStatus("Actions.OneActionFuelFeed", "pending", "DTMAPI.GameBridge.DolocTown API", "Fuel/feeder fill evidence exists in ONEACTION-002; waiting for AgentStateInteract.OnExit to become patchable in this run.");
            runtime.SetHookStatus("Fishing.Automation", "pending", "DTMAPI.GameBridge.DolocTown API", "Wait-phase InstantBite evidence exists in AUTOFISH-001; waiting for fishing hook install in this run. Full automation remains experimental.");
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", "pending", "DTMAPI.GameBridge.DolocTown API", "Tool-animation evidence exists in ACTIONSPEED-001; waiting for AgentStateTool hook install in this run. ACTIONSPEED-002 now covers fuel/feed add, eat/drink continuous use, IWaterContainer and in-water bottle fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest.");
            runtime.SetHookStatus("Items.FishRoeTooltip", "pending", "DTMAPI.GameBridge.DolocTown API", "Fish roe tooltip evidence exists in FISHROE-001; waiting for item display hook install in this run.");
            runtime.SetHookStatus("Animals.ViewerRendering", "pending", "DTMAPI.GameBridge.DolocTown API", "Animal bell UI evidence exists in ANIMAL-001; waiting for animal viewer hooks in this run.");
        }

        internal void SetFishRoeHooksInstalled(bool installed)
        {
            fishRoeHooksInstalled = installed;
        }

        internal void SetActionHooksInstalled(bool installed)
        {
            actionHooksInstalled = installed;
        }

        internal void SetFishingHooksInstalled(bool installed)
        {
            fishingHooksInstalled = installed;
        }

        internal void SetActionSpeedToolHooksInstalled(bool installed)
        {
            actionSpeedToolHooksInstalled = installed;
        }

        internal void SetActionSpeedInteractionHooksInstalled(bool installed)
        {
            actionSpeedInteractionHooksInstalled = installed;
        }

        internal void SetAnimalViewerHookInstalled(bool installed)
        {
            animalViewerHookInstalled = installed;
        }

        public void Configure(IManifest owner, ActionCompletionOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionOptions[owner.UniqueID] = options ?? new ActionCompletionOptions();
            runtime.RuntimeMonitor.Log("Action completion bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionCompletionApi.GetStatus(string uniqueId)
        {
            return actionOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionHooksInstalled ? "configured-verified-resource-hook" : "configured-pending-hook", actionHooksInstalled ? "Policy accepted; resource/tool-hit, wrong-tool guard, and fuel/feeder native consume/fill paths have third-save smoke evidence. Vegetation/dandelion is a native VegetationRenderer.OnFell exception path, not a DungeonResource one-action path." : "Policy accepted; action-completion evidence exists, but this run has not installed the gameplay hook yet.")
                : new BridgeFeatureStatus("not-configured", "No action completion policy was registered for this mod.");
        }

        public void Configure(IManifest owner, ActionSpeedOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionSpeedOptions[owner.UniqueID] = NormalizeActionSpeedOptions(options);
            runtime.RuntimeMonitor.Log("Action speed bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionSpeedApi.GetStatus(string uniqueId)
        {
            return actionSpeedOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionSpeedToolHooksInstalled ? "configured-verified-runtime-hooks" : "configured-pending-tool-hook", actionSpeedToolHooksInstalled ? "Tool animation speed is verified. Fuel/feed add, eat/drink continuous use, IWaterContainer and in-water bottle fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest have third-save smoke evidence" + (actionSpeedInteractionHooksInstalled ? " and interaction hooks are installed in this run." : "; waiting for interaction hooks in this run.") : "Policy accepted; waiting for AgentStateTool hooks in this run.")
                : new BridgeFeatureStatus("not-configured", "No action-speed policy was registered for this mod.");
        }

        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId)
        {
            return TryFindActionSpeedToolPolicy(out ownerId, out _);
        }

        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;
                if ((candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
                    (candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
                    (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
                    (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
                    (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1) ||
                    (candidate.AutoFillBottle && candidate.BottleFillMultiplier > 1) ||
                    (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        public void Configure(IManifest owner, FishingAutomationOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishingOptions[owner.UniqueID] = options ?? new FishingAutomationOptions();
            if (!fishingStates.ContainsKey(owner.UniqueID))
                fishingStates[owner.UniqueID] = new FishingAutomationState();
            runtime.RuntimeMonitor.Log("Fishing automation bridge configured by " + owner.UniqueID + ".");
        }

        public void SetEnabled(IManifest owner, bool enabled, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!fishingStates.TryGetValue(owner.UniqueID, out FishingAutomationState state))
            {
                state = new FishingAutomationState();
                fishingStates[owner.UniqueID] = state;
            }
            state.Enabled = enabled;
            state.Phase = enabled ? "Starting" : "Idle";
            state.LastReason = reason ?? string.Empty;
            runtime.RuntimeMonitor.Log("Fishing automation state " + owner.UniqueID + " enabled=" + enabled + " reason=" + state.LastReason);
        }

        public FishingAutomationState GetState(string uniqueId)
        {
            return fishingStates.TryGetValue(uniqueId ?? string.Empty, out FishingAutomationState state)
                ? state
                : new FishingAutomationState();
        }

        BridgeFeatureStatus IFishingAutomationApi.GetStatus(string uniqueId)
        {
            return fishingOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishingHooksInstalled ? "configured-experimental-hook" : "configured-pending-hook", fishingHooksInstalled ? "Policy accepted and fishing phase hooks are installed; wait-phase InstantBite has smoke evidence, while broader automation remains experimental." : "Policy accepted; fishing phase and input hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fishing automation policy was registered for this mod.");
        }

        internal bool TryGetEnabledFishingAutomationOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                if (entry.Value?.Enabled == true && fishingOptions.ContainsKey(entry.Key))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        public void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishRoeOptions[owner.UniqueID] = options ?? new FishRoeTooltipOptions();
            if (lookup != null)
                fishRoeLookups[owner.UniqueID] = lookup;
            runtime.RuntimeMonitor.Log("Fish roe tooltip bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IItemTooltipApi.GetStatus(string uniqueId)
        {
            return fishRoeOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishRoeHooksInstalled ? "configured-verified-tooltip-hook" : "configured-pending-hook", fishRoeHooksInstalled ? "Lookup provider accepted and item display hooks are installed; fish roe tooltip evidence is recorded, but the API remains experimental." : "Lookup provider accepted; item tooltip hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fish roe tooltip provider was registered for this mod.");
        }

        public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            animalOptions[owner.UniqueID] = options ?? new AnimalHusbandryProgressOptions();
            runtime.RuntimeMonitor.Log("Animal viewer bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IAnimalViewerApi.GetStatus(string uniqueId)
        {
            return animalOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(animalViewerHookInstalled ? "configured-verified-animal-viewer-hook" : "configured-pending-hook", animalViewerHookInstalled ? "Display options accepted and animal viewer data hooks are installed; real animal bell UI evidence is recorded, but the API remains experimental." : "Display options accepted; animal viewer hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No animal viewer display policy was registered for this mod.");
        }

        internal string DecorateFishRoeTitle(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeTitle)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = " (" + fishTitle + ")";
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;
                if (string.IsNullOrWhiteSpace(result))
                    result = FirstText(info.RoeTitle, "Fish roe");
                result += marker;
                LogOnce(loggedFishRoeApplications, entry.Key + ":title:" + fishId, "Fish roe title hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        internal string DecorateFishRoeDetail(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeDetails)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = "Hatches: " + fishTitle;
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;

                var parts = new List<string> { marker };
                if (!string.IsNullOrWhiteSpace(info.IncubateText))
                    parts.Add("Incubate: " + info.IncubateText);
                if (!string.IsNullOrWhiteSpace(info.GrowText))
                    parts.Add("Grow: " + info.GrowText);
                if (!string.IsNullOrWhiteSpace(info.ParentSummary))
                    parts.Add(info.ParentSummary);

                string extra = string.Join("; ", parts.ToArray());
                result = string.IsNullOrWhiteSpace(result) ? extra : result.TrimEnd() + Environment.NewLine + extra;
                LogOnce(loggedFishRoeApplications, entry.Key + ":detail:" + fishId, "Fish roe detail hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        internal void DecorateAnimalFullInfoData(object data, object animal)
        {
            if (data == null || animal == null)
                return;
            AnimalProgressInfo? progress = TryBuildAnimalProgress(animal);
            if (progress == null)
                return;

            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                FieldInfo? stateDescription = data.GetType().GetField("stateDescription", BindingFlags.Public | BindingFlags.Instance);
                if (stateDescription == null)
                    return;

                string current = stateDescription.GetValue(data) as string ?? string.Empty;
                string label = string.IsNullOrWhiteSpace(options.ProgressLabel) ? "隐藏产物" : options.ProgressLabel.Trim();
                string line = label + ": " + progress.OutputTitle + " " + progress.Current + "/" + progress.Threshold;
                if (current.IndexOf(line, StringComparison.Ordinal) >= 0)
                    continue;

                stateDescription.SetValue(data, string.IsNullOrWhiteSpace(current) ? line : line + Environment.NewLine + current.TrimStart());
                LogOnce(loggedAnimalApplications, entry.Key + ":" + progress.AnimalId + ":" + progress.OutputId, "Animal viewer progress hook applied by " + entry.Key + " for " + progress.AnimalId + "/" + progress.OutputId + ".");
            }
        }

        internal bool RecordAnimalViewerUiEvidence(object viewer, object data)
        {
            if (viewer == null || data == null || animalViewerUiEvidenceRecorded)
                return false;

            string stateDescription = ReadStringMember(data, "stateDescription");
            if (string.IsNullOrWhiteSpace(stateDescription))
                return false;
            if (!ReadBoolMember(data, "notEmpty", true) || !ReadBoolMember(data, "visible", true))
                return false;
            if (!TryFindAnimalProgressMarker(stateDescription, out string ownerId, out string label))
                return false;

            animalViewerUiEvidenceRecorded = true;
            string title = ReadStringMember(data, "title");
            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "ANIMAL-001", timestamp);
            Directory.CreateDirectory(evidenceDir);
            latestAnimalViewerEvidenceDir = evidenceDir;

            string screenshotPath = Path.Combine(evidenceDir, "animal-viewer-ui.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(evidenceDir, "summary.txt");
            File.WriteAllText(summaryPath,
                "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                "Owner=" + ownerId + Environment.NewLine +
                "Label=" + label + Environment.NewLine +
                "Title=" + title + Environment.NewLine +
                "ViewerType=" + viewer.GetType().FullName + Environment.NewLine +
                "DataType=" + data.GetType().FullName + Environment.NewLine +
                "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "StateDescription=" + stateDescription.Replace(Environment.NewLine, " | ") + Environment.NewLine);

            runtime.RuntimeMonitor.Log("Animal viewer UI evidence OK owner=" + ownerId + " title=" + title + " label=" + label + " screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
            runtime.SetHookStatus("Animals.ViewerRendering", "verified", "Harmony Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanel.RefreshViewer", "Real animal viewer UI showed visible progress text. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalPanelUi", "verified", "DolocAPI.EnterUI(AnimalPanelUiState) + AnimalPanel.RefreshViewer", "Opened official AnimalPanel UI and observed progress text. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalViewerUi", "verified", "AnimalViewer.Show", "Observed animal progress text in the real animal viewer UI. Evidence=" + evidenceDir);
            return true;
        }

        internal bool CaptureDelayedAnimalViewerUiEvidenceScreenshot()
        {
            if (animalViewerUiDelayedScreenshotRecorded || string.IsNullOrWhiteSpace(latestAnimalViewerEvidenceDir))
                return false;

            animalViewerUiDelayedScreenshotRecorded = true;
            string screenshotPath = Path.Combine(latestAnimalViewerEvidenceDir!, "animal-viewer-ui-delayed.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(latestAnimalViewerEvidenceDir!, "summary.txt");
            try
            {
                File.AppendAllText(summaryPath,
                    "DelayedCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "DelayedScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "DelayedScreenshot=" + screenshotPath + Environment.NewLine);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to append delayed animal UI screenshot summary.", ex.ToString());
            }

            runtime.RuntimeMonitor.Log("Animal viewer UI delayed screenshot " + (screenshotRequested ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
            runtime.SetHookStatus("Smoke.AnimalViewerUiScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Delayed animal viewer screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
            return screenshotRequested;
        }

        internal bool RecordAnimalPanelUiEvidence(object panel, int index)
        {
            if (panel == null || animalViewerUiEvidenceRecorded)
                return false;

            object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
            object? data = null;
            if (currentDatas is Array array && index >= 0 && index < array.Length)
                data = array.GetValue(index);
            else if (currentDatas is IEnumerable enumerable)
            {
                int currentIndex = 0;
                foreach (object item in enumerable)
                {
                    if (currentIndex == index)
                    {
                        data = item;
                        break;
                    }
                    currentIndex++;
                }
            }

            if (!animalPanelUiProbeLogged)
            {
                animalPanelUiProbeLogged = true;
                runtime.RuntimeMonitor.Log("Animal panel UI evidence probe index=" + index + " panel=" + panel.GetType().FullName + " currentDatas=" + (currentDatas?.GetType().FullName ?? "null") + " data=" + (data?.GetType().FullName ?? "null") + ".");
            }

            if (data != null && RecordAnimalViewerUiEvidence(panel, data))
                return true;

            if (currentDatas is Array scanArray)
            {
                foreach (object? item in scanArray)
                {
                    if (item != null && RecordAnimalViewerUiEvidence(panel, item))
                        return true;
                }
            }

            return false;
        }

        internal bool ApplyOneActionToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null || actionOptions.Count == 0)
                return false;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null || IsResourceRemoved(resource))
                return false;

            int currentHealth = ReadIntMember(resource, "currentHealth", 0);
            if (currentHealth <= 0)
                return false;

            if (!TryFindActionPolicy(resource, out string ownerId, out ActionCompletionOptions options))
                return false;

            object? currentTool = ReadMember(toolCollider, "currentTool");
            if (currentTool == null)
                return false;

            object? hitPoint = resource.GetType().GetProperty("PositionCenter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            if (hitPoint == null)
                return false;

            Type? resourceFellDataType = ResolveType("DolocTown.ResourceFellData, Assembly-CSharp");
            if (resourceFellDataType == null)
                return false;

            if (!TryBuildValidatedResourceFellData(resourceFellDataType, resource, currentTool, hitPoint, out object? nativeFellData, out string rejectReason))
            {
                string skippedResource = GetResourceName(resource);
                string skippedTool = ReadStringMember(currentTool, "name");
                string skippedToolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                LogOnce(loggedActionApplications, "skip:" + ownerId + ":" + skippedResource + ":" + skippedTool + ":" + rejectReason, "One-action tool hook skipped resource " + skippedResource + " for " + ownerId + " tool=" + skippedTool + " toolType=" + skippedToolType + " reason=" + rejectReason + ".");
                return false;
            }

            try
            {
                int toolLevel = ReadIntMember(nativeFellData!, "toolLevel", 0);
                bool levelMatch = ReadBoolMember(nativeFellData!, "levelMatch", false);
                bool shouldCounterBack = ReadBoolMember(nativeFellData!, "shouldCounterBack", true);
                bool shouldRaiseToolTip = ReadBoolMember(nativeFellData!, "shouldRaiseToolTip", true);
                string overrideSpawnLut = ReadMember(nativeFellData!, "overrideSpawnLut") as string ?? string.Empty;
                object? extraItems = ReadMember(nativeFellData!, "extraItems");
                object? fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { levelMatch, toolLevel, currentHealth, hitPoint, shouldCounterBack, shouldRaiseToolTip, overrideSpawnLut, extraItems });
                if (fellData == null)
                    return false;
                MethodInfo? fell = resource.GetType().GetMethod("_Fell", BindingFlags.Public | BindingFlags.Instance);
                if (fell == null)
                    return false;
                fell.Invoke(resource, new object?[] { fellData });
                string resourceName = GetResourceName(resource);
                string resourceClass = GetResourceClass(resource);
                string toolName = ReadStringMember(currentTool, "name");
                string toolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                int afterHealth = ReadIntMember(resource, "currentHealth", 0);
                bool removed = IsResourceRemoved(resource);
                LastOneActionApplicationSummary = "owner=" + ownerId + ", resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolType=" + toolType + ", toolLevel=" + toolLevel + ", damage=" + currentHealth + ", healthAfter=" + afterHealth + ", removed=" + removed;
                OneActionApplicationCount++;
                string key = ownerId + ":" + resourceName;
                if (options.VerboseLogging || !loggedActionApplications.Contains(key))
                    runtime.RuntimeMonitor.Log("One-action tool hook completed resource " + resourceName + " for " + ownerId + " tool=" + toolName + " toolType=" + toolType + " damage=" + currentHealth + ".");
                loggedActionApplications.Add(key);
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools Postfix -> DungeonResource._Fell", LastOneActionApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action tool hook failed.", ex.ToString());
                return false;
            }
        }

        internal bool ApplyOneActionEquipmentFillAfterInteract()
        {
            if (actionOptions.Count == 0)
                return false;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedEquipment == null || selectedItem == null)
                return false;

            Type equipmentType = selectedEquipment.GetType();
            if (IsTypeOrBase(equipmentType, "DolocTown.PowerGeneratorFuel"))
                return TryApplyOneActionFuelFill(selectedEquipment, selectedItem, dolocApi);
            if (IsTypeOrBase(equipmentType, "DolocTown.Feeder"))
                return TryApplyOneActionFeederFill(selectedEquipment, selectedItem, dolocApi);
            return false;
        }

        private bool TryApplyOneActionFuelFill(object generator, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isSuitableFuel = FindMethodInHierarchy(generator.GetType(), "IsSuitableFuel", 1);
            MethodInfo? addFuel = FindMethodInHierarchy(generator.GetType(), "AddFuel", 2);
            if (isSuitableFuel == null || addFuel == null)
                return false;
            if (!InvokeBool(isSuitableFuel, generator, firstSelectedItem))
                return false;

            double before = ReadDoubleMember(generator, "FuelPercent", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && !IsFuelGeneratorFull(generator))
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedItem == null || !InvokeBool(isSuitableFuel, generator, selectedItem))
                        break;

                    object? proto = ReadMember(selectedItem, "proto");
                    if (proto == null)
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFuel.Invoke(generator, new object?[] { proto, true });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action fuel fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(generator, "FuelPercent", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=FuelMachine, item=" + itemName + ", extraConsumed=" + consumed + ", fuelBefore=" + FormatRatio(before) + ", fuelAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action fuel fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " fuel=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFuel", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryApplyOneActionFeederFill(object feeder, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isAnimalFeeds = FindMethodInHierarchy(feeder.GetType(), "IsAnimalFeeds", 2);
            MethodInfo? addFeeds = FindMethodInHierarchy(feeder.GetType(), "AddFeeds", 1);
            if (isAnimalFeeds == null || addFeeds == null)
                return false;

            object? firstProto = ReadMember(firstSelectedItem, "proto");
            if (firstProto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, firstProto, out _))
                return false;

            double before = ReadDoubleMember(feeder, "progress", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && ReadDoubleMember(feeder, "progress", 0) < 0.999)
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    object? proto = selectedItem == null ? null : ReadMember(selectedItem, "proto");
                    if (selectedItem == null || proto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, proto, out _))
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFeeds.Invoke(feeder, new object?[] { proto });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action feeder fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(feeder, "progress", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=Feeder, item=" + itemName + ", extraConsumed=" + consumed + ", progressBefore=" + FormatRatio(before) + ", progressAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action feeder fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " progress=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFeeds", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryBuildValidatedResourceFellData(Type resourceFellDataType, object resource, object tool, object hitPoint, out object? fellData, out string rejectReason)
        {
            fellData = null;
            rejectReason = string.Empty;
            try
            {
                fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { resource, tool, hitPoint });
                if (fellData == null)
                {
                    rejectReason = "native-fell-data-null";
                    return false;
                }
                if (!ReadBoolMember(fellData, "Valid", false))
                {
                    rejectReason = "tool-type-mismatch";
                    return false;
                }
                if (!ReadBoolMember(fellData, "levelMatch", false))
                {
                    int toolLevel = ReadIntMember(fellData, "toolLevel", -1);
                    rejectReason = "tool-level-mismatch level=" + toolLevel;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                rejectReason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        internal bool TryFindOneActionPolicyForSmoke(object resource, out string ownerId)
        {
            return TryFindActionPolicy(resource, out ownerId, out _);
        }

        internal bool ApplyActionSpeedToolEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? tool = ReadMember(state, "tool");
            if (!IsAcceleratedTool(tool))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            double multiplier = ClampMultiplier(options.ToolMultiplier);
            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            object? toolRenderer = ReadMember(body, "ToolRenderer");
            if (toolRenderer != null)
            {
                changed += ApplyAnimatorSpeed(ReadMember(toolRenderer, "animator"), multiplier, "tool-renderer", samples);
                object? collider = ReadMember(toolRenderer, "_collider");
                if (collider != null)
                    changed += ApplyAnimatorSpeed(ReadMember(collider, "_animator"), multiplier, "tool-collider", samples);
            }

            if (changed <= 0)
                return false;

            string toolName = ReadStringMember(tool!, "name");
            if (string.IsNullOrWhiteSpace(toolName))
                toolName = tool!.GetType().Name;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", tool=" + toolName + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(ownerId + ":" + toolName))
                runtime.RuntimeMonitor.Log("ActionSpeed tool animation speed applied by " + ownerId + " tool=" + toolName + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(ownerId + ":" + toolName);
            runtime.SetHookStatus("Smoke.ActionSpeedTool", "verified", "AgentStateTool.OnEnter Postfix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal bool ApplyActionSpeedInteractEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, kind, multiplier, target, "AgentStateInteract.OnEnter Postfix", "Smoke.ActionSpeedInteraction");
        }

        internal bool ApplyActionSpeedEatEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, "EatDrink", options.EatDrinkMultiplier, "AgentStateEat", "AgentStateEat.OnEnter Postfix", "Smoke.ActionSpeedEatDrink");
        }

        internal bool AdjustActionSpeedUseItemContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            float original = dt;
            dt = (float)Math.Min(1, original * multiplier);
            string logKey = ownerId + ":UseItemContinues:" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed right-click continuous use scaled by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###") + ".");
            loggedActionSpeedApplications.Add(logKey);
            ActionSpeedApplicationCount++;
            ActionSpeedContinuousUseApplicationCount++;
            LastActionSpeedContinuousUseSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###");
            LastActionSpeedApplicationSummary = LastActionSpeedContinuousUseSummary;
            runtime.SetHookStatus("Smoke.ActionSpeedContinuousUse", "experimental", "AgentControllerState.UseItemContinues Prefix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal void RestoreActionSpeed(string reason)
        {
            if (originalAnimatorSpeeds.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();
            runtime.RuntimeMonitor.Log("ActionSpeed animator speeds restored reason=" + reason + " restored=" + restored + ".");
        }

        internal void NotifyFishingPhase(string phase, object? source)
        {
            if (string.IsNullOrWhiteSpace(phase) || fishingStates.Count == 0)
                return;

            string sourceName = source?.GetType().Name ?? "FishingState";
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState state = entry.Value ?? new FishingAutomationState();
                state.Phase = phase;
                state.LastReason = "hook:" + sourceName;
            }

            LogOnce(loggedFishingPhases, phase, "Fishing phase hook observed phase=" + phase + " source=" + sourceName + ".");
        }

        internal bool ApplyFishingWaitAutomation(object waitState)
        {
            if (waitState == null || fishingStates.Count == 0 || fishingOptions.Count == 0)
                return false;

            if (!ReadBoolMember(waitState, "_waitForFishBite", false))
                return false;

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            if (!options.InstantBite)
                return false;

            try
            {
                MethodInfo? rollFish = FindMethodInHierarchy(waitState.GetType(), "RollFish", 0);
                if (rollFish == null)
                    return false;

                object? rolledValue = rollFish.Invoke(waitState, null);
                bool rolled = rolledValue is bool value && value;
                if (!rolled)
                {
                    state.Phase = "Wait:AutoBiteFailed";
                    state.LastReason = "auto:InstantBite:no-fish";
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", "InstantBite attempted but RollFish returned false.");
                    return false;
                }

                WriteBoolMember(waitState, "_waitForFishBite", false);
                WriteBoolMember(waitState, "_hasRolled", true);
                WriteFloatMember(waitState, "_hookProbability", 1f);
                WriteFloatMember(waitState, "_fishOnHookDuration", 100f);
                TryRefreshFishingRendererAfterBite(waitState);

                state.Phase = "Wait:AutoBite";
                state.LastReason = "auto:InstantBite";
                FishingAutomationApplicationCount++;

                object? body = ReadMember(waitState, "body");
                object? cache = body == null ? null : ReadMember(body, "FishingCache");
                object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
                object? pool = cache == null ? null : ReadMember(cache, "FishingPool");
                string fishId = fishProto == null ? "unknown" : ReadStringMember(fishProto, "Id");
                string poolName = pool == null ? "unknown" : ReadStringMember(pool, "PoolName");
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=InstantBite, phase=Wait, fish=" + fishId + ", pool=" + poolName + ", applications=" + FishingAutomationApplicationCount;

                if (options.VerboseLogging || !loggedFishingPhases.Contains("AutoBite:" + ownerId))
                    runtime.RuntimeMonitor.Log("Fishing automation instant-bite applied by " + ownerId + " fish=" + fishId + " pool=" + poolName + ".");
                loggedFishingPhases.Add("AutoBite:" + ownerId);
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "AgentStateFishingWait.OnPlay Postfix", LastFishingAutomationApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing wait automation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state)
        {
            ownerId = string.Empty;
            options = null!;
            state = null!;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState candidateState = entry.Value ?? new FishingAutomationState();
                if (!candidateState.Enabled)
                    continue;
                if (!fishingOptions.TryGetValue(entry.Key, out FishingAutomationOptions candidateOptions))
                    continue;
                ownerId = entry.Key;
                options = candidateOptions ?? new FishingAutomationOptions();
                state = candidateState;
                return true;
            }
            return false;
        }

        private static void TryRefreshFishingRendererAfterBite(object waitState)
        {
            object? body = ReadMember(waitState, "body");
            object? renderer = body == null ? null : ReadMember(body, "fishRodRenderer");
            object? line = renderer == null ? null : ReadMember(renderer, "Line");
            line?.GetType().GetMethod("UseStraightLine", BindingFlags.Public | BindingFlags.Instance)?.Invoke(line, null);
            renderer?.GetType().GetMethod("EnableFishShadow", BindingFlags.Public | BindingFlags.Instance)?.Invoke(renderer, null);
        }

        private bool TryLookupFishRoe(string ownerId, string fishId, out FishRoeDisplayInfo info)
        {
            info = null!;
            if (!fishRoeLookups.TryGetValue(ownerId ?? string.Empty, out Func<string, FishRoeDisplayInfo?> lookup) || string.IsNullOrWhiteSpace(fishId))
                return false;
            try
            {
                FishRoeDisplayInfo? lookedUp = lookup(fishId);
                if (lookedUp == null)
                    return false;
                info = lookedUp;
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fish roe provider failed for " + ownerId + "/" + fishId + ".", ex.ToString());
                return false;
            }
        }

        private bool TryFindActionPolicy(object resource, out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            Type resourceType = resource.GetType();
            string typeName = resourceType.FullName ?? resourceType.Name;
            string resourceClass = GetResourceClass(resource);

            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled)
                    continue;

                bool matches =
                    (candidate.CompleteTrees && IsTreeResource(resourceType)) ||
                    (candidate.CompleteOres && (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteGarbage && (typeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteWeeds && IsTypeOrBase(resourceType, "DolocTown.DungeonResourceWeeds"));

                if (!matches)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }

            return false;
        }

        private bool TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteMachineFuel)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteFeeder)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private static bool IsTreeResource(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.DungeonResourceTree") ||
                IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk");
        }

        private bool TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.ToolSpeedEnabled || candidate.ToolMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.EatDrinkSpeedEnabled || candidate.EatDrinkMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if (candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1 && IsPlantInteraction(selectedItem, selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Plant";
                    multiplier = candidate.PlantMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1 && IsMachineAddInteraction(selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "MachineAdd";
                    multiplier = candidate.MachineAddMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1 && IsHarvestInteraction(selectedEquipment, currentInteractable))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Harvest";
                    multiplier = candidate.HarvestMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private bool TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem == null)
                return false;

            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if ((candidate.BottleFillSpeedEnabled || candidate.AutoFillBottle) && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if ((candidate.EatDrinkSpeedEnabled || candidate.ContinuousDrinkWithRightClick) && candidate.EatDrinkMultiplier > 1 && IsEatDrinkItem(selectedItem))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "EatDrink";
                    multiplier = candidate.EatDrinkMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private static ActionSpeedOptions NormalizeActionSpeedOptions(ActionSpeedOptions? options)
        {
            options ??= new ActionSpeedOptions();
            options.ToolMultiplier = ClampMultiplier(options.ToolMultiplier);
            options.BottleFillMultiplier = ClampMultiplier(options.BottleFillMultiplier);
            options.EatDrinkMultiplier = ClampMultiplier(options.EatDrinkMultiplier);
            options.MachineAddMultiplier = ClampMultiplier(options.MachineAddMultiplier);
            options.HarvestMultiplier = ClampMultiplier(options.HarvestMultiplier);
            options.PlantMultiplier = ClampMultiplier(options.PlantMultiplier);
            return options;
        }

        private static bool IsAcceleratedTool(object? tool)
        {
            if (tool == null)
                return false;

            object? toolType = ReadMember(tool, "ToolType");
            string name = toolType?.ToString() ?? string.Empty;
            return name.Equals("AXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("SICKLE", StringComparison.OrdinalIgnoreCase);
        }

        private int ApplyAnimatorSpeed(object? animator, double multiplier, string label, List<string> samples)
        {
            if (animator == null)
                return 0;

            double original = ReadAnimatorSpeed(animator, 1);
            if (!originalAnimatorSpeeds.ContainsKey(animator))
                originalAnimatorSpeeds[animator] = original;
            else
                original = originalAnimatorSpeeds[animator];

            double target = original * multiplier;
            if (!TryWriteAnimatorSpeed(animator, target))
                return 0;

            if (samples.Count < 6)
                samples.Add(label + ":" + original.ToString("0.###") + "->" + target.ToString("0.###"));
            return 1;
        }

        private bool ApplyActionSpeedToBody(object body, string ownerId, ActionSpeedOptions options, string kind, double multiplier, string target, string source, string hookId)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            if (changed <= 0)
                return false;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            string logKey = ownerId + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed interaction animation speed applied by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(logKey);
            runtime.SetHookStatus(hookId, "experimental", source, LastActionSpeedApplicationSummary);
            return true;
        }

        private static bool IsBottleFillInteraction(object? selectedItem, object? selectedEquipment, bool isInWater)
        {
            if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle"))
                return false;
            return isInWater || ImplementsInterface(selectedEquipment?.GetType(), "DolocTown.IWaterContainer");
        }

        private static bool IsEatDrinkItem(object? selectedItem)
        {
            if (selectedItem == null)
                return false;

            Type itemType = selectedItem.GetType();
            if (ImplementsInterface(itemType, "DolocTown.IEatable"))
                return true;

            string typeName = itemType.FullName ?? itemType.Name;
            return typeName.IndexOf("ItemFood", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("ItemDrink", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPlantInteraction(object? selectedItem, object? selectedEquipment)
        {
            if (selectedItem == null || selectedEquipment == null)
                return false;
            if (!IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemSeed"))
                return false;
            Type equipmentType = selectedEquipment.GetType();
            if (ReadBoolMember(selectedEquipment, "IsPlanted", false) || ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                return false;
            return IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") || IsTypeOrBase(equipmentType, "DolocTown.FlowerPot");
        }

        private static bool IsMachineAddInteraction(object? selectedEquipment)
        {
            if (selectedEquipment == null)
                return false;
            Type type = selectedEquipment.GetType();
            return IsTypeOrBase(type, "DolocTown.PowerGeneratorFuel") || IsTypeOrBase(type, "DolocTown.Feeder");
        }

        private static bool IsHarvestInteraction(object? selectedEquipment, object? currentInteractable)
        {
            if (selectedEquipment != null)
            {
                Type equipmentType = selectedEquipment.GetType();
                if (IsTypeOrBase(equipmentType, "DolocTown.ResinCollector") && ReadIntMember(selectedEquipment, "currentValue", 0) > 0)
                    return true;
                if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") && ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                    return true;
                if (ImplementsInterface(equipmentType, "DolocTown.IGatherableEquipment"))
                    return true;
            }

            if (currentInteractable == null)
                return false;
            Type interactableType = currentInteractable.GetType();
            string name = interactableType.FullName ?? interactableType.Name;
            return name.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ImplementsInterface(interactableType, "DolocTown.IGatherableEquipment");
        }

        private static object? ReadCurrentActionSpeedInteractable(Type? dolocApi)
        {
            object? currentInteractable = ReadStaticMember(dolocApi, "CurrentInteractableObject");
            if (currentInteractable != null)
                return currentInteractable;

            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? interactableManager = agentController == null ? null : ReadMember(agentController, "interactableManager");
            if (interactableManager == null)
                return null;

            object? baseManager = ReadMember(interactableManager, "baseManager");
            object? current = baseManager == null ? null : ReadMember(baseManager, "Current");
            if (current != null)
                return UnwrapActionSpeedInteractable(current);

            object? subManagers = ReadMember(interactableManager, "subManagers");
            if (subManagers is IDictionary dictionary)
            {
                foreach (object? manager in dictionary.Values)
                {
                    current = manager == null ? null : ReadMember(manager, "Current");
                    if (current != null)
                        return UnwrapActionSpeedInteractable(current);
                }
            }

            return null;
        }

        private static object? UnwrapActionSpeedInteractable(object? interactable)
        {
            if (interactable == null)
                return null;

            object? vegetation = ReadMember(interactable, "Vegetation");
            if (vegetation != null)
                return vegetation;

            object? equipment = ReadMember(interactable, "equipment");
            return equipment ?? interactable;
        }

        private static string DescribeActionSpeedTarget(object? selectedItem, object? selectedEquipment, object? currentInteractable)
        {
            string item = selectedItem == null ? "none" : FirstText(ReadStringMember(selectedItem, "name"), selectedItem.GetType().Name);
            string equipment = selectedEquipment == null ? "none" : FirstText(ReadStringMember(selectedEquipment, "equipmentName"), selectedEquipment.GetType().Name);
            string interactable = currentInteractable == null ? "none" : FirstText(ReadStringMember(currentInteractable, "VegetationName"), currentInteractable.GetType().Name);
            return "item=" + item + ",equipment=" + equipment + ",interactable=" + interactable;
        }

        private static double ReadAnimatorSpeed(object animator, double fallback)
        {
            object? value = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animator);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static bool TryWriteAnimatorSpeed(object animator, double value)
        {
            try
            {
                PropertyInfo? speed = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance);
                if (speed == null || !speed.CanWrite)
                    return false;
                speed.SetValue(animator, Convert.ChangeType(value, speed.PropertyType));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(1, value));
        }

        private static object? TryGetDungeonResourceFromCollider(object collider)
        {
            Type? rendererType = ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (rendererType == null)
                return null;

            MethodInfo? getComponent = null;
            foreach (MethodInfo method in collider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == "GetComponent" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)
                {
                    getComponent = method.MakeGenericMethod(rendererType);
                    break;
                }
            }

            object? renderer = getComponent?.Invoke(collider, null);
            return renderer?.GetType().GetProperty("DungeonResource", BindingFlags.Public | BindingFlags.Instance)?.GetValue(renderer);
        }

        private static bool IsResourceRemoved(object resource)
        {
            object? value = resource.GetType().GetProperty("IsRemoved", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value is bool removed && removed;
        }

        private static string GetResourceName(object resource)
        {
            object? value = resource.GetType().GetProperty("ResourceName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value as string ?? resource.GetType().Name;
        }

        private static string GetResourceClass(object resource)
        {
            object? proto = resource.GetType().GetProperty("Proto", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            object? resourceClass = proto?.GetType().GetProperty("ResourceClass", BindingFlags.Public | BindingFlags.Instance)?.GetValue(proto);
            return resourceClass?.ToString() ?? string.Empty;
        }

        private static bool IsFuelGeneratorFull(object generator)
        {
            object? generatorFuel = ReadMember(generator, "generatorFuel");
            if (generatorFuel != null && ReadBoolMember(generatorFuel, "IsFull", false))
                return true;

            double percent = ReadDoubleMember(generator, "FuelPercent", 0);
            return percent >= 0.999;
        }

        private static bool InvokeBool(MethodInfo method, object target, object arg)
        {
            try
            {
                object? result = method.Invoke(target, new object?[] { arg });
                return result is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeIsAnimalFeeds(MethodInfo method, object target, object? itemInfo, out int energy)
        {
            energy = 0;
            try
            {
                object?[] args = new object?[] { itemInfo, energy };
                object? result = method.Invoke(target, args);
                if (!(result is bool ok) || !ok)
                    return false;
                if (args.Length > 1 && args[1] != null)
                    energy = Convert.ToInt32(args[1]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCostSelf(object item)
        {
            MethodInfo? costWithOut = FindMethodInHierarchy(item.GetType(), "CostSelf", 2);
            if (costWithOut != null)
            {
                ParameterInfo[] parameters = costWithOut.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType.IsByRef)
                {
                    object?[] args = new object?[] { null, false };
                    object? result = costWithOut.Invoke(item, args);
                    return result is bool ok && ok;
                }
            }

            MethodInfo? costSingle = FindMethodInHierarchy(item.GetType(), "CostSelf", 1);
            if (costSingle == null)
                return false;
            object? consumed = costSingle.Invoke(item, new object?[] { false });
            return consumed != null;
        }

        private static string FormatRatio(double value)
        {
            return value < 0 ? "unknown" : value.ToString("0.###");
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            Type type = instance.GetType();
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value == null ? fallback : Convert.ToInt32(value);
        }

        private static double ReadDoubleMember(object instance, string name, double fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static bool TryGetFishRoeId(object item, out string fishId)
        {
            fishId = string.Empty;
            if (item == null)
                return false;
            Type type = item.GetType();
            if (!IsTypeOrBase(type, "DolocTown.ItemFishRoe"))
                return false;
            object? value = type.GetProperty("fishName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item);
            fishId = value as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(fishId);
        }

        private AnimalProgressInfo? TryBuildAnimalProgress(object animal)
        {
            Type type = animal.GetType();
            string animalId = type.GetProperty("protoName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animal) as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(animalId))
                return null;

            object? valuesObject = type.GetField("husbandryValues", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(animal);
            if (!(valuesObject is IDictionary values) || values.Count == 0)
                return null;

            AnimalProgressInfo? best = null;
            foreach (DictionaryEntry entry in values)
            {
                string outputId = entry.Key as string ?? string.Empty;
                if (string.IsNullOrWhiteSpace(outputId))
                    continue;
                int current = Convert.ToInt32(entry.Value);
                if (!TryGetHusbandryThreshold(animalId, outputId, out int threshold) || threshold <= 0)
                    continue;

                double ratio = Math.Max(0, Math.Min(1, current / (double)threshold));
                if (best == null || ratio > best.Progress || (Math.Abs(ratio - best.Progress) < 0.0001 && current > best.Current))
                {
                    best = new AnimalProgressInfo
                    {
                        AnimalId = animalId,
                        OutputId = outputId,
                        OutputTitle = ResolveItemTitle(outputId),
                        Current = current,
                        Threshold = threshold,
                        Progress = ratio
                    };
                }
            }

            return best;
        }

        private bool TryGetHusbandryThreshold(string animalId, string outputId, out int threshold)
        {
            string cacheKey = animalId + "|" + outputId;
            if (husbandryThresholdCache.TryGetValue(cacheKey, out threshold))
                return threshold > 0;

            threshold = 0;
            try
            {
                Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
                object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? husbandry = tables?.GetType().GetProperty("TbHusbandry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
                MethodInfo? method = husbandry?.GetType().GetMethod("TryGetThreshold", BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                    return false;

                object?[] args = new object?[] { animalId, outputId, threshold };
                object? result = method.Invoke(husbandry, args);
                if (!(result is bool ok) || !ok)
                    return false;

                threshold = Convert.ToInt32(args[2]);
                husbandryThresholdCache[cacheKey] = threshold;
                return threshold > 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query husbandry threshold for " + cacheKey + ".", ex.ToString());
                return false;
            }
        }

        private string ResolveItemTitle(string itemId)
        {
            if (itemTitleCache.TryGetValue(itemId, out string title))
                return title;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? queryItemProto = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto != null)
                {
                    object?[] args = new object?[] { itemId, null };
                    object? result = queryItemProto.Invoke(null, args);
                    if (result is bool ok && ok && args[1] != null)
                    {
                        title = args[1]!.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(args[1]) as string ?? itemId;
                        itemTitleCache[itemId] = title;
                        return title;
                    }
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query item title for " + itemId + ".", ex.ToString());
            }
            itemTitleCache[itemId] = itemId;
            return itemId;
        }

        private static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool ImplementsInterface(Type? type, string fullName)
        {
            if (type == null)
                return false;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (string.Equals(interfaceType.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (value != null)
                return value;
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        }

        private static bool ReadStaticBoolMember(Type? type, string name, bool fallback)
        {
            object? value = ReadStaticMember(type, name);
            return value is bool result ? result : fallback;
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private bool TryFindAnimalProgressMarker(string stateDescription, out string ownerId, out string label)
        {
            ownerId = string.Empty;
            label = string.Empty;
            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                string candidate = string.IsNullOrWhiteSpace(options.ProgressLabel) ? "隐藏产物" : options.ProgressLabel.Trim();
                if (ContainsProgressLabel(stateDescription, candidate))
                {
                    ownerId = entry.Key;
                    label = candidate;
                    return true;
                }
            }

            if (ContainsProgressLabel(stateDescription, "Special produce"))
            {
                ownerId = "unknown";
                label = "Special produce";
                return true;
            }

            if (ContainsProgressLabel(stateDescription, "隐藏产物"))
            {
                ownerId = "unknown";
                label = "隐藏产物";
                return true;
            }

            return false;
        }

        private static bool ContainsProgressLabel(string text, string label)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label))
                return false;
            return text.IndexOf(label.Trim() + ":", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf(label.Trim() + "：", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        private static bool WriteBoolMember(object instance, string name, bool value)
        {
            return WriteMember(instance, name, typeof(bool), value);
        }

        private static bool WriteFloatMember(object instance, string name, float value)
        {
            return WriteMember(instance, name, typeof(float), value);
        }

        private static bool WriteMember(object instance, string name, Type expectedType, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == expectedType)
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == expectedType)
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                    type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
                if (value != null)
                    return value;
            }
            return null;
        }

        private static MethodInfo? FindMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.Name == name && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }

        private static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = ResolveType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.ScreenCapture, UnityEngine");
                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (!keys.Add(key))
                return;
            runtime.RuntimeMonitor.Log(message);
        }

        private sealed class AnimalProgressInfo
        {
            public string AnimalId { get; set; } = string.Empty;
            public string OutputId { get; set; } = string.Empty;
            public string OutputTitle { get; set; } = string.Empty;
            public int Current { get; set; }
            public int Threshold { get; set; }
            public double Progress { get; set; }
        }
    }
}
