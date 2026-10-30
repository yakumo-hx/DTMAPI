using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.Config;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.MoreEquipmentSlotsShortProbe;

public sealed class ModEntry : DtmMod, IDisposable
{
    private const string Owner = "DTMAPI.Internal.MoreEquipmentSlotsShortProbe.Native";
    private IDtmHelper helper = null!;
    private readonly Harmony harmony = new Harmony(Owner);
    private IEnumerator<int>? run;
    private object runtime = null!;
    private string phase = "", state = "", fixture = "";
    private bool finished, oldPause, paused, allowSave;
    private float deadline;
    private int loads, saves;
    private static ModEntry? self;
    private static int detectorChecks;
    private static readonly Dictionary<object, int> removals = new Dictionary<object, int>();
    private IDictionary Functions => (IDictionary)Read(DolocAPI.archiveHandle.farmData.agentData.agentEquipment, "functions")!;
    private IList Slots => (IList)Read(runtime, "workingSlots")!;
    private string ExpectedPath => Path.Combine(state, "mes-instance-expected.txt");

    public override void Entry(IDtmHelper helper)
    {
        this.helper = helper;
        phase = Environment.GetEnvironmentVariable("DTMAPI_MES_STATE_PHASE") ?? "";
        fixture = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") ?? "";
        state = Environment.GetEnvironmentVariable("DTMAPI_STATE_DIR") ?? "";
        Check(new[] { "exercise", "cold", "recover", "recovered" }.Contains(phase), "explicit bounded phase=" + phase);
        Check(File.Exists(Path.Combine(fixture, ".dtmapi-disposable-save-fixture.json")) &&
              Path.GetFullPath(state) == Path.GetFullPath(Path.Combine(fixture, "DTMAPI")), "isolated fixture marker and state root");
        self = this;
        harmony.Patch(AccessTools.Method(typeof(DolocAPI), "SaveGame"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(SaveGuard)));
        harmony.Patch(AccessTools.Method(typeof(AgentEquipmentFunctionHiddenRoomDetector), "CheckHasUnvisitedHiddenArea"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(Detect)));
        harmony.Patch(AccessTools.Method(typeof(AgentEquipmentFunction), "OnRemove"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(Removing)));
        helper.Events.Save.SaveLoaded += Loaded;
        helper.Events.Save.SaveSaved += Saved;
        helper.Events.GameLoop.UpdateTicked += Tick;
        deadline = Time.realtimeSinceStartup + 116;
        Log("ARMED slot=4; phase=" + phase);
    }
    private static bool SaveGuard(ref bool __result)
    {
        if (self?.allowSave == true) return true;
        self?.Log("FAIL unexpected native save"); __result = false; return false;
    }
    private static void Detect() => detectorChecks++;
    private static void Removing(object __instance) => removals[__instance] = removals.TryGetValue(__instance, out int n) ? n + 1 : 1;
    private void Saved(object? sender, SaveSavedEventArgs e) { saves++; Log("SaveSaved=" + saves); }
    private void Loaded(object? sender, SaveLoadedEventArgs e)
    {
        Check(e.SaveSlot == 4, "loaded intended native slot");
        loads++;
        oldPause = DolocAPI.gameLoop.IsGlobalPaused;
        paused = true; DolocAPI.gameLoop.IsGlobalPaused = true;
        if (run == null) run = Exercise();
    }
    private void Tick(object? sender, UpdateTickedEventArgs e)
    {
        if (finished) return;
        try
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("Probe deadline");
            if (run != null && !run.MoveNext()) { run.Dispose(); run = null; finished = true; }
        }
        catch (Exception ex)
        {
            Log("FAIL " + ex); run?.Dispose(); run = null; finished = true;
            RestoreClock(); if (DolocAPI.IsDataLoaded) DolocAPI.ReturnHome(); Application.Quit();
        }
    }
    private IEnumerable<int> Ready()
    {
        float by = Time.realtimeSinceStartup + 30;
        while (!DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.agent == null || !DolocAPI.agent.gameObject.activeInHierarchy)
        { if (Time.realtimeSinceStartup > by) throw new Exception("World readiness"); yield return 0; }
        foreach (int x in Wait(.5f)) yield return x;
    }
    private IEnumerator<int> Exercise()
    {
        foreach (int x in Ready()) yield return x;
        object handler = Read(DolocAPI.dataPersistenceManager, "fileDataHandler")!;
        string nativePath = (string)Call(handler, "GetDataFullPath", 4)!;
        Check(Path.GetDirectoryName(Path.GetFullPath(nativePath)) == Path.GetFullPath(Path.Combine(fixture, "SAVE")), "native LocalSave points into disposable SAVE");
        if (phase == "exercise" || phase == "cold")
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "DTMAPI.MoreEquipmentSlots");
            runtime = AccessTools.Field(assembly.GetType("DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks", true), "runtime").GetValue(null)!;
            Check(runtime != null, "current Product runtime active");
        }
        if (phase == "exercise")
        {
            Check(Slots.Cast<object>().All(s => string.IsNullOrEmpty((string)Read(s, "ItemId")!)), "isolated baseline has no product assets");
            var radiation = FindEffect<AgentEquipmentFunctionImmuneRadiation>();
            string buff = DolocAPI.GlobalParameter.BuffTableRadiation.First();
            Check(DolocAPI.BuffManager.Add(buff) && DolocAPI.BuffManager.HasBuff(buff), "native radiation buff established");
            Equip(radiation, 0);
            Check(!DolocAPI.BuffManager.HasBuff(buff), "native radiation OnCreate removes the actual buff");
            var detector = FindEffect<AgentEquipmentFunctionHiddenRoomDetector>();
            int before = detectorChecks;
            Equip(detector, 1);
            object detectorFunction = Functions[WorkingItem(1)]!;
            Check(detectorChecks == before + 1, "native detector OnCreate executes room check once");
            Unequip(0);
            Refresh();
            Check(ReferenceEquals(Functions[WorkingItem(1)], detectorFunction) && detectorChecks == before + 1,
                "unrelated slot and refresh retain detector instance and timer");
            Unequip(1);
            Check(removals.TryGetValue(detectorFunction, out int removed) && removed == 1, "native detector OnRemove paired once");

            var shield = (ItemHatShield)FindItem("ItemFunctionHatShield");
            shield.SetShieldValue(3); Equip(shield, 0);
            var herb = (ItemHerbPackage)FindItem("ItemFunctionHerbPackage"); Equip(herb, 1);
            var herbFunction = (AgentEquipmentFunctionHerbPackage)Functions[WorkingItem(1)]!;
            Check(herbFunction.TryResistFaint(false) && !((ItemHerbPackage)WorkingItem(1)).isFilled && !herbFunction.TryResistFaint(false), "native herb consumption changes the equipped instance once");
            var revival = (ItemCooldown)FindEffect<AgentEquipmentFunctionVultureBadge>(); Equip(revival, 2);
            var revivalFunction = (AgentEquipmentFunctionVultureBadge)Functions[WorkingItem(2)]!;
            Check(revivalFunction.TryResistFatalInjury() && ((ItemCooldown)WorkingItem(2)).cdDuration > 0 && !revivalFunction.TryResistFatalInjury(), "native revival consumes availability on the equipped instance");
            int cooldown = ((ItemCooldown)WorkingItem(2)).cdDuration;
            Refresh();
            Check(ReferenceEquals(Functions[WorkingItem(1)], herbFunction) && ReferenceEquals(Functions[WorkingItem(2)], revivalFunction), "refresh retains both consumed effects");
            string[] expected = { shield.name, herb.name, revival.name, cooldown.ToString() };
            File.WriteAllLines(ExpectedPath, expected);
            for (int i = 0; i < 3; i++)
            {
                Unequip(i);
                Item returned = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll().First(p => p != null && p.name == expected[i]);
                CheckState(returned, i, cooldown);
                Check((bool)Call(runtime, "EquipFromBackpack", returned.name, i)!, "re-equipped returned instance " + i);
            }
            Save();
            ((ItemHatShield)WorkingItem(0)).SetShieldValue(1);
            ((ItemHerbPackage)WorkingItem(1)).SetFilled(true);
            ((ItemCooldown)WorkingItem(2)).cdDuration = 0;
            Refresh();
            RestoreClock(); DolocAPI.ReturnHome();
            foreach (int x in Wait(1)) yield return x;
            Check(((string)Call(runtime, "BuildLifecycleSummary", null, null)!).Contains(";roots=0"), "no-save title releases native item roots");
        }
        if (phase == "exercise")
        {
            // Reload without any further native save: committed equipment must win.
            DolocAPI.userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
            Check(DolocAPI.LoadGame(4), "reload after unsaved native state mutation");
            foreach (int x in Ready()) yield return x;
        }
        string[] values = File.ReadAllLines(ExpectedPath);
        int expectedCooldown = int.Parse(values[3]);
        if (phase == "exercise" || phase == "cold")
        {
            for (int i = 0; i < 3; i++) CheckState(WorkingItem(i), i, expectedCooldown);
            Log("PASS committed state after " + (phase == "exercise" ? "no-save reload" : "cold process"));
        }
        else
        {
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "DTMAPI.MoreEquipmentSlots"), "disabled Product assembly is absent");
            if (phase == "recover") Save();
            foreach (var pair in values.Take(3).Select((id, i) => new { id, i }))
            {
                Item[] items = DolocAPI.archiveHandle.InventorySystem.inventory.ReadAll().Where(p => p != null && p.name == pair.id).ToArray();
                Check(items.Length == 1, "cold Host exact destination count=" + pair.id);
                CheckState(items[0], pair.i, expectedCooldown);
            }
        }
        RestoreClock(); DolocAPI.ReturnHome();
        foreach (int x in Wait(1.5f)) yield return x;
        if (runtime != null) Check(((string)Call(runtime, "BuildLifecycleSummary", null, null)!).Contains(";roots=0"), "title releases all native item roots");
        Log("PASS phase=" + phase + "; loads=" + loads + "; saves=" + saves);
        Application.Quit();
    }
    private void Save()
    {
        int before = saves; allowSave = true;
        try { Check(DolocAPI.SaveGame(4) && saves == before + 1, "native save succeeded with observed SaveSaved"); }
        finally { allowSave = false; }
    }
    private void Refresh() => Call(runtime, "ApplyStoredFunctions", "instance acceptance");
    private Item WorkingItem(int i) => (Item)Call(runtime, "GetWorkingItem", Slots[i])!;
    private void Unequip(int i) => Check((bool)Call(runtime, "RequestUnequip", i)!, "native unequip " + i);
    private void Equip(Item item, int index)
    {
        // The isolated copy may already contain the selected native item ID.
        // Establish a single test instance before asserting exact destinations.
        var inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
        foreach (Item existing in inventory.ReadAll().Where(p => p != null && p.name == item.name).ToArray())
            existing.CostSelfFromInventory(inventory, false);
        Check(DolocAPI.TryPlaceInBackpack(item, false), "native fixture backpack admission " + item.name);
        Check((bool)Call(runtime, "EquipFromBackpack", item.name, index)!, "equip " + item.name);
    }
    private Item FindItem(string function) => DolocAPI.GenerateItem(DolocConfig.Tables.TbItem.DataList.First(p => p.Function.GetType().Name == function), 1);
    private Item FindEffect<T>() where T : AgentEquipmentFunction
    {
        foreach (var proto in DolocConfig.Tables.TbItem.DataList.Where(p => p.Function.GetType().Name.StartsWith("ItemFunctionHat") || p.Function.GetType().Name == "ItemFunctionPassive" || p.Function.GetType().Name == "ItemFunctionCooldown"))
        {
            object f = proto.Function;
            string skill = (string)(Read(Read(f, "HatId_Ref"), "Skill") ?? Read(f, "Skill") ?? "");
            Item item = DolocAPI.GenerateItem(proto, 1);
            if (AgentEquipmentFunction.CreateAgentEquipmentFunction(item, DolocAPI.archiveHandle.farmData.agentData.agentEquipment, skill, out var effect) && effect is T) return item;
        }
        throw new Exception("Native fixture missing " + typeof(T).Name);
    }
    private void CheckState(Item item, int index, int cooldown)
    {
        Check(index == 0 ? item is ItemHatShield s && s.ShieldValue == 3 :
            index == 1 ? item is ItemHerbPackage h && !h.isFilled : item is ItemCooldown c && c.cdDuration == cooldown,
            "exact native state retained slot=" + index + "; item=" + item.name + "; cooldown=" + (item is ItemCooldown cd ? cd.cdDuration : -1));
    }
    private static object? Read(object? obj, string name) => obj == null ? null :
        AccessTools.Property(obj.GetType(), name)?.GetValue(obj, null) ?? AccessTools.Field(obj.GetType(), name)?.GetValue(obj);
    private static object? Call(object obj, string method, params object?[] args) => AccessTools.Method(obj.GetType(), method).Invoke(obj, args);
    private static IEnumerable<int> Wait(float seconds) { float end = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < end) yield return 0; }
    private void Check(bool ok, string message) { if (!ok) throw new Exception(message); Log("PASS " + message); }
    private void Log(string message) => Monitor.Log("MESStateProbe " + message);
    private void RestoreClock() { if (paused) { DolocAPI.gameLoop.IsGlobalPaused = oldPause; paused = false; } }
    public void Dispose()
    {
        // Runtime owns event removal and closes that helper before Dispose.
        var errors = new List<Exception>();
        try { run?.Dispose(); run = null; } catch (Exception ex) { errors.Add(ex); }
        try { RestoreClock(); } catch (Exception ex) { errors.Add(ex); }
        try { harmony.UnpatchSelf(); } catch (Exception ex) { errors.Add(ex); }
        self = null; runtime = null!; removals.Clear();
        if (errors.Count > 0) throw new AggregateException(errors);
        Log("cleanup hooks=0;roots=0;callbacks=RuntimeOwner");
    }
}
