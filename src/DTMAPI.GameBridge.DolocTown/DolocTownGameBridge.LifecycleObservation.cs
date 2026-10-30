using System;
using System.Reflection;
using System.Threading;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private int lifecycleObservationSequence;
        private int pendingLifecycleObservationFrames;

        internal void ObserveSaveLoadedNativeState()
        {
            lifecycleObservationSequence++;
            pendingLifecycleObservationFrames = 600;
            ObserveNativeState("SaveLoaded.Callback");
        }

        internal void ObserveFirstNativeWorldFrame()
        {
            if (pendingLifecycleObservationFrames <= 0) return;
            int elapsedFrames = 601 - pendingLifecycleObservationFrames--;
            if (elapsedFrames == 1 || elapsedFrames == 60 || elapsedFrames == 300 || elapsedFrames == 600)
                if (ObserveNativeState("NormalGameState.OnUpdate.Postfix+" + elapsedFrames)) pendingLifecycleObservationFrames = 0;
        }

        private bool ObserveNativeState(string boundary)
        {
            try
            {
                Type? api = Type.GetType("DolocAPI, Assembly-CSharp", false);
                if (api == null) return false;
                object? Read(string name) => api.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
                bool data = Read("IsDataLoaded") is bool loaded && loaded;
                bool normal = Read("IsNormalState") is bool state && state;
                bool archive = Read("archiveHandle") != null;
                bool agent = Read("agent") != null;
                bool room = Read("CurrentRoom") != null;
                runtime.RuntimeMonitor.Log("Lifecycle.NativeState boundary=" + boundary + "; observation=" + lifecycleObservationSequence +
                    "; thread=" + Thread.CurrentThread.ManagedThreadId + "; saveGeneration=" + runtime.ResourceLifecycleSaveGeneration +
                    "; dataLoaded=" + data + "; normalState=" + normal + "; archivePresent=" + archive +
                    "; agentPresent=" + agent + "; roomPresent=" + room + "; note=observed-presence-not-public-world-ready");
                return data && normal && archive && agent && room;
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("Lifecycle.NativeState unavailable boundary=" + boundary + "; error=" + ex.GetType().Name);
                return false;
            }
        }
    }
}
