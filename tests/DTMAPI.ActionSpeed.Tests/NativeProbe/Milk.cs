using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DolocTown;
using DolocTown.Config;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.ActionSpeedShortProbe;

public sealed partial class ModEntry
{
    private IEnumerator<int> MilkAcceptance()
    {
        float readyBy = Time.realtimeSinceStartup + 30;
        while (!DolocAPI.IsNormalState || DolocAPI.agent == null || !DolocAPI.agent.gameObject.activeInHierarchy)
        { if (Time.realtimeSinceStartup > readyBy) throw new Exception("Milk world readiness timeout"); yield return 0; }
        body = DolocAPI.agent; animator = body.GetComponent<Animator>(); baseSpeed = animator.speed;
        position = body.position2d; index = DolocAPI.SelectedItemIndex;
        originalItem = DolocAPI.archiveHandle.InventorySystem.inventory.Read(index);
        var type = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Yuuka.DTMAPI.ActionSpeed").GetType("Yuuka.DTMAPI.ActionSpeed.NativeActions", true)!;
        object runtime = type.GetProperty("active", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        config = ((Delegate)AccessTools.Field(type, "settings").GetValue(runtime)!).DynamicInvoke()!;
        foreach (var property in config.GetType().GetProperties()) original.Add(property, property.GetValue(config, null)!);
        captured = true; previousPause = DolocAPI.gameLoop.IsGlobalPaused; clockCaptured = true;
        DolocAPI.gameLoop.IsGlobalPaused = true;
        var host = (IAnimalHost)DolocAPI.CurrentRoom;
        var animal = new Animal(DolocConfig.Tables.TbAnimal.GetOrDefault("marsh_pangolin"), DolocAPI.archiveHandle.DateNow);
        Check(host.AddAnimal(animal, DolocAPI.AgentRoomCellPosition), "native animal host admitted a temporary marsh pangolin");
        animal.DEBUG_SetAdult(true); animal.Mood = 100;
        Animal previousAnimal = DolocAPI.CurrentAnimal;
        try
        {
            Set("AutoFill", false); Set("InteractMultiplier", 3d);
            Set("GatherMultiplier", 4d); Set("FillMultiplier", 5d);
            var collect = AccessTools.Method(typeof(ItemBottle), "CollectMilkFromAnimal");
            string bottleId = DolocConfig.Tables.TbItem.DataList.First(p => p.Function.GetType().Name == "ItemFunctionBottle").Id;
            foreach (bool enabled in new[] { false, true })
            {
                Idle(); Select(bottleId, 6); animal.Metabolism = 100; DolocAPI.CurrentAnimal = animal;
                Set("InteractEnabled", enabled); int milkBefore = CountMilk();
                Check((bool)collect.Invoke(DolocAPI.SelectedItem, null)!, "native milk eligibility succeeds enabled=" + enabled);
                ObserveInteraction(enabled ? 3 : 1, "milk enabled=" + enabled);
                foreach (int x in Wait(1.5f)) yield return x;
                Check(DolocAPI.SelectedItem.count == 5 && animal.Metabolism == 0 && CountMilk() == milkBefore + 1,
                    "native milk completion consumes one bottle, produces one milk and exhausts availability");
                Near(baseSpeed, animator.speed, "milk completion restores baseline");
                Idle(); DolocAPI.CurrentAnimal = animal;
                Check(!(bool)collect.Invoke(DolocAPI.SelectedItem, null)! && !(body.StateManager.current is AgentStateInteract) &&
                    DolocAPI.SelectedItem.count == 5 && CountMilk() == milkBefore + 1,
                    "unready animal rejects repeat without bottle consumption or extra milk");
            }
        }
        finally
        {
            DolocAPI.CurrentAnimal = previousAnimal;
            Check(host.RemoveAnimal(animal), "temporary native animal released");
            Restore(); RestoreWorldClock();
        }
        DolocAPI.ReturnHome();
        foreach (int x in Wait(2)) yield return x;
        Log("PASS milk acceptance; native on/off behavior, exact consumption, rejection, speed restoration and title observed; physical right hold acceptance remains separate");
        Application.Quit();
    }
    private static int CountMilk() => DolocAPI.CountItem(DolocAPI.GlobalParameter.ItemRefMilk) +
        ((IDropItemHost)DolocAPI.CurrentRoom).DM_dropitem.AllDatas.Count(i => !i.IsRemoved && i.ItemName == DolocAPI.GlobalParameter.ItemRefMilk);
}
