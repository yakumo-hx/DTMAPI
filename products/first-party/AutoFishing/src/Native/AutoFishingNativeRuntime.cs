using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class AutoFishingNativeRuntime
    {
        private readonly FishingProductContext context;
        private readonly FishingProductHookInstaller hookInstaller;
        private FishingPrimitiveHookRuntime? primitiveHookRuntime;

        internal AutoFishingNativeRuntime(FishingProductContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Primitives = new FishingPrimitivesService(context, this);
            hookInstaller = new FishingProductHookInstaller(context);
        }

        internal FishingPrimitivesService Primitives { get; }
        internal bool HooksInstalled => hookInstaller.IsInstalled;
        internal int InstalledPatchCount => hookInstaller.InstalledPatchCount;
        internal FishingPrimitiveHookRuntime? CallbackRuntime => primitiveHookRuntime?.HasActiveRuntimeConsumer == true ? primitiveHookRuntime : null;

        internal void RecordCallbackFailure(string operation, Exception ex)
        {
            context.RuntimeMonitor.Log("AutoFishing native callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
        }

        internal void InstallHooksAtomically()
        {
            hookInstaller.InstallAtomically();
            FishingProductCallbacks.Attach(this);
        }

        internal bool CanActivatePrimitiveSession(out string reason)
        {
            if (!HooksInstalled)
            {
                reason = "AutoFishing's complete product-owned fishing patch inventory is unavailable.";
                return false;
            }
            if (primitiveHookRuntime != null)
            {
                reason = "AutoFishing already has an active native runtime.";
                return false;
            }
            reason = string.Empty;
            return true;
        }

        internal FishingPrimitiveActivationResult TryActivatePrimitiveSession(FishingPrimitivesService primitiveService)
        {
            if (!CanActivatePrimitiveSession(out string reason))
                return new FishingPrimitiveActivationResult(false, "busy", reason, null);

            FishingPrimitiveHookRuntime? candidate = null;
            try
            {
                candidate = new FishingPrimitiveHookRuntime(context, primitiveService);
                candidate.SetFishingHooksInstalled(true);
                primitiveService.AttachHookRuntime(candidate);
                primitiveHookRuntime = candidate;
                return new FishingPrimitiveActivationResult(true, "ready", "AutoFishing product-native runtime is attached.", candidate);
            }
            catch (Exception ex)
            {
                if (candidate != null)
                    RollbackPrimitiveActivation(candidate, "activation-failed:" + ex.GetType().Name);
                context.RuntimeMonitor.Log("AutoFishing native runtime attachment failed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return new FishingPrimitiveActivationResult(false, "activation-failed", "AutoFishing native runtime attachment failed: " + ex.GetType().Name + ".", null);
            }
        }

        internal void RollbackPrimitiveActivation(FishingPrimitiveHookRuntime candidate, string reason)
        {
            if (candidate == null)
                return;
            try
            {
                candidate.Reset(reason ?? string.Empty);
            }
            finally
            {
                Primitives.DetachHookRuntime(candidate);
                if (ReferenceEquals(primitiveHookRuntime, candidate))
                    primitiveHookRuntime = null;
                candidate.SetFishingHooksInstalled(false);
            }
        }

        internal void RestorePrimitiveAnimation(string reason)
        {
            primitiveHookRuntime?.RestoreExperimentalAnimatorSpeeds("product-animation-release:" + (reason ?? string.Empty));
        }

        internal void ReleasePrimitiveSession(string reason)
        {
            if (primitiveHookRuntime == null)
                return;
            FishingPrimitiveHookRuntime released = primitiveHookRuntime;
            RollbackPrimitiveActivation(released, reason ?? string.Empty);
        }

        internal void RollbackFailedEntry()
        {
            DeactivateOwner("entry-failed");
        }

        internal void DeactivateOwner(string reason)
        {
            Exception? runtimeFailure = null;
            Exception? unpatchFailure = null;
            // Cut the static callback root first so no later native callback can enter
            // product state while session/native rollback is in progress.
            FishingProductCallbacks.Detach(this);
            try
            {
                if (primitiveHookRuntime != null)
                    RollbackPrimitiveActivation(primitiveHookRuntime, reason ?? string.Empty);
            }
            catch (Exception ex)
            {
                runtimeFailure = ex;
            }

            try
            {
                hookInstaller.UnpatchOwnedHooks();
            }
            catch (Exception ex)
            {
                unpatchFailure = ex;
            }

            if (runtimeFailure != null && unpatchFailure != null)
                throw new AggregateException("AutoFishing native runtime and exact-owner Harmony cleanup both failed.", runtimeFailure, unpatchFailure);
            if (runtimeFailure != null)
                throw new InvalidOperationException("AutoFishing native runtime cleanup failed.", runtimeFailure);
            if (unpatchFailure != null)
                throw new InvalidOperationException("AutoFishing exact-owner Harmony cleanup failed.", unpatchFailure);
        }
    }

    internal readonly struct FishingPrimitiveActivationResult
    {
        internal FishingPrimitiveActivationResult(bool success, string status, string message, FishingPrimitiveHookRuntime? runtime)
        {
            Success = success;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            Runtime = runtime;
        }

        internal bool Success { get; }
        internal string Status { get; }
        internal string Message { get; }
        internal FishingPrimitiveHookRuntime? Runtime { get; }
    }
}
