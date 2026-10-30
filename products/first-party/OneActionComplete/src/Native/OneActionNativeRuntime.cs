using System;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionNativeRuntime
    {
        private readonly OneActionHookInstaller hooks;
        internal OneActionNativeRuntime(IMonitor monitor)
        {
            Engine = new OneActionEngine(monitor);
            hooks = new OneActionHookInstaller(monitor);
        }
        internal OneActionEngine Engine { get; }
        internal int InstalledPatchCount => hooks.InstalledPatchCount;
        internal void Configure(OneActionConfig config) => Engine.Configure(config);
        internal void InstallHooksAtomically()
        {
            hooks.InstallAtomically();
            try { OneActionCallbacks.Attach(this); }
            catch { hooks.UnpatchOwnedHooks(); throw; }
        }
        internal void DeactivateOwner()
        {
            OneActionCallbacks.Detach(this);
            hooks.UnpatchOwnedHooks();
        }
        internal void RecordCallbackFailure(string operation, Exception ex) =>
            Engine.Monitor.Log("OneActionComplete native callback failed operation=" + operation + "; error=" + ex.Message, LogLevel.Warn);
    }
}
