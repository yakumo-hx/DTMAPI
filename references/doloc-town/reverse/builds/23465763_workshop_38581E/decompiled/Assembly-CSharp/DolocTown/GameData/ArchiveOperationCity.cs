using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Plant;

namespace DolocTown.GameData;

public static class ArchiveOperationCity
{
	public static bool QueryCityRoom(this ArchiveDataHandle handle, string shortSceneName, out CityRoom room)
	{
		return handle.cityData.cityRooms.TryGetValue(shortSceneName, out room);
	}

	public static IEnumerable<Npc> GetNpcsInScene(this ArchiveDataHandle handle, int sceneIndex)
	{
		return handle.cityData.npcManager.GetNpcsInScene(sceneIndex);
	}

	public static bool MoveNpcToScene(this ArchiveDataHandle handle, string npcName, string sceneName)
	{
		return handle.cityData.npcManager.PutNpcToScene(npcName, sceneName);
	}

	public static bool QueryDialogueHostHasEventNow(this ArchiveDataHandle handle, string npcName)
	{
		return handle.cityData.dialogueManager.CheckEventStatus(npcName);
	}

	public static bool AddDialogueNode(this ArchiveDataHandle handle, string nodeName, string npcName)
	{
		bool result = handle.cityData.dialogueManager.AddDialogueNode(nodeName, npcName);
		handle.UpdateNpcEventState(npcName);
		return result;
	}

	public static bool SetDialogueEntrance(this ArchiveDataHandle handle, string nodeName, string npcName)
	{
		bool result = handle.cityData.dialogueManager.SetEntrance(nodeName, npcName);
		handle.UpdateNpcEventState(npcName);
		return result;
	}

	public static bool RemoveDialogueNode(this ArchiveDataHandle handle, string nodeName, string npcName)
	{
		bool result = handle.cityData.dialogueManager.RemoveDialogueNode(nodeName, npcName);
		handle.UpdateNpcEventState(npcName);
		return result;
	}

	private static void UpdateNpcEventState(this ArchiveDataHandle handle, string npcName)
	{
		if (DolocAPI.QueryNpc(npcName, out var npc))
		{
			npc.RefreshNpcEventStatus();
		}
	}

	public static bool QueryStore(this ArchiveDataHandle handle, string storeName, out IStore store)
	{
		return handle.cityData.storeManager.QueryStore(storeName, out store);
	}

	public static bool QueryStore<T>(this ArchiveDataHandle handle, string storeName, out T store) where T : class, IStore
	{
		return handle.cityData.storeManager.QueryStore(storeName, out store);
	}

	public static void UnlockSeedItemInStore(this ArchiveDataHandle handle)
	{
		foreach (string unlockedSeedNode in DolocAPI.archiveHandle.farmData.unlockedSeedNodes)
		{
			string[] seedStoreIds = DolocAPI.GlobalParameter.SeedStoreIds;
			for (int i = 0; i < seedStoreIds.Length; i++)
			{
				DolocAPI.UnlockStoreItem(seedStoreIds[i], unlockedSeedNode);
			}
			SeedUnlockInfo orDefault = DolocConfig.Tables.TbSeedUnlock.GetOrDefault(unlockedSeedNode);
			if (orDefault != null)
			{
				DolocAPI.SendEmail(orDefault.EmailId, allowRepeat: false);
			}
		}
	}

	public static void RefreshArchiveDialogue(this ArchiveDataHandle handle)
	{
		if (!DolocAPI.archiveHandle.cityData.documentManager.plantDocMgr.allPlantDocUnlocked)
		{
			DolocAPI.RemoveDialogueNode("unlock_all_docs");
			DolocAPI.AddDialogueNode("archive_plant_guide_actsk");
		}
	}

	public static void RefreshAllStores(this ArchiveDataHandle handle)
	{
		handle.cityData.storeManager.RefreshAllStores();
	}

	public static bool SetObjectLockState(this ArchiveDataHandle handle, string lockObjectId, bool value)
	{
		return handle.cityData.globalInteractableObjectManager.SaveLockState(lockObjectId, value);
	}

	public static bool DisableGate(this ArchiveDataHandle handle, string portalId)
	{
		return handle.cityData.gateManager.DisableGate(portalId);
	}

	public static bool EnableGate(this ArchiveDataHandle handle, string portalId)
	{
		return handle.cityData.gateManager.EnableGate(portalId);
	}

	public static bool CheckGateEnable(this ArchiveDataHandle handle, string portalId)
	{
		return !handle.cityData.gateManager.CheckGateEnable(portalId);
	}

	public static int GetUnlockedPlantDocumentCount(this ArchiveDataHandle handle)
	{
		return handle.cityData.documentManager.plantDocMgr.unlockedDocuments.Count;
	}
}
