using System;
using System.Collections.Generic;
using System.Reflection;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace Yuuka.DTMAPI.ActionSpeed;

internal sealed partial class NativeActions : IDisposable
{
    internal const string HarmonyOwner = "Yuuka.DTMAPI.ActionSpeed.Native";
    private readonly Harmony harmony = new Harmony(HarmonyOwner);
    private readonly Func<ActionConfig> settings;
    private readonly FillSchedule fill = new FillSchedule();
    private readonly List<SpeedLease> clocks = new List<SpeedLease>(4);
    private BodyController? body;
    private AgentHatRenderer? hat;
    private ToolRenderer? toolRenderer;
    private AgentStateBase? acceleratedState;
    // Bound once. Calling DrawWaterInWater retains native guards/callbacks and cannot take milk instead.
    private readonly Action<ItemBottle> drawWater = AccessTools.MethodDelegate<Action<ItemBottle>>(
        AccessTools.DeclaredMethod(typeof(ItemBottle), "DrawWaterInWater"));
    private readonly Func<PlantBasin, bool> couldHarvest = AccessTools.MethodDelegate<Func<PlantBasin, bool>>(
        AccessTools.DeclaredPropertyGetter(typeof(PlantBasin), "CouldHarvest"));

    internal NativeActions(Func<ActionConfig> settings) => this.settings = settings;

    internal void Install()
    {
        Activate();
        try
        {
            Operation(typeof(ItemBottle), "DrawWaterInWater", NativeAction.BottleFill);
            Operation(typeof(ItemBottle), "DrawWaterInContainer", NativeAction.BottleFill);
            Operation(typeof(ItemBottle), "CollectMilkFromAnimal", NativeAction.Milk);
            Operation(typeof(ItemWaterCan), "TryDrawWater", NativeAction.CanRefill);
            Operation(typeof(SimpleWell), "OnInteract", NativeAction.WellFill);
            Operation(typeof(ItemSeed), "PlantSeed", NativeAction.PlantSeed, typeof(PlantBasin));
            Operation(typeof(ItemSeed), "PlantSeed", NativeAction.PlantSeed, typeof(FlowerPot));
            Operation(typeof(ItemSeedTree), "OnUseAsTool", NativeAction.PlantTree);
            Operation(typeof(PlantBasinTree), "TryPlantCrop", NativeAction.PlantTree);
            Operation(typeof(ItemFertilizer), "Fertilizer", NativeAction.Fertilize);
            Operation(typeof(ItemFilm), "Protect", NativeAction.Film);
            Operation(typeof(ItemFarmingGun), "OnUseAsTool", NativeAction.FarmingGun);
            Operation(typeof(PlantBasin), "OnInteract", NativeAction.CropInteract);
            Operation(typeof(PlantBasinGrass), "OnInteract", NativeAction.Forage);
            Operation(typeof(VegetationCrop), "OnInteract", NativeAction.Forage);
            Operation(typeof(VegetationBerryThicket), "OnInteract", NativeAction.Forage);
            Operation(typeof(VegetationGrowLuminous), "OnInteract", NativeAction.Forage);
            Operation(typeof(DungeonResourceCrop), "OnInteract", NativeAction.Forage);
            Operation(typeof(ChickenNest), "OnInteract", NativeAction.Forage);
            Operation(typeof(ResinCollector), "OnInteract", NativeAction.Resin);
            Operation(typeof(PowerGeneratorFuel), "OnInteract", NativeAction.Fuel);
            Operation(typeof(PowerGeneratorBiogas), "OnInteract", NativeAction.Fuel);
            Operation(typeof(Feeder), "OnInteract", NativeAction.Feed);
            Operation(typeof(FishTank), "OnInteract", NativeAction.Feed);
            Operation(typeof(AnimalRenderer), "_HandleInteract", NativeAction.Pet);
            Operation(typeof(AffectorElectric), "OnInteract", NativeAction.Electrical);
            Operation(typeof(Television), "OnInteract", NativeAction.Electrical);
            Operation(typeof(GarbageShredder), "OnInteract", NativeAction.Electrical);
            Operation(typeof(Synthesizer), "OnInteract", NativeAction.Electrical);

            foreach (var type in new[] { typeof(AgentStateTool), typeof(AgentStateWater), typeof(AgentStateEat), typeof(AgentStateInteract) })
                Patch(type, "OnEnter", null, nameof(Entered));
            foreach (var type in new[] { typeof(AgentStateBase), typeof(AgentStateTool), typeof(AgentStateWater), typeof(AgentStateInteract) })
                Patch(type, "OnExit", nameof(Leaving));
            Patch(typeof(BodyController), "PlayAnimation", nameof(Playing), null, typeof(string), typeof(float));
            Patch(typeof(AgentControllerState), "UseItemContinues", nameof(Drinking), null, typeof(float));
            Patch(typeof(AgentControllerState), "OnUpdateInNormalState", null, nameof(NormalUpdate), typeof(float));
            Patch(typeof(AgentControllerState), "OnPause", nameof(Pausing));
        }
        catch { Dispose(); throw; }
    }

