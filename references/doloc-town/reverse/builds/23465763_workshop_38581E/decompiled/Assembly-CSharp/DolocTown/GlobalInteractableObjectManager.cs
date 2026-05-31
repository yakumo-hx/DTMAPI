using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Room;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class GlobalInteractableObjectManager
{
	private Dictionary<string, Dictionary<string, InteractableObjectLogic>> objectLogicCache = new Dictionary<string, Dictionary<string, InteractableObjectLogic>>();

	[JsonProperty]
	public HashSet<string> unlockedObjectIds { get; private set; }

	[JsonProperty]
	public List<InteractableObjectLogic> objectLogics { get; private set; }

	[JsonConstructor]
	public GlobalInteractableObjectManager(HashSet<string> unlockedObjectIds = null, List<InteractableObjectLogic> objectLogics = null)
	{
		this.unlockedObjectIds = unlockedObjectIds ?? new HashSet<string>();
		this.objectLogics = objectLogics ?? new List<InteractableObjectLogic>();
		DolocAPI.OnAfterLoadArchiveData.AddListener(OnAfterLoadArchiveData);
	}

	private void OnAfterLoadArchiveData(bool isNewGame)
	{
		List<InteractableObjectLogic> list = new List<InteractableObjectLogic>();
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.OnAfterLoadArchiveData(isNewGame);
			if (!objectLogic.IsValid)
			{
				list.Add(objectLogic);
			}
		}
		foreach (InteractableObjectLogic item in list)
		{
			objectLogics.Remove(item);
		}
		foreach (InteractableObjectLogic objectLogic2 in objectLogics)
		{
			objectLogicCache.TryAdd(objectLogic2.roomId, new Dictionary<string, InteractableObjectLogic>());
			objectLogicCache[objectLogic2.roomId][objectLogic2.guid] = objectLogic2;
		}
	}

	public bool SaveLockState(string id, bool value)
	{
		if (id.IsNullOrEmpty())
		{
			return false;
		}
		bool flag = false;
		if (!value)
		{
			if (unlockedObjectIds.Add(id))
			{
				DolocAPI.BroadcastString(GameEventType.UNLOCK_INTERACTABLE, id);
			}
			return true;
		}
		return unlockedObjectIds.Remove(id);
	}

	public bool LoadLockState(string id)
	{
		if (!id.IsNullOrEmpty())
		{
			return !unlockedObjectIds.Contains(id);
		}
		return true;
	}

	public StationInfo[] GetUnlockedStationsInMap()
	{
		return (from x in unlockedObjectIds?.Select((string x) => DolocConfig.Tables.TbStation.GetOrDefault(x))
			where x?.FreeTeleport ?? false
			select x).ToArray();
	}

	public InteractableObjectLogic GetLogicEntity(string roomId, string guid)
	{
		if (!objectLogicCache.TryGetValue(roomId, out var value))
		{
			return null;
		}
		return value.GetValueOrDefault(guid);
	}

	public void RegisterLogicEntity(InteractableObjectLogic logic)
	{
		if (logic != null)
		{
			InteractableObjectLogic logicEntity = GetLogicEntity(logic.roomId, logic.guid);
			if (logicEntity != null)
			{
				objectLogics.Remove(logicEntity);
			}
			objectLogics.Add(logic);
			objectLogicCache.TryAdd(logic.roomId, new Dictionary<string, InteractableObjectLogic>());
			objectLogicCache[logic.roomId][logic.guid] = logic;
		}
	}

	public void UnregisterLogicEntity(InteractableObjectLogic logic)
	{
		if (logic != null)
		{
			objectLogics.Remove(logic);
			if (objectLogicCache.ContainsKey(logic.roomId))
			{
				objectLogicCache[logic.roomId].Remove(logic.guid);
			}
		}
	}

	public void UpdatePerSecond(bool isRender)
	{
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.UpdatePerSecond(isRender);
		}
	}

	public void DailyRefresh(bool isRender)
	{
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.DailyRefresh(isRender);
		}
	}

	public void WeeklyRefresh(bool isRender)
	{
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.WeeklyRefresh(isRender);
		}
	}

	public void MonthlyRefresh(bool isRender)
	{
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.MonthlyRefresh(isRender);
		}
	}

	public void YearlyRefresh(bool isRender)
	{
		foreach (InteractableObjectLogic objectLogic in objectLogics)
		{
			objectLogic.YearlyRefresh(isRender);
		}
	}
}
