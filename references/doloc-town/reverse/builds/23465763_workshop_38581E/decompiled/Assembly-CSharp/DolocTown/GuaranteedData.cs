using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class GuaranteedData
{
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	private GuaranteedType type;

	[JsonProperty]
	private Dictionary<string, int> currentCounts;

	[JsonProperty]
	private Dictionary<string, int> activatedCounts;

	[JsonProperty]
	private Dictionary<string, int> globalCounts;

	[JsonProperty]
	private HashSet<string> unlockedIds;

	private TbGlobalGuaranteed table => DolocConfig.Tables.TbGlobalGuaranteed;

	[JsonConstructor]
	public GuaranteedData(GuaranteedType type, Dictionary<string, int> currentCounts = null, Dictionary<string, int> activatedCounts = null, Dictionary<string, int> globalCounts = null, HashSet<string> unlockedIds = null)
	{
		this.type = type;
		this.currentCounts = currentCounts ?? new Dictionary<string, int>();
		this.activatedCounts = activatedCounts ?? new Dictionary<string, int>();
		this.globalCounts = globalCounts ?? new Dictionary<string, int>();
		this.unlockedIds = unlockedIds ?? new HashSet<string>();
	}

	public bool TryGetGuaranteedInfo(string spawnId, out GlobalGuaranteedInfo info)
	{
		info = table.Get(type, spawnId);
		return info != null;
	}

	public bool TickGuaranteedId(string spawnId)
	{
		GlobalGuaranteedInfo globalGuaranteedInfo = table.Get(type, spawnId);
		if (globalGuaranteedInfo == null || globalGuaranteedInfo.GuaranteeThreshold <= 0)
		{
			return false;
		}
		if (globalGuaranteedInfo.ActiveCount > 0 && activatedCounts.TryGetValue(spawnId, out var value) && value >= globalGuaranteedInfo.ActiveCount)
		{
			return false;
		}
		currentCounts.TryAdd(spawnId, 0);
		currentCounts[spawnId]++;
		if (currentCounts[spawnId] < globalGuaranteedInfo.GuaranteeThreshold)
		{
			return false;
		}
		ResetGuaranteedId(spawnId);
		TickActiveCount(spawnId, force: true);
		return true;
	}

	public void TickActiveCount(string spawnId, bool force)
	{
		GlobalGuaranteedInfo globalGuaranteedInfo = table.Get(type, spawnId);
		if (globalGuaranteedInfo != null && (force || globalGuaranteedInfo.ShouldConsumeGuarantee))
		{
			activatedCounts.TryAdd(spawnId, 0);
			activatedCounts[spawnId]++;
		}
	}

	public void ResetGuaranteedId(string spawnId)
	{
		currentCounts.Remove(spawnId);
	}

	public void UnlockId(string spawnId)
	{
		unlockedIds.Add(spawnId);
	}

	public bool CheckGuaranteedItemUnlock(string spawnId)
	{
		if (!TryGetGuaranteedInfo(spawnId, out var info))
		{
			return true;
		}
		if (info.DefaultLocked)
		{
			return unlockedIds.Contains(spawnId);
		}
		return true;
	}

	public bool CheckWithinGlobalLimit(string spawnId)
	{
		if (!TryGetGuaranteedInfo(spawnId, out var info))
		{
			return true;
		}
		if (info.GlobalLimit <= 0 || !globalCounts.TryGetValue(spawnId, out var value))
		{
			return true;
		}
		return value < info.GlobalLimit;
	}

	public void TryAddGlobalCount(string spawnId, int count, out int validCount)
	{
		validCount = count;
		if (TryGetGuaranteedInfo(spawnId, out var info) && info.GlobalLimit > 0)
		{
			globalCounts.TryAdd(spawnId, 0);
			validCount = Mathf.Min(count, info.GlobalLimit - globalCounts[spawnId]);
			validCount = Mathf.Max(0, validCount);
			globalCounts[spawnId] += validCount;
		}
	}
}
