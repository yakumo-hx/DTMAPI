using System;
using System.Runtime.CompilerServices;

// Small authored test doubles; these model ownership and faults, not game logic.
public static partial class DolocAPI
{
    public static DolocTown.GameData.ArchiveDataHandle archiveHandle { get; set; } = new DolocTown.GameData.ArchiveDataHandle();
    public static DolocTown.TimeScaleManager timeScaleManager { get; set; } = new DolocTown.TimeScaleManager();
    public static FixtureGameLoop gameLoop { get; } = new FixtureGameLoop();
    public static int Loads;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool LoadGame(int slot) { Loads++; archiveHandle = new DolocTown.GameData.ArchiveDataHandle(); return true; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NewGame(int slot) { Loads++; archiveHandle = new DolocTown.GameData.ArchiveDataHandle(); }
}
public sealed class FixtureGameLoop
{
    public bool IsPaused { get; set; }
    public bool IsGlobalPaused { get; set; }
}
namespace DolocTown
{
    public sealed class TimeScaleManager
    {
        public float Value = 1;
        public bool FailWrite, FailRead, FailReadAfterWrite;
        public int Writes;
        public float currentTimeScale
        {
            get { if (FailRead) { FailRead = false; throw new InvalidOperationException("read fault"); } return Value; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SetTimeScale(float value, bool showTip)
        {
            if (FailWrite) { FailWrite = false; throw new InvalidOperationException("write fault"); }
            Value = value; Writes++;
            if (FailReadAfterWrite) { FailReadAfterWrite = false; FailRead = true; }
        }
    }
}
namespace DolocTown.GameData
{
    public sealed class ArchiveDataHandle { public FarmArchiveData farmData { get; } = new FarmArchiveData(); }
    public sealed class FarmArchiveData { public AgentArchiveData agentData = new AgentArchiveData(); }
    public sealed class AgentArchiveData
    {
        public int health = 10, energy = 20, spirit = 30;
        public int MaxHealth { get; set; } = 100;
        public int MaxEnergy { get; set; } = 200;
        public int MaxSpirit { get; set; } = 300;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void _UpdateSpirit() { spirit--; }
        public void Tick() { _UpdateSpirit(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AddSpirit(int value) { spirit += value; }
    }
    public static class ArchiveOperationGlobal
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int _CostHealth(ArchiveDataHandle archive, int value) { return archive.farmData.agentData.health -= value; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool _CostEnergy(ArchiveDataHandle archive, int value) { archive.farmData.agentData.energy -= value; return true; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void _CostEnergyNoProtect(ArchiveDataHandle archive, int value) { archive.farmData.agentData.energy -= value; }
    }
}
