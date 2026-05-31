using System;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public class BoardMission
{
	public readonly BoardMissionInfo BoardMissionInfo;

	[JsonProperty]
	private int leftTime;

	[JsonProperty]
	private string rewardId;

	public bool IsValid => BoardMissionInfo != null;

	public DolocTown.Config.Mission.MissionContent Content => BoardMissionInfo.Content;

	public GameEventType GameEventType
	{
		get
		{
			if (!Enum.TryParse<GameEventType>(Content.EventType, out var result))
			{
				return GameEventType.NONE;
			}
			return result;
		}
	}

	public bool IsFixedMission => BoardMissionInfo.TimeLimit == 0;

	public int TimeLimit => BoardMissionInfo.TimeLimit;

	public string MissionLv => BoardMissionInfo.Level;

	public string RewardId => rewardId;

	[JsonProperty]
	[DebugInfo("看板任务名称", Color = "ffff00")]
	public string Id => BoardMissionInfo?.Id;

	public bool Disappear => --leftTime <= 0;

	public RewardProto[] Rewards
	{
		get
		{
			if (DolocConfig.Tables.TbRewardPool.DataMap.TryGetValue(rewardId, out var value))
			{
				return value.Rewards;
			}
			return BoardMissionInfo.Rewards;
		}
	}

	public bool CreateMissionContent(out DolocTown.GameData.MissionContent content)
	{
		content = null;
		if (GameEventType == GameEventType.NONE)
		{
			return false;
		}
		MissionRequire_Simple require = ((GameEventType == GameEventType.COMPLETE_DIALOGUE) ? new MissionRequire_Simple(Content.EventType, 1, Id) : new MissionRequire_Simple(Content.EventType, Mathf.Max(1, Content.Count), Content.Args));
		content = new MissionContentSingle(require);
		return true;
	}

	public BoardMission(BoardMissionInfo boardMissionInfo, int day, string rewardId)
	{
		BoardMissionInfo = boardMissionInfo;
		leftTime = day;
		this.rewardId = rewardId;
	}

	[JsonConstructor]
	private BoardMission(string Id, int leftTime, string rewardId)
	{
		BoardMissionInfo = DolocConfig.Tables.TbBoardMission.GetOrDefault(Id);
		this.leftTime = leftTime;
		this.rewardId = rewardId;
	}
}
