using System;
using System.Runtime.CompilerServices;

// DTMAPI-authored signature/dispatch fixture, not game implementation. Nothing
// here models Unity execution, save state, game compatibility or player behavior.
internal static class NativeHost
{
    internal static Action DuringCall = () => { };
    [MethodImpl(MethodImplOptions.NoInlining)] internal static void Call() => DuringCall();
}
namespace UnityEngine
{
    public class Component
    {
        public T GetComponent<T>() where T : class => null!;
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
    }
    public class Animator : Component { public float speed { get; set; } = 1; }
}
public class AgentStateBase
{
    protected DolocTown.BodyController body = null!;
    [MethodImpl(MethodImplOptions.NoInlining)] public virtual void OnExit() => NativeHost.Call();
}
public static class DolocAPI
{
    public static bool IsDataLoaded, IsNormalState, IsAgentRiding, IsAgentInWater;
    public static DolocTown.BodyController agent = null!;
    public static DolocTown.Item SelectedItem = null!;
    public static Parameters GlobalParameter = new Parameters();
    public static Archive archiveHandle = new Archive();
    public static bool HasEnoughEnergy(float amount) => false;
    public class Parameters { public string ItemRefBottleOfWater = "water"; public float ToolEnergyCost = 1; }
    public class Archive { public Inventory InventorySystem = new Inventory(); }
    public class Inventory { public Buffer buffer = new Buffer(); }
    public class Buffer { public bool IsFull; }
}
public static class DolocUtils { public static bool IsInteractWithUI() => false; }
namespace DolocTown
{
    public class BodyController : UnityEngine.Component
    {
        public AgentHatRenderer HatRenderer = null!;
        public ToolRenderer ToolRenderer = null!;
        public bool IsCurrentStateSupportInteract, IsCurrentStateSupportUseItem;
        [MethodImpl(MethodImplOptions.NoInlining)] public void PlayAnimation(string animationName, float blend) => NativeHost.Call();
    }
    public class AgentHatRenderer : UnityEngine.Component { }
    public class ToolRenderer : UnityEngine.Component { }
    public interface IEatable { }
    public class Item { public string name = ""; public int count; }
    public class ItemBottle : Item, IEatable
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public bool CollectMilkFromAnimal() { NativeHost.Call(); return true; }
        [MethodImpl(MethodImplOptions.NoInlining)] private void DrawWaterInWater() => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] private void DrawWaterInContainer() => NativeHost.Call();
    }
    public class ItemWaterCan { [MethodImpl(MethodImplOptions.NoInlining)] public bool TryDrawWater() { NativeHost.Call(); return true; } }
    public class ItemSeed
    {
        [MethodImpl(MethodImplOptions.NoInlining)] private void PlantSeed(PlantBasin basin) => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] private void PlantSeed(FlowerPot pot) => NativeHost.Call();
    }
    public class FlowerPot { }
    public class ItemSeedTree { [MethodImpl(MethodImplOptions.NoInlining)] public void OnUseAsTool() => NativeHost.Call(); }
    public class PlantBasinTree { [MethodImpl(MethodImplOptions.NoInlining)] private bool TryPlantCrop() { NativeHost.Call(); return true; } }
    public class ItemFertilizer { [MethodImpl(MethodImplOptions.NoInlining)] private void Fertilizer() => NativeHost.Call(); }
    public class ItemFilm { [MethodImpl(MethodImplOptions.NoInlining)] private void Protect() => NativeHost.Call(); }
    public class ItemFarmingGun { [MethodImpl(MethodImplOptions.NoInlining)] public void OnUseAsTool() => NativeHost.Call(); }
    public class PlantBasin
    {
        public bool CouldHarvest => true;
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call();
    }
    public class SimpleWell { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class PlantBasinGrass { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class VegetationCrop { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class VegetationBerryThicket { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class VegetationGrowLuminous { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class DungeonResourceCrop { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class ChickenNest { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class ResinCollector { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class PowerGeneratorFuel { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class PowerGeneratorBiogas { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class Feeder { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class FishTank { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class AnimalRenderer { [MethodImpl(MethodImplOptions.NoInlining)] private void _HandleInteract() => NativeHost.Call(); }
    public class AffectorElectric { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class Television { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class GarbageShredder { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class Synthesizer { [MethodImpl(MethodImplOptions.NoInlining)] public void OnInteract() => NativeHost.Call(); }
    public class AgentStateTool : AgentStateBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnEnter() => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] public override void OnExit() => NativeHost.Call();
    }
    public class AgentStateWater : AgentStateBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnEnter() => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] public override void OnExit() => NativeHost.Call();
    }
    public class AgentStateEat : AgentStateBase { [MethodImpl(MethodImplOptions.NoInlining)] public void OnEnter() => NativeHost.Call(); }
    public class AgentStateInteract : AgentStateBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnEnter() => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] public override void OnExit() => NativeHost.Call();
    }
    public class AgentControllerState
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public void UseItemContinues(float dt) => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] private void OnUpdateInNormalState(float dt) => NativeHost.Call();
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnPause() => NativeHost.Call();
    }
}
