using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.ActionSpeed
{
    internal sealed class ActionSpeedNativeRuntime
    {
        private readonly ActionSpeedEngine engine;
        private readonly ActionSpeedHookInstaller hooks;

        internal ActionSpeedNativeRuntime(IDtmHelper helper)
        {
            engine = new ActionSpeedEngine(helper ?? throw new ArgumentNullException(nameof(helper)));
            hooks = new ActionSpeedHookInstaller(helper.Monitor);
        }

        internal int InstalledPatchCount => hooks.InstalledPatchCount;
        internal string LifecycleSummary => engine.GetActionSpeedLifecycleSummary() + ", patches=" + hooks.InstalledPatchCount;

        internal void InstallHooksAtomically()
        {
            hooks.InstallAtomically();
            try { ActionSpeedCallbacks.Attach(this); }
            catch
            {
                hooks.UnpatchOwnedHooks();
                throw;
            }
        }

        internal void Configure(ActionSpeedConfig config) => engine.Configure(config);
        internal void Update() => engine.Update();
        internal void ResetBoundary(string reason) => engine.ResetBoundary(reason);
        internal void Restore(string reason) => engine.RestoreActionSpeed(reason);
        internal void ApplyToolEnter(object state) => engine.ApplyActionSpeedToolEnter(state);
        internal void ApplyInteractEnter(object state) => engine.ApplyActionSpeedInteractEnter(state);
        internal void ApplyEatEnter(object state) => engine.ApplyActionSpeedEatEnter(state);
        internal void AdjustUseItemContinues(ref float dt) => engine.AdjustActionSpeedUseItemContinuesDelta(ref dt);
        internal void AdjustInteractContinues(ref float dt) => engine.AdjustActionSpeedInteractContinuesDelta(ref dt);
        internal void MarkAnimalInteract(object target) => engine.MarkNativeAnimalInteract(target);

        internal void RecordCallbackFailure(string operation, Exception ex) =>
            engine.Monitor.Log("ActionSpeed native callback failed closed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);

        internal void DeactivateOwner(string reason)
        {
            Exception? runtimeFailure = null;
            Exception? unpatchFailure = null;
            ActionSpeedCallbacks.Detach(this);
            try
            {
                engine.ResetBoundary(reason ?? string.Empty);
            }
            catch (Exception ex)
            {
                runtimeFailure = ex;
            }
            finally
            {
                engine.DisableQaObservation();
            }

            try
            {
                hooks.UnpatchOwnedHooks();
            }
            catch (Exception ex)
            {
                unpatchFailure = ex;
            }

            if (runtimeFailure != null && unpatchFailure != null)
                throw new AggregateException("ActionSpeed native restoration and exact-owner Harmony cleanup both failed.", runtimeFailure, unpatchFailure);
            if (runtimeFailure != null)
                throw new InvalidOperationException("ActionSpeed native restoration failed.", runtimeFailure);
            if (unpatchFailure != null)
                throw new InvalidOperationException("ActionSpeed exact-owner Harmony cleanup failed.", unpatchFailure);
        }
    }
}
