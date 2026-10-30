using System;
using System.Reflection;
using DolocTown;
using global::DTMAPI.Abstractions;
using HarmonyLib;
using UnityEngine;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionHookInstaller
    {
        internal const string HarmonyOwner = "Yuuka.DTMAPI.OneActionComplete.Native";
        private const string GameBridgeOwner = "dtmapi.gamebridge.doloctown";
        private readonly IMonitor monitor;
        private Harmony? harmony;
        internal OneActionHookInstaller(IMonitor monitor) => this.monitor = monitor;
        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (InstalledPatchCount == 4) return;
            MethodInfo tool = typeof(ToolCollider).GetMethod("HandleTools", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Collider2D) }, null)
                ?? throw new MissingMethodException("ToolCollider.HandleTools(Collider2D)");
            MethodInfo interact = typeof(AgentStateInteract).GetMethod("OnExit", Type.EmptyTypes)
                ?? throw new MissingMethodException("AgentStateInteract.OnExit()");
            FieldInfo field = typeof(ToolCollider).GetField("currentTool", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException("ToolCollider.currentTool");
            if (field.FieldType != typeof(ItemTool)) throw new InvalidOperationException("Native currentTool type changed.");
            Patches? owners = Harmony.GetPatchInfo(tool);
            if (owners != null && owners.Owners.Contains(GameBridgeOwner))
                throw new InvalidOperationException("Frozen action-completion compatibility already owns ToolCollider.HandleTools.");
            var toolBefore = Callback(nameof(OneActionCallbacks.ToolColliderHandleToolsPrefix));
            var toolAfter = Callback(nameof(OneActionCallbacks.ToolColliderHandleToolsPostfix));
            var interactBefore = Callback(nameof(OneActionCallbacks.AgentStateInteractExitPrefix));
            var interactAfter = Callback(nameof(OneActionCallbacks.AgentStateInteractExitPostfix));
            harmony = new Harmony(HarmonyOwner);
            try
            {
                harmony.Patch(tool, prefix: toolBefore, postfix: toolAfter);
                harmony.Patch(interact, prefix: interactBefore, postfix: interactAfter);
                InstalledPatchCount = 4;
                monitor.Log("OneActionComplete product-owned patches installed owner=" + HarmonyOwner + "; targets=2; patches=4.");
            }
            catch { UnpatchOwnedHooks(); throw; }
        }
        internal void UnpatchOwnedHooks()
        {
            harmony?.UnpatchSelf();
            InstalledPatchCount = 0;
        }
        private static HarmonyMethod Callback(string name) => new HarmonyMethod(typeof(OneActionCallbacks).GetMethod(name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(OneActionCallbacks).FullName, name));
    }
}
