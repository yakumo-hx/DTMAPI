using DolocTown.Config.EnvOptimizer;

namespace DolocTown.GameData;

public static class ArchiveOperationUniversalUnlock
{
	public static bool IsBetterWaterUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockDataCollection.isUnlockedBetterWater;
	}

	public static void UnlockBetterWater(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockDataCollection.isUnlockedBetterWater = true;
	}

	public static bool IsResonatorUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockDataCollection.isUnlockedResonator;
	}

	public static void UnlockResonator(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockDataCollection.isUnlockedResonator = true;
	}

	public static bool IsEnvOptimizerComponentUnlocked(this ArchiveDataHandle handle, EnvOptimizerComponentType componentType)
	{
		return handle.farmData.envOptimizerSystem.IsComponentActive(componentType);
	}

	public static void UnlockBuildingLinkGate(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockDataCollection.isUnlockedBuildingLinkGate = true;
	}

	public static bool IsBuildingLinkGateUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockDataCollection.isUnlockedBuildingLinkGate;
	}

	public static bool IsCalendarUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockDataCollection.isUnlockedCalendar;
	}

	public static void UnlockCalendar(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockDataCollection.isUnlockedCalendar = true;
	}

	public static bool IsAdditionalPassiveSlotUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.unlockDataCollection.isUnlockedAdditionalPassiveSlot;
	}

	public static void UnlockAdditionalPassiveSlot(this ArchiveDataHandle handle)
	{
		handle.farmData.unlockDataCollection.isUnlockedAdditionalPassiveSlot = true;
	}
}
