using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.ActionSpeedShortProbe;

public sealed partial class ModEntry
{
    private bool lifetimeResidual;
    private MethodInfo lifetimePrefix = null!, lifetimeFinalizer = null!;
    private bool IsLifetimeRequested() =>
        Environment.GetEnvironmentVariable("DTMAPI_ACTIONSPEED_LIFETIME_PROBE") == "1" ||
        System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(closeoutCommand)!, "actionspeed-lifetime-probe.txt"));

    private void ArmLifetimeCallbacks()
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Yuuka.DTMAPI.ActionSpeed")
            .GetType("Yuuka.DTMAPI.ActionSpeed.NativeActions", true)!;
        lifetimePrefix = AccessTools.DeclaredMethod(type, "BeginOperation");
        lifetimeFinalizer = AccessTools.DeclaredMethod(type, "EndOperation");
        closeoutHarmony.Patch(AccessTools.DeclaredMethod(typeof(AffectorElectric), "OnInteract"),
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(ResidualPrefix)),
            finalizer: new HarmonyMethod(typeof(ModEntry), nameof(ResidualFinalizer)));
    }
    private static void ResidualPrefix(object __instance, MethodBase __originalMethod, out object? __state)
    {
        __state = null;
        var self = closeoutSelf;
        if (self == null || !self.lifetimeResidual) return;
        object?[] args = { __instance, __originalMethod, null };
        self.lifetimePrefix.Invoke(null, args);
        __state = args[2];
    }
    private static void ResidualFinalizer(object? __state)
    {
        if (__state != null) closeoutSelf?.lifetimeFinalizer.Invoke(null, new[] { __state });
    }

    // A bounded NoNativeSave check of the changed callback/exit boundary.
    // Fault injection and partial cleanup retries remain in the actual lifetime unit tests.
    private IEnumerator<int> ReviewLifetime()
    {
        foreach (int x in Wait(3)) yield return x;
        float deadline = Time.realtimeSinceStartup + 30;
        while (!DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.agent == null || !DolocAPI.agent.gameObject.activeInHierarchy)
        { if (Time.realtimeSinceStartup > deadline) throw new Exception("lifetime world readiness timeout"); yield return 0; }
        bool arrived = false;
        Check(DolocAPI.DoTransport(DolocAPI.gameManager.gameInitConfig.initMarkPoint, () => arrived = true), "lifetime farm transport requested");
        deadline = Time.realtimeSinceStartup + 20;
        while (!arrived || !DolocAPI.IsNormalState)
        { if (Time.realtimeSinceStartup > deadline) throw new Exception("lifetime transport timeout"); yield return 0; }
        foreach (int x in Wait(2)) yield return x;
        body = DolocAPI.agent; animator = body.GetComponent<Animator>(); baseSpeed = animator.speed;
        position = body.position2d; index = DolocAPI.SelectedItemIndex;
        originalItem = DolocAPI.archiveHandle.InventorySystem.inventory.Read(index);
        Type type = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Yuuka.DTMAPI.ActionSpeed")
            .GetType("Yuuka.DTMAPI.ActionSpeed.NativeActions", true)!;
        object owner = type.GetProperty("active", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        object settings = AccessTools.Field(type, "settings").GetValue(owner)!;
        config = ((Delegate)settings).DynamicInvoke()!;
        foreach (var property in config.GetType().GetProperties()) original.Add(property, property.GetValue(config, null)!);
        captured = true; previousPause = DolocAPI.gameLoop.IsGlobalPaused; clockCaptured = true;
        DolocAPI.gameLoop.IsGlobalPaused = true;
        Set("AutoFill", false); Set("InteractEnabled", true); Set("InteractMultiplier", 3d);
        var appliance = MakeFixture<AffectorElectric>();
        MethodInfo operation = AccessTools.DeclaredMethod(typeof(AffectorElectric), "OnInteract");
        MethodInfo prefix = AccessTools.DeclaredMethod(type, "BeginOperation");
        MethodInfo finalizer = AccessTools.DeclaredMethod(type, "EndOperation");
        const string productOwner = "Yuuka.DTMAPI.ActionSpeed.Native";
        IDisposable? replacement = null;
        try
        {
            Idle(); InvokeInteraction(appliance, typeof(AffectorElectric));
            ObserveInteraction(3, "lifetime original native operation");
            object?[] receiptArgs = { appliance, operation, null };
            prefix.Invoke(null, receiptArgs);
            ((IDisposable)owner).Dispose();
            Near(baseSpeed, animator.speed, "dispose restores active native animation");
            Check(!Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m).Owners.Contains(productOwner)), "dispose removes exact product Harmony owner");
            finalizer.Invoke(null, new[] { receiptArgs[2] });

            // The probe's forwarding callbacks were registered during Entry. Only their
            // test gate changes here; Advanced supervision stays enabled and unchanged.
            lifetimeResidual = true;
            Idle(); bool before = appliance.IsTurnOn;
            InvokeInteraction(appliance, typeof(AffectorElectric));
            ObserveInteraction(1, "residual native Prefix and Finalizer are dormant");
            foreach (int x in Wait(1.5f)) yield return x;
            Check(appliance.IsTurnOn != before, "residual callbacks preserve native electrical completion");
            lifetimeResidual = false;

            replacement = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { settings }, null)!;
            AccessTools.DeclaredMethod(type, "Install").Invoke(replacement, null);
            ((IDisposable)owner).Dispose();
            finalizer.Invoke(null, new[] { receiptArgs[2] });
            Idle(); InvokeInteraction(appliance, typeof(AffectorElectric));
            ObserveInteraction(3, "old dispose and stale receipt preserve replacement native owner");
            replacement.Dispose();
            Near(baseSpeed, animator.speed, "replacement cleanup restores native animation");
            Check(!Harmony.GetAllPatchedMethods().Any(m => Harmony.GetPatchInfo(m).Owners.Contains(productOwner)), "replacement exact owner removed");
        }
        finally
        {
            lifetimeResidual = false;
            replacement?.Dispose();
        }
        Restore(); RestoreWorldClock(); finished = true;
        DolocAPI.ReturnHome();
        deadline = Time.realtimeSinceStartup + 20;
        while (!DolocAPI.userInput.CheckState<HomePageUiState>())
        { if (Time.realtimeSinceStartup > deadline) throw new Exception("lifetime title timeout"); yield return 0; }
        Log("PASS lifetime native callback, restore, residual and replacement boundaries; shared settings entry and physical RMB acceptance remain separate");
        foreach (int x in Wait(2)) yield return x;
        Application.Quit();
    }
}
