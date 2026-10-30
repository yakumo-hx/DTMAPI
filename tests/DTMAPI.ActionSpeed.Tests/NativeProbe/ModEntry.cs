using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.ActionSpeedShortProbe;

public sealed partial class ModEntry : DtmMod, IDisposable
{
    private IDtmHelper helper = null!;
    private IEnumerator<int>? run;
    private object config = null!;
    private readonly Dictionary<PropertyInfo, object> original = new Dictionary<PropertyInfo, object>();
    private BodyController body = null!;
    private Animator animator = null!;
    private Item? originalItem;
    private int index;
    private Vector2 position;
    private bool captured;
    private bool finished;
    private bool loadRequested;
    private bool homeObserved;
    private float loadAfter;
    private float baseSpeed;
    public override void Entry(IDtmHelper helper)
    {
        this.helper = helper;
        ArmCloseout();
        if (IsLifetimeRequested()) ArmLifetimeCallbacks();
        helper.Events.Save.SaveLoaded += Loaded;
        helper.Events.GameLoop.UpdateTicked += Tick;
        loadAfter = Time.realtimeSinceStartup + 8;
        Log("armed for native slot 4, NoNativeSave; no save API is called");
    }
    private void Loaded(object? sender, SaveLoadedEventArgs e)
    {
        Log("SaveLoaded observed slot=" + e.SaveSlot);
        if (run != null || captured || finished) return;
        if (e.SaveSlot != 4) { Log("BLOCKED unexpected native slot " + e.SaveSlot); return; }
        run = Environment.GetEnvironmentVariable("DTMAPI_AS_MILK_PROBE") == "1" ? MilkAcceptance() :
            Environment.GetEnvironmentVariable("DTMAPI_AS_CONTROLS_PROBE") == "1" ? Exercise() :
            IsLifetimeRequested() ? ReviewLifetime() : Closeout();
    }
    private void Tick(object? sender, UpdateTickedEventArgs e)
    {
        PollCloseout();
        if (!loadRequested && !finished)
        {
            if (!DolocAPI.IsGameInitialized || !DolocAPI.userInput.CheckState<HomePageUiState>()) return;
            if (!homeObserved) { homeObserved = true; loadAfter = Time.realtimeSinceStartup + 3; return; }
            if (Time.realtimeSinceStartup < loadAfter) return;
            loadRequested = true;
            if (DolocAPI.IsDataLoaded) { finished = true; Log("BLOCKED a save was loaded before the probe request"); return; }
            DolocAPI.userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
            Check(DolocAPI.LoadGame(4), "requested native LoadGame(4), without saving");
        }
        if (run == null) return;
        try { if (!run.MoveNext()) { run.Dispose(); run = null; } }
        catch (Exception error) { Log("FAIL " + error); run?.Dispose(); run = null; Restore(); finished = true; }
    }
    private IEnumerator<int> Exercise()
    {
        foreach (int x in Wait(3)) yield return x;
        float readyBy = Time.realtimeSinceStartup + 30;
        while (!DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.archiveHandle.currentRoom == null || DolocAPI.agent == null || !DolocAPI.agent.gameObject.activeInHierarchy)
        { if (Time.realtimeSinceStartup >= readyBy) throw new Exception("World did not become ready within 30 seconds"); yield return 0; }
        body = DolocAPI.agent; animator = body.GetComponent<Animator>();
        baseSpeed = animator.speed; position = body.position2d; index = DolocAPI.SelectedItemIndex;
        originalItem = DolocAPI.archiveHandle.InventorySystem.inventory.Read(index);
        var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Yuuka.DTMAPI.ActionSpeed");
        Check(assembly.GetName().Version!.Major == 2, "candidate 2.0 assembly loaded");
        var runtimeType = assembly.GetType("Yuuka.DTMAPI.ActionSpeed.NativeActions", true)!;
        var runtime = runtimeType.GetProperty("active", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        config = ((Delegate)AccessTools.Field(runtimeType, "settings").GetValue(runtime)!).DynamicInvoke()!;
        foreach (var property in config.GetType().GetProperties()) original.Add(property, property.GetValue(config, null)!);
        captured = true; previousPause = DolocAPI.gameLoop.IsGlobalPaused; clockCaptured = true;
        DolocAPI.gameLoop.IsGlobalPaused = true;
        Set("AutoFill", false); Set("ToolEnabled", true); Set("ToolMultiplier", 3d);
        Set("EatEnabled", true); Set("EatMultiplier", 3d); Set("FillEnabled", true); Set("FillMultiplier", 4d);
        DolocAPI.AddEnergy(100);
        foreach (var name in new[] { "old_axe", "old_pickaxe", "old_sickle" })
        {
            Idle(); Select(name, 1); animator.speed = 0.8f;
            DolocAPI.gameStateManager.agentController.UseTool();
            Check(body.StateManager.current is AgentStateTool, name + " entered native tool state");
            Near(2.4f, animator.speed, name + " body speed");
            Near(3f, body.ToolRenderer.GetComponent<Animator>().speed, name + " tool speed");
            foreach (int x in Wait(1.2f)) yield return x;
            Near(0.8f, animator.speed, name + " completed and restored");
            animator.speed = baseSpeed;
        }
        Idle(); Select("old_watercan", 1);
        var can = DolocAPI.SelectedItem as ItemWaterCan;
        if (can == null) throw new Exception("Watering-can item id unavailable");
        else
        {
            can.SupplyToFull(); int before = can.currentDurability;
            DolocAPI.gameStateManager.agentController.UseTool();
            Check(body.StateManager.current is AgentStateWater, "full can chooses native watering");
            Near(baseSpeed * 3, animator.speed, "watering uses tool multiplier, not fill 4x");
            Check(can.currentDurability == before - 1, "native watering consumes one water");
            foreach (int x in Wait(1.2f)) yield return x;
            Near(baseSpeed, animator.speed, "watering completion restores");
        }
        Idle(); Select("bread", 8); DolocAPI.gameStateManager.agentController.UseItem();
        Check(body.StateManager.current is AgentStateEat, "native food state");
        Near(baseSpeed * 3, animator.speed, "food animation accelerated");
        foreach (int x in Wait(1)) yield return x;
        Near(baseSpeed, animator.speed, "food animation restored");
        var controller = DolocAPI.gameStateManager.agentController;
        var continues = AccessTools.MethodDelegate<Action<AgentControllerState, float>>(AccessTools.Method(typeof(AgentControllerState), "UseItemContinues"));
        var timer = (DolocTown.Timer)AccessTools.Field(typeof(AgentControllerState), "useItemTimer").GetValue(controller)!;
        var timeField = AccessTools.Field(typeof(DolocTown.Timer), "currentTime");
        var intervalField = AccessTools.Field(typeof(DolocTown.Timer), "currentInterval");
        Set("ContinuousDrink", true);
        timer.ReStart(); continues(controller, 0.05f);
        Near(0.05f, (float)timeField.GetValue(timer)!, "ordinary food keeps native timer delta");
        Idle(); Select(DolocAPI.GlobalParameter.ItemRefBottleOfWater, 12);
        Set("EatEnabled", false); timer.ReStart(); continues(controller, 0.05f);
        Near(0.15f, (float)timeField.GetValue(timer)!, "bottled water alone gets 3x timer delta");
        Set("ContinuousDrink", false); timer.ReStart(); continues(controller, 0.05f);
        Near(0.05f, (float)timeField.GetValue(timer)!, "water cadence off retains native delta");
        Set("ContinuousDrink", true); timer.ReStart();
        var repeatsField = AccessTools.Field(typeof(DolocTown.Timer), "currentRepeatCount");
        int last = 0; float deadline = Time.realtimeSinceStartup + 15;
        while (last < 6 && Time.realtimeSinceStartup < deadline)
        {
            continues(controller, Time.deltaTime);
            int count = (int)repeatsField.GetValue(timer)!;
            if (count > last)
            {
                last = count;
                Near(count < 3 ? 0.5f : count < 5 ? 0.3f : 0.2f, (float)intervalField.GetValue(timer)!, "native segmented interval " + count);
                Near(baseSpeed, animator.speed, "cadence does not enable eating animation " + count);
            }
            yield return 0;
        }
        Check(last >= 6, "native held-use method reaches stable short segment");
        foreach (int x in Wait(1)) yield return x;
        // A pond's visible water surface alone does not establish native water contact.
        bool arrived = false;
        Check(DolocAPI.DoTransport("小林地-左端", () => arrived = true), "native transport to woodland water fixture");
        float arriveBy = Time.realtimeSinceStartup + 20;
        while (!arrived || !DolocAPI.IsNormalState)
        { if (Time.realtimeSinceStartup >= arriveBy) throw new Exception("Woodland transport did not finish"); yield return 0; }
        foreach (int x in Wait(2)) yield return x;
        var waters = UnityEngine.Object.FindObjectsOfType<InteractiveWater>();
        Log("woodland active water surfaces=" + waters.Length);
        if (waters.Length == 0) throw new Exception("No active water surface in this room");
        else
        {
            var water = waters.OrderBy(w => Vector2.Distance(position, w.transform.position)).First();
            var collider = water.GetComponent<Collider2D>();
            if (collider == null) throw new Exception("Selected water has no direct collider");
            else
            {
                Idle(); body.position2d = collider.bounds.center;
                foreach (int x in Wait(2)) yield return x;
                Log("water-position=" + body.position2d + ", nativeInWater=" + DolocAPI.IsAgentInWater);
                if (DolocAPI.IsAgentInWater)
                {
                    Idle(); Select(DolocAPI.GlobalParameter.ItemRefWastePlasticBottle, 5);
                    Set("AutoFill", false); Set("StrongAutoFill", true);
                    foreach (int x in Wait(0.6f)) yield return x;
                    Check(DolocAPI.SelectedItem.count == 5 && !(body.StateManager.current is AgentStateInteract), "individual auto-fill off prevents native bottling despite strong mode");
                    foreach (bool speed in new[] { false, true })
                    {
                        Idle(); Select(DolocAPI.GlobalParameter.ItemRefWastePlasticBottle, 5);
                        Set("FillEnabled", speed); Set("AutoFill", true); Set("StrongAutoFill", speed);
                        float end = Time.realtimeSinceStartup + 6; int observations = 0;
                        while (Time.realtimeSinceStartup < end && (DolocAPI.SelectedItem?.count ?? 0) > 2)
                        {
                            if (body.StateManager.current is AgentStateInteract)
                            { if (observations == 0) Near(baseSpeed * (speed ? 4 : 1), animator.speed, "auto-fill speed flag=" + speed); observations++; }
                            yield return 0;
                        }
                        Set("AutoFill", false);
                        Check(observations > 0 && (DolocAPI.SelectedItem?.count ?? 0) <= 2, "automatic native bottling repeats, fill-speed=" + speed);
                        foreach (int x in Wait(1)) yield return x;
                    }
                }
                else throw new Exception("Native water touch did not become eligible after reposition");
            }
        }
        Restore(); RestoreWorldClock();
        finished = true;
        Log("PASS independent controls: native tool/eat/water clocks, drinking on/off and automatic filling on/off; physical held-right-click has separate evidence.");
        DolocAPI.QuitCurrentRoom(null!); DolocAPI.ReturnHome();
        float titleBy = Time.realtimeSinceStartup + 15;
        while (!DolocAPI.userInput.CheckState<HomePageUiState>())
        { if (Time.realtimeSinceStartup >= titleBy) throw new Exception("Native return to title did not complete"); yield return 0; }
        foreach (int x in Wait(2)) yield return x;
        helper.UI.OpenConfigPage("Yuuka.DTMAPI.ActionSpeed");
        Log("opened product config page for visual inspection through the public UI API");
    }
    private void Idle() { body.StateManager.Overwrite<AgentStateIdle>(); body.Status.Velocity = Vector2.zero; }
    private void Select(string name, int count)
    {
        var inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
        inventory.Take(index);
        var item = DolocAPI.GenerateItem(name, count) ?? throw new Exception("Native item unavailable: " + name);
        inventory.PlaceItemAt(index, item); DolocAPI.RefreshQuickInventorySelected();
    }
    private void Set(string property, object value) => config.GetType().GetProperty(property)!.SetValue(config, value, null);
    private static IEnumerable<int> Wait(float seconds)
    { float end = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < end) yield return 0; }
    private void Near(float expected, float actual, string message) => Check(Math.Abs(expected - actual) < 0.001f, message + " expected=" + expected + " actual=" + actual);
    private void Check(bool success, string message) { if (!success) throw new Exception(message); Log("OK " + message); }
    private void Log(string message) => Monitor.Log("ACTION-REBUILD " + message, LogLevel.Info);
    private void Restore()
    {
        if (!captured || !DolocAPI.IsDataLoaded) return;
        Set("AutoFill", false); Idle();
        foreach (var pair in original) pair.Key.SetValue(config, pair.Value, null);
        body.position2d = position; animator.speed = baseSpeed;
        var inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
        inventory.Take(index); if (originalItem != null) inventory.PlaceItemAt(index, originalItem.Clone(originalItem.count));
        DolocAPI.RefreshQuickInventorySelected(); original.Clear(); captured = false;
    }
    public void Dispose()
    {
        var errors = new List<Exception>();
        try { run?.Dispose(); run = null; } catch (Exception ex) { errors.Add(ex); }
        try { Restore(); } catch (Exception ex) { errors.Add(ex); }
        try { RestoreWorldClock(); } catch (Exception ex) { errors.Add(ex); }
        try { closeoutHarmony.UnpatchSelf(); } catch (Exception ex) { errors.Add(ex); }
        closeoutSelf = null;
        if(errors.Count > 0) throw new AggregateException(errors);
    }
}