    private void Operation(Type type, string name, NativeAction action, params Type[] parameters)
    {
        var method = Required(type, name, parameters);
        operations.Add(method, action);
        harmony.Patch(method, prefix: Hook(nameof(BeginOperation)), finalizer: Hook(nameof(EndOperation)));
    }
    private static MethodInfo Required(Type type, string name, Type[] parameters) =>
        AccessTools.DeclaredMethod(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);
    private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(NativeActions), name);
    private void Patch(Type type, string method, string? prefix = null, string? postfix = null, params Type[] parameters) =>
        harmony.Patch(Required(type, method, parameters), prefix == null ? null : Hook(prefix), postfix == null ? null : Hook(postfix));

    private ActionKind ClassifyOperation(NativeAction operation, object instance) =>
        ActionRules.Classify(operation, instance is PlantBasin basin && couldHarvest(basin));

    private static void Entered(AgentStateBase __instance, BodyController ___body)
    {
        var self = active;
        if (self == null || ___body != DolocAPI.agent) return;
        ActionKind kind = __instance is AgentStateTool || __instance is AgentStateWater ? ActionKind.Tool
            : __instance is AgentStateEat ? ActionKind.Eat : self.scope.Current;
        self.Bind(___body);
        self.RestoreMotion();
        float factor = self.settings().Factor(kind);
        if (factor <= 1) return;
        self.acceleratedState = __instance;
        foreach (var clock in self.clocks) clock.Apply(factor);
    }

    private void Bind(BodyController nextBody)
    {
        if (body == nextBody && hat == nextBody.HatRenderer && toolRenderer == nextBody.ToolRenderer) return;
        RestoreMotion(); clocks.Clear();
        body = nextBody; hat = nextBody.HatRenderer; toolRenderer = nextBody.ToolRenderer;
        Add(nextBody.GetComponent<Animator>());
        if (toolRenderer != null) Add(toolRenderer.GetComponent<Animator>());
        if (hat != null) foreach (var animator in hat.GetComponentsInChildren<Animator>(true)) Add(animator);
    }
    private void Add(Animator animator) { if (animator != null) clocks.Add(new SpeedLease(new UnityClock(animator))); }
    private sealed class UnityClock : IAnimationClock
    {
        private readonly Animator animator;
        internal UnityClock(Animator animator) => this.animator = animator;
        public float Speed { get => animator.speed; set => animator.speed = value; }
        public bool IsAlive => animator != null;
    }
    private static void Leaving(AgentStateBase __instance)
    { if (ReferenceEquals(active?.acceleratedState, __instance)) active?.RestoreMotion(); }
    private static void Playing(BodyController __instance)
    { var self = active; if (self != null && self.body == __instance) self.RestoreMotion(); }
    private static void Drinking(ref float dt)
    {
        var self = active;
        if (self == null) return;
        var item = DolocAPI.SelectedItem;
        dt *= ActionRules.DrinkClockFactor(self.settings().ContinuousDrink,
            item != null && item is IEatable && item.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater);
    }
    private static void NormalUpdate(float dt) => active?.TryAutoFill(dt);
    private static void Pausing() => active?.fill.Reset();
    private void TryAutoFill(float dt)
    {
        var config = settings();
        if (!config.AutoFill || !DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.IsAgentRiding)
        { fill.Reset(); return; }
        var agent = DolocAPI.agent;
        var bottle = DolocAPI.SelectedItem as ItemBottle;
        bool eligible = ActionRules.CanAutoFill(config.AutoFill, DolocAPI.IsNormalState, DolocAPI.IsAgentInWater,
            bottle != null && bottle.count > 0, agent != null && agent.IsCurrentStateSupportInteract && agent.IsCurrentStateSupportUseItem,
            DolocAPI.HasEnoughEnergy(DolocAPI.GlobalParameter.ToolEnergyCost / 2),
            DolocAPI.archiveHandle.InventorySystem.buffer.IsFull, DolocUtils.IsInteractWithUI());
        if (fill.Tick(dt, eligible, config.StrongAutoFill ? config.StrongAutoFillInterval : config.AutoFillInterval)) drawWater(bottle!);
    }
    private void RestoreMotion()
    {
        SpeedLease.RestoreAll(clocks);
        acceleratedState = null;
    }
    internal void Reset()
    {
        fill.Reset(); scope.Reset();
        RestoreMotion(); clocks.Clear(); body = null; hat = null; toolRenderer = null;
    }
    private void Unpatch() => harmony.UnpatchSelf();
}
