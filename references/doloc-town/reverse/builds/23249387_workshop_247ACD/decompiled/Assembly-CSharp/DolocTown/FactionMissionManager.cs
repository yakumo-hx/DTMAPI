using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class FactionMissionManager
{
	private Dictionary<string, FactionMission> factionMissions = new Dictionary<string, FactionMission>();

	private TbFactionMission Config => DolocConfig.Tables.TbFactionMission;

	[JsonProperty]
	public FactionMission[] TotalMissions => factionMissions.Values.ToArray();

	[JsonConstructor]
	public FactionMissionManager(FactionMission[] TotalMissions = null)
	{
		if (TotalMissions != null)
		{
			foreach (FactionMission factionMission in TotalMissions)
			{
				if (factionMission.IsValid)
				{
					factionMissions.Add(factionMission.Id, factionMission);
				}
			}
		}
		foreach (FactionMissionInfo data in Config.DataList)
		{
			if (data.DefaultUnlock)
			{
				AddFactionMission(data);
			}
		}
	}

	private bool AddFactionMission(FactionMissionInfo proto)
	{
		FactionMission value = new FactionMission(proto);
		return factionMissions.TryAdd(proto.Id, value);
	}

	public bool AddFactionMission(string missionId)
	{
		if (!Config.DataMap.TryGetValue(missionId, out var value))
		{
			Debug.LogError("没有配置数据: " + missionId);
			return false;
		}
		return AddFactionMission(value);
	}

	public FactionMission[] GetFactionMissions(FactionType mainSeries, FactionMissionType subSeries)
	{
		List<FactionMission> list = new List<FactionMission>();
		foreach (FactionMission value in factionMissions.Values)
		{
			if (value.MainSeries == mainSeries && value.SubSeries == subSeries)
			{
				list.Add(value);
			}
		}
		return (from m in list
			orderby m.IsComplete ? 1 : 0, m.Proto.OrderInType descending
			select m).ToArray();
	}

	public FactionMissionType[] GetFactionMissionSubTypes(FactionType mainSeries)
	{
		HashSet<FactionMissionType> hashSet = new HashSet<FactionMissionType>();
		foreach (FactionMission value in factionMissions.Values)
		{
			if (value.MainSeries == mainSeries)
			{
				hashSet.Add(value.SubSeries);
			}
		}
		return hashSet.OrderBy((FactionMissionType subType) => (int)subType).ToArray();
	}

	public bool QueryFactionMission(string id, out FactionMission mission)
	{
		return factionMissions.TryGetValue(id, out mission);
	}

	public bool ContainsFactionMission(string id)
	{
		return factionMissions.ContainsKey(id);
	}

	public void SubmitFactionMissionItem(string missionId, string itemName)
	{
		if (factionMissions.TryGetValue(missionId, out var value))
		{
			value.SubmitFactionMissionItem(itemName);
			if (value.IsComplete)
			{
				FinishFactionMission(missionId);
			}
		}
	}

	public void SubmitMoney(string missionId)
	{
		if (factionMissions.TryGetValue(missionId, out var value))
		{
			value.SubmitMoney();
			if (value.IsComplete)
			{
				FinishFactionMission(missionId);
			}
		}
	}

	public bool CheckFactionMissionItem(string missionId, string itemName, int count)
	{
		if (factionMissions.TryGetValue(missionId, out var value))
		{
			return value.CanSubmitFactionMissionItem(itemName, count);
		}
		return false;
	}

	public void __StartAllFactionMissions()
	{
		foreach (string key in Config.DataMap.Keys)
		{
			AddFactionMission(key);
		}
	}

	public int GetCompletedCount(FactionType mainSeries)
	{
		return factionMissions.Values.Count((FactionMission mission) => mission.MainSeries == mainSeries && mission.IsComplete);
	}

	private void FinishFactionMission(string id)
	{
		if (factionMissions.TryGetValue(id, out var value))
		{
			value.CashMissionReward();
			DolocAPI.archiveHandle.cityData.treatyPortFactionManager.AddReputationValueByMissionId(id, value.FamePoints);
			DolocAPI.BroadcastString(GameEventType.FACTION_MISSION_FINISH, id);
			DolocAPI.CurrentRoom?.SceneHandle?.RefreshCondition();
		}
	}

	public void __ClearAllFactionMissions()
	{
		factionMissions.Clear();
	}
}
