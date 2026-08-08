using System;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private Action? applicationQuitOverrideForTests;

        internal Action? ApplicationQuitOverrideForTests
        {
            set => applicationQuitOverrideForTests = value;
        }

        internal void RequestApplicationQuitForTests(string reason) => RequestApplicationQuitFromQaHost(reason);

        internal void RetainQaHostAndRequestApplicationQuitAfterRuntimeStartFailure(string reason)
        {
            if (preparedQaHost == null || qaHostClosed)
                return;
            qaHostFailureObserved = true;
            RetainQaHostForProcessExit("failure:runtime-start-entered:" + (reason ?? string.Empty));
            RequestApplicationQuitWithoutQaHostClose(
                "Runtime Start entered before bootstrap failure; optional QA pre-Runtime owner retained until process shutdown; errorType=" +
                (reason ?? string.Empty));
        }

        private void RequestReturnHomeFromQaHost()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? returnHome = GameBridgeNativeHelpers.FindMethodInHierarchy(dolocApi, "ReturnHome", 1);
            if (returnHome == null)
                throw new MissingMethodException("DolocAPI.ReturnHome(bool) was not found for the optional fixture participant.");

            runtime.RuntimeMonitor.Log("Optional QA participant requesting DolocAPI.ReturnHome after completing its in-save requirements.");
            returnHome.Invoke(null, new object[] { false });
        }

        private void RequestApplicationQuitFromQaHost(string reason)
        {
            if (!qaHostPreRuntimePrepared || !qaHostFailureObserved)
                CloseQaHostBeforeFixtureExit(reason);
            RequestApplicationQuitWithoutQaHostClose(reason);
        }

        private void RequestApplicationQuitWithoutQaHostClose(string reason)
        {
            if (qaHostApplicationQuitRequested)
                return;
            runtime.RuntimeMonitor.Log("Optional QA participant requesting game quit: " + (reason ?? string.Empty));
            if (applicationQuitOverrideForTests != null)
            {
                applicationQuitOverrideForTests();
                qaHostApplicationQuitRequested = true;
                return;
            }
            Type? application = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Application, UnityEngine");
            MethodInfo? quit = application?.GetMethod("Quit", Type.EmptyTypes);
            if (quit == null)
                throw new MissingMethodException("UnityEngine.Application.Quit() was not found.");
            quit.Invoke(null, null);
            qaHostApplicationQuitRequested = true;
        }

    }
}
