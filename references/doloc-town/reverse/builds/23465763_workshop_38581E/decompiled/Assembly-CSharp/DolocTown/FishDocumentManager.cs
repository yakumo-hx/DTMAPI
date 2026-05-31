using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Fishing;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class FishDocumentManager
{
	private TbFish table => DolocConfig.Tables.TbFish;

	[JsonConstructor]
	public FishDocumentManager()
	{
	}

	public int GetObtainedUniqueFishCount()
	{
		if (!(DolocAPI.archiveHandle.farmData.eventRecorderManager.GetRecorder(GameEventType.FISHING_CATCH_FISH) is GameEventRecorderString gameEventRecorderString))
		{
			return 0;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (FishInfo data in table.DataList)
		{
			if (data.IsFish)
			{
				if (gameEventRecorderString.GetCount(data.Id) > 0)
				{
					hashSet.Add(data.Id);
				}
				if (DolocAPI.GetEventTriggerCount(GameEventType.CATCH_FISH_BY_HAT, data.Id) > 0)
				{
					hashSet.Add(data.Id);
				}
			}
		}
		foreach (FarmFishInfo data2 in DolocConfig.Tables.TbFarmFish.DataList)
		{
			if (DolocAPI.GetEventTriggerCount(GameEventType.FRY_GROW_UP, data2.Id) > 0)
			{
				hashSet.Add(data2.Id);
			}
		}
		return hashSet.Count;
	}

	public bool CheckFishDocumentUnlocked(string fishName)
	{
		if (DolocAPI.GetEventTriggerCount(GameEventType.FISHING_CATCH_FISH, fishName) <= 0 && DolocAPI.GetEventTriggerCount(GameEventType.FRY_GROW_UP, fishName) <= 0)
		{
			return DolocAPI.GetEventTriggerCount(GameEventType.CATCH_FISH_BY_HAT, fishName) > 0;
		}
		return true;
	}
}
