using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionNativeRuntime
    {
        private readonly OneActionEngine engine;
        private readonly OneActionHookInstaller hooks;

        internal OneActionNativeRuntime(IDtmHelper helper)
        {
            engine = new OneActionEngine(helper ?? throw new ArgumentNullException(nameof(helper)));
            hooks = new OneActionHookInstaller(helper.Monitor);
        }

        internal int InstalledPatchCount => hooks.InstalledPatchCount;

        internal void InstallHooksAtomically()
        {
            hooks.InstallAtomically();
            try
            {
                OneActionCallbacks.Attach(this);
            }
            catch
            {
                hooks.UnpatchOwnedHooks();
                throw;
            }
        }

        internal void Configure(OneActionConfig config) => engine.Configure(config);
        internal void ResetBoundary(string reason) => engine.ResetBoundary(reason);
        internal void ApplyToolHit(object toolCollider, object collider) => engine.ApplyToolHit(toolCollider, collider);
        internal void ApplyEquipmentFill() => engine.ApplyEquipmentFillAfterInteract();

        internal void RecordCallbackFailure(string operation, Exception ex) =>
            engine.Monitor.Log("OneActionComplete native callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);

        internal void DeactivateOwner(string reason)
        {
            Exception? runtimeFailure = null;
            Exception? unpatchFailure = null;
            OneActionCallbacks.Detach(this);
            try
            {
                engine.ResetBoundary(reason ?? string.Empty);
            }
            catch (Exception ex)
            {
                runtimeFailure = ex;
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
                throw new AggregateException("OneActionComplete native restoration and exact-owner Harmony cleanup both failed.", runtimeFailure, unpatchFailure);
            if (runtimeFailure != null)
                throw new InvalidOperationException("OneActionComplete native restoration failed.", runtimeFailure);
            if (unpatchFailure != null)
                throw new InvalidOperationException("OneActionComplete exact-owner Harmony cleanup failed.", unpatchFailure);
        }
    }
}
