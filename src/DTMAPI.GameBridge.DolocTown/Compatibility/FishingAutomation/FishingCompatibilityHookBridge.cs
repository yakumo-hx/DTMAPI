using System;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingCompatibilityHookBridge
    {
        internal const string HarmonyOwner = "dtmapi.gamebridge.doloctown.fishingcompatibility";
        private const string ManagedProductUniqueId = "Yuuka.DTMAPI.AutoFishing";
        private static readonly string ManagedProductHarmonyOwner = ManagedModClassifier.GetExpectedHarmonyOwner(ManagedProductUniqueId);
        private static readonly PatchTarget[] OwnedInventory =
        {
            new PatchTarget("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnEnter", 0),
            new PatchTarget("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnPlay", 0),
            new PatchTarget("DolocTown.AgentStateFishingCast, Assembly-CSharp", "OnEnter", 0),
            new PatchTarget("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnEnter", 0),
            new PatchTarget("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnPlay", 0),
            new PatchTarget("DolocTown.AgentStateFishingWait, Assembly-CSharp", "NextState", 0),
            new PatchTarget("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StartGame", 2),
            new PatchTarget("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", 1),
            new PatchTarget("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StopGame", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseTool", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseToolInProgress", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItem", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItemInProgress", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishing", 0),
            new PatchTarget("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishingInProgress", 0),
            new PatchTarget("DolocTown.FishRodRenderer, Assembly-CSharp", "CastHook", 0),
            new PatchTarget("DolocTown.FishRodRenderer, Assembly-CSharp", "Pull", 1),
            new PatchTarget("DolocTown.FishRodRenderer, Assembly-CSharp", "PullCancel", 0),
            new PatchTarget("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnEnter", 0),
            new PatchTarget("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnExit", 0),
            new PatchTarget("AgentStateBase, Assembly-CSharp", "OnExit", 0)
        };
        private readonly DtmApiRuntime runtime;
        private IFishingCompatibilityHookRuntime? service;
        private bool ownerCleanupFailed;
        private string lastInstallFailure = string.Empty;

        public FishingCompatibilityHookBridge(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void AttachRuntime(IFishingCompatibilityHookRuntime hookRuntime) => service = hookRuntime;

        public void DetachRuntime(IFishingCompatibilityHookRuntime hookRuntime)
        {
            if (ReferenceEquals(service, hookRuntime))
                service = null;
        }

        internal bool ReadyEnterPatched { get; private set; }

        internal bool ReadyPlayPatched { get; private set; }

        internal bool CastEnterPatched { get; private set; }

        internal bool WaitEnterPatched { get; private set; }

        internal bool WaitPlayPatched { get; private set; }

        internal bool WaitNextStatePatched { get; private set; }

        internal bool MiniGameStartPatched { get; private set; }

        internal bool MiniGameUpdatePrefixPatched { get; private set; }

        internal bool MiniGameUpdatePatched { get; private set; }

        internal bool MiniGameStopPatched { get; private set; }

        internal bool InputUseToolPatched { get; private set; }

        internal bool InputUseToolInProgressPatched { get; private set; }

        internal bool InputUseItemPatched { get; private set; }

        internal bool InputUseItemInProgressPatched { get; private set; }

        internal bool InputFishingPatched { get; private set; }

        internal bool InputFishingInProgressPatched { get; private set; }

        internal bool FishRodCastHookPatched { get; private set; }

        internal bool FishRodPullPatched { get; private set; }

        internal bool FishRodPullCancelPatched { get; private set; }

        internal bool PullEnterPatched { get; private set; }

        internal bool PullExitPatched { get; private set; }

        internal bool BaseExitPatched { get; private set; }

        internal bool InstallationBlocked => !HooksReady && !string.IsNullOrWhiteSpace(lastInstallFailure);

        internal string LastInstallFailure => lastInstallFailure;

        public bool HooksReady => ReadyEnterPatched &&
            ReadyPlayPatched &&
            CastEnterPatched &&
            WaitEnterPatched &&
            WaitPlayPatched &&
            WaitNextStatePatched &&
            MiniGameStartPatched &&
            MiniGameUpdatePrefixPatched &&
            MiniGameUpdatePatched &&
            MiniGameStopPatched &&
            InputUseToolPatched &&
            InputUseToolInProgressPatched &&
            InputUseItemPatched &&
            InputUseItemInProgressPatched &&
            InputFishingPatched &&
            InputFishingInProgressPatched &&
            FishRodCastHookPatched &&
            FishRodPullPatched &&
            FishRodPullCancelPatched &&
            PullEnterPatched &&
            PullExitPatched &&
            BaseExitPatched;

        public string MissingHookIds
        {
            get
            {
                var missing = new System.Text.StringBuilder();
                AppendMissing(missing, ReadyEnterPatched, "Ready.OnEnter");
                AppendMissing(missing, ReadyPlayPatched, "Ready.OnPlay");
                AppendMissing(missing, CastEnterPatched, "Cast.OnEnter");
                AppendMissing(missing, WaitEnterPatched, "Wait.OnEnter");
                AppendMissing(missing, WaitPlayPatched, "Wait.OnPlay");
                AppendMissing(missing, WaitNextStatePatched, "Wait.NextState");
                AppendMissing(missing, MiniGameStartPatched, "MiniGame.Start");
                AppendMissing(missing, MiniGameUpdatePrefixPatched, "MiniGame.Update.Prefix");
                AppendMissing(missing, MiniGameUpdatePatched, "MiniGame.Update.Postfix");
                AppendMissing(missing, MiniGameStopPatched, "MiniGame.Stop");
                AppendMissing(missing, InputUseToolPatched, "Input.NormalUseTool");
                AppendMissing(missing, InputUseToolInProgressPatched, "Input.NormalUseToolInProgress");
                AppendMissing(missing, InputUseItemPatched, "Input.NormalUseItem");
                AppendMissing(missing, InputUseItemInProgressPatched, "Input.NormalUseItemInProgress");
                AppendMissing(missing, InputFishingPatched, "Input.NormalFishing");
                AppendMissing(missing, InputFishingInProgressPatched, "Input.NormalFishingInProgress");
                AppendMissing(missing, FishRodCastHookPatched, "FishRodRenderer.CastHook");
                AppendMissing(missing, FishRodPullPatched, "FishRodRenderer.Pull");
                AppendMissing(missing, FishRodPullCancelPatched, "FishRodRenderer.PullCancel");
                AppendMissing(missing, PullEnterPatched, "Pull.OnEnter");
                AppendMissing(missing, PullExitPatched, "Pull.OnExit");
                AppendMissing(missing, BaseExitPatched, "AgentStateBase.OnExit");
                return missing.ToString();
            }
        }

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (patcher == null)
                throw new ArgumentNullException(nameof(patcher));
            if (!patcher.OwnerId.Equals(HarmonyOwner, StringComparison.Ordinal))
                throw new InvalidOperationException("Compatibility fishing requires its dedicated Harmony owner " + HarmonyOwner + ".");
            if (HooksReady)
            {
                service?.SetFishingHooksInstalled(true);
                PublishStatuses();
                return;
            }
            if (!CanInstallOwnedInventory(patcher, out string blockedReason))
            {
                FailClosed(patcher, blockedReason);
                return;
            }

            if (!ReadyEnterPatched)
            {
                ReadyEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityReadyEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!ReadyPlayPatched)
            {
                ReadyPlayPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnPlay", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityReadyPlayPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!CastEnterPatched)
            {
                CastEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingCast, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityCastEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!WaitEnterPatched)
            {
                WaitEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityWaitEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!WaitPlayPatched)
            {
                WaitPlayPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnPlay", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityWaitPlayPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!WaitNextStatePatched)
            {
                WaitNextStatePatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "NextState", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityWaitNextStatePostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!MiniGameStartPatched)
            {
                MiniGameStartPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StartGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityMiniGameStartPostfix), BindingFlags.Public | BindingFlags.Static), 2);
            }

            if (!MiniGameUpdatePatched)
            {
                MiniGameUpdatePatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityMiniGameUpdatePostfix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!MiniGameUpdatePrefixPatched)
            {
                MiniGameUpdatePrefixPatched = patcher.TryPatchPrefix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityMiniGameUpdatePrefix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!MiniGameStopPatched)
            {
                MiniGameStopPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StopGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityMiniGameStopPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseToolPatched)
            {
                InputUseToolPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseTool", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalUseToolPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseToolInProgressPatched)
            {
                InputUseToolInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseToolInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalUseToolInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseItemPatched)
            {
                InputUseItemPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItem", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalUseItemPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseItemInProgressPatched)
            {
                InputUseItemInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItemInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalUseItemInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputFishingPatched)
            {
                InputFishingPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishing", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalFishingPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputFishingInProgressPatched)
            {
                InputFishingInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishingInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityInputNormalFishingInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!FishRodCastHookPatched)
            {
                FishRodCastHookPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "CastHook", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityRodCastHookPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!FishRodPullPatched)
            {
                FishRodPullPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "Pull", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityRodPullPostfix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!FishRodPullCancelPatched)
            {
                FishRodPullCancelPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "PullCancel", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityRodPullCancelPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!PullEnterPatched)
            {
                PullEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityPullEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!PullExitPatched)
            {
                PullExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityPullExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!BaseExitPatched)
            {
                BaseExitPatched = patcher.TryPatchPostfix("AgentStateBase, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCompatibilityBaseExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!HooksReady)
            {
                FailClosed(patcher, "The compatibility fishing patch inventory was incomplete: " + MissingHookIds + ".");
                return;
            }

            ownerCleanupFailed = false;
            lastInstallFailure = string.Empty;
            service?.SetFishingHooksInstalled(true);
            PublishStatuses();
        }

        public bool UninstallHooks(HarmonyReflectionPatcher? patcher, string reason)
        {
            service?.SetFishingHooksInstalled(false);
            bool cleaned = patcher == null || patcher.TryUnpatchAllOwnedPatches();
            ownerCleanupFailed = !cleaned;
            ResetPatchStatuses();
            lastInstallFailure = cleaned
                ? string.Empty
                : "Compatibility fishing owner cleanup failed during " + (reason ?? string.Empty) + ".";
            PublishStatuses();
            return cleaned;
        }

        public void EnsureInstalled(HarmonyReflectionPatcher? patcher)
        {
            if (patcher != null && !HooksReady)
                InstallHooks(patcher);
        }

        private bool CanInstallOwnedInventory(HarmonyReflectionPatcher patcher, out string blockedReason)
        {
            if (ownerCleanupFailed)
            {
                if (!patcher.TryUnpatchAllOwnedPatches())
                {
                    blockedReason = "A previous compatibility fishing owner cleanup is still incomplete.";
                    return false;
                }
                ownerCleanupFailed = false;
                ResetPatchStatuses();
            }

            if (!TryFindManagedModOwnerOnInventory(patcher, out string managedOwner, out string inspectionFailure))
            {
                blockedReason = "Compatibility fishing could not verify Harmony owner isolation: " + inspectionFailure;
                return false;
            }
            if (!string.IsNullOrWhiteSpace(managedOwner))
            {
                blockedReason = "Compatibility fishing refused to install because managed Mod owner " + managedOwner + " already patches its native inventory.";
                return false;
            }

            blockedReason = string.Empty;
            return true;
        }

        private void FailClosed(HarmonyReflectionPatcher patcher, string reason)
        {
            bool cleaned = patcher.TryUnpatchAllOwnedPatches();
            ownerCleanupFailed = !cleaned;
            ResetPatchStatuses();
            service?.SetFishingHooksInstalled(false);
            lastInstallFailure = (reason ?? "Compatibility fishing patch installation failed.") +
                (cleaned ? " Exact-owner rollback completed." : " Exact-owner rollback failed; callbacks remain disabled.");
            runtime.RuntimeMonitor.Log(lastInstallFailure, global::DTMAPI.Abstractions.LogLevel.Warn);
            PublishStatuses();
        }

        private static bool TryFindManagedModOwnerOnInventory(HarmonyReflectionPatcher patcher, out string owner, out string failure)
        {
            owner = string.Empty;
            failure = string.Empty;
            bool harmonyLoaded = false;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetType("HarmonyLib.Harmony", throwOnError: false, ignoreCase: false) != null)
                    {
                        harmonyLoaded = true;
                        break;
                    }
                }
                catch
                {
                }
            }
            if (!harmonyLoaded)
                return true;

            var targets = new System.Collections.Generic.List<MethodBase>(OwnedInventory.Length);
            foreach (PatchTarget expected in OwnedInventory)
            {
                MethodInfo? target = patcher.ResolveMethod(expected.TypeName, expected.MethodName, expected.ParameterCount);
                if (target == null)
                {
                    failure = "could not resolve native target " + expected.TypeName + "." + expected.MethodName + "/" + expected.ParameterCount + ".";
                    return false;
                }
                targets.Add(target);
            }

            AdvancedHarmonySnapshot snapshot = new ReflectionAdvancedHarmonyInspector().Capture();
            if (!snapshot.Available)
            {
                failure = snapshot.Failure;
                return false;
            }
            foreach (AdvancedHarmonyPatchRecord patch in snapshot.Patches)
            {
                if (!patch.Owner.Equals(ManagedProductHarmonyOwner, StringComparison.Ordinal))
                    continue;
                foreach (MethodBase target in targets)
                {
                    if (!patch.Targets(target))
                        continue;
                    owner = patch.Owner;
                    return true;
                }
            }
            return true;
        }

        private readonly struct PatchTarget
        {
            internal PatchTarget(string typeName, string methodName, int parameterCount)
            {
                TypeName = typeName;
                MethodName = methodName;
                ParameterCount = parameterCount;
            }

            internal string TypeName { get; }

            internal string MethodName { get; }

            internal int ParameterCount { get; }
        }

        private void ResetPatchStatuses()
        {
            ReadyEnterPatched = false;
            ReadyPlayPatched = false;
            CastEnterPatched = false;
            WaitEnterPatched = false;
            WaitPlayPatched = false;
            WaitNextStatePatched = false;
            MiniGameStartPatched = false;
            MiniGameUpdatePrefixPatched = false;
            MiniGameUpdatePatched = false;
            MiniGameStopPatched = false;
            InputUseToolPatched = false;
            InputUseToolInProgressPatched = false;
            InputUseItemPatched = false;
            InputUseItemInProgressPatched = false;
            InputFishingPatched = false;
            InputFishingInProgressPatched = false;
            FishRodCastHookPatched = false;
            FishRodPullPatched = false;
            FishRodPullCancelPatched = false;
            PullEnterPatched = false;
            PullExitPatched = false;
            BaseExitPatched = false;
        }

        private static void AppendMissing(System.Text.StringBuilder target, bool installed, string id)
        {
            if (installed)
                return;
            if (target.Length > 0)
                target.Append(',');
            target.Append(id);
        }

        private void PublishStatuses()
        {
            string status = HooksReady ? "experimental" : ownerCleanupFailed ? "failed" : string.IsNullOrWhiteSpace(lastInstallFailure) ? "pending" : "blocked";
            string details = HooksReady
                ? "Only an active consumer of the frozen IFishingAutomationApi compatibility facade requested these legacy native fishing hooks. Managed Advanced CodeMods use separate canonical Harmony inventories and are rejected on target collision. Compatibility Harmony owner=" + HarmonyOwner + "."
                : string.IsNullOrWhiteSpace(lastInstallFailure)
                    ? "The frozen IFishingAutomationApi compatibility route is waiting for its legacy targets; it is not the AutoFishing product implementation."
                    : lastInstallFailure;
            runtime.SetHookStatus("Fishing.Automation", status, "frozen IFishingAutomationApi compatibility owner", details);
        }
    }
}
