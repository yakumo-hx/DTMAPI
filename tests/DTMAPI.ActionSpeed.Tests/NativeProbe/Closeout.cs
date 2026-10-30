using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.Config;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Internal.ActionSpeedShortProbe;

public sealed partial class ModEntry
{
    private static ModEntry? closeoutSelf;
    private readonly Harmony closeoutHarmony = new Harmony("DTMAPI.Internal.ActionSpeedShortProbe.Native");
    private string closeoutCommand = "";
    private float closeoutDeadline, closeoutPoll;
    private bool clockCaptured, previousPause, waitingForInput, previousHeld;
    private int previousWaterCount, createdFixtures;

    private void ArmCloseout()
    {
        closeoutSelf = this;
        string state = Path.GetDirectoryName(Path.GetDirectoryName(helper.Diagnostics.GetLatestLogPath()))!;
        closeoutCommand = Path.Combine(state, "actionspeed-short-probe.txt");
        if(File.Exists(closeoutCommand)) File.Delete(closeoutCommand);
        closeoutDeadline = Time.realtimeSinceStartup + 600;
        closeoutHarmony.Patch(AccessTools.DeclaredMethod(typeof(DolocAPI), "SaveGame"), new HarmonyMethod(typeof(ModEntry), nameof(RejectSave)));
    }
    private static bool RejectSave(ref bool __result)
    { closeoutSelf?.Log("FAIL native SaveGame attempted"); __result=false; return false; }
    private void RestoreWorldClock()
    { if(clockCaptured){DolocAPI.gameLoop.IsGlobalPaused=previousPause; clockCaptured=false;} }
    private void PollCloseout()
    {
        if(Time.realtimeSinceStartup < closeoutPoll) return;
        closeoutPoll = Time.realtimeSinceStartup + .1f;
        if(waitingForInput && DolocAPI.IsDataLoaded)
        {
            bool held=DolocAPI.UserInput.NormalUseItemInProgress;
            int count=DolocAPI.SelectedItem?.count ?? 0;
            if(held!=previousHeld || count!=previousWaterCount)
                Log("INPUT held-use-item="+held+"; selected="+DolocAPI.SelectedItem?.name+"; count="+count+"; state="+body.StateManager.current.GetType().Name+"; speed="+animator.speed);
            previousHeld=held; previousWaterCount=count;
        }
        string command=File.Exists(closeoutCommand) ? File.ReadAllText(closeoutCommand).Trim() : "";
        if(command.Length>0) File.Delete(closeoutCommand);
        if(command=="title" || Time.realtimeSinceStartup>closeoutDeadline)
        {
            waitingForInput=false; finished=true; run?.Dispose(); run=null;
            Restore(); RestoreWorldClock();
            if(DolocAPI.IsDataLoaded) DolocAPI.ReturnHome();
            closeoutDeadline=float.PositiveInfinity;
            Log("returned to title; settings use DTMAPI Mod Config Menu");
        }
        if(command=="quit") Application.Quit();
    }
    private IEnumerator<int> Closeout()
    {
        foreach(int x in Wait(3)) yield return x;
        float readyBy=Time.realtimeSinceStartup+30;
        while(!DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.agent==null || !DolocAPI.agent.gameObject.activeInHierarchy)
        { if(Time.realtimeSinceStartup>readyBy) throw new Exception("closeout world readiness timeout"); yield return 0; }
        bool arrived=false;
        Check(DolocAPI.DoTransport(DolocAPI.gameManager.gameInitConfig.initMarkPoint,()=>arrived=true),"requested native transport to the farm");
        float arriveBy=Time.realtimeSinceStartup+20;
        while(!arrived || !DolocAPI.IsNormalState)
        { if(Time.realtimeSinceStartup>arriveBy) throw new Exception("native farm transport timed out"); yield return 0; }
        foreach(int x in Wait(2)) yield return x;
        body=DolocAPI.agent; animator=body.GetComponent<Animator>(); baseSpeed=animator.speed;
        position=body.position2d; index=DolocAPI.SelectedItemIndex; originalItem=DolocAPI.archiveHandle.InventorySystem.inventory.Read(index);
        var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Yuuka.DTMAPI.ActionSpeed");
        var type=assembly.GetType("Yuuka.DTMAPI.ActionSpeed.NativeActions",true)!;
        object runtime=type.GetProperty("active",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        config=((Delegate)AccessTools.Field(type,"settings").GetValue(runtime)!).DynamicInvoke()!;
        foreach(var property in config.GetType().GetProperties()) original.Add(property,property.GetValue(config,null)!);
        captured=true; previousPause=DolocAPI.gameLoop.IsGlobalPaused; clockCaptured=true; DolocAPI.gameLoop.IsGlobalPaused=true;
        Set("AutoFill",false); Set("InteractMultiplier",3d); Set("GatherMultiplier",4d); Set("PlantMultiplier",2d);
        DolocAPI.AddEnergy(100);
        Log("closeout starts; room="+DolocAPI.CurrentRoom.GetType().Name+"; temporary native equipment fixtures stay in memory; NoNativeSave");

        foreach(bool enabled in new[]{false,true})
        {
            Set("InteractEnabled",enabled);
            var fuel=MakeFixture<PowerGeneratorFuel>();
            var fuelItem=DolocConfig.Tables.TbItem.DataList.First(p=>p.ElectricEnergy>0 && fuel.IsSuitableFuel(DolocAPI.GenerateItem(p,1)));
            Idle(); Select(fuelItem.Id,6); float beforeFuel=fuel.FuelPercent;
            InvokeInteraction(fuel,typeof(PowerGeneratorFuel));
            ObserveInteraction(enabled?3:1,"fuel enabled="+enabled);
            foreach(int x in Wait(1.5f)) yield return x;
            Check(DolocAPI.SelectedItem.count==5 && fuel.FuelPercent>beforeFuel,"native fuel callback consumes one item and increases fuel");
            Near(baseSpeed,animator.speed,"fuel restored");

            var feeder=MakeFixture<Feeder>();
            var feed=DolocConfig.Tables.TbItem.DataList.First(p=>feeder.IsAnimalFeeds(p,out _));
            Idle(); Select(feed.Id,6); float beforeFeed=feeder.progress;
            InvokeInteraction(feeder,typeof(Feeder)); ObserveInteraction(enabled?3:1,"feed enabled="+enabled);
            foreach(int x in Wait(1.5f)) yield return x;
            Check(DolocAPI.SelectedItem.count==5 && feeder.progress>beforeFeed,"native feed callback consumes one item and increases feed");
            Near(baseSpeed,animator.speed,"feed restored");

            var appliance=MakeFixture<AffectorElectric>(); bool beforeSwitch=appliance.IsTurnOn;
            Idle(); InvokeInteraction(appliance,typeof(AffectorElectric)); ObserveInteraction(enabled?3:1,"electrical enabled="+enabled);
            foreach(int x in Wait(1.5f)) yield return x;
            Check(appliance.IsTurnOn!=beforeSwitch,"native electrical callback toggles the switch");
            Near(baseSpeed,animator.speed,"electrical restored");

            Set("PlantEnabled",enabled); Set("GatherEnabled",enabled);
            var basin=MakeFixture<PlantBasin>();
            var seedInfo=DolocConfig.Tables.TbSeed.DataList.First(p=>p.SeedType==basin.SeedTypeInfo.Id &&
                ((ItemSeed)DolocAPI.GenerateItem(p.Id,1)).CheckCurrentSeasonValid());
            Idle(); Select(seedInfo.Id,6); ((ItemSeed)DolocAPI.SelectedItem).PlantSeed(basin);
            ObserveInteraction(enabled?2:1,"plant enabled="+enabled);
            foreach(int x in Wait(1.5f)) yield return x;
            Check(basin.IsPlanted && DolocAPI.SelectedItem.count==5,"native planting consumes exactly one matching seed");
            Near(baseSpeed,animator.speed,"plant restored");
            for(int i=0;i<seedInfo.LevelCount && !basin.Crop.isMature;i++) basin.Crop._ApplyGrowth(100000,true);
            Check(basin.Crop.isMature,"native growth prepared a mature crop");
            Idle(); InvokeInteraction(basin,typeof(PlantBasin)); ObserveInteraction(enabled?4:1,"gather enabled="+enabled);
            foreach(int x in Wait(1.5f)) yield return x;
            Check(!basin.IsPlanted || !basin.Crop.isMature,"native harvest changes the mature crop");
            Check(DolocAPI.SelectedItem.count==5,"harvest does not consume the selected seed");
            Near(baseSpeed,animator.speed,"gather restored");
        }
        Set("InteractEnabled",true);
        var rejectFuel=MakeFixture<PowerGeneratorFuel>();
        Idle(); Select("old_axe",1); float rejectedFuel=rejectFuel.FuelPercent;
        InvokeInteraction(rejectFuel,typeof(PowerGeneratorFuel));
        Check(!(body.StateManager.current is AgentStateInteract) && rejectFuel.FuelPercent==rejectedFuel && DolocAPI.SelectedItem.count==1,"unsuitable fuel remains rejected without consumption or acceleration");
        Near(baseSpeed,animator.speed,"rejected action leaves speed unchanged");
        Idle(); Select(DolocAPI.GlobalParameter.ItemRefBottleOfWater,30); Set("ContinuousDrink",true); Set("EatEnabled",false);
        waitingForInput=true; previousWaterCount=30;
        Log("PASS pending object callbacks and six category coverage (prior tool/water/eat evidence reused); WAIT physical held right mouse input with bottled water selected");
        finished=true;
    }
    private T MakeFixture<T>() where T:Equipment
    {
        var table=(Dictionary<string,Type>)AccessTools.Field(typeof(EquipmentManager),"typeLut").GetValue(null)!;
        var proto=DolocConfig.Tables.TbEquipment.DataList.Where(p=>!p.isDecal && table.TryGetValue(p.Function.GetType().Name,out Type target) &&
            typeof(T).IsAssignableFrom(target) && (typeof(T)!=typeof(PlantBasin) || target==typeof(PlantBasin)))
            .OrderBy(p=>p.CoverSize.x*p.CoverSize.y).First();
        var host=(IEquipmentHost)DolocAPI.CurrentRoom;
        Vector2Int? anchor=null;
        for(int y=1;y<host.DM_terrain.Size.y-proto.CoverSize.y && anchor==null;y++)
            for(int x=0;x<host.DM_terrain.Size.x-proto.CoverSize.x;x++)
            {
                var candidate=new Vector2Int(x,y);
                if(host.IsEquipmentBuildable(proto.GetCoveredPositions(candidate,proto.CoverSize),proto.GetCoveredPositions(new Vector2Int(x,y-1),new Vector2Int(proto.CoverSize.x,1)),true))
                { anchor=candidate; break; }
            }
        if(anchor==null) throw new Exception("No native-buildable fixture space for "+proto.Id);
        // The native footprint check and factory create actual renderers and item callbacks, entirely in memory.
        Vector3 world=(Vector3)DolocAPI.CurrentRoom.RoomPosition+new Vector3((anchor.Value.x+proto.CoverSize.x*.5f)*1.5f,anchor.Value.y*1.5f,0);
        var equipment=host.CreateEquipment(world,anchor.Value,proto,false);
        createdFixtures++;
        Check(equipment is T && equipment.Renderer!=null,"native fixture="+proto.Id+"; type="+equipment.GetType().Name);
        return (T)equipment;
    }
    private static void InvokeInteraction(Equipment equipment,Type declaringType) => AccessTools.DeclaredMethod(declaringType,"OnInteract").Invoke(equipment,null);
    private void ObserveInteraction(float factor,string label)
    { Check(body.StateManager.current is AgentStateInteract,label+" enters native Interact"); Near(baseSpeed*factor,animator.speed,label+" speed"); }
}
