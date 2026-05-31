using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Settings;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class BoardMissionManager
{
	private bool hasNewMission;

	[JsonProperty]
	private List<string> randomMissionPool = new List<string>();

	[JsonProperty]
	private Queue<string> fixedMissionPool = new Queue<string>();

	[JsonProperty]
	private List<string> fixedMissionAlternativePool = new List<string>();

	private TbBoardMission Config => DolocConfig.Tables.TbBoardMission;

	public GlobalParameterInfo Parameter => DolocAPI.GlobalParameter;

	public TbBoardMissionLevel MissionLevel => DolocConfig.Tables.TbBoardMissionLevel;

	private int BattleLv => DolocAPI.archiveHandle.GetTechLevel(TechPointType.BATTLE);

	public bool HasNewMission
	{
		get
		{
			return hasNewMission;
		}
		set
		{
			if (hasNewMission != value)
			{
				hasNewMission = value;
				RefreshNoticeBoardTip();
			}
		}
	}

	private int CurrentLvIndex
	{
		get
		{
			int result = -1;
			for (int i = 0; i < MissionLevel.DataList.Count; i++)
			{
				if (MissionLevel.DataList[i].Level <= BattleLv)
				{
					result = i;
				}
			}
			return result;
		}
	}

	private IEnumerable<string> AllMissionLv => MissionLevel.DataList.Select((BoardMissionLevelInfo x) => x.MissionLv);

	private bool HasFixedMission
	{
		get
		{
			BoardMission[] issueAllMissions = IssueAllMissions;
			foreach (BoardMission boardMission in issueAllMissions)
			{
				if (boardMission != null && boardMission.IsFixedMission)
				{
					return true;
				}
			}
			return false;
		}
	}

	[JsonProperty]
	public BoardMission[] IssueAllMissions { get; private set; }

	[JsonConstructor]
	public BoardMissionManager(BoardMission[] IssueAllMissions = null, List<string> randomMissionPool = null, Queue<string> fixedMissionPool = null, List<string> fixedMissionAlternativePool = null)
	{
		this.IssueAllMissions = new BoardMission[Parameter.MissionUpperLimit];
		if (!IssueAllMissions.IsNullOrEmpty())
		{
			for (int i = 0; i < IssueAllMissions.Length; i++)
			{
				BoardMission boardMission = IssueAllMissions[i];
				if (boardMission != null && boardMission.IsValid)
				{
					this.IssueAllMissions[i] = IssueAllMissions[i];
				}
			}
		}
		this.randomMissionPool = randomMissionPool ?? new List<string>();
		this.fixedMissionPool = fixedMissionPool ?? new Queue<string>();
		this.fixedMissionAlternativePool = fixedMissionAlternativePool ?? new List<string>();
		DolocAPI.OnAfterLoadArchiveData.AddListener(OnLoadData);
	}

	private void OnLoadData(bool isNew)
	{
		if (isNew)
		{
			foreach (BoardMissionInfo data in Config.DataList)
			{
				if (data.TimeLimit == 0)
				{
					fixedMissionAlternativePool.Add(data.Id);
				}
				else if (CheckMonth(data.Season))
				{
					randomMissionPool.Add(data.Id);
				}
			}
			return;
		}
		Queue<string> queue = new Queue<string>();
		foreach (string item2 in randomMissionPool)
		{
			if (!Config.DataMap.TryGetValue(item2, out var value) || !CheckMonth(value.Season))
			{
				queue.Enqueue(item2);
			}
		}
		while (queue.Count > 0)
		{
			string item = queue.Dequeue();
			randomMissionPool.Remove(item);
		}
		for (int i = 0; i < IssueAllMissions.Length; i++)
		{
			if (IssueAllMissions[i] != null && !CheckMonth(IssueAllMissions[i].BoardMissionInfo.Season))
			{
				IssueAllMissions[i] = null;
			}
		}
	}

	public void StartBoardMission(string missionId)
	{
		if (Config.DataMap.TryGetValue(missionId, out var value))
		{
			string rewardId = ((value.Rewards.Length == 0) ? GetRandomReward(value.Level) : string.Empty);
			new BoardMission(value, 0, rewardId).CreateMissionContent(out var content);
			DolocAPI.archiveHandle.StartMission(missionId, content, rewardId, value.TimeLimit);
		}
	}

	public bool IssueMission(string missionId)
	{
		if (missionId.IsNullOrEmpty())
		{
			return false;
		}
		if (!Config.DataMap.TryGetValue(missionId, out var value))
		{
			DolocAPI.outputError("没有配置数据: " + missionId);
			return false;
		}
		BoardMission[] issueAllMissions = IssueAllMissions;
		foreach (BoardMission boardMission in issueAllMissions)
		{
			if (boardMission != null && boardMission.Id == missionId)
			{
				return false;
			}
		}
		for (int j = 0; j < IssueAllMissions.Length; j++)
		{
			if (IssueAllMissions[j] == null)
			{
				int day = ((value.TimeLimit == 0) ? Parameter.TakeTimeLimitRandom : Parameter.TakeTimeLimitFixed);
				string rewardId = ((value.Rewards.Length == 0) ? GetRandomReward(value.Level) : string.Empty);
				IssueAllMissions[j] = new BoardMission(value, day, rewardId);
				return true;
			}
		}
		return false;
	}

	public bool AcceptBoardMission(string missionId)
	{
		for (int i = 0; i < IssueAllMissions.Length; i++)
		{
			BoardMission boardMission = IssueAllMissions[i];
			if (boardMission != null && !(boardMission.Id != missionId))
			{
				if (!CanTakeMission(boardMission.MissionLv))
				{
					return false;
				}
				if (!boardMission.CreateMissionContent(out var content))
				{
					return false;
				}
				if (!DolocAPI.archiveHandle.StartMission(missionId, content, boardMission.RewardId, boardMission.TimeLimit))
				{
					return false;
				}
				IssueAllMissions[i] = null;
				if (!boardMission.IsFixedMission)
				{
					randomMissionPool.Remove(missionId);
				}
				return true;
			}
		}
		return false;
	}

	public void AddFixedMission(string missionId)
	{
		if (fixedMissionAlternativePool.Contains(missionId))
		{
			fixedMissionAlternativePool.Remove(missionId);
			fixedMissionPool.Enqueue(missionId);
		}
	}

	public bool CanTakeMission(string missionLv)
	{
		if (MissionLevel.DataMap.TryGetValue(missionLv, out var value))
		{
			return value.Level <= BattleLv;
		}
		return false;
	}

	public void MissionTiming(bool force = false)
	{
		for (int i = 0; i < IssueAllMissions.Length; i++)
		{
			if (IssueAllMissions[i] == null)
			{
				continue;
			}
			BoardMission boardMission = IssueAllMissions[i];
			if (boardMission.Disappear)
			{
				if (boardMission.IsFixedMission)
				{
					fixedMissionPool.Enqueue(boardMission.Id);
				}
				else
				{
					randomMissionPool.Remove(boardMission.Id);
				}
				IssueAllMissions[i] = null;
			}
		}
		if (!force && DolocAPI.archiveHandle.DateNow.TotalDays % Parameter.MissionRefreshDay != 0)
		{
			return;
		}
		bool flag = false;
		BoardMission[] issueAllMissions = IssueAllMissions;
		foreach (BoardMission boardMission2 in issueAllMissions)
		{
			flag = flag || boardMission2 == null;
		}
		if (flag)
		{
			HasNewMission = !HasFixedMission && RefreshFixedMission();
			if (!HasNewMission)
			{
				HasNewMission = RefreshRandomMission();
			}
		}
	}

	public void RefreshNoticeBoardTip()
	{
		if (!(DolocAPI.CurrentRoom?.SceneHandle is RoomHandle roomHandle))
		{
			return;
		}
		NoticeBoard[] interactableByType = roomHandle.GetInteractableByType<NoticeBoard>();
		if (!interactableByType.IsNullOrEmpty())
		{
			NoticeBoard[] array = interactableByType;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetMissionTipState(hasNewMission);
			}
		}
	}

	public int GetMissionExp(string missionLv)
	{
		if (MissionLevel.DataMap.TryGetValue(missionLv, out var value))
		{
			return value.Exp;
		}
		return 0;
	}

	public string GetCurrentBattleLv()
	{
		string result = string.Empty;
		foreach (BoardMissionLevelInfo data in MissionLevel.DataList)
		{
			if (data.Level <= BattleLv)
			{
				result = data.MissionLv;
			}
		}
		return result;
	}

	public void RefreshBoardMissionPool()
	{
		foreach (BoardMissionInfo data in Config.DataList)
		{
			if (data.TimeLimit != 0 && !CheckMonth(data.Season))
			{
				randomMissionPool.Remove(data.Id);
			}
		}
		for (int i = 0; i < IssueAllMissions.Length; i++)
		{
			if (IssueAllMissions[i] != null && !CheckMonth(IssueAllMissions[i].BoardMissionInfo.Season))
			{
				IssueAllMissions[i] = null;
			}
		}
	}

	private void ResetRandomMissionPoolByLv(string missionLv)
	{
		foreach (BoardMissionInfo data in Config.DataList)
		{
			if (data.TimeLimit > 0 && data.Level == missionLv && CheckMonth(data.Season))
			{
				randomMissionPool.Add(data.Id);
			}
		}
	}

	private bool RefreshFixedMission()
	{
		if (fixedMissionPool.Count == 0)
		{
			return false;
		}
		if (!Config.DataMap.TryGetValue(fixedMissionPool.Peek(), out var value) || !CheckMonth(value.Season))
		{
			return false;
		}
		string missionId = fixedMissionPool.Dequeue();
		return IssueMission(missionId);
	}

	private bool RefreshRandomMission()
	{
		int value;
		int num = RandomUtils.RussianRoulette(new int[3]
		{
			Parameter.MissionRefreshWeights.x,
			Parameter.MissionRefreshWeights.y,
			Parameter.MissionRefreshWeights.z
		}, out value);
		string text = string.Empty;
		switch (num)
		{
		case 1:
			text = GetRandomMissionByLv(MissionLevel.DataList[Math.Clamp(CurrentLvIndex, 0, MissionLevel.DataList.Count - 1)].MissionLv);
			break;
		case 2:
			text = GetRandomMissionByLv(MissionLevel.DataList[Math.Clamp(CurrentLvIndex + 1, 0, MissionLevel.DataList.Count - 1)].MissionLv);
			break;
		}
		if (text.IsNullOrEmpty())
		{
			text = GetLowRandomMission();
		}
		return IssueMission(text);
	}

	private string GetLowRandomMission()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < CurrentLvIndex; i++)
		{
			string lv = MissionLevel.DataList[Math.Clamp(i, 0, MissionLevel.DataList.Count - 1)].MissionLv;
			if (randomMissionPool.Find((string x) => Config.GetOrDefault(x).Level == lv).IsNullOrEmpty())
			{
				ResetRandomMissionPoolByLv(lv);
			}
			list.AddRange(randomMissionPool.Where((string x) => Config.GetOrDefault(x).Level == lv));
		}
		return list.Choice();
	}

	private string GetRandomMissionByLv(string missionLv)
	{
		if (randomMissionPool.Find((string x) => Config.GetOrDefault(x).Level == missionLv).IsNullOrEmpty())
		{
			ResetRandomMissionPoolByLv(missionLv);
		}
		return randomMissionPool.Where((string x) => Config.GetOrDefault(x).Level == missionLv).ToList().Choice();
	}

	private string GetRandomReward(string lv)
	{
		return DolocConfig.Tables.TbRewardPool.DataList.Where((RewardPoolInfo x) => x.Level == lv).ToList().Choice()?.Id;
	}

	private bool CheckMonth(int[] months)
	{
		if (!months.IsNullOrEmpty())
		{
			return months.Any((int x) => x == DolocAPI.archiveHandle.timeData.dateNow.Month);
		}
		return true;
	}
}
