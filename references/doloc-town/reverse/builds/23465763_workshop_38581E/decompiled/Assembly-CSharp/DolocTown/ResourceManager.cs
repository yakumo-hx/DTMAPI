using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Fishing;
using DolocTown.Config.Resource;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class ResourceManager
{
	[JsonProperty]
	public HashSet<string> unlockResources { get; private set; }

	[JsonProperty]
	public HashSet<string> unlockFishes { get; private set; }

	[JsonProperty]
	public HashSet<string> unlockVegetation { get; private set; }

	[JsonConstructor]
	public ResourceManager(HashSet<string> unlockResources = null, HashSet<string> unlockFishes = null, HashSet<string> unlockVegetation = null)
	{
		this.unlockResources = unlockResources ?? new HashSet<string>();
		this.unlockFishes = unlockFishes ?? new HashSet<string>();
		this.unlockVegetation = unlockVegetation ?? new HashSet<string>();
	}

	public void UnlockResource(string resource)
	{
		unlockResources.Add(resource);
	}

	public void UnlockFish(string name)
	{
		unlockFishes.Add(name);
	}

	public void LockFish(string name)
	{
		unlockFishes.Remove(name);
	}

	public void UnlockVegetation(string name)
	{
		unlockVegetation.Add(name);
	}

	public void LockVegetation(string name)
	{
		unlockVegetation.Remove(name);
	}

	public bool CheckResourceUnlocked(string name)
	{
		ResourceInfo orDefault = DolocConfig.Tables.TbResource.GetOrDefault(name);
		if (orDefault == null)
		{
			return false;
		}
		if (!orDefault.DefaultUnlock)
		{
			return unlockResources.Contains(name);
		}
		return true;
	}

	public bool CheckFishUnlocked(string name)
	{
		FishInfo orDefault = DolocConfig.Tables.TbFish.GetOrDefault(name);
		if (orDefault == null)
		{
			return false;
		}
		GlobalGuaranteedInfo globalGuaranteedInfo = DolocConfig.Tables.TbGlobalGuaranteed.Get(GuaranteedType.Fishing, name);
		if (globalGuaranteedInfo != null && globalGuaranteedInfo.GlobalLimit > 0 && DolocAPI.GetEventTriggerCount(GameEventType.FISHING_CATCH_TRASH, name) + DolocAPI.GetEventTriggerCount(GameEventType.FISHING_CATCH_FISH, name) >= globalGuaranteedInfo.GlobalLimit)
		{
			return false;
		}
		if (!orDefault.DefaultUnlock)
		{
			return unlockFishes.Contains(name);
		}
		return true;
	}

	public bool CheckVegetationUnlocked(string name)
	{
		VegetationInfo orDefault = DolocConfig.Tables.TbVegetation.GetOrDefault(name);
		if (orDefault == null)
		{
			return false;
		}
		if (!orDefault.DefaultUnlock)
		{
			return unlockVegetation.Contains(name);
		}
		return true;
	}
}
