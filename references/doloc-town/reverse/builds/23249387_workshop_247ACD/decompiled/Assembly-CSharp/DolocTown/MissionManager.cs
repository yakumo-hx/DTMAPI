using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.GameData;
using DolocTown.GameDataTracker;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionManager
{
	private Action<IMission> CallbackOnMissionCompleted;

	private readonly Queue<IMission> _completeMissionBuffer = new Queue<IMission>();

	private readonly Dictionary<string, MissionLog[]> _missionLogsCache = new Dictionary<string, MissionLog[]>();

	private Dictionary<string, IMission> missions = new Dictionary<string, IMission>();

	private readonly List<IMissionManagerComponent> _missionManagerComponents = new List<IMissionManagerComponent>();

	[JsonProperty]
	private HashSet<string> finishMissions = new HashSet<string>();

	[JsonProperty]
	private HashSet<string> finishDecorators = new HashSet<string>();

	public string[] FinishMissions => finishMissions.ToArray();

	public string[] FinishDecorators => finishDecorators.ToArray();

	[DebugInfo]
	[JsonProperty]
	public IMission[] totalMissions
	{
		get
		{
			IMission[] array = new IMission[missions.Count];
			missions.Values.CopyTo(array, 0);
			return array;
		}
	}

	[DebugInfo]
	public IMission[] TotalExplicitMissions => missions.Values.Where((IMission m) => !m.IsImplicit).ToArray();

	private void InsertExtraControl()
	{
		AddMissionManagerComponent(new RefreshRoomAfterMissionComplete());
		AddMissionManagerComponent(new GameDataTrackerMissionGuide.MissionListener());
		AddMissionManagerComponent(new MissionCollectionRecorder());
		AddMissionManagerComponent(new BoardMissionMessage());
		AddMissionManagerComponent(new MissionDebugLogs());
	}

	public MissionManager(Action<IMission> callbackOnMissionCompleted)
	{
		CallbackOnMissionCompleted = callbackOnMissionCompleted;
		InsertExtraControl();
	}

	[JsonConstructor]
	private MissionManager(IMission[] totalMissions, HashSet<string> finishMissions, HashSet<string> finishDecorators)
	{
		foreach (IMission mission in totalMissions)
		{
			if (mission.isDeserializationValid)
			{
				missions.Add(mission.Id, mission);
			}
			else
			{
				Debug.LogError("任务\"" + mission.Id + "\"反序列化失败，已丢弃");
			}
		}
		this.finishMissions = finishMissions;
		this.finishDecorators = finishDecorators;
		InsertExtraControl();
	}

	public void __AfterLoadMission()
	{
		_ClearInvalidMissions();
		foreach (IMission value in missions.Values)
		{
			value.AfterLoadData();
		}
		RenderAllMissionTips();
	}

	private void _ClearInvalidMissions()
	{
		Queue<IMission> queue = new Queue<IMission>();
		foreach (IMission item in missions.Values.Where((IMission mission) => mission.IsInvalid))
		{
			queue.Enqueue(item);
		}
		while (queue.Count > 0)
		{
			IMission mission2 = queue.Dequeue();
			RemoveMission(mission2.Id);
			Debug.LogError("任务\"" + mission2.Id + "\"配置失效，已被移除");
		}
	}

	public void _ClearAllCompletedMissions()
	{
		if (!DolocAPI.IsNewVersion)
		{
			return;
		}
		Debug.Log("<color=orange>检测到游戏版本更新，开始检查任务完成状态...</color>");
		foreach (IMission value in missions.Values)
		{
			bool isCompleteLoadArchive = value.IsCompleteLoadArchive;
			Debug.Log($"<color=orange>检查任务\"{value.Id}\"是否完成:{isCompleteLoadArchive}</color>");
			if (isCompleteLoadArchive)
			{
				_completeMissionBuffer.Enqueue(value);
			}
		}
		while (_completeMissionBuffer.Count > 0)
		{
			CompleteMission(_completeMissionBuffer.Dequeue());
		}
	}

	public void _ClearCompletedMissions(IEnumerable<string> missionIds)
	{
		if (missionIds == null)
		{
			return;
		}
		foreach (string missionId in missionIds)
		{
			if (missions.TryGetValue(missionId, out var value))
			{
				bool isCompleteLoadArchive = value.IsCompleteLoadArchive;
				Debug.Log($"<color=orange>检查任务\"{value.Id}\"是否完成:{isCompleteLoadArchive}</color>");
				if (isCompleteLoadArchive)
				{
					_completeMissionBuffer.Enqueue(value);
				}
			}
		}
		while (_completeMissionBuffer.Count > 0)
		{
			CompleteMission(_completeMissionBuffer.Dequeue());
		}
	}

	public void __SetCallbackOnMissionCompleted(Action<IMission> callbackOnMissionCompleted)
	{
		CallbackOnMissionCompleted = callbackOnMissionCompleted;
	}

	public void UpdatePerHour()
	{
		Queue<string> queue = new Queue<string>();
		foreach (IMission item in missions.Values.Where((IMission mission) => mission.HasTimeLimit && mission.UpdatePerHour()))
		{
			queue.Enqueue(item.Id);
			if (!item.IsImplicit)
			{
				DolocAPI.ShowMessageBoxSmallErr(string.Format(DolocConfig.StaticTexts.BoardMissionOverdue, item.Title));
			}
		}
		while (queue.Count > 0)
		{
			RemoveMission(queue.Dequeue());
		}
	}

	public void UpdatePerHourNoRender()
	{
		Queue<string> queue = new Queue<string>();
		foreach (IMission item in missions.Values.Where((IMission mission) => mission.HasTimeLimit))
		{
			if (item.UpdatePerHour())
			{
				queue.Enqueue(item.Id);
			}
		}
		while (queue.Count > 0)
		{
			RemoveMission(queue.Dequeue());
		}
	}

	public bool CheckDecorator(string id)
	{
		return finishDecorators.Contains(id);
	}

	public void ManualAddDecorator(string id)
	{
		finishDecorators.Add(id);
	}

	public void ManualRemoveDecorator(string id)
	{
		finishDecorators.Remove(id);
	}

	public bool QueryMission(string id, out IMission mission)
	{
		return missions.TryGetValue(id, out mission);
	}

	public bool IsMissionComplete(string id)
	{
		return finishMissions.Contains(id);
	}

	public bool IsMissionListening(string id)
	{
		return missions.ContainsKey(id);
	}

	public bool StartMission(IMission mission)
	{
		if (mission == null || missions.ContainsKey(mission.Id))
		{
			return false;
		}
		if (mission is MissionDecorator missionDecorator && finishDecorators.Contains(missionDecorator.DecoratorId))
		{
			return false;
		}
		missions.Add(mission.Id, mission);
		foreach (IMissionManagerComponent missionManagerComponent in _missionManagerComponents)
		{
			missionManagerComponent.OnMissionStart(mission);
		}
		if (mission.IsImplicit)
		{
			return true;
		}
		DolocAPI.AddResidentMissionTip(mission.Id, useSound: true);
		DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.MissionUpdate + " " + mission.Title);
		return true;
	}

	public bool RemoveMissionCompleteRecord(string id)
	{
		if (id.IsNullOrEmpty())
		{
			return false;
		}
		return finishMissions.Remove(id);
	}

	public bool RemoveMission(string id)
	{
		if (!missions.Remove(id, out var value))
		{
			return false;
		}
		foreach (IMissionManagerComponent missionManagerComponent in _missionManagerComponents)
		{
			missionManagerComponent.OnMissionRemoved(value);
		}
		if (!_missionLogsCache.TryAdd(value.Id, value.MissionLogs.ToArray()))
		{
			_missionLogsCache[value.Id] = value.MissionLogs.ToArray();
		}
		if (!value.IsImplicit)
		{
			DolocAPI.RemoveResidentMissionTip(id);
		}
		return true;
	}

	public bool QueryMissionLogs(string id, out MissionLog[] logs)
	{
		if (!QueryMission(id, out var mission))
		{
			return _missionLogsCache.TryGetValue(id, out logs);
		}
		logs = mission.MissionLogs.ToArray();
		return true;
	}

	public void CompleteMission(string missionId)
	{
		if (QueryMission(missionId, out var mission))
		{
			CompleteMission(mission);
		}
	}

	public void CompleteMission(IMission mission)
	{
		RemoveMission(mission.Id);
		finishMissions.Add(mission.Id);
		foreach (IMissionManagerComponent missionManagerComponent in _missionManagerComponents)
		{
			missionManagerComponent.OnMissionCompleted(mission);
		}
		if (mission is MissionDecorator { HasDecoratorId: not false } missionDecorator)
		{
			finishDecorators.Add(missionDecorator.DecoratorId);
			return;
		}
		mission.CashRewards();
		CallbackOnMissionCompleted?.Invoke(mission);
	}

	public void SendMessage(GameEventType type, GameEventArgs args)
	{
		foreach (IMission value in missions.Values)
		{
			if (value.SendMessage(type, args, out var statusChanged))
			{
				_completeMissionBuffer.Enqueue(value);
				if (!statusChanged)
				{
					continue;
				}
				foreach (IMissionManagerComponent missionManagerComponent in _missionManagerComponents)
				{
					missionManagerComponent.OnMissionChanged(type, args, value);
				}
				continue;
			}
			if (statusChanged)
			{
				foreach (IMissionManagerComponent missionManagerComponent2 in _missionManagerComponents)
				{
					missionManagerComponent2.OnMissionChanged(type, args, value);
				}
			}
			if (!value.IsImplicit && statusChanged)
			{
				DolocAPI.RefreshResidentMissionTip(value.Id);
			}
		}
		while (_completeMissionBuffer.Count > 0)
		{
			CompleteMission(_completeMissionBuffer.Dequeue());
		}
	}

	public void DailyRefresh()
	{
		foreach (IMission value in missions.Values)
		{
			if (!value.AttachModules.IsNullOrEmpty())
			{
				MissionAttachModule[] attachModules = value.AttachModules;
				for (int i = 0; i < attachModules.Length; i++)
				{
					attachModules[i].OnDailyRefresh(value);
				}
			}
		}
	}

	public void WeeklyRefresh()
	{
		foreach (IMission value in missions.Values)
		{
			if (!value.AttachModules.IsNullOrEmpty())
			{
				MissionAttachModule[] attachModules = value.AttachModules;
				for (int i = 0; i < attachModules.Length; i++)
				{
					attachModules[i].OnWeeklyRefresh(value);
				}
			}
		}
	}

	public void MonthlyRefresh()
	{
		foreach (IMission value in missions.Values)
		{
			if (!value.AttachModules.IsNullOrEmpty())
			{
				MissionAttachModule[] attachModules = value.AttachModules;
				for (int i = 0; i < attachModules.Length; i++)
				{
					attachModules[i].OnMonthlyRefresh(value);
				}
			}
		}
	}

	public void YearlyRefresh()
	{
		foreach (IMission value in missions.Values)
		{
			if (!value.AttachModules.IsNullOrEmpty())
			{
				MissionAttachModule[] attachModules = value.AttachModules;
				for (int i = 0; i < attachModules.Length; i++)
				{
					attachModules[i].OnYearlyRefresh(value);
				}
			}
		}
	}

	private void RenderAllMissionTips()
	{
		IMission[] totalExplicitMissions = TotalExplicitMissions;
		for (int i = 0; i < totalExplicitMissions.Length; i++)
		{
			DolocAPI.AddResidentMissionTip(totalExplicitMissions[i].Id, useSound: false);
		}
	}

	public void AddMissionManagerComponent(IMissionManagerComponent component)
	{
		if (component != null && !_missionManagerComponents.Contains(component))
		{
			_missionManagerComponents.Add(component);
		}
	}

	public void RemoveMissionManagerComponent(IMissionManagerComponent component)
	{
		if (component != null)
		{
			_missionManagerComponents.Remove(component);
		}
	}

	public void __ForceRemoveMission(string missionId)
	{
		RemoveMission(missionId);
		finishMissions.Remove(missionId);
	}
}
